using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace Servax_bleu_unificacion.Controllers
{
    public class ReporteMortalidadItem
    {
        public string NombreCorral { get; set; }
        public DateTime Mes { get; set; }
        public decimal TotalAlimentoKg { get; set; }
        public int TotalMortalidad { get; set; }
    }

    /// <summary>
    /// Task flow Fase 2: "Generar reporte de mortalidad".
    /// También cubre el RF del cliente: "reportes exportables (alimentación vs. mortalidad)".
    /// </summary>
    public class ReporteController : Controller
    {
        private Conexion cn = new Conexion();

        // GET: Reporte/Mortalidad
        public ActionResult Mortalidad(int? idCorral, DateTime? desde, DateTime? hasta)
        {
            var datos = ObtenerDatosMortalidad(idCorral, desde, hasta);

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                ViewBag.Corrales = ObtenerCorrales(con);
            }

            ViewBag.IdCorralFiltro = idCorral;
            ViewBag.Desde = desde;
            ViewBag.Hasta = hasta;
            return View(datos);
        }

        // GET: Reporte/MortalidadCsv — exportación (RF: reportes exportables)
        public ActionResult MortalidadCsv(int? idCorral, DateTime? desde, DateTime? hasta)
        {
            var datos = ObtenerDatosMortalidad(idCorral, desde, hasta);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Corral,Mes,TotalAlimentoKg,TotalMortalidad");
            foreach (var item in datos)
            {
                sb.AppendLine($"{item.NombreCorral},{item.Mes:yyyy-MM},{item.TotalAlimentoKg},{item.TotalMortalidad}");
            }

            return File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "reporte_mortalidad.csv");
        }

        private List<ReporteMortalidadItem> ObtenerDatosMortalidad(int? idCorral, DateTime? desde, DateTime? hasta)
        {
            var datos = new List<ReporteMortalidadItem>();

            using (SqlConnection con = cn.ObtenerConexion())
            {
                con.Open();
                string query = @"
                    SELECT c.Nombre AS NombreCorral,
                           DATEFROMPARTS(YEAR(ra.Fecha), MONTH(ra.Fecha), 1) AS Mes,
                           SUM(ra.CantidadKg) AS TotalAlimentoKg,
                           SUM(ra.Mortalidad) AS TotalMortalidad
                    FROM RegistroAlimentacion ra
                    INNER JOIN Corral c ON ra.IdCorral = c.IdCorral
                    WHERE (@idCorral IS NULL OR ra.IdCorral = @idCorral)
                      AND (@desde IS NULL OR ra.Fecha >= @desde)
                      AND (@hasta IS NULL OR ra.Fecha <= @hasta)
                    GROUP BY c.Nombre, YEAR(ra.Fecha), MONTH(ra.Fecha)
                    ORDER BY c.Nombre, Mes;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@idCorral", (object)idCorral ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@desde", (object)desde ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@hasta", (object)hasta ?? DBNull.Value);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            datos.Add(new ReporteMortalidadItem
                            {
                                NombreCorral = dr["NombreCorral"] as string,
                                Mes = dr.GetDateTime(dr.GetOrdinal("Mes")),
                                TotalAlimentoKg = dr["TotalAlimentoKg"] != DBNull.Value ? dr.GetDecimal(dr.GetOrdinal("TotalAlimentoKg")) : 0,
                                TotalMortalidad = dr["TotalMortalidad"] != DBNull.Value ? dr.GetInt32(dr.GetOrdinal("TotalMortalidad")) : 0
                            });
                        }
                    }
                }
            }

            return datos;
        }

        private List<SelectListItem> ObtenerCorrales(SqlConnection con)
        {
            var lista = new List<SelectListItem>();
            using (SqlCommand cmd = new SqlCommand("SELECT IdCorral, Nombre FROM Corral ORDER BY Nombre;", con))
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read()) lista.Add(new SelectListItem { Value = dr["IdCorral"].ToString(), Text = dr["Nombre"].ToString() });
            }
            return lista;
        }
    }
}
