using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Mvc;
using Servax_bleu_unificacion.Models;

namespace Servax_bleu_unificacion.Controllers
{
    /// <summary>
    /// Cubre el RF del cliente: "historial de inventario por corral (cantidad,
    /// estado, calidad de agua, nutrientes)". Es una bitácora/snapshot periódico,
    /// no un registro transaccional — ver nota en Docs/ERD_ServaxBleu.md.
    /// </summary>
    public class HistorialCorralController : Controller
    {
        private Conexion cn = new Conexion();

        // GET: HistorialCorral
        public ActionResult Index(int? idCorral)
        {
            var lista = new List<HistorialCorral>();
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"SELECT h.IdHistorial, h.IdCorral, h.Fecha, h.CantidadPeces, h.EstadoGeneral,
                                         h.ResumenCalidadAgua, h.ResumenNutrientes, h.Observaciones,
                                         c.Nombre AS NombreCorral
                                  FROM HistorialCorral h
                                  INNER JOIN Corral c ON h.IdCorral = c.IdCorral
                                  WHERE (@idCorral IS NULL OR h.IdCorral = @idCorral)
                                  ORDER BY h.Fecha DESC;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@idCorral", (object)idCorral ?? DBNull.Value);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read()) lista.Add(Mapear(dr, conJoin: true));
                    }
                }
            }

            ViewBag.Corrales = ObtenerCorrales();
            ViewBag.IdCorralFiltro = idCorral;
            return View(lista);
        }

        // GET: HistorialCorral/Create
        public ActionResult Create()
        {
            ViewBag.Corrales = ObtenerCorrales();
            return View(new HistorialCorral { Fecha = DateTime.Today });
        }

        // POST: HistorialCorral/Create
        // También se puede llamar automáticamente al cierre de mes desde otro
        // proceso (RegistroAlimentacion/MuestreoAgua) para armar el snapshot;
        // por ahora es captura manual del responsable del corral.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(HistorialCorral hist)
        {
            if (hist.IdCorral <= 0) ModelState.AddModelError("IdCorral", "Selecciona un corral.");

            if (!ModelState.IsValid)
            {
                ViewBag.Corrales = ObtenerCorrales();
                return View(hist);
            }

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"INSERT INTO HistorialCorral (IdCorral, Fecha, CantidadPeces, EstadoGeneral, ResumenCalidadAgua, ResumenNutrientes, Observaciones)
                                  VALUES (@IdCorral, @Fecha, @CantidadPeces, @EstadoGeneral, @ResumenCalidadAgua, @ResumenNutrientes, @Observaciones);";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    AgregarParametros(cmd, hist);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Exito"] = "Snapshot de historial guardado correctamente.";
            return RedirectToAction("Index");
        }

        // GET: HistorialCorral/Delete/5
        public ActionResult Delete(int id)
        {
            HistorialCorral hist = ObtenerPorId(id);
            if (hist == null) return HttpNotFound();
            return View(hist);
        }

        // POST: HistorialCorral/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("DELETE FROM HistorialCorral WHERE IdHistorial = @IdHistorial;", con))
                {
                    cmd.Parameters.AddWithValue("@IdHistorial", id);
                    cmd.ExecuteNonQuery();
                }
            }
            TempData["Exito"] = "Registro de historial eliminado.";
            return RedirectToAction("Index");
        }

        // -----------------------------------------------------------------
        private HistorialCorral ObtenerPorId(int id)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"SELECT IdHistorial, IdCorral, Fecha, CantidadPeces, EstadoGeneral, ResumenCalidadAgua, ResumenNutrientes, Observaciones
                                  FROM HistorialCorral WHERE IdHistorial = @IdHistorial;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@IdHistorial", id);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read()) return Mapear(dr, conJoin: false);
                    }
                }
            }
            return null;
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

        private static HistorialCorral Mapear(SqlDataReader dr, bool conJoin)
        {
            var hist = new HistorialCorral
            {
                IdHistorial = dr.GetInt32(dr.GetOrdinal("IdHistorial")),
                IdCorral = dr.GetInt32(dr.GetOrdinal("IdCorral")),
                Fecha = dr.GetDateTime(dr.GetOrdinal("Fecha")),
                CantidadPeces = dr["CantidadPeces"] != DBNull.Value ? dr.GetInt32(dr.GetOrdinal("CantidadPeces")) : 0,
                EstadoGeneral = dr["EstadoGeneral"] as string,
                ResumenCalidadAgua = dr["ResumenCalidadAgua"] as string,
                ResumenNutrientes = dr["ResumenNutrientes"] as string,
                Observaciones = dr["Observaciones"] as string
            };
            if (conJoin) hist.NombreCorral = dr["NombreCorral"] as string;
            return hist;
        }

        private static void AgregarParametros(SqlCommand cmd, HistorialCorral hist)
        {
            cmd.Parameters.AddWithValue("@IdCorral", hist.IdCorral);
            cmd.Parameters.AddWithValue("@Fecha", hist.Fecha);
            cmd.Parameters.AddWithValue("@CantidadPeces", hist.CantidadPeces);
            cmd.Parameters.AddWithValue("@EstadoGeneral", (object)hist.EstadoGeneral ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ResumenCalidadAgua", (object)hist.ResumenCalidadAgua ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ResumenNutrientes", (object)hist.ResumenNutrientes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Observaciones", (object)hist.Observaciones ?? DBNull.Value);
        }
    }
}
