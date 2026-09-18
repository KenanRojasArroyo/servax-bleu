using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Mvc;
using Servax_bleu_unificacion.Models;

namespace Servax_bleu_unificacion.Controllers
{
    public class BarcoController : Controller
    {
        private Conexion cn = new Conexion();

        // GET: Barco
        public ActionResult Index()
        {
            var lista = new List<Barco>();
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = "SELECT IdBarco, Nombre, CapacidadToneladas, Estado, FechaAlta, Observaciones FROM Barco ORDER BY Nombre;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read()) lista.Add(Mapear(dr));
                }
            }
            return View(lista);
        }

        // GET: Barco/Create
        public ActionResult Create()
        {
            return View(new Barco { Estado = "Activo", FechaAlta = DateTime.Today });
        }

        // POST: Barco/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Barco barco)
        {
            if (string.IsNullOrWhiteSpace(barco.Nombre)) ModelState.AddModelError("Nombre", "El nombre del barco es obligatorio.");
            if (barco.CapacidadToneladas <= 0) ModelState.AddModelError("CapacidadToneladas", "La capacidad debe ser mayor a cero.");

            if (!ModelState.IsValid) return View(barco);

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"INSERT INTO Barco (Nombre, CapacidadToneladas, Estado, FechaAlta, Observaciones)
                                  VALUES (@Nombre, @CapacidadToneladas, @Estado, @FechaAlta, @Observaciones);";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    AgregarParametros(cmd, barco);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Exito"] = $"Barco \"{barco.Nombre}\" dado de alta correctamente.";
            return RedirectToAction("Index");
        }

        // GET: Barco/Edit/5
        public ActionResult Edit(int id)
        {
            Barco barco = ObtenerPorId(id);
            if (barco == null) return HttpNotFound();
            return View(barco);
        }

        // POST: Barco/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, Barco barco)
        {
            if (string.IsNullOrWhiteSpace(barco.Nombre)) ModelState.AddModelError("Nombre", "El nombre del barco es obligatorio.");
            if (barco.CapacidadToneladas <= 0) ModelState.AddModelError("CapacidadToneladas", "La capacidad debe ser mayor a cero.");

            if (!ModelState.IsValid)
            {
                barco.IdBarco = id;
                return View(barco);
            }

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"UPDATE Barco SET Nombre = @Nombre, CapacidadToneladas = @CapacidadToneladas,
                                    Estado = @Estado, FechaAlta = @FechaAlta, Observaciones = @Observaciones
                                  WHERE IdBarco = @IdBarco;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    AgregarParametros(cmd, barco);
                    cmd.Parameters.AddWithValue("@IdBarco", id);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Exito"] = $"Barco \"{barco.Nombre}\" actualizado correctamente.";
            return RedirectToAction("Index");
        }

        // GET: Barco/Delete/5
        public ActionResult Delete(int id)
        {
            Barco barco = ObtenerPorId(id);
            if (barco == null) return HttpNotFound();
            return View(barco);
        }

        // POST: Barco/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                try
                {
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM Barco WHERE IdBarco = @IdBarco;", con))
                    {
                        cmd.Parameters.AddWithValue("@IdBarco", id);
                        cmd.ExecuteNonQuery();
                    }
                    TempData["Exito"] = "Barco eliminado correctamente.";
                }
                catch (SqlException ex)
                {
                    TempData["Error"] = "No se pudo eliminar: el barco tiene distribuciones de carnada asociadas. " +
                                         "Considera marcarlo como Inactivo en vez de eliminarlo. Detalle: " + ex.Message;
                }
            }
            return RedirectToAction("Index");
        }

        // -----------------------------------------------------------------
        private Barco ObtenerPorId(int id)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = "SELECT IdBarco, Nombre, CapacidadToneladas, Estado, FechaAlta, Observaciones FROM Barco WHERE IdBarco = @IdBarco;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@IdBarco", id);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read()) return Mapear(dr);
                    }
                }
            }
            return null;
        }

        private static Barco Mapear(SqlDataReader dr)
        {
            return new Barco
            {
                IdBarco = dr.GetInt32(dr.GetOrdinal("IdBarco")),
                Nombre = dr["Nombre"] as string,
                CapacidadToneladas = dr.GetDecimal(dr.GetOrdinal("CapacidadToneladas")),
                Estado = dr["Estado"] as string,
                FechaAlta = dr["FechaAlta"] != DBNull.Value ? (DateTime?)dr["FechaAlta"] : null,
                Observaciones = dr["Observaciones"] as string
            };
        }

        private static void AgregarParametros(SqlCommand cmd, Barco barco)
        {
            cmd.Parameters.AddWithValue("@Nombre", barco.Nombre ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CapacidadToneladas", barco.CapacidadToneladas);
            cmd.Parameters.AddWithValue("@Estado", (object)barco.Estado ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FechaAlta", (object)barco.FechaAlta ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Observaciones", (object)barco.Observaciones ?? DBNull.Value);
        }
    }
}
