using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using ExcelDataReader;
using Servax_bleu_unificacion.Servicios;

namespace Servax_bleu_unificacion.Controllers
{
    public class HomeController : Controller
    {
        private Conexion cn = new Conexion();
        private readonly TextToSqlService textToSql = new TextToSqlService();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }

        public async Task<ActionResult> ProbarCorreo()
        {
            string asunto = "⚠️ Valores fuera del rango - Servax Bleu";
            string mensajeHtml = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 2px solid #007bff; border-radius: 8px;'>
                    <h2 style='color: #007bff;'>Notificación Exitosa</h2>
                    <p>Este es un correo automático generado para avisar cuando se detecta un valor fuera del rango.</p>
                    <hr />
                    <p><strong>Estado:</strong> Conexión SMTP funcional</p>
                    <p><strong>Fecha de envío:</strong> {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>
                </div>";

            await ServicioNotificaciones.EnviarCorreoAlertaAsync(asunto, mensajeHtml);

            return Content("<h3>¡Proceso terminado!</h3><p>Revisa la bandeja de entrada del correo configurado en Web.config.</p>");
        }

        // Muestra la vista consultando la Fecha
        public ActionResult ObtenerCalidad(DateTime? fechaInicio, DateTime? fechaFin)
        {
            DataTable dtCalidad = new DataTable();

            using (SqlConnection con = cn.ObtenerConexion())
            {
                string query = @"SELECT id, Fecha, Temperatura, Oxigeno, Profundidad 
                         FROM Calidad 
                         WHERE (@fechaInicio IS NULL OR Fecha >= @fechaInicio) 
                           AND (@fechaFin IS NULL OR Fecha <= @fechaFin)
                         ORDER BY Fecha ASC, id ASC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@fechaInicio", SqlDbType.Date).Value = (object)fechaInicio ?? DBNull.Value;
                    cmd.Parameters.Add("@fechaFin", SqlDbType.Date).Value = (object)fechaFin ?? DBNull.Value;

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dtCalidad);
                }
            }

            ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");

            return View(dtCalidad);
        }

        [HttpPost]
        public async Task<JsonResult> ConsultarAsistente(string mensaje)
        {
            if (string.IsNullOrWhiteSpace(mensaje))
            {
                return Json(new { success = false, respuesta = "Por favor escribe una consulta válida." });
            }

            try
            {
                // 1. Obtener la consulta SQL generada por el modelo local (Ollama + Qwen2.5)
                //    La validación de solo-lectura ya ocurre dentro de TextToSqlService.
                string sqlQuery = await textToSql.GenerarConsultaSqlAsync(mensaje);

                // 2. Ejecutar la consulta en SQL Server con mapeo seguro de columnas
                var listaDatos = new List<object>();
                using (SqlConnection con = cn.ObtenerConexion())
                {
                    using (SqlCommand cmd = new SqlCommand(sqlQuery, con))
                    {
                        con.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                // Mapeo dinámico para evitar caídas si el modelo omite alguna columna
                                listaDatos.Add(new
                                {
                                    id = TieneColumna(dr, "id") ? dr["id"] : 0,
                                    Fecha = TieneColumna(dr, "Fecha") && dr["Fecha"] != DBNull.Value ? Convert.ToDateTime(dr["Fecha"]).ToString("dd/MM/yyyy") : "-",
                                    Temperatura = TieneColumna(dr, "Temperatura") && dr["Temperatura"] != DBNull.Value ? dr["Temperatura"] : 0,
                                    Oxigeno = TieneColumna(dr, "Oxigeno") && dr["Oxigeno"] != DBNull.Value ? dr["Oxigeno"] : 0,
                                    Profundidad = TieneColumna(dr, "Profundidad") && dr["Profundidad"] != DBNull.Value ? dr["Profundidad"] : 0
                                });
                            }
                        }
                    }
                }

                return Json(new
                {
                    success = true,
                    respuesta = $"Consulta interpretada: `{sqlQuery}`",
                    datos = listaDatos
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, respuesta = ex.Message });
            }
        }

        // Función auxiliar para leer SQL sin errores
        private bool TieneColumna(SqlDataReader reader, string nombreColumna)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(nombreColumna, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        [HttpPost]
        public ActionResult CargarExcel(HttpPostedFileBase archivoExcel)
        {
            if (archivoExcel == null || archivoExcel.ContentLength == 0)
            {
                TempData["Error"] = "Por favor selecciona o arrastra un archivo de Excel válido (.xlsx o .xls).";
                return RedirectToAction("ObtenerCalidad");
            }

            try
            {
                using (Stream stream = archivoExcel.InputStream)
                {
                    using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var ds = reader.AsDataSet(new ExcelDataSetConfiguration()
                        {
                            ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                            {
                                UseHeaderRow = true
                            }
                        });

                        DataTable dt = ds.Tables[0];

                        int colTemp = -1;
                        int colOxi = -1;
                        int colProf = -1;
                        int colFecha = -1;

                        for (int i = 0; i < dt.Columns.Count; i++)
                        {
                            string colName = NormalizarTexto(dt.Columns[i].ColumnName);

                            if (colName.Contains("temp"))
                                colTemp = i;
                            else if (colName.Contains("oxig") || colName.Contains("oxi") || colName.Contains("o2"))
                                colOxi = i;
                            else if (colName.Contains("prof"))
                                colProf = i;
                            else if (colName.Contains("fech") || colName.Contains("date") || colName.Contains("dia"))
                                colFecha = i;
                        }

                        if (colTemp == -1 || colOxi == -1 || colProf == -1)
                        {
                            TempData["Error"] = "El Excel debe incluir las columnas Temperatura, Oxígeno y Profundidad (en cualquier orden).";
                            return RedirectToAction("ObtenerCalidad");
                        }

                        int insertados = 0;
                        using (SqlConnection con = cn.ObtenerConexion())
                        {
                            con.Open();
                            foreach (DataRow row in dt.Rows)
                            {
                                string strTemp = row[colTemp]?.ToString().Replace(',', '.');
                                string strOxi = row[colOxi]?.ToString().Replace(',', '.');
                                string strProf = row[colProf]?.ToString().Replace(',', '.');

                                DateTime fechaRegistro = DateTime.Today;

                                if (colFecha != -1 && row[colFecha] != DBNull.Value && row[colFecha] != null)
                                {
                                    object valFecha = row[colFecha];
                                    if (valFecha is DateTime dtNativa)
                                    {
                                        fechaRegistro = dtNativa.Date;
                                    }
                                    else if (DateTime.TryParse(valFecha.ToString(), out DateTime dtParsed))
                                    {
                                        fechaRegistro = dtParsed.Date;
                                    }
                                }

                                if (double.TryParse(strTemp, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double temp) &&
                                    double.TryParse(strOxi, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double oxi) &&
                                    double.TryParse(strProf, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double prof))
                                {
                                    string query = "INSERT INTO Calidad (Fecha, Temperatura, Oxigeno, Profundidad) VALUES (@fecha, @temp, @oxi, @prof)";
                                    using (SqlCommand cmd = new SqlCommand(query, con))
                                    {
                                        cmd.Parameters.Add("@fecha", SqlDbType.Date).Value = fechaRegistro;
                                        cmd.Parameters.AddWithValue("@temp", temp);
                                        cmd.Parameters.AddWithValue("@oxi", oxi);
                                        cmd.Parameters.AddWithValue("@prof", prof);
                                        cmd.ExecuteNonQuery();
                                        insertados++;
                                    }
                                }
                            }
                        }

                        TempData["Exito"] = $"¡Se registraron exitosamente {insertados} lecturas procesando la fecha del Excel!";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al procesar el archivo Excel: " + ex.Message;
            }

            return RedirectToAction("ObtenerCalidad");
        }

        private string NormalizarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";
            texto = texto.ToLower().Trim();
            texto = texto.Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u");
            return texto;
        }
    }
}