using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Mvc;
using Servax_bleu_unificacion.Models;

namespace Servax_bleu_unificacion.Controllers
{
    public class AlimentoController : Controller
    {
        private Conexion cn = new Conexion();

        // GET: Alimento
        public ActionResult Index()
        {
            var lista = new List<Alimento>();
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = "SELECT IdAlimento, Nombre, TipoAlimento, UnidadMedida, StockActual, CostoUnitario, Observaciones FROM Alimento ORDER BY Nombre;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read()) lista.Add(Mapear(dr));
                }
            }
            return View(lista);
        }

        // GET: Alimento/Create
        public ActionResult Create()
        {
            return View(new Alimento());
        }

        // POST: Alimento/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Alimento alimento)
        {
            if (string.IsNullOrWhiteSpace(alimento.Nombre)) ModelState.AddModelError("Nombre", "El nombre del alimento es obligatorio.");

            if (!ModelState.IsValid) return View(alimento);

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"INSERT INTO Alimento (Nombre, TipoAlimento, UnidadMedida, StockActual, CostoUnitario, Observaciones)
                                  VALUES (@Nombre, @TipoAlimento, @UnidadMedida, @StockActual, @CostoUnitario, @Observaciones);";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    AgregarParametros(cmd, alimento);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Exito"] = $"Alimento \"{alimento.Nombre}\" registrado correctamente.";
            return RedirectToAction("Index");
        }

        // GET: Alimento/Edit/5
        public ActionResult Edit(int id)
        {
            Alimento alimento = ObtenerPorId(id);
            if (alimento == null) return HttpNotFound();
            return View(alimento);
        }

        // POST: Alimento/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, Alimento alimento)
        {
            if (string.IsNullOrWhiteSpace(alimento.Nombre)) ModelState.AddModelError("Nombre", "El nombre del alimento es obligatorio.");

            if (!ModelState.IsValid)
            {
                alimento.IdAlimento = id;
                return View(alimento);
            }

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"UPDATE Alimento SET Nombre = @Nombre, TipoAlimento = @TipoAlimento, UnidadMedida = @UnidadMedida,
                                    StockActual = @StockActual, CostoUnitario = @CostoUnitario, Observaciones = @Observaciones
                                  WHERE IdAlimento = @IdAlimento;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    AgregarParametros(cmd, alimento);
                    cmd.Parameters.AddWithValue("@IdAlimento", id);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Exito"] = $"Alimento \"{alimento.Nombre}\" actualizado correctamente.";
            return RedirectToAction("Index");
        }

        // GET: Alimento/Delete/5
        public ActionResult Delete(int id)
        {
            Alimento alimento = ObtenerPorId(id);
            if (alimento == null) return HttpNotFound();
            return View(alimento);
        }

        // POST: Alimento/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                try
                {
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM Alimento WHERE IdAlimento = @IdAlimento;", con))
                    {
                        cmd.Parameters.AddWithValue("@IdAlimento", id);
                        cmd.ExecuteNonQuery();
                    }
                    TempData["Exito"] = "Alimento eliminado correctamente.";
                }
                catch (SqlException ex)
                {
                    TempData["Error"] = "No se pudo eliminar: el alimento tiene registros de alimentación o distribución asociados. " +
                                         "Detalle: " + ex.Message;
                }
            }
            return RedirectToAction("Index");
        }

        // -----------------------------------------------------------------
        private Alimento ObtenerPorId(int id)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = "SELECT IdAlimento, Nombre, TipoAlimento, UnidadMedida, StockActual, CostoUnitario, Observaciones FROM Alimento WHERE IdAlimento = @IdAlimento;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@IdAlimento", id);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read()) return Mapear(dr);
                    }
                }
            }
            return null;
        }

        private static Alimento Mapear(SqlDataReader dr)
        {
            return new Alimento
            {
                IdAlimento = dr.GetInt32(dr.GetOrdinal("IdAlimento")),
                Nombre = dr["Nombre"] as string,
                TipoAlimento = dr["TipoAlimento"] as string,
                UnidadMedida = dr["UnidadMedida"] as string,
                StockActual = dr["StockActual"] != DBNull.Value ? dr.GetDecimal(dr.GetOrdinal("StockActual")) : 0,
                CostoUnitario = dr["CostoUnitario"] != DBNull.Value ? dr.GetDecimal(dr.GetOrdinal("CostoUnitario")) : 0,
                Observaciones = dr["Observaciones"] as string
            };
        }

        private static void AgregarParametros(SqlCommand cmd, Alimento alimento)
        {
            cmd.Parameters.AddWithValue("@Nombre", alimento.Nombre ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TipoAlimento", (object)alimento.TipoAlimento ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UnidadMedida", (object)alimento.UnidadMedida ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StockActual", alimento.StockActual);
            cmd.Parameters.AddWithValue("@CostoUnitario", alimento.CostoUnitario);
            cmd.Parameters.AddWithValue("@Observaciones", (object)alimento.Observaciones ?? DBNull.Value);
        }
    }
}
