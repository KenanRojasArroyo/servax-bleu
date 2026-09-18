using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using Servax_bleu_unificacion.Models;

namespace Servax_bleu_unificacion.Controllers
{
    public class BarcoResumen
    {
        public int IdBarco { get; set; }
        public string Nombre { get; set; }
        public decimal Capacidad { get; set; }
    }

    /// <summary>
    /// Task flow Fase 2: "Distribuir carnada entre barcos".
    ///
    /// REGLA DE NEGOCIO: el cliente todavía no ha confirmado la fórmula exacta
    /// de distribución de carnada por tonelaje. Mientras tanto, este controller
    /// usa un REPARTO PROPORCIONAL A LA CAPACIDAD DE CADA BARCO (Barco.CapacidadToneladas)
    /// como placeholder razonable y explicable — no es la fórmula final del negocio.
    /// Cuando el cliente la confirme, solo hay que reemplazar CalcularDistribucion().
    /// </summary>
    public class DistribucionAlimentoController : Controller
    {
        private Conexion cn = new Conexion();

        // GET: DistribucionAlimento
        public ActionResult Index()
        {
            var lista = new List<DistribucionAlimento>();

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"SELECT d.IdDistribucion, d.IdBarco, d.IdAlimento, d.IdCorral, d.Fecha, d.CantidadToneladas, d.FormulaAplicada, d.Observaciones,
                                         b.Nombre AS NombreBarco, a.Nombre AS NombreAlimento, c.Nombre AS NombreCorral
                                  FROM DistribucionAlimento d
                                  INNER JOIN Barco b ON d.IdBarco = b.IdBarco
                                  INNER JOIN Alimento a ON d.IdAlimento = a.IdAlimento
                                  INNER JOIN Corral c ON d.IdCorral = c.IdCorral
                                  ORDER BY d.Fecha DESC;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new DistribucionAlimento
                        {
                            IdDistribucion = dr.GetInt32(dr.GetOrdinal("IdDistribucion")),
                            IdBarco = dr.GetInt32(dr.GetOrdinal("IdBarco")),
                            IdAlimento = dr.GetInt32(dr.GetOrdinal("IdAlimento")),
                            IdCorral = dr.GetInt32(dr.GetOrdinal("IdCorral")),
                            Fecha = dr.GetDateTime(dr.GetOrdinal("Fecha")),
                            CantidadToneladas = dr.GetDecimal(dr.GetOrdinal("CantidadToneladas")),
                            FormulaAplicada = dr["FormulaAplicada"] as string,
                            Observaciones = dr["Observaciones"] as string,
                            NombreBarco = dr["NombreBarco"] as string,
                            NombreAlimento = dr["NombreAlimento"] as string,
                            NombreCorral = dr["NombreCorral"] as string
                        });
                    }
                }
            }

            return View(lista);
        }

        // GET: DistribucionAlimento/Distribuir
        public ActionResult Distribuir()
        {
            CargarListasDesplegables();
            return View();
        }

        // POST: DistribucionAlimento/Distribuir
        // Recibe un total de toneladas a repartir para un corral/alimento, y lo
        // divide entre los barcos activos proporcionalmente a su capacidad.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Distribuir(int idCorral, int idAlimento, decimal cantidadTotalToneladas, DateTime fecha)
        {
            if (cantidadTotalToneladas <= 0)
            {
                TempData["Error"] = "La cantidad total a distribuir debe ser mayor a cero.";
                CargarListasDesplegables();
                return View();
            }

            var barcosActivos = ObtenerBarcosActivos();
            if (barcosActivos.Count == 0)
            {
                TempData["Error"] = "No hay barcos activos registrados. Da de alta al menos uno en el módulo de Barcos.";
                CargarListasDesplegables();
                return View();
            }

            var reparto = CalcularDistribucion(cantidadTotalToneladas, barcosActivos);

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                foreach (var asignacion in reparto)
                {
                    string query = @"INSERT INTO DistribucionAlimento (IdBarco, IdAlimento, IdCorral, Fecha, CantidadToneladas, FormulaAplicada, Observaciones)
                                      VALUES (@IdBarco, @IdAlimento, @IdCorral, @Fecha, @CantidadToneladas, @FormulaAplicada, @Observaciones);";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@IdBarco", asignacion.IdBarco);
                        cmd.Parameters.AddWithValue("@IdAlimento", idAlimento);
                        cmd.Parameters.AddWithValue("@IdCorral", idCorral);
                        cmd.Parameters.AddWithValue("@Fecha", fecha);
                        cmd.Parameters.AddWithValue("@CantidadToneladas", asignacion.Toneladas);
                        cmd.Parameters.AddWithValue("@FormulaAplicada", "Proporcional a CapacidadToneladas (placeholder, pendiente confirmar con cliente)");
                        cmd.Parameters.AddWithValue("@Observaciones", (object)DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            TempData["Exito"] = $"Distribución generada: {cantidadTotalToneladas} ton repartidas entre {reparto.Count} barco(s).";
            return RedirectToAction("Index");
        }

        // GET: DistribucionAlimento/Delete/5
        public ActionResult Delete(int id)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"SELECT d.IdDistribucion, d.CantidadToneladas, d.Fecha, b.Nombre AS NombreBarco
                                  FROM DistribucionAlimento d INNER JOIN Barco b ON d.IdBarco = b.IdBarco
                                  WHERE d.IdDistribucion = @Id;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            return View(new DistribucionAlimento
                            {
                                IdDistribucion = dr.GetInt32(0),
                                CantidadToneladas = dr.GetDecimal(1),
                                Fecha = dr.GetDateTime(2),
                                NombreBarco = dr["NombreBarco"] as string
                            });
                        }
                    }
                }
            }
            return HttpNotFound();
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("DELETE FROM DistribucionAlimento WHERE IdDistribucion = @Id;", con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            TempData["Exito"] = "Registro de distribución eliminado.";
            return RedirectToAction("Index");
        }

        // -----------------------------------------------------------------
        // LA FÓRMULA (placeholder) — reemplazar aquí cuando el cliente
        // confirme la regla real de negocio.
        // -----------------------------------------------------------------
        private List<(int IdBarco, decimal Toneladas)> CalcularDistribucion(decimal cantidadTotal, List<BarcoResumen> barcos)
        {
            decimal capacidadTotal = barcos.Sum(b => b.Capacidad);
            var reparto = new List<(int, decimal)>();
            decimal acumulado = 0m;

            for (int i = 0; i < barcos.Count; i++)
            {
                decimal toneladas;
                if (i == barcos.Count - 1)
                {
                    // el último barco absorbe el residuo de redondeo, para que la suma cuadre exacto
                    toneladas = Math.Round(cantidadTotal - acumulado, 2);
                }
                else
                {
                    toneladas = Math.Round(cantidadTotal * (barcos[i].Capacidad / capacidadTotal), 2);
                    acumulado += toneladas;
                }
                reparto.Add((barcos[i].IdBarco, toneladas));
            }

            return reparto;
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private List<BarcoResumen> ObtenerBarcosActivos()
        {
            var lista = new List<BarcoResumen>();
            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = "SELECT IdBarco, Nombre, CapacidadToneladas FROM Barco WHERE Estado = 'Activo' ORDER BY CapacidadToneladas DESC;";
                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new BarcoResumen { IdBarco = dr.GetInt32(0), Nombre = dr.GetString(1), Capacidad = dr.GetDecimal(2) });
                    }
                }
            }
            return lista;
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
            ViewBag.BarcosActivos = ObtenerBarcosActivos();
        }
    }
}
