using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using Servax_bleu_unificacion.Models;

namespace Servax_bleu_unificacion.Controllers
{
    public class EspecieController : Controller
    {
        private Conexion cn = new Conexion();

        // GET: Especie
        public ActionResult Index()
        {
            var lista = new List<Especie>();

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"SELECT IdEspecie, Nombre, NombreCientifico, Tipo, EsToxica, Descripcion, Activo
                                  FROM Especie
                                  ORDER BY Nombre;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(MapearEspecie(dr));
                    }
                }
            }

            return View(lista);
        }

        // GET: Especie/Details/5
        public ActionResult Details(int id)
        {
            Especie especie = ObtenerPorId(id);
            if (especie == null) return HttpNotFound();
            return View(especie);
        }

        // GET: Especie/Create
        public ActionResult Create()
        {
            return View(new Especie { Activo = true });
        }

        // POST: Especie/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Especie especie)
        {
            if (string.IsNullOrWhiteSpace(especie.Nombre))
            {
                ModelState.AddModelError("Nombre", "El nombre de la especie es obligatorio.");
            }

            if (!ModelState.IsValid)
            {
                return View(especie);
            }

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"INSERT INTO Especie (Nombre, NombreCientifico, Tipo, EsToxica, Descripcion, Activo)
                                  VALUES (@Nombre, @NombreCientifico, @Tipo, @EsToxica, @Descripcion, @Activo);";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    AgregarParametros(cmd, especie);
                    cmd.ExecuteNonQuery();
                }
            }

            // Aviso relevante para el RF de "alertas por especies tóxicas": si se dio de
            // alta como tóxica, se deja constancia visible aquí mismo para el responsable.
            TempData["Exito"] = especie.EsToxica
                ? $"Especie \"{especie.Nombre}\" creada y marcada como TÓXICA."
                : $"Especie \"{especie.Nombre}\" creada correctamente.";

            return RedirectToAction("Index");
        }

        // GET: Especie/Edit/5
        public ActionResult Edit(int id)
        {
            Especie especie = ObtenerPorId(id);
            if (especie == null) return HttpNotFound();
            return View(especie);
        }

        // POST: Especie/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, Especie especie)
        {
            if (string.IsNullOrWhiteSpace(especie.Nombre))
            {
                ModelState.AddModelError("Nombre", "El nombre de la especie es obligatorio.");
            }

            if (!ModelState.IsValid)
            {
                especie.IdEspecie = id;
                return View(especie);
            }

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"UPDATE Especie SET
                                    Nombre = @Nombre,
                                    NombreCientifico = @NombreCientifico,
                                    Tipo = @Tipo,
                                    EsToxica = @EsToxica,
                                    Descripcion = @Descripcion,
                                    Activo = @Activo
                                  WHERE IdEspecie = @IdEspecie;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    AgregarParametros(cmd, especie);
                    cmd.Parameters.AddWithValue("@IdEspecie", id);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Exito"] = $"Especie \"{especie.Nombre}\" actualizada correctamente.";
            return RedirectToAction("Index");
        }

        // GET: Especie/Delete/5
        public ActionResult Delete(int id)
        {
            Especie especie = ObtenerPorId(id);
            if (especie == null) return HttpNotFound();
            return View(especie);
        }

        // POST: Especie/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();

                try
                {
                    string query = "DELETE FROM Especie WHERE IdEspecie = @IdEspecie;";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@IdEspecie", id);
                        cmd.ExecuteNonQuery();
                    }
                    TempData["Exito"] = "Especie eliminada correctamente.";
                }
                catch (SqlException ex)
                {
                    // Lo más probable: violación de FK porque la especie tiene
                    // InventarioPez o CrecimientoAnual asociados.
                    TempData["Error"] = "No se pudo eliminar la especie: tiene registros relacionados " +
                                         "(inventario o crecimiento anual). Considera marcarla como Inactiva " +
                                         "en vez de eliminarla. Detalle: " + ex.Message;
                }
            }

            return RedirectToAction("Index");
        }

        // ---------------------------------------------------------------
        // Helpers privados
        // ---------------------------------------------------------------

        private Especie ObtenerPorId(int id)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"SELECT IdEspecie, Nombre, NombreCientifico, Tipo, EsToxica, Descripcion, Activo
                                  FROM Especie WHERE IdEspecie = @IdEspecie;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@IdEspecie", id);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            return MapearEspecie(dr);
                        }
                    }
                }
            }
            return null;
        }

        private static Especie MapearEspecie(SqlDataReader dr)
        {
            return new Especie
            {
                IdEspecie = dr.GetInt32(dr.GetOrdinal("IdEspecie")),
                Nombre = dr["Nombre"] as string,
                NombreCientifico = dr["NombreCientifico"] as string,
                Tipo = dr["Tipo"] as string,
                EsToxica = dr["EsToxica"] != DBNull.Value && (bool)dr["EsToxica"],
                Descripcion = dr["Descripcion"] as string,
                Activo = dr["Activo"] != DBNull.Value && (bool)dr["Activo"]
            };
        }

        private static void AgregarParametros(SqlCommand cmd, Especie especie)
        {
            cmd.Parameters.AddWithValue("@Nombre", especie.Nombre ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@NombreCientifico", (object)especie.NombreCientifico ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Tipo", (object)especie.Tipo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EsToxica", especie.EsToxica);
            cmd.Parameters.AddWithValue("@Descripcion", (object)especie.Descripcion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Activo", especie.Activo);
        }
    }
}
