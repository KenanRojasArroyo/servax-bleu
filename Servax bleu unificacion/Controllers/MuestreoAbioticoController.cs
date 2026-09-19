using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Servax_bleu_unificacion.Models;
using Servax_bleu_unificacion.Servicios;

namespace Servax_bleu_unificacion.Controllers
{
    // -----------------------------------------------------------------
    // Reemplaza al caso de uso "calidad de agua por sitio" que antes vivía
    // en MuestreoAguaController contra la tabla legacy MuestreoAgua.
    // Ahora persiste contra MuestreoAbiotico + MuestreoNutriente, que sí
    // reflejan cómo Arian captura los datos reales (por SITIO, por Turno,
    // con 6 nutrientes normalizados en vez de un VARCHAR de texto libre).
    //
    // La otra mitad de lo que hacía MuestreoAguaController (temperatura/
    // oxígeno por CORRAL vía sensor) vive ahora en LecturaSensorCorralController.
    // -----------------------------------------------------------------
    public class MuestreoAbioticoController : Controller
    {
        private Conexion cn = new Conexion();

        private static readonly string[] NUTRIENTES_ORDEN =
            { "Nitritos", "Nitratos", "Silicatos", "Hierro", "Amonio", "Fosfatos" };

        // ⚠️ Umbral de referencia sin confirmar con Arian todavía (igual que en
        // el MuestreoAguaController original) — solo cubre oxígeno disuelto,
        // porque es la única métrica de esta tabla con un rango de literatura
        // razonable. Los 6 nutrientes NO tienen todavía un umbral de "normal"
        // confirmado por el cliente, así que no se evalúan como irregularidad.
        private const double OXIGENO_MIN = 5.0;

        // GET: MuestreoAbiotico
        public ActionResult Index(int? idSitio)
        {
            var lista = new List<MuestreoAbiotico>();

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                // Se usa la vista vw_CalidadAguaSitio: ya trae los nutrientes
                // pivoteados a columnas, evita 6 joins manuales aquí.
                string query = @"SELECT Sitio, Fecha, Turno, OxigenoDisueltoMgL, TurbidezM,
                                         Nitritos, Nitratos, Silicatos, Hierro, Amonio, Fosfatos
                                  FROM vw_CalidadAguaSitio
                                  WHERE (@idSitio IS NULL OR Sitio = (SELECT Nombre FROM Sitio WHERE IdSitio = @idSitio))
                                  ORDER BY Fecha DESC, Turno;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@idSitio", (object)idSitio ?? DBNull.Value);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var m = new MuestreoAbiotico
                            {
                                NombreSitio = dr["Sitio"] as string,
                                Fecha = dr.GetDateTime(dr.GetOrdinal("Fecha")),
                                Turno = dr["Turno"] as string,
                                OxigenoDisueltoMgL = dr["OxigenoDisueltoMgL"] != DBNull.Value ? (double?)dr["OxigenoDisueltoMgL"] : null,
                                TurbidezM = dr["TurbidezM"] != DBNull.Value ? (double?)dr["TurbidezM"] : null
                            };
                            foreach (var n in NUTRIENTES_ORDEN)
                                m.Nutrientes[n] = dr[n] != DBNull.Value ? (double?)dr[n] : null;
                            lista.Add(m);
                        }
                    }
                }
            }

            ViewBag.Sitios = ObtenerSitios();
            ViewBag.IdSitioFiltro = idSitio;
            return View(lista);
        }

        // GET: MuestreoAbiotico/Create
        public ActionResult Create()
        {
            ViewBag.Sitios = ObtenerSitios();
            return View(new MuestreoAbiotico { Fecha = DateTime.Today, Turno = "Mañana" });
        }

        // POST: MuestreoAbiotico/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(MuestreoAbiotico muestreo)
        {
            if (muestreo.IdSitio <= 0)
                ModelState.AddModelError("IdSitio", "Selecciona un sitio.");

            if (!ModelState.IsValid)
            {
                ViewBag.Sitios = ObtenerSitios();
                return View(muestreo);
            }

            int idMuestreoNuevo;

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                using (SqlTransaction tx = con.BeginTransaction())
                {
                    try
                    {
                        string queryCabecera = @"INSERT INTO MuestreoAbiotico (IdSitio, Fecha, Turno, OxigenoDisueltoMgL, TurbidezM, Observaciones)
                                                  OUTPUT INSERTED.IdMuestreo
                                                  VALUES (@IdSitio, @Fecha, @Turno, @OxigenoDisueltoMgL, @TurbidezM, @Observaciones);";
                        using (SqlCommand cmd = new SqlCommand(queryCabecera, con, tx))
                        {
                            AgregarParametrosCabecera(cmd, muestreo);
                            idMuestreoNuevo = (int)cmd.ExecuteScalar();
                        }

                        InsertarNutrientes(con, tx, idMuestreoNuevo, muestreo.Nutrientes);

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }

            await EvaluarYNotificarSiHayIrregularidad(muestreo);

            TempData["Exito"] = "Muestreo abiótico registrado correctamente.";
            return RedirectToAction("Index");
        }

        // GET: MuestreoAbiotico/Edit/5
        public ActionResult Edit(int id)
        {
            MuestreoAbiotico muestreo = ObtenerPorId(id);
            if (muestreo == null) return HttpNotFound();
            ViewBag.Sitios = ObtenerSitios();
            return View(muestreo);
        }

        // POST: MuestreoAbiotico/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, MuestreoAbiotico muestreo)
        {
            if (!ModelState.IsValid)
            {
                muestreo.IdMuestreo = id;
                ViewBag.Sitios = ObtenerSitios();
                return View(muestreo);
            }

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                using (SqlTransaction tx = con.BeginTransaction())
                {
                    try
                    {
                        string queryCabecera = @"UPDATE MuestreoAbiotico SET
                                                    IdSitio = @IdSitio, Fecha = @Fecha, Turno = @Turno,
                                                    OxigenoDisueltoMgL = @OxigenoDisueltoMgL, TurbidezM = @TurbidezM,
                                                    Observaciones = @Observaciones
                                                  WHERE IdMuestreo = @IdMuestreo;";
                        using (SqlCommand cmd = new SqlCommand(queryCabecera, con, tx))
                        {
                            AgregarParametrosCabecera(cmd, muestreo);
                            cmd.Parameters.AddWithValue("@IdMuestreo", id);
                            cmd.ExecuteNonQuery();
                        }

                        // Simplificación deliberada: borrar y reinsertar los 6 valores de
                        // nutriente en vez de un MERGE — a este volumen (6 filas) no vale
                        // la pena la complejidad de un upsert fila por fila.
                        using (SqlCommand cmdDel = new SqlCommand("DELETE FROM MuestreoNutriente WHERE IdMuestreo = @IdMuestreo;", con, tx))
                        {
                            cmdDel.Parameters.AddWithValue("@IdMuestreo", id);
                            cmdDel.ExecuteNonQuery();
                        }
                        InsertarNutrientes(con, tx, id, muestreo.Nutrientes);

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }

            await EvaluarYNotificarSiHayIrregularidad(muestreo);

            TempData["Exito"] = "Muestreo abiótico actualizado correctamente.";
            return RedirectToAction("Index");
        }

        // GET: MuestreoAbiotico/Delete/5
        public ActionResult Delete(int id)
        {
            MuestreoAbiotico muestreo = ObtenerPorId(id);
            if (muestreo == null) return HttpNotFound();
            return View(muestreo);
        }

        // POST: MuestreoAbiotico/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                using (SqlTransaction tx = con.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmdDel = new SqlCommand("DELETE FROM MuestreoNutriente WHERE IdMuestreo = @IdMuestreo;", con, tx))
                        {
                            cmdDel.Parameters.AddWithValue("@IdMuestreo", id);
                            cmdDel.ExecuteNonQuery();
                        }
                        using (SqlCommand cmd = new SqlCommand("DELETE FROM MuestreoAbiotico WHERE IdMuestreo = @IdMuestreo;", con, tx))
                        {
                            cmd.Parameters.AddWithValue("@IdMuestreo", id);
                            cmd.ExecuteNonQuery();
                        }
                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }

            TempData["Exito"] = "Muestreo eliminado correctamente.";
            return RedirectToAction("Index");
        }

        // -----------------------------------------------------------------
        // Alerta — solo evalúa oxígeno disuelto (ver comentario en la constante
        // OXIGENO_MIN). Turbidez y nutrientes quedan pendientes de umbral.
        // -----------------------------------------------------------------
        private async Task EvaluarYNotificarSiHayIrregularidad(MuestreoAbiotico m)
        {
            if (!m.OxigenoDisueltoMgL.HasValue || m.OxigenoDisueltoMgL >= OXIGENO_MIN) return;

            string nombreSitio = ObtenerNombreSitio(m.IdSitio);
            string asunto = $"⚠️ Alerta de calidad de agua — Sitio {nombreSitio}";
            string mensajeHtml = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 2px solid #dc3545; border-radius: 8px;'>
                    <h2 style='color: #dc3545;'>Oxígeno disuelto fuera de rango</h2>
                    <p><strong>Sitio:</strong> {nombreSitio}</p>
                    <p><strong>Fecha:</strong> {m.Fecha:dd/MM/yyyy} · <strong>Turno:</strong> {m.Turno}</p>
                    <p><strong>Oxígeno disuelto:</strong> {m.OxigenoDisueltoMgL} mg/L (mínimo esperado {OXIGENO_MIN} mg/L)</p>
                    <hr />
                    <p style='font-size: 12px; color: #666;'>
                        Umbral de referencia sin confirmar con el cliente — turbidez y nutrientes
                        todavía no tienen rango de irregularidad definido, ajustar cuando Arian lo confirme.
                    </p>
                </div>";

            await ServicioNotificaciones.EnviarCorreoAlertaAsync(asunto, mensajeHtml);
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private void InsertarNutrientes(SqlConnection con, SqlTransaction tx, int idMuestreo, Dictionary<string, double?> nutrientes)
        {
            string query = @"INSERT INTO MuestreoNutriente (IdMuestreo, IdNutriente, Valor)
                              SELECT @IdMuestreo, IdNutriente, @Valor FROM Nutriente WHERE Nombre = @Nombre;";
            foreach (var nombre in NUTRIENTES_ORDEN)
            {
                double? valor = nutrientes.ContainsKey(nombre) ? nutrientes[nombre] : null;
                using (SqlCommand cmd = new SqlCommand(query, con, tx))
                {
                    cmd.Parameters.AddWithValue("@IdMuestreo", idMuestreo);
                    cmd.Parameters.AddWithValue("@Nombre", nombre);
                    cmd.Parameters.AddWithValue("@Valor", (object)valor ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private MuestreoAbiotico ObtenerPorId(int id)
        {
            MuestreoAbiotico m = null;
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(
                    @"SELECT IdMuestreo, IdSitio, Fecha, Turno, OxigenoDisueltoMgL, TurbidezM, Observaciones
                      FROM MuestreoAbiotico WHERE IdMuestreo = @IdMuestreo;", con))
                {
                    cmd.Parameters.AddWithValue("@IdMuestreo", id);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read()) return null;
                        m = new MuestreoAbiotico
                        {
                            IdMuestreo = dr.GetInt32(dr.GetOrdinal("IdMuestreo")),
                            IdSitio = dr.GetInt32(dr.GetOrdinal("IdSitio")),
                            Fecha = dr.GetDateTime(dr.GetOrdinal("Fecha")),
                            Turno = dr["Turno"] as string,
                            OxigenoDisueltoMgL = dr["OxigenoDisueltoMgL"] != DBNull.Value ? (double?)dr["OxigenoDisueltoMgL"] : null,
                            TurbidezM = dr["TurbidezM"] != DBNull.Value ? (double?)dr["TurbidezM"] : null,
                            Observaciones = dr["Observaciones"] as string
                        };
                    }
                }

                using (SqlCommand cmd = new SqlCommand(
                    @"SELECT n.Nombre, mn.Valor FROM MuestreoNutriente mn
                      JOIN Nutriente n ON n.IdNutriente = mn.IdNutriente
                      WHERE mn.IdMuestreo = @IdMuestreo;", con))
                {
                    cmd.Parameters.AddWithValue("@IdMuestreo", id);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            m.Nutrientes[dr["Nombre"].ToString()] = dr["Valor"] != DBNull.Value ? (double?)dr["Valor"] : null;
                        }
                    }
                }
            }
            return m;
        }

        private string ObtenerNombreSitio(int idSitio)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT Nombre FROM Sitio WHERE IdSitio = @IdSitio;", con))
                {
                    cmd.Parameters.AddWithValue("@IdSitio", idSitio);
                    return cmd.ExecuteScalar()?.ToString() ?? $"#{idSitio}";
                }
            }
        }

        private List<SelectListItem> ObtenerSitios()
        {
            var lista = new List<SelectListItem>();
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT IdSitio, Nombre FROM Sitio ORDER BY Nombre;", con))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        lista.Add(new SelectListItem { Value = dr["IdSitio"].ToString(), Text = dr["Nombre"].ToString() });
                }
            }
            return lista;
        }

        private static void AgregarParametrosCabecera(SqlCommand cmd, MuestreoAbiotico m)
        {
            cmd.Parameters.AddWithValue("@IdSitio", m.IdSitio);
            cmd.Parameters.AddWithValue("@Fecha", m.Fecha);
            cmd.Parameters.AddWithValue("@Turno", m.Turno);
            cmd.Parameters.AddWithValue("@OxigenoDisueltoMgL", (object)m.OxigenoDisueltoMgL ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TurbidezM", (object)m.TurbidezM ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Observaciones", (object)m.Observaciones ?? DBNull.Value);
        }
    }
}
