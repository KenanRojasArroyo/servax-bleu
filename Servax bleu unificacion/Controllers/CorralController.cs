using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using Servax_bleu_unificacion.Models;

namespace Servax_bleu_unificacion.Controllers
{
    public class CorralController : Controller
    {
        private Conexion cn = new Conexion();

        // GET: Corral
        public ActionResult Index()
        {
            var lista = new List<Corral>();

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"SELECT IdCorral, Nombre, Ubicacion, CapacidadMaxima, FechaInstalacion, Estado, Observaciones
                                  FROM Corral
                                  ORDER BY Nombre;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(MapearCorral(dr));
                    }
                }
            }

            return View(lista);
        }

        // GET: Corral/Details/5
        public ActionResult Details(int id)
        {
            Corral corral = ObtenerPorId(id);
            if (corral == null) return HttpNotFound();
            return View(corral);
        }

        // GET: Corral/Create
        public ActionResult Create()
        {
            return View(new Corral());
        }

        // POST: Corral/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Corral corral)
        {
            if (string.IsNullOrWhiteSpace(corral.Nombre))
            {
                ModelState.AddModelError("Nombre", "El nombre del corral es obligatorio.");
            }

            if (!ModelState.IsValid)
            {
                return View(corral);
            }

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"INSERT INTO Corral (Nombre, Ubicacion, CapacidadMaxima, FechaInstalacion, Estado, Observaciones)
                                  VALUES (@Nombre, @Ubicacion, @CapacidadMaxima, @FechaInstalacion, @Estado, @Observaciones);";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    AgregarParametros(cmd, corral);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Exito"] = $"Corral \"{corral.Nombre}\" creado correctamente.";
            return RedirectToAction("Index");
        }

        // GET: Corral/Edit/5
        public ActionResult Edit(int id)
        {
            Corral corral = ObtenerPorId(id);
            if (corral == null) return HttpNotFound();
            return View(corral);
        }

        // POST: Corral/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, Corral corral)
        {
            if (string.IsNullOrWhiteSpace(corral.Nombre))
            {
                ModelState.AddModelError("Nombre", "El nombre del corral es obligatorio.");
            }

            if (!ModelState.IsValid)
            {
                corral.IdCorral = id;
                return View(corral);
            }

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"UPDATE Corral SET
                                    Nombre = @Nombre,
                                    Ubicacion = @Ubicacion,
                                    CapacidadMaxima = @CapacidadMaxima,
                                    FechaInstalacion = @FechaInstalacion,
                                    Estado = @Estado,
                                    Observaciones = @Observaciones
                                  WHERE IdCorral = @IdCorral;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    AgregarParametros(cmd, corral);
                    cmd.Parameters.AddWithValue("@IdCorral", id);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Exito"] = $"Corral \"{corral.Nombre}\" actualizado correctamente.";
            return RedirectToAction("Index");
        }

        // GET: Corral/Delete/5
        public ActionResult Delete(int id)
        {
            Corral corral = ObtenerPorId(id);
            if (corral == null) return HttpNotFound();
            return View(corral);
        }

        // POST: Corral/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();

                try
                {
                    string query = "DELETE FROM Corral WHERE IdCorral = @IdCorral;";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@IdCorral", id);
                        cmd.ExecuteNonQuery();
                    }
                    TempData["Exito"] = "Corral eliminado correctamente.";
                }
                catch (SqlException ex)
                {
                    // Lo más probable: violación de FK porque el corral tiene
                    // InventarioPez / MuestreoAgua / CrecimientoAnual / RegistroAlimentacion asociados.
                    TempData["Error"] = "No se pudo eliminar el corral: tiene registros relacionados " +
                                         "(inventario, muestreos, alimentación o crecimiento). " +
                                         "Elimina o reasigna esos registros primero. Detalle: " + ex.Message;
                }
            }

            return RedirectToAction("Index");
        }

        // ---------------------------------------------------------------
        // Helpers privados
        // ---------------------------------------------------------------

        private Corral ObtenerPorId(int id)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"SELECT IdCorral, Nombre, Ubicacion, CapacidadMaxima, FechaInstalacion, Estado, Observaciones
                                  FROM Corral WHERE IdCorral = @IdCorral;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@IdCorral", id);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            return MapearCorral(dr);
                        }
                    }
                }
            }
            return null;
        }

        private static Corral MapearCorral(SqlDataReader dr)
        {
            return new Corral
            {
                IdCorral = dr.GetInt32(dr.GetOrdinal("IdCorral")),
                Nombre = dr["Nombre"] as string,
                Ubicacion = dr["Ubicacion"] as string,
                CapacidadMaxima = dr["CapacidadMaxima"] != DBNull.Value ? (decimal?)dr["CapacidadMaxima"] : null,
                FechaInstalacion = dr["FechaInstalacion"] != DBNull.Value ? (DateTime?)dr["FechaInstalacion"] : null,
                Estado = dr["Estado"] as string,
                Observaciones = dr["Observaciones"] as string
            };
        }

        private static void AgregarParametros(SqlCommand cmd, Corral corral)
        {
            cmd.Parameters.AddWithValue("@Nombre", corral.Nombre ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Ubicacion", (object)corral.Ubicacion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CapacidadMaxima", (object)corral.CapacidadMaxima ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FechaInstalacion", (object)corral.FechaInstalacion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Estado", (object)corral.Estado ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Observaciones", (object)corral.Observaciones ?? DBNull.Value);
        }
    }
}
