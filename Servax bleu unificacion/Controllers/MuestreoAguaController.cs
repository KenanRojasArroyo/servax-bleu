using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using ExcelDataReader;
using Servax_bleu_unificacion.Models;
using Servax_bleu_unificacion.Servicios;

namespace Servax_bleu_unificacion.Controllers
{
    public class MuestreoAguaController : Controller
    {
        private Conexion cn = new Conexion();

        // -----------------------------------------------------------------
        // UMBRALES DE ALERTA (placeholder de negocio)
        // -----------------------------------------------------------------
        // Todavía no hay reglas de negocio confirmadas por el cliente para
        // "picos"/"irregularidades" de calidad de agua. Estos rangos son un
        // punto de partida razonable para engorda de atún (bibliografía
        // general de acuicultura marina), NO un dato validado por Arian.
        // Hay que confirmarlos en la próxima entrevista — mientras tanto,
        // esto es lo que le da un trigger real a ServicioNotificaciones,
        // que hasta ahora solo se probaba manualmente desde /Home/ProbarCorreo.
        private const double TEMP_MIN = 18.0;
        private const double TEMP_MAX = 26.0;
        private const double OXIGENO_MIN = 5.0;
        private const double PH_MIN = 7.5;
        private const double PH_MAX = 8.5;
        private const double SALINIDAD_MIN = 33.0;
        private const double SALINIDAD_MAX = 37.0;

        // GET: MuestreoAgua
        public ActionResult Index(int? idCorral)
        {
            var lista = new List<MuestreoAgua>();

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"SELECT m.IdMuestreo, m.IdCorral, m.Fecha, m.Temperatura, m.Oxigeno, m.Profundidad,
                                         m.PH, m.Salinidad, m.Nutrientes, m.Irregularidad, m.Observaciones,
                                         c.Nombre AS NombreCorral
                                  FROM MuestreoAgua m
                                  INNER JOIN Corral c ON m.IdCorral = c.IdCorral
                                  WHERE (@idCorral IS NULL OR m.IdCorral = @idCorral)
                                  ORDER BY m.Fecha DESC;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@idCorral", (object)idCorral ?? DBNull.Value);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(MapearMuestreo(dr, incluirNombreCorral: true));
                        }
                    }
                }
            }

            ViewBag.Corrales = ObtenerCorrales();
            ViewBag.IdCorralFiltro = idCorral;
            return View(lista);
        }

        // GET: MuestreoAgua/Create
        public ActionResult Create()
        {
            ViewBag.Corrales = ObtenerCorrales();
            return View(new MuestreoAgua { Fecha = DateTime.Today });
        }

        // POST: MuestreoAgua/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(MuestreoAgua muestreo)
        {
            if (muestreo.IdCorral <= 0)
            {
                ModelState.AddModelError("IdCorral", "Selecciona un corral.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Corrales = ObtenerCorrales();
                return View(muestreo);
            }

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"INSERT INTO MuestreoAgua (IdCorral, Fecha, Temperatura, Oxigeno, Profundidad, PH, Salinidad, Nutrientes, Irregularidad, Observaciones)
                                  VALUES (@IdCorral, @Fecha, @Temperatura, @Oxigeno, @Profundidad, @PH, @Salinidad, @Nutrientes, @Irregularidad, @Observaciones);";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    AgregarParametros(cmd, muestreo);
                    cmd.ExecuteNonQuery();
                }
            }

            // *** Trigger de alerta *** — esto es lo que le faltaba al correo de Gmail.
            await EvaluarYNotificarSiHayIrregularidad(muestreo);

            TempData["Exito"] = "Muestreo de agua registrado correctamente.";
            return RedirectToAction("Index");
        }

        // GET: MuestreoAgua/Edit/5
        public ActionResult Edit(int id)
        {
            MuestreoAgua muestreo = ObtenerPorId(id);
            if (muestreo == null) return HttpNotFound();
            ViewBag.Corrales = ObtenerCorrales();
            return View(muestreo);
        }

        // POST: MuestreoAgua/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, MuestreoAgua muestreo)
        {
            if (!ModelState.IsValid)
            {
                muestreo.IdMuestreo = id;
                ViewBag.Corrales = ObtenerCorrales();
                return View(muestreo);
            }

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"UPDATE MuestreoAgua SET
                                    IdCorral = @IdCorral, Fecha = @Fecha, Temperatura = @Temperatura,
                                    Oxigeno = @Oxigeno, Profundidad = @Profundidad, PH = @PH,
                                    Salinidad = @Salinidad, Nutrientes = @Nutrientes,
                                    Irregularidad = @Irregularidad, Observaciones = @Observaciones
                                  WHERE IdMuestreo = @IdMuestreo;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    AgregarParametros(cmd, muestreo);
                    cmd.Parameters.AddWithValue("@IdMuestreo", id);
                    cmd.ExecuteNonQuery();
                }
            }

            await EvaluarYNotificarSiHayIrregularidad(muestreo);

            TempData["Exito"] = "Muestreo actualizado correctamente.";
            return RedirectToAction("Index");
        }

        // GET: MuestreoAgua/Delete/5
        public ActionResult Delete(int id)
        {
            MuestreoAgua muestreo = ObtenerPorId(id);
            if (muestreo == null) return HttpNotFound();
            return View(muestreo);
        }

        // POST: MuestreoAgua/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = "DELETE FROM MuestreoAgua WHERE IdMuestreo = @IdMuestreo;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@IdMuestreo", id);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Exito"] = "Muestreo eliminado correctamente.";
            return RedirectToAction("Index");
        }

        // -----------------------------------------------------------------
        // Carga masiva por Excel (mismo patrón que HomeController.CargarExcel
        // para Calidad, adaptado a MuestreoAgua: requiere columna Corral para
        // resolver el IdCorral por nombre).
        // -----------------------------------------------------------------
        [HttpPost]
        public async Task<ActionResult> CargarExcel(HttpPostedFileBase archivoExcel)
        {
            if (archivoExcel == null || archivoExcel.ContentLength == 0)
            {
                TempData["Error"] = "Selecciona un archivo de Excel válido (.xlsx o .xls).";
                return RedirectToAction("Index");
            }

            var corralesPorNombre = ObtenerCorrales()
                .ToDictionary(c => Normalizar(c.Text), c => int.Parse(c.Value));

            int insertados = 0;
            int omitidos = 0;
            var muestreosParaAlertar = new List<MuestreoAgua>();

            try
            {
                using (Stream stream = archivoExcel.InputStream)
                using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var ds = reader.AsDataSet(new ExcelDataSetConfiguration
                    {
                        ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
                    });
                    DataTable dt = ds.Tables[0];

                    var col = MapearColumnas(dt, "corral", "fecha", "temperatura", "oxigeno", "profundidad", "ph", "salinidad", "nutrientes", "irregularidad", "observaciones");

                    using (SqlConnection con = cn.ObtenerConexion())
                    {
                        con.Open();
                        foreach (DataRow row in dt.Rows)
                        {
                            string nombreCorral = col["corral"] >= 0 ? row[col["corral"]]?.ToString().Trim() : null;
                            if (string.IsNullOrWhiteSpace(nombreCorral) || !corralesPorNombre.TryGetValue(Normalizar(nombreCorral), out int idCorral))
                            {
                                omitidos++;
                                continue; // fila sin corral reconocible, se salta
                            }

                            var muestreo = new MuestreoAgua
                            {
                                IdCorral = idCorral,
                                Fecha = ParsearFecha(col, row, "fecha") ?? DateTime.Today,
                                Temperatura = ParsearDecimal(col, row, "temperatura"),
                                Oxigeno = ParsearDecimal(col, row, "oxigeno"),
                                Profundidad = ParsearDecimal(col, row, "profundidad"),
                                PH = ParsearDecimal(col, row, "ph"),
                                Salinidad = ParsearDecimal(col, row, "salinidad"),
                                Nutrientes = col["nutrientes"] >= 0 ? row[col["nutrientes"]]?.ToString() : null,
                                Irregularidad = col["irregularidad"] >= 0 ? row[col["irregularidad"]]?.ToString() : null,
                                Observaciones = col["observaciones"] >= 0 ? row[col["observaciones"]]?.ToString() : null
                            };

                            string query = @"INSERT INTO MuestreoAgua (IdCorral, Fecha, Temperatura, Oxigeno, Profundidad, PH, Salinidad, Nutrientes, Irregularidad, Observaciones)
                                              VALUES (@IdCorral, @Fecha, @Temperatura, @Oxigeno, @Profundidad, @PH, @Salinidad, @Nutrientes, @Irregularidad, @Observaciones);";
                            using (SqlCommand cmd = new SqlCommand(query, con))
                            {
                                AgregarParametros(cmd, muestreo);
                                cmd.ExecuteNonQuery();
                            }

                            insertados++;
                            if (EsIrregular(muestreo)) muestreosParaAlertar.Add(muestreo);
                        }
                    }
                }

                // Una sola alerta resumen si la carga masiva trajo irregularidades,
                // en vez de un correo por cada fila (evita spam de bandeja).
                if (muestreosParaAlertar.Count > 0)
                {
                    await NotificarResumenCargaExcel(muestreosParaAlertar);
                }

                TempData["Exito"] = $"Carga completada: {insertados} muestreos insertados" +
                                     (omitidos > 0 ? $", {omitidos} filas omitidas (corral no reconocido)." : ".");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al procesar el archivo Excel: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // -----------------------------------------------------------------
        // Trigger de alerta — evalúa un muestreo contra los umbrales y,
        // si algo está fuera de rango, dispara el correo ya existente
        // en ServicioNotificaciones (antes solo se probaba manualmente).
        // -----------------------------------------------------------------
        private bool EsIrregular(MuestreoAgua m)
        {
            return (m.Temperatura.HasValue && (m.Temperatura < TEMP_MIN || m.Temperatura > TEMP_MAX))
                || (m.Oxigeno.HasValue && m.Oxigeno < OXIGENO_MIN)
                || (m.PH.HasValue && (m.PH < PH_MIN || m.PH > PH_MAX))
                || (m.Salinidad.HasValue && (m.Salinidad < SALINIDAD_MIN || m.Salinidad > SALINIDAD_MAX))
                || !string.IsNullOrWhiteSpace(m.Irregularidad);
        }

        private async Task EvaluarYNotificarSiHayIrregularidad(MuestreoAgua m)
        {
            if (!EsIrregular(m)) return;

            string nombreCorral = ObtenerNombreCorral(m.IdCorral);
            var motivos = new List<string>();
            if (m.Temperatura.HasValue && (m.Temperatura < TEMP_MIN || m.Temperatura > TEMP_MAX))
                motivos.Add($"Temperatura {m.Temperatura} °C (rango esperado {TEMP_MIN}-{TEMP_MAX} °C)");
            if (m.Oxigeno.HasValue && m.Oxigeno < OXIGENO_MIN)
                motivos.Add($"Oxígeno {m.Oxigeno} mg/L (mínimo esperado {OXIGENO_MIN} mg/L)");
            if (m.PH.HasValue && (m.PH < PH_MIN || m.PH > PH_MAX))
                motivos.Add($"PH {m.PH} (rango esperado {PH_MIN}-{PH_MAX})");
            if (m.Salinidad.HasValue && (m.Salinidad < SALINIDAD_MIN || m.Salinidad > SALINIDAD_MAX))
                motivos.Add($"Salinidad {m.Salinidad} ppt (rango esperado {SALINIDAD_MIN}-{SALINIDAD_MAX} ppt)");
            if (!string.IsNullOrWhiteSpace(m.Irregularidad))
                motivos.Add($"Irregularidad reportada: {m.Irregularidad}");

            string asunto = $"⚠️ Alerta de calidad de agua — Corral {nombreCorral}";
            string mensajeHtml = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 2px solid #dc3545; border-radius: 8px;'>
                    <h2 style='color: #dc3545;'>Muestreo fuera de rango</h2>
                    <p><strong>Corral:</strong> {nombreCorral}</p>
                    <p><strong>Fecha del muestreo:</strong> {m.Fecha:dd/MM/yyyy}</p>
                    <ul>{string.Join("", motivos.ConvertAll(mo => $"<li>{mo}</li>"))}</ul>
                    <hr />
                    <p style='font-size: 12px; color: #666;'>
                        Umbrales de referencia sin confirmar con el cliente todavía — ajustar en
                        MuestreoAguaController.cs una vez que Arian confirme los rangos reales.
                    </p>
                </div>";

            await ServicioNotificaciones.EnviarCorreoAlertaAsync(asunto, mensajeHtml);
        }

        private async Task NotificarResumenCargaExcel(List<MuestreoAgua> irregulares)
        {
            string filas = string.Join("", irregulares.ConvertAll(m =>
                $"<li>Corral {ObtenerNombreCorral(m.IdCorral)} — {m.Fecha:dd/MM/yyyy}</li>"));

            string mensajeHtml = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 2px solid #dc3545; border-radius: 8px;'>
                    <h2 style='color: #dc3545;'>Carga masiva: {irregulares.Count} muestreo(s) fuera de rango</h2>
                    <ul>{filas}</ul>
                    <p style='font-size: 12px; color: #666;'>Revisa el detalle de cada uno en /MuestreoAgua.</p>
                </div>";

            await ServicioNotificaciones.EnviarCorreoAlertaAsync(
                $"⚠️ {irregulares.Count} muestreos fuera de rango (carga por Excel)", mensajeHtml);
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private MuestreoAgua ObtenerPorId(int id)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"SELECT IdMuestreo, IdCorral, Fecha, Temperatura, Oxigeno, Profundidad, PH, Salinidad, Nutrientes, Irregularidad, Observaciones
                                  FROM MuestreoAgua WHERE IdMuestreo = @IdMuestreo;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@IdMuestreo", id);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read()) return MapearMuestreo(dr, incluirNombreCorral: false);
                    }
                }
            }
            return null;
        }

        private string ObtenerNombreCorral(int idCorral)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT Nombre FROM Corral WHERE IdCorral = @IdCorral;", con))
                {
                    cmd.Parameters.AddWithValue("@IdCorral", idCorral);
                    var resultado = cmd.ExecuteScalar();
                    return resultado?.ToString() ?? $"#{idCorral}";
                }
            }
        }

        private List<SelectListItem> ObtenerCorrales()
        {
            var lista = new List<SelectListItem>();
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT IdCorral, Nombre FROM Corral ORDER BY Nombre;", con))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new SelectListItem { Value = dr["IdCorral"].ToString(), Text = dr["Nombre"].ToString() });
                    }
                }
            }
            return lista;
        }

        private static MuestreoAgua MapearMuestreo(SqlDataReader dr, bool incluirNombreCorral)
        {
            var m = new MuestreoAgua
            {
                IdMuestreo = dr.GetInt32(dr.GetOrdinal("IdMuestreo")),
                IdCorral = dr.GetInt32(dr.GetOrdinal("IdCorral")),
                Fecha = dr.GetDateTime(dr.GetOrdinal("Fecha")),
                Temperatura = dr["Temperatura"] != DBNull.Value ? (double?)dr["Temperatura"] : null,
                Oxigeno = dr["Oxigeno"] != DBNull.Value ? (double?)dr["Oxigeno"] : null,
                Profundidad = dr["Profundidad"] != DBNull.Value ? (double?)dr["Profundidad"] : null,
                PH = dr["PH"] != DBNull.Value ? (double?)dr["PH"] : null,
                Salinidad = dr["Salinidad"] != DBNull.Value ? (double?)dr["Salinidad"] : null,
                Nutrientes = dr["Nutrientes"] as string,
                Irregularidad = dr["Irregularidad"] as string,
                Observaciones = dr["Observaciones"] as string
            };
            if (incluirNombreCorral) m.NombreCorral = dr["NombreCorral"] as string;
            return m;
        }

        private static void AgregarParametros(SqlCommand cmd, MuestreoAgua m)
        {
            cmd.Parameters.AddWithValue("@IdCorral", m.IdCorral);
            cmd.Parameters.AddWithValue("@Fecha", m.Fecha);
            cmd.Parameters.AddWithValue("@Temperatura", (object)m.Temperatura ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Oxigeno", (object)m.Oxigeno ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Profundidad", (object)m.Profundidad ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PH", (object)m.PH ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Salinidad", (object)m.Salinidad ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Nutrientes", (object)m.Nutrientes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Irregularidad", (object)m.Irregularidad ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Observaciones", (object)m.Observaciones ?? DBNull.Value);
        }

        private static string Normalizar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";
            texto = texto.ToLower().Trim();
            return texto.Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u");
        }

        private static Dictionary<string, int> MapearColumnas(DataTable dt, params string[] claves)
        {
            var resultado = claves.ToDictionary(c => c, c => -1);
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                string colNorm = Normalizar(dt.Columns[i].ColumnName);
                foreach (var clave in claves)
                {
                    if (resultado[clave] == -1 && colNorm.Contains(clave))
                    {
                        resultado[clave] = i;
                    }
                }
            }
            return resultado;
        }

        private static double? ParsearDecimal(Dictionary<string, int> col, DataRow row, string clave)
        {
            if (col[clave] < 0) return null;
            string valor = row[col[clave]]?.ToString().Replace(',', '.');
            return double.TryParse(valor, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double resultado)
                ? resultado
                : (double?)null;
        }

        private static DateTime? ParsearFecha(Dictionary<string, int> col, DataRow row, string clave)
        {
            if (col[clave] < 0) return null;
            object valor = row[col[clave]];
            if (valor is DateTime dt) return dt;
            return DateTime.TryParse(valor?.ToString(), out DateTime parsed) ? parsed : (DateTime?)null;
        }
    }
}
