using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Mvc;
using Servax_bleu_unificacion.Models;

namespace Servax_bleu_unificacion.Controllers
{
    /// <summary>
    /// CRUD de CrecimientoAnual (RF del cliente: "historial de crecimiento anual").
    /// Alimenta la gráfica "CrecimientoEspecie" del dashboard (HomeController.Index),
    /// que hasta ahora no tenía ninguna pantalla para capturar datos en esta tabla.
    /// </summary>
    public class CrecimientoAnualController : Controller
    {
        private Conexion cn = new Conexion();

        // GET: CrecimientoAnual
        public ActionResult Index(int? idCorral)
        {
            var lista = new List<CrecimientoAnual>();

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"SELECT ca.IdCrecimiento, ca.IdCorral, ca.IdEspecie, ca.Anio, ca.PesoPromedioInicial, ca.PesoPromedioFinal, ca.TasaCrecimiento, ca.Observaciones,
                                         c.Nombre AS NombreCorral, e.Nombre AS NombreEspecie
                                  FROM CrecimientoAnual ca
                                  INNER JOIN Corral c ON ca.IdCorral = c.IdCorral
                                  INNER JOIN Especie e ON ca.IdEspecie = e.IdEspecie
                                  WHERE (@idCorral IS NULL OR ca.IdCorral = @idCorral)
                                  ORDER BY ca.Anio DESC, c.Nombre;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@idCorral", (object)idCorral ?? DBNull.Value);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read()) lista.Add(MapearCrecimiento(dr, conJoins: true));
                    }
                }
            }

            ViewBag.Corrales = ObtenerCorrales();
            ViewBag.IdCorralFiltro = idCorral;
            return View(lista);
        }

        // GET: CrecimientoAnual/Create
        public ActionResult Create()
        {
            CargarListasDesplegables();
            return View(new CrecimientoAnual { Anio = DateTime.Today.Year });
        }

        // POST: CrecimientoAnual/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CrecimientoAnual ca)
        {
            if (ca.IdCorral <= 0) ModelState.AddModelError("IdCorral", "Selecciona un corral.");
            if (ca.IdEspecie <= 0) ModelState.AddModelError("IdEspecie", "Selecciona una especie.");

            if (!ModelState.IsValid)
            {
                CargarListasDesplegables();
                return View(ca);
            }

            CalcularTasaSiFalta(ca);

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"INSERT INTO CrecimientoAnual (IdCorral, IdEspecie, Anio, PesoPromedioInicial, PesoPromedioFinal, TasaCrecimiento, Observaciones)
                                  VALUES (@IdCorral, @IdEspecie, @Anio, @PesoPromedioInicial, @PesoPromedioFinal, @TasaCrecimiento, @Observaciones);";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    AgregarParametros(cmd, ca);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Exito"] = "Registro de crecimiento anual guardado correctamente.";
            return RedirectToAction("Index");
        }

        // GET: CrecimientoAnual/Edit/5
        public ActionResult Edit(int id)
        {
            CrecimientoAnual ca = ObtenerPorId(id);
            if (ca == null) return HttpNotFound();
            CargarListasDesplegables();
            return View(ca);
        }

        // POST: CrecimientoAnual/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, CrecimientoAnual ca)
        {
            if (!ModelState.IsValid)
            {
                ca.IdCrecimiento = id;
                CargarListasDesplegables();
                return View(ca);
            }

            CalcularTasaSiFalta(ca);

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"UPDATE CrecimientoAnual SET
                                    IdCorral = @IdCorral, IdEspecie = @IdEspecie, Anio = @Anio,
                                    PesoPromedioInicial = @PesoPromedioInicial, PesoPromedioFinal = @PesoPromedioFinal,
                                    TasaCrecimiento = @TasaCrecimiento, Observaciones = @Observaciones
                                  WHERE IdCrecimiento = @IdCrecimiento;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    AgregarParametros(cmd, ca);
                    cmd.Parameters.AddWithValue("@IdCrecimiento", id);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Exito"] = "Registro de crecimiento anual actualizado correctamente.";
            return RedirectToAction("Index");
        }

        // GET: CrecimientoAnual/Delete/5
        public ActionResult Delete(int id)
        {
            CrecimientoAnual ca = ObtenerPorId(id);
            if (ca == null) return HttpNotFound();
            return View(ca);
        }

        // POST: CrecimientoAnual/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("DELETE FROM CrecimientoAnual WHERE IdCrecimiento = @IdCrecimiento;", con))
                {
                    cmd.Parameters.AddWithValue("@IdCrecimiento", id);
                    cmd.ExecuteNonQuery();
                }
            }
            TempData["Exito"] = "Registro de crecimiento anual eliminado.";
            return RedirectToAction("Index");
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private CrecimientoAnual ObtenerPorId(int id)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"SELECT IdCrecimiento, IdCorral, IdEspecie, Anio, PesoPromedioInicial, PesoPromedioFinal, TasaCrecimiento, Observaciones
                                  FROM CrecimientoAnual WHERE IdCrecimiento = @IdCrecimiento;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@IdCrecimiento", id);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read()) return MapearCrecimiento(dr, conJoins: false);
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

        // Si no capturan la tasa de crecimiento a mano pero sí los dos pesos,
        // se calcula sola: ((Final - Inicial) / Inicial) * 100.
        private static void CalcularTasaSiFalta(CrecimientoAnual ca)
        {
            if (ca.TasaCrecimiento == null && ca.PesoPromedioInicial.HasValue && ca.PesoPromedioInicial.Value != 0 && ca.PesoPromedioFinal.HasValue)
            {
                ca.TasaCrecimiento = (float)(((ca.PesoPromedioFinal.Value - ca.PesoPromedioInicial.Value) / ca.PesoPromedioInicial.Value) * 100);
            }
        }

        private static CrecimientoAnual MapearCrecimiento(SqlDataReader dr, bool conJoins)
        {
            var ca = new CrecimientoAnual
            {
                IdCrecimiento = dr.GetInt32(dr.GetOrdinal("IdCrecimiento")),
                IdCorral = dr.GetInt32(dr.GetOrdinal("IdCorral")),
                IdEspecie = dr.GetInt32(dr.GetOrdinal("IdEspecie")),
                Anio = dr.GetInt32(dr.GetOrdinal("Anio")),
                PesoPromedioInicial = dr["PesoPromedioInicial"] != DBNull.Value ? (decimal?)dr["PesoPromedioInicial"] : null,
                PesoPromedioFinal = dr["PesoPromedioFinal"] != DBNull.Value ? (decimal?)dr["PesoPromedioFinal"] : null,
                TasaCrecimiento = dr["TasaCrecimiento"] != DBNull.Value ? (float?)Convert.ToDouble(dr["TasaCrecimiento"]) : null,
                Observaciones = dr["Observaciones"] as string
            };
            if (conJoins)
            {
                ca.NombreCorral = dr["NombreCorral"] as string;
                ca.NombreEspecie = dr["NombreEspecie"] as string;
            }
            return ca;
        }

        private static void AgregarParametros(SqlCommand cmd, CrecimientoAnual ca)
        {
            cmd.Parameters.AddWithValue("@IdCorral", ca.IdCorral);
            cmd.Parameters.AddWithValue("@IdEspecie", ca.IdEspecie);
            cmd.Parameters.AddWithValue("@Anio", ca.Anio);
            cmd.Parameters.AddWithValue("@PesoPromedioInicial", (object)ca.PesoPromedioInicial ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PesoPromedioFinal", (object)ca.PesoPromedioFinal ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TasaCrecimiento", (object)ca.TasaCrecimiento ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Observaciones", (object)ca.Observaciones ?? DBNull.Value);
        }
    }
}
