using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ExcelDataReader;
using Servax_bleu_unificacion.Models;

namespace Servax_bleu_unificacion.Controllers
{
    public class InventarioPezController : Controller
    {
        private Conexion cn = new Conexion();

        // GET: InventarioPez
        public ActionResult Index(int? idCorral)
        {
            var lista = new List<InventarioPez>();

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"SELECT ip.IdInventario, ip.IdCorral, ip.IdEspecie, ip.Cantidad, ip.PesoPromedioKg, ip.Estado, ip.FechaRegistro, ip.Observaciones,
                                         c.Nombre AS NombreCorral, e.Nombre AS NombreEspecie
                                  FROM InventarioPez ip
                                  INNER JOIN Corral c ON ip.IdCorral = c.IdCorral
                                  INNER JOIN Especie e ON ip.IdEspecie = e.IdEspecie
                                  WHERE (@idCorral IS NULL OR ip.IdCorral = @idCorral)
                                  ORDER BY ip.FechaRegistro DESC;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@idCorral", (object)idCorral ?? DBNull.Value);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read()) lista.Add(MapearInventario(dr, conJoins: true));
                    }
                }
            }

            ViewBag.Corrales = ObtenerCorrales();
            ViewBag.IdCorralFiltro = idCorral;
            return View(lista);
        }

        // GET: InventarioPez/Create
        public ActionResult Create()
        {
            CargarListasDesplegables();
            return View(new InventarioPez { FechaRegistro = DateTime.Today });
        }

        // POST: InventarioPez/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(InventarioPez inv)
        {
            if (inv.IdCorral <= 0) ModelState.AddModelError("IdCorral", "Selecciona un corral.");
            if (inv.IdEspecie <= 0) ModelState.AddModelError("IdEspecie", "Selecciona una especie.");

            if (!ModelState.IsValid)
            {
                CargarListasDesplegables();
                return View(inv);
            }

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"INSERT INTO InventarioPez (IdCorral, IdEspecie, Cantidad, PesoPromedioKg, Estado, FechaRegistro, Observaciones)
                                  VALUES (@IdCorral, @IdEspecie, @Cantidad, @PesoPromedioKg, @Estado, @FechaRegistro, @Observaciones);";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    AgregarParametros(cmd, inv);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Exito"] = "Inventario registrado correctamente.";
            return RedirectToAction("Index");
        }

        // GET: InventarioPez/Edit/5
        public ActionResult Edit(int id)
        {
            InventarioPez inv = ObtenerPorId(id);
            if (inv == null) return HttpNotFound();
            CargarListasDesplegables();
            return View(inv);
        }

        // POST: InventarioPez/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, InventarioPez inv)
        {
            if (!ModelState.IsValid)
            {
                inv.IdInventario = id;
                CargarListasDesplegables();
                return View(inv);
            }

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"UPDATE InventarioPez SET
                                    IdCorral = @IdCorral, IdEspecie = @IdEspecie, Cantidad = @Cantidad,
                                    PesoPromedioKg = @PesoPromedioKg, Estado = @Estado,
                                    FechaRegistro = @FechaRegistro, Observaciones = @Observaciones
                                  WHERE IdInventario = @IdInventario;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    AgregarParametros(cmd, inv);
                    cmd.Parameters.AddWithValue("@IdInventario", id);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Exito"] = "Inventario actualizado correctamente.";
            return RedirectToAction("Index");
        }

        // GET: InventarioPez/Delete/5
        public ActionResult Delete(int id)
        {
            InventarioPez inv = ObtenerPorId(id);
            if (inv == null) return HttpNotFound();
            return View(inv);
        }

        // POST: InventarioPez/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("DELETE FROM InventarioPez WHERE IdInventario = @IdInventario;", con))
                {
                    cmd.Parameters.AddWithValue("@IdInventario", id);
                    cmd.ExecuteNonQuery();
                }
            }
            TempData["Exito"] = "Registro de inventario eliminado.";
            return RedirectToAction("Index");
        }

        // -----------------------------------------------------------------
        // Carga masiva por Excel (mismo patrón que MuestreoAguaController).
        // Requiere columnas Corral y Especie con el nombre exacto ya dado
        // de alta en el sistema para poder resolver los IDs.
        // -----------------------------------------------------------------
        [HttpPost]
        public ActionResult CargarExcel(HttpPostedFileBase archivoExcel)
        {
            if (archivoExcel == null || archivoExcel.ContentLength == 0)
            {
                TempData["Error"] = "Selecciona un archivo de Excel válido (.xlsx o .xls).";
                return RedirectToAction("Index");
            }

            var corralesPorNombre = ObtenerCorrales().ToDictionary(c => Normalizar(c.Text), c => int.Parse(c.Value));
            var especiesPorNombre = ObtenerEspecies().ToDictionary(e => Normalizar(e.Text), e => int.Parse(e.Value));

            int insertados = 0, omitidos = 0;

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
                    var col = MapearColumnas(dt, "corral", "especie", "cantidad", "pesopromedio", "estado", "fecharegistro", "observaciones");

                    using (SqlConnection con = cn.ObtenerConexion())
                    {
                        con.Open();
                        foreach (DataRow row in dt.Rows)
                        {
                            string nombreCorral = col["corral"] >= 0 ? row[col["corral"]]?.ToString().Trim() : null;
                            string nombreEspecie = col["especie"] >= 0 ? row[col["especie"]]?.ToString().Trim() : null;

                            if (string.IsNullOrWhiteSpace(nombreCorral) || !corralesPorNombre.TryGetValue(Normalizar(nombreCorral), out int idCorral) ||
                                string.IsNullOrWhiteSpace(nombreEspecie) || !especiesPorNombre.TryGetValue(Normalizar(nombreEspecie), out int idEspecie))
                            {
                                omitidos++;
                                continue;
                            }

                            var inv = new InventarioPez
                            {
                                IdCorral = idCorral,
                                IdEspecie = idEspecie,
                                Cantidad = ParsearEntero(col, row, "cantidad") ?? 0,
                                PesoPromedioKg = ParsearDecimal(col, row, "pesopromedio"),
                                Estado = col["estado"] >= 0 ? row[col["estado"]]?.ToString() : null,
                                FechaRegistro = ParsearFecha(col, row, "fecharegistro") ?? DateTime.Today,
                                Observaciones = col["observaciones"] >= 0 ? row[col["observaciones"]]?.ToString() : null
                            };

                            string query = @"INSERT INTO InventarioPez (IdCorral, IdEspecie, Cantidad, PesoPromedioKg, Estado, FechaRegistro, Observaciones)
                                              VALUES (@IdCorral, @IdEspecie, @Cantidad, @PesoPromedioKg, @Estado, @FechaRegistro, @Observaciones);";
                            using (SqlCommand cmd = new SqlCommand(query, con))
                            {
                                AgregarParametros(cmd, inv);
                                cmd.ExecuteNonQuery();
                            }
                            insertados++;
                        }
                    }
                }

                TempData["Exito"] = $"Carga completada: {insertados} registros insertados" +
                                     (omitidos > 0 ? $", {omitidos} filas omitidas (corral/especie no reconocidos)." : ".");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al procesar el archivo Excel: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private InventarioPez ObtenerPorId(int id)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"SELECT IdInventario, IdCorral, IdEspecie, Cantidad, PesoPromedioKg, Estado, FechaRegistro, Observaciones
                                  FROM InventarioPez WHERE IdInventario = @IdInventario;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@IdInventario", id);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read()) return MapearInventario(dr, conJoins: false);
                    }
                }
            }
            return null;
        }

        private void CargarListasDesplegables()
        {
            ViewBag.Corrales = ObtenerCorrales();
            ViewBag.Especies = ObtenerEspecies();
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
                    while (dr.Read()) lista.Add(new SelectListItem { Value = dr["IdCorral"].ToString(), Text = dr["Nombre"].ToString() });
                }
            }
            return lista;
        }

        private List<SelectListItem> ObtenerEspecies()
        {
            var lista = new List<SelectListItem>();
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT IdEspecie, Nombre FROM Especie ORDER BY Nombre;", con))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read()) lista.Add(new SelectListItem { Value = dr["IdEspecie"].ToString(), Text = dr["Nombre"].ToString() });
                }
            }
            return lista;
        }

        private static InventarioPez MapearInventario(SqlDataReader dr, bool conJoins)
        {
            var inv = new InventarioPez
            {
                IdInventario = dr.GetInt32(dr.GetOrdinal("IdInventario")),
                IdCorral = dr.GetInt32(dr.GetOrdinal("IdCorral")),
                IdEspecie = dr.GetInt32(dr.GetOrdinal("IdEspecie")),
                Cantidad = dr.GetInt32(dr.GetOrdinal("Cantidad")),
                PesoPromedioKg = dr["PesoPromedioKg"] != DBNull.Value ? (decimal?)dr["PesoPromedioKg"] : null,
                Estado = dr["Estado"] as string,
                FechaRegistro = dr["FechaRegistro"] != DBNull.Value ? (DateTime?)dr["FechaRegistro"] : null,
                Observaciones = dr["Observaciones"] as string
            };
            if (conJoins)
            {
                inv.NombreCorral = dr["NombreCorral"] as string;
                inv.NombreEspecie = dr["NombreEspecie"] as string;
            }
            return inv;
        }

        private static void AgregarParametros(SqlCommand cmd, InventarioPez inv)
        {
            cmd.Parameters.AddWithValue("@IdCorral", inv.IdCorral);
            cmd.Parameters.AddWithValue("@IdEspecie", inv.IdEspecie);
            cmd.Parameters.AddWithValue("@Cantidad", inv.Cantidad);
            cmd.Parameters.AddWithValue("@PesoPromedioKg", (object)inv.PesoPromedioKg ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Estado", (object)inv.Estado ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FechaRegistro", (object)inv.FechaRegistro ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Observaciones", (object)inv.Observaciones ?? DBNull.Value);
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
                string colNorm = Normalizar(dt.Columns[i].ColumnName).Replace(" ", "");
                foreach (var clave in claves)
                {
                    if (resultado[clave] == -1 && colNorm.Contains(clave)) resultado[clave] = i;
                }
            }
            return resultado;
        }

        private static int? ParsearEntero(Dictionary<string, int> col, DataRow row, string clave)
        {
            if (col[clave] < 0) return null;
            return int.TryParse(row[col[clave]]?.ToString(), out int resultado) ? resultado : (int?)null;
        }

        private static decimal? ParsearDecimal(Dictionary<string, int> col, DataRow row, string clave)
        {
            if (col[clave] < 0) return null;
            string valor = row[col[clave]]?.ToString().Replace(',', '.');
            return decimal.TryParse(valor, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal resultado)
                ? resultado : (decimal?)null;
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
