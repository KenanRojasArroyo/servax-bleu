using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Mvc;
using Servax_bleu_unificacion.Models;

namespace Servax_bleu_unificacion.Controllers
{
    public class RegistroAlimentacionController : Controller
    {
        private Conexion cn = new Conexion();

        // GET: RegistroAlimentacion
        public ActionResult Index(int? idCorral)
        {
            var lista = new List<RegistroAlimentacion>();
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"SELECT r.IdRegistro, r.IdCorral, r.IdAlimento, r.Fecha, r.CantidadKg, r.Responsable, r.Mortalidad, r.Observaciones,
                                         c.Nombre AS NombreCorral, a.Nombre AS NombreAlimento
                                  FROM RegistroAlimentacion r
                                  INNER JOIN Corral c ON r.IdCorral = c.IdCorral
                                  INNER JOIN Alimento a ON r.IdAlimento = a.IdAlimento
                                  WHERE (@idCorral IS NULL OR r.IdCorral = @idCorral)
                                  ORDER BY r.Fecha DESC;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@idCorral", (object)idCorral ?? DBNull.Value);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read()) lista.Add(Mapear(dr, conJoins: true));
                    }
                }
            }

            CargarListasDesplegables();
            ViewBag.IdCorralFiltro = idCorral;
            return View(lista);
        }

        // GET: RegistroAlimentacion/Create
        public ActionResult Create()
        {
            CargarListasDesplegables();
            return View(new RegistroAlimentacion { Fecha = DateTime.Today });
        }

        // POST: RegistroAlimentacion/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(RegistroAlimentacion reg)
        {
            if (reg.IdCorral <= 0) ModelState.AddModelError("IdCorral", "Selecciona un corral.");
            if (reg.IdAlimento <= 0) ModelState.AddModelError("IdAlimento", "Selecciona un alimento.");
            if (reg.CantidadKg <= 0) ModelState.AddModelError("CantidadKg", "La cantidad debe ser mayor a cero.");

            if (!ModelState.IsValid)
            {
                CargarListasDesplegables();
                return View(reg);
            }

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"INSERT INTO RegistroAlimentacion (IdCorral, IdAlimento, Fecha, CantidadKg, Responsable, Mortalidad, Observaciones)
                                  VALUES (@IdCorral, @IdAlimento, @Fecha, @CantidadKg, @Responsable, @Mortalidad, @Observaciones);";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    AgregarParametros(cmd, reg);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Exito"] = reg.Mortalidad > 0
                ? $"Registro guardado. Se reportaron {reg.Mortalidad} muertes — revisa el Reporte de Mortalidad."
                : "Registro de alimentación guardado correctamente.";
            return RedirectToAction("Index");
        }

        // GET: RegistroAlimentacion/Edit/5
        public ActionResult Edit(int id)
        {
            RegistroAlimentacion reg = ObtenerPorId(id);
            if (reg == null) return HttpNotFound();
            CargarListasDesplegables();
            return View(reg);
        }

        // POST: RegistroAlimentacion/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, RegistroAlimentacion reg)
        {
            if (!ModelState.IsValid)
            {
                reg.IdRegistro = id;
                CargarListasDesplegables();
                return View(reg);
            }

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"UPDATE RegistroAlimentacion SET
                                    IdCorral = @IdCorral, IdAlimento = @IdAlimento, Fecha = @Fecha,
                                    CantidadKg = @CantidadKg, Responsable = @Responsable,
                                    Mortalidad = @Mortalidad, Observaciones = @Observaciones
                                  WHERE IdRegistro = @IdRegistro;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    AgregarParametros(cmd, reg);
                    cmd.Parameters.AddWithValue("@IdRegistro", id);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Exito"] = "Registro actualizado correctamente.";
            return RedirectToAction("Index");
        }

        // GET: RegistroAlimentacion/Delete/5
        public ActionResult Delete(int id)
        {
            RegistroAlimentacion reg = ObtenerPorId(id);
            if (reg == null) return HttpNotFound();
            return View(reg);
        }

        // POST: RegistroAlimentacion/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("DELETE FROM RegistroAlimentacion WHERE IdRegistro = @IdRegistro;", con))
                {
                    cmd.Parameters.AddWithValue("@IdRegistro", id);
                    cmd.ExecuteNonQuery();
                }
            }
            TempData["Exito"] = "Registro eliminado correctamente.";
            return RedirectToAction("Index");
        }

        // -----------------------------------------------------------------
        private RegistroAlimentacion ObtenerPorId(int id)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"SELECT IdRegistro, IdCorral, IdAlimento, Fecha, CantidadKg, Responsable, Mortalidad, Observaciones
                                  FROM RegistroAlimentacion WHERE IdRegistro = @IdRegistro;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@IdRegistro", id);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read()) return Mapear(dr, conJoins: false);
                    }
                }
            }
            return null;
        }

        private void CargarListasDesplegables()
        {
            var corrales = new List<SelectListItem>();
            var alimentos = new List<SelectListItem>();
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT IdCorral, Nombre FROM Corral ORDER BY Nombre;", con))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read()) corrales.Add(new SelectListItem { Value = dr["IdCorral"].ToString(), Text = dr["Nombre"].ToString() });
                }
                using (SqlCommand cmd = new SqlCommand("SELECT IdAlimento, Nombre FROM Alimento ORDER BY Nombre;", con))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read()) alimentos.Add(new SelectListItem { Value = dr["IdAlimento"].ToString(), Text = dr["Nombre"].ToString() });
                }
            }
            ViewBag.Corrales = corrales;
            ViewBag.Alimentos = alimentos;
        }

        private static RegistroAlimentacion Mapear(SqlDataReader dr, bool conJoins)
        {
            var reg = new RegistroAlimentacion
            {
                IdRegistro = dr.GetInt32(dr.GetOrdinal("IdRegistro")),
                IdCorral = dr.GetInt32(dr.GetOrdinal("IdCorral")),
                IdAlimento = dr.GetInt32(dr.GetOrdinal("IdAlimento")),
                Fecha = dr.GetDateTime(dr.GetOrdinal("Fecha")),
                CantidadKg = dr.GetDecimal(dr.GetOrdinal("CantidadKg")),
                Responsable = dr["Responsable"] as string,
                Mortalidad = dr["Mortalidad"] != DBNull.Value ? dr.GetInt32(dr.GetOrdinal("Mortalidad")) : 0,
                Observaciones = dr["Observaciones"] as string
            };
            if (conJoins)
            {
                reg.NombreCorral = dr["NombreCorral"] as string;
                reg.NombreAlimento = dr["NombreAlimento"] as string;
            }
            return reg;
        }

        private static void AgregarParametros(SqlCommand cmd, RegistroAlimentacion reg)
        {
            cmd.Parameters.AddWithValue("@IdCorral", reg.IdCorral);
            cmd.Parameters.AddWithValue("@IdAlimento", reg.IdAlimento);
            cmd.Parameters.AddWithValue("@Fecha", reg.Fecha);
            cmd.Parameters.AddWithValue("@CantidadKg", reg.CantidadKg);
            cmd.Parameters.AddWithValue("@Responsable", (object)reg.Responsable ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Mortalidad", reg.Mortalidad);
            cmd.Parameters.AddWithValue("@Observaciones", (object)reg.Observaciones ?? DBNull.Value);
        }
    }
}
