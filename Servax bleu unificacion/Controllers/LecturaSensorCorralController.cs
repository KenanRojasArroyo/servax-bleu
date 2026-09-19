using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.Mvc;
using Servax_bleu_unificacion.Models;
using Servax_bleu_unificacion.Servicios;

namespace Servax_bleu_unificacion.Controllers
{
    // -----------------------------------------------------------------
    // Reemplaza la mitad de MuestreoAguaController que capturaba
    // Temperatura/Oxígeno por CORRAL. Ahora contra LecturaSensorCorral,
    // que además distingue Sensor automático (AquaHub/BloopTracker) vs.
    // dato manual de mediodía y la profundidad de la lectura (3m/20m) —
    // tal como lo muestran las hojas reales de "Sistema Lobina" /
    // "Línea de Cosecha".
    // -----------------------------------------------------------------
    public class LecturaSensorCorralController : Controller
    {
        private Conexion cn = new Conexion();

        // Mismos umbrales que usaba MuestreoAguaController originalmente para
        // Temperatura/Oxígeno — siguen sin confirmar con Arian.
        private const double TEMP_MIN = 18.0;
        private const double TEMP_MAX = 26.0;
        private const double OXIGENO_MIN = 5.0;

        // GET: LecturaSensorCorral
        public ActionResult Index(int? idCorral)
        {
            var lista = new List<LecturaSensorCorral>();

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"SELECT l.IdLectura, l.IdCorral, l.Fecha, l.Profundidad, l.MetodoCaptura, l.IdSensor,
                                         l.Temperatura, l.OxigenoMgL, l.SaturacionOxigenoPct,
                                         c.Nombre AS NombreCorral, s.NumeroSensor, s.Marca AS MarcaSensor
                                  FROM LecturaSensorCorral l
                                  INNER JOIN Corral c ON c.IdCorral = l.IdCorral
                                  LEFT JOIN Sensor s ON s.IdSensor = l.IdSensor
                                  WHERE (@idCorral IS NULL OR l.IdCorral = @idCorral)
                                  ORDER BY l.Fecha DESC, l.Profundidad;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@idCorral", (object)idCorral ?? DBNull.Value);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read()) lista.Add(Mapear(dr));
                    }
                }
            }

            ViewBag.Corrales = ObtenerCorrales();
            ViewBag.IdCorralFiltro = idCorral;
            return View(lista);
        }

        // GET: LecturaSensorCorral/Create
        public ActionResult Create(int? idCorral)
        {
            ViewBag.Corrales = ObtenerCorrales();
            ViewBag.Sensores = idCorral.HasValue ? ObtenerSensoresPorCorral(idCorral.Value) : new List<SelectListItem>();
            return View(new LecturaSensorCorral { Fecha = DateTime.Today, MetodoCaptura = "Sensor", IdCorral = idCorral ?? 0 });
        }

        // GET: LecturaSensorCorral/ObtenerSensores?idCorral=5  (AJAX para refrescar el combo de sensores al cambiar de corral)
        public JsonResult ObtenerSensores(int idCorral)
        {
            var sensores = ObtenerSensoresPorCorral(idCorral);
            return Json(sensores, JsonRequestBehavior.AllowGet);
        }

        // POST: LecturaSensorCorral/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(LecturaSensorCorral lectura)
        {
            if (lectura.IdCorral <= 0)
                ModelState.AddModelError("IdCorral", "Selecciona un corral.");
            if (lectura.MetodoCaptura == "Sensor" && !lectura.IdSensor.HasValue)
                ModelState.AddModelError("IdSensor", "Selecciona el sensor que tomó la lectura.");

            if (!ModelState.IsValid)
            {
                ViewBag.Corrales = ObtenerCorrales();
                ViewBag.Sensores = lectura.IdCorral > 0 ? ObtenerSensoresPorCorral(lectura.IdCorral) : new List<SelectListItem>();
                return View(lectura);
            }

            if (lectura.MetodoCaptura == "Manual") lectura.IdSensor = null;

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"INSERT INTO LecturaSensorCorral (IdCorral, Fecha, Profundidad, MetodoCaptura, IdSensor, Temperatura, OxigenoMgL, SaturacionOxigenoPct)
                                  VALUES (@IdCorral, @Fecha, @Profundidad, @MetodoCaptura, @IdSensor, @Temperatura, @OxigenoMgL, @SaturacionOxigenoPct);";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    AgregarParametros(cmd, lectura);
                    cmd.ExecuteNonQuery();
                }
            }

            await EvaluarYNotificarSiHayIrregularidad(lectura);

            TempData["Exito"] = "Lectura registrada correctamente.";
            return RedirectToAction("Index");
        }

        // GET: LecturaSensorCorral/Edit/5
        public ActionResult Edit(int id)
        {
            LecturaSensorCorral lectura = ObtenerPorId(id);
            if (lectura == null) return HttpNotFound();
            ViewBag.Corrales = ObtenerCorrales();
            ViewBag.Sensores = ObtenerSensoresPorCorral(lectura.IdCorral);
            return View(lectura);
        }

        // POST: LecturaSensorCorral/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, LecturaSensorCorral lectura)
        {
            if (lectura.MetodoCaptura == "Sensor" && !lectura.IdSensor.HasValue)
                ModelState.AddModelError("IdSensor", "Selecciona el sensor que tomó la lectura.");

            if (!ModelState.IsValid)
            {
                lectura.IdLectura = id;
                ViewBag.Corrales = ObtenerCorrales();
                ViewBag.Sensores = ObtenerSensoresPorCorral(lectura.IdCorral);
                return View(lectura);
            }

            if (lectura.MetodoCaptura == "Manual") lectura.IdSensor = null;

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"UPDATE LecturaSensorCorral SET
                                    IdCorral = @IdCorral, Fecha = @Fecha, Profundidad = @Profundidad,
                                    MetodoCaptura = @MetodoCaptura, IdSensor = @IdSensor,
                                    Temperatura = @Temperatura, OxigenoMgL = @OxigenoMgL, SaturacionOxigenoPct = @SaturacionOxigenoPct
                                  WHERE IdLectura = @IdLectura;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    AgregarParametros(cmd, lectura);
                    cmd.Parameters.AddWithValue("@IdLectura", id);
                    cmd.ExecuteNonQuery();
                }
            }

            await EvaluarYNotificarSiHayIrregularidad(lectura);

            TempData["Exito"] = "Lectura actualizada correctamente.";
            return RedirectToAction("Index");
        }

        // GET: LecturaSensorCorral/Delete/5
        public ActionResult Delete(int id)
        {
            LecturaSensorCorral lectura = ObtenerPorId(id);
            if (lectura == null) return HttpNotFound();
            return View(lectura);
        }

        // POST: LecturaSensorCorral/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("DELETE FROM LecturaSensorCorral WHERE IdLectura = @IdLectura;", con))
                {
                    cmd.Parameters.AddWithValue("@IdLectura", id);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Exito"] = "Lectura eliminada correctamente.";
            return RedirectToAction("Index");
        }

        // -----------------------------------------------------------------
        // Alerta — mismo criterio de Temperatura/Oxígeno del MuestreoAguaController
        // original, ahora evaluado por lectura de corral+profundidad+método.
        // -----------------------------------------------------------------
        private async Task EvaluarYNotificarSiHayIrregularidad(LecturaSensorCorral l)
        {
            bool tempFuera = l.Temperatura.HasValue && (l.Temperatura < TEMP_MIN || l.Temperatura > TEMP_MAX);
            bool oxigenoFuera = l.OxigenoMgL.HasValue && l.OxigenoMgL < OXIGENO_MIN;
            if (!tempFuera && !oxigenoFuera) return;

            string nombreCorral = ObtenerNombreCorral(l.IdCorral);
            var motivos = new List<string>();
            if (tempFuera) motivos.Add($"Temperatura {l.Temperatura} °C (rango esperado {TEMP_MIN}-{TEMP_MAX} °C)");
            if (oxigenoFuera) motivos.Add($"Oxígeno {l.OxigenoMgL} mg/L (mínimo esperado {OXIGENO_MIN} mg/L)");

            string asunto = $"⚠️ Alerta de sensor — Corral {nombreCorral} ({l.Profundidad} m)";
            string mensajeHtml = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 2px solid #dc3545; border-radius: 8px;'>
                    <h2 style='color: #dc3545;'>Lectura fuera de rango</h2>
                    <p><strong>Corral:</strong> {nombreCorral} · <strong>Profundidad:</strong> {l.Profundidad} m</p>
                    <p><strong>Fecha:</strong> {l.Fecha:dd/MM/yyyy} · <strong>Método:</strong> {l.MetodoCaptura}</p>
                    <ul>{string.Join("", motivos.ConvertAll(m => $"<li>{m}</li>"))}</ul>
                    <hr />
                    <p style='font-size: 12px; color: #666;'>Umbrales de referencia sin confirmar con el cliente todavía.</p>
                </div>";

            await ServicioNotificaciones.EnviarCorreoAlertaAsync(asunto, mensajeHtml);
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private LecturaSensorCorral ObtenerPorId(int id)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"SELECT l.IdLectura, l.IdCorral, l.Fecha, l.Profundidad, l.MetodoCaptura, l.IdSensor,
                                         l.Temperatura, l.OxigenoMgL, l.SaturacionOxigenoPct,
                                         c.Nombre AS NombreCorral, s.NumeroSensor, s.Marca AS MarcaSensor
                                  FROM LecturaSensorCorral l
                                  INNER JOIN Corral c ON c.IdCorral = l.IdCorral
                                  LEFT JOIN Sensor s ON s.IdSensor = l.IdSensor
                                  WHERE l.IdLectura = @IdLectura;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@IdLectura", id);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read()) return Mapear(dr);
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
                    return cmd.ExecuteScalar()?.ToString() ?? $"#{idCorral}";
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
                        lista.Add(new SelectListItem { Value = dr["IdCorral"].ToString(), Text = dr["Nombre"].ToString() });
                }
            }
            return lista;
        }

        private List<SelectListItem> ObtenerSensoresPorCorral(int idCorral)
        {
            var lista = new List<SelectListItem>();
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(
                    @"SELECT IdSensor, NumeroSensor, Marca, ProfundidadInstalacion
                      FROM Sensor WHERE IdCorral = @IdCorral AND Activo = 1
                      ORDER BY ProfundidadInstalacion;", con))
                {
                    cmd.Parameters.AddWithValue("@IdCorral", idCorral);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new SelectListItem
                            {
                                Value = dr["IdSensor"].ToString(),
                                Text = $"{dr["Marca"]} {dr["NumeroSensor"]} ({dr["ProfundidadInstalacion"]} m)"
                            });
                        }
                    }
                }
            }
            return lista;
        }

        private static LecturaSensorCorral Mapear(SqlDataReader dr)
        {
            return new LecturaSensorCorral
            {
                IdLectura = dr.GetInt32(dr.GetOrdinal("IdLectura")),
                IdCorral = dr.GetInt32(dr.GetOrdinal("IdCorral")),
                Fecha = dr.GetDateTime(dr.GetOrdinal("Fecha")),
                Profundidad = (double)dr["Profundidad"],
                MetodoCaptura = dr["MetodoCaptura"] as string,
                IdSensor = dr["IdSensor"] != DBNull.Value ? (int?)dr["IdSensor"] : null,
                Temperatura = dr["Temperatura"] != DBNull.Value ? (double?)dr["Temperatura"] : null,
                OxigenoMgL = dr["OxigenoMgL"] != DBNull.Value ? (double?)dr["OxigenoMgL"] : null,
                SaturacionOxigenoPct = dr["SaturacionOxigenoPct"] != DBNull.Value ? (double?)dr["SaturacionOxigenoPct"] : null,
                NombreCorral = dr["NombreCorral"] as string,
                NumeroSensor = dr["NumeroSensor"] as string,
                MarcaSensor = dr["MarcaSensor"] as string
            };
        }

        private static void AgregarParametros(SqlCommand cmd, LecturaSensorCorral l)
        {
            cmd.Parameters.AddWithValue("@IdCorral", l.IdCorral);
            cmd.Parameters.AddWithValue("@Fecha", l.Fecha);
            cmd.Parameters.AddWithValue("@Profundidad", l.Profundidad);
            cmd.Parameters.AddWithValue("@MetodoCaptura", l.MetodoCaptura);
            cmd.Parameters.AddWithValue("@IdSensor", (object)l.IdSensor ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Temperatura", (object)l.Temperatura ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@OxigenoMgL", (object)l.OxigenoMgL ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SaturacionOxigenoPct", (object)l.SaturacionOxigenoPct ?? DBNull.Value);
        }
    }
}
