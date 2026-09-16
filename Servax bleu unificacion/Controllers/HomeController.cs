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
        private Conexion cn = new Conexion(); //[cite: 3]
        private readonly TextToSqlService textToSql = new TextToSqlService(); //[cite: 3]

        public ActionResult Index()
        {
            DashboardIndexViewModel model = new DashboardIndexViewModel();

            using (SqlConnection con = cn.ObtenerConexion()) //[cite: 3]
            {
                con.Open();

                // 1. MuestreoAgua (tabla normalizada por corral; no existe FK real hacia Calidad, así que no se hace join contra ella)
                string queryCalidad = @"
                    SELECT m.Fecha, AVG(m.Temperatura) AS Temperatura, AVG(m.Oxigeno) AS Oxigeno
                    FROM MuestreoAgua m
                    GROUP BY m.Fecha
                    ORDER BY m.Fecha ASC;";

                using (SqlDataAdapter da = new SqlDataAdapter(queryCalidad, con))
                {
                    da.Fill(model.CalidadMuestreo);
                }

                // 2. InventarioPez con Especie y Corral
                string queryInventario = @"
                    SELECT co.Nombre AS Corral, e.Nombre AS Especie, SUM(ip.Cantidad) AS CantidadPeces
                    FROM InventarioPez ip
                    INNER JOIN Especie e ON ip.IdEspecie = e.IdEspecie
                    INNER JOIN Corral co ON ip.IdCorral = co.IdCorral
                    GROUP BY co.Nombre, e.Nombre
                    ORDER BY co.Nombre;";
                using (SqlDataAdapter da = new SqlDataAdapter(queryInventario, con))
                {
                    da.Fill(model.InventarioCorralEspecie);
                }

                // 3. CrecimientoAnual con Especie (se usa PesoPromedioFinal; la tabla no tiene un único "PesoPromedioKg")
                string queryCrecimiento = @"
                    SELECT ca.Anio, e.Nombre AS Especie, AVG(ca.PesoPromedioFinal) AS PesoPromedio
                    FROM CrecimientoAnual ca
                    INNER JOIN Especie e ON ca.IdEspecie = e.IdEspecie
                    GROUP BY ca.Anio, e.Nombre
                    ORDER BY ca.Anio ASC;";
                using (SqlDataAdapter da = new SqlDataAdapter(queryCrecimiento, con))
                {
                    da.Fill(model.CrecimientoEspecie);
                }

                // 4. RegistroAlimentacion con InventarioPez con Especie
                string queryAlimentacion = @"
                    SELECT CAST(ra.Fecha AS DATE) AS Fecha, e.Nombre AS Especie, SUM(ra.CantidadKg) AS TotalAlimento, SUM(ip.Cantidad) AS TotalPeces
                    FROM RegistroAlimentacion ra
                    INNER JOIN InventarioPez ip ON ra.IdCorral = ip.IdCorral
                    INNER JOIN Especie e ON ip.IdEspecie = e.IdEspecie
                    GROUP BY CAST(ra.Fecha AS DATE), e.Nombre
                    ORDER BY Fecha ASC;";
                using (SqlDataAdapter da = new SqlDataAdapter(queryAlimentacion, con))
                {
                    da.Fill(model.AlimentacionInventario);
                }
            }

            return View(model);
        }

        public ActionResult About() //[cite: 3]
        {
            ViewBag.Message = "Your application description page."; //[cite: 3]
            return View(); //[cite: 3]
        }

        public ActionResult Contact() //[cite: 3]
        {
            ViewBag.Message = "Your contact page."; //[cite: 3]
            return View(); //[cite: 3]
        }

        public async Task<ActionResult> ProbarCorreo() //[cite: 3]
        {
            string asunto = "⚠️ Valores fuera del rango - Servax Bleu"; //[cite: 3]
            string mensajeHtml = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 2px solid #007bff; border-radius: 8px;'>
                    <h2 style='color: #007bff;'>Notificación Exitosa</h2>
                    <p>Este es un correo automático generado para avisar cuando se detecta un valor fuera del rango.</p>
                    <hr />
                    <p><strong>Estado:</strong> Conexión SMTP funcional</p>
                    <p><strong>Fecha de envío:</strong> {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>
                </div>"; //[cite: 3]

            await ServicioNotificaciones.EnviarCorreoAlertaAsync(asunto, mensajeHtml); //[cite: 3]

            return Content("<h3>¡Proceso terminado!</h3><p>Revisa la bandeja de entrada del correo configurado en Web.config.</p>"); //[cite: 3]
        }

        public ActionResult Monitoreo(DateTime? fechaInicio, DateTime? fechaFin) //[cite: 3]
        {
            DataTable dtCalidad = new DataTable(); //[cite: 3]

            using (SqlConnection con = cn.ObtenerConexion()) //[cite: 3]
            {
                string query = @"SELECT id, Fecha, Temperatura, Oxigeno, Profundidad 
                         FROM Calidad 
                         WHERE (@fechaInicio IS NULL OR Fecha >= @fechaInicio) 
                           AND (@fechaFin IS NULL OR Fecha <= @fechaFin)
                         ORDER BY Fecha ASC, id ASC"; //[cite: 3]

                using (SqlCommand cmd = new SqlCommand(query, con)) //[cite: 3]
                {
                    cmd.Parameters.Add("@fechaInicio", SqlDbType.Date).Value = (object)fechaInicio ?? DBNull.Value; //[cite: 3]
                    cmd.Parameters.Add("@fechaFin", SqlDbType.Date).Value = (object)fechaFin ?? DBNull.Value; //[cite: 3]

                    SqlDataAdapter da = new SqlDataAdapter(cmd); //[cite: 3]
                    da.Fill(dtCalidad); //[cite: 3]
                }
            }

            ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd"); //[cite: 3]
            ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd"); //[cite: 3]

            return View(dtCalidad); //[cite: 3]
        }

        [HttpPost]
        public async Task<JsonResult> ConsultarAsistente(string mensaje) //[cite: 3]
        {
            if (string.IsNullOrWhiteSpace(mensaje)) //[cite: 3]
            {
                return Json(new { success = false, respuesta = "Por favor escribe una consulta válida." }); //[cite: 3]
            }

            try
            {
                string sqlQuery = await textToSql.GenerarConsultaSqlAsync(mensaje); //[cite: 3]

                var listaDatos = new List<object>(); //[cite: 3]
                using (SqlConnection con = cn.ObtenerConexion()) //[cite: 3]
                {
                    using (SqlCommand cmd = new SqlCommand(sqlQuery, con)) //[cite: 3]
                    {
                        con.Open(); //[cite: 3]
                        using (SqlDataReader dr = cmd.ExecuteReader()) //[cite: 3]
                        {
                            while (dr.Read()) //[cite: 3]
                            {
                                listaDatos.Add(new
                                {
                                    id = TieneColumna(dr, "id") ? dr["id"] : 0, //[cite: 3]
                                    Fecha = TieneColumna(dr, "Fecha") && dr["Fecha"] != DBNull.Value ? Convert.ToDateTime(dr["Fecha"]).ToString("dd/MM/yyyy") : "-", //[cite: 3]
                                    Temperatura = TieneColumna(dr, "Temperatura") && dr["Temperatura"] != DBNull.Value ? dr["Temperatura"] : 0, //[cite: 3]
                                    Oxigeno = TieneColumna(dr, "Oxigeno") && dr["Oxigeno"] != DBNull.Value ? dr["Oxigeno"] : 0, //[cite: 3]
                                    Profundidad = TieneColumna(dr, "Profundidad") && dr["Profundidad"] != DBNull.Value ? dr["Profundidad"] : 0 //[cite: 3]
                                });
                            }
                        }
                    }
                }

                return Json(new //[cite: 3]
                {
                    success = true,
                    respuesta = $"Consulta interpretada: `{sqlQuery}`", //[cite: 3]
                    datos = listaDatos //[cite: 3]
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, respuesta = ex.Message }); //[cite: 3]
            }
        }

        private bool TieneColumna(SqlDataReader reader, string nombreColumna) //[cite: 3]
        {
            for (int i = 0; i < reader.FieldCount; i++) //[cite: 3]
            {
                if (reader.GetName(i).Equals(nombreColumna, StringComparison.InvariantCultureIgnoreCase)) //[cite: 3]
                    return true; //[cite: 3]
            }
            return false; //[cite: 3]
        }

        [HttpPost]
        public ActionResult CargarExcel(HttpPostedFileBase archivoExcel) //[cite: 3]
        {
            if (archivoExcel == null || archivoExcel.ContentLength == 0) //[cite: 3]
            {
                TempData["Error"] = "Por favor selecciona o arrastra un archivo de Excel válido (.xlsx o .xls)."; //[cite: 3]
                return RedirectToAction("Monitoreo"); //[cite: 3]
            }

            try
            {
                using (Stream stream = archivoExcel.InputStream) //[cite: 3]
                {
                    using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream)) //[cite: 3]
                    {
                        var ds = reader.AsDataSet(new ExcelDataSetConfiguration() //[cite: 3]
                        {
                            ConfigureDataTable = (_) => new ExcelDataTableConfiguration() //[cite: 3]
                            {
                                UseHeaderRow = true //[cite: 3]
                            }
                        });

                        DataTable dt = ds.Tables[0]; //[cite: 3]

                        int colTemp = -1; //[cite: 3]
                        int colOxi = -1; //[cite: 3]
                        int colProf = -1; //[cite: 3]
                        int colFecha = -1; //[cite: 3]

                        for (int i = 0; i < dt.Columns.Count; i++) //[cite: 3]
                        {
                            string colName = NormalizarTexto(dt.Columns[i].ColumnName); //[cite: 3]

                            if (colName.Contains("temp")) //[cite: 3]
                                colTemp = i; //[cite: 3]
                            else if (colName.Contains("oxig") || colName.Contains("oxi") || colName.Contains("o2")) //[cite: 3]
                                colOxi = i; //[cite: 3]
                            else if (colName.Contains("prof")) //[cite: 3]
                                colProf = i; //[cite: 3]
                            else if (colName.Contains("fech") || colName.Contains("date") || colName.Contains("dia")) //[cite: 3]
                                colFecha = i; //[cite: 3]
                        }

                        if (colTemp == -1 || colOxi == -1 || colProf == -1) //[cite: 3]
                        {
                            TempData["Error"] = "El Excel debe incluir las columnas Temperatura, Oxígeno y Profundidad (en cualquier orden)."; //[cite: 3]
                            return RedirectToAction("Monitoreo"); //[cite: 3]
                        }

                        int insertados = 0; //[cite: 3]
                        using (SqlConnection con = cn.ObtenerConexion()) //[cite: 3]
                        {
                            con.Open(); //[cite: 3]
                            foreach (DataRow row in dt.Rows) //[cite: 3]
                            {
                                string strTemp = row[colTemp]?.ToString().Replace(',', '.'); //[cite: 3]
                                string strOxi = row[colOxi]?.ToString().Replace(',', '.'); //[cite: 3]
                                string strProf = row[colProf]?.ToString().Replace(',', '.'); //[cite: 3]

                                DateTime fechaRegistro = DateTime.Today; //[cite: 3]

                                if (colFecha != -1 && row[colFecha] != DBNull.Value && row[colFecha] != null) //[cite: 3]
                                {
                                    object valFecha = row[colFecha]; //[cite: 3]
                                    if (valFecha is DateTime dtNativa) //[cite: 3]
                                    {
                                        fechaRegistro = dtNativa.Date; //[cite: 3]
                                    }
                                    else if (DateTime.TryParse(valFecha.ToString(), out DateTime dtParsed)) //[cite: 3]
                                    {
                                        fechaRegistro = dtParsed.Date; //[cite: 3]
                                    }
                                }

                                if (double.TryParse(strTemp, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double temp) && //[cite: 3]
                                    double.TryParse(strOxi, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double oxi) && //[cite: 3]
                                    double.TryParse(strProf, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double prof)) //[cite: 3]
                                {
                                    string query = "INSERT INTO Calidad (Fecha, Temperatura, Oxigeno, Profundidad) VALUES (@fecha, @temp, @oxi, @prof)"; //[cite: 3]
                                    using (SqlCommand cmd = new SqlCommand(query, con)) //[cite: 3]
                                    {
                                        cmd.Parameters.Add("@fecha", SqlDbType.Date).Value = fechaRegistro; //[cite: 3]
                                        cmd.Parameters.AddWithValue("@temp", temp); //[cite: 3]
                                        cmd.Parameters.AddWithValue("@oxi", oxi); //[cite: 3]
                                        cmd.Parameters.AddWithValue("@prof", prof); //[cite: 3]
                                        cmd.ExecuteNonQuery(); //[cite: 3]
                                        insertados++; //[cite: 3]
                                    }
                                }
                            }
                        }

                        TempData["Exito"] = $"¡Se registraron exitosamente {insertados} lecturas procesando la fecha del Excel!"; //[cite: 3]
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al procesar el archivo Excel: " + ex.Message; //[cite: 3]
            }

            return RedirectToAction("Monitoreo"); //[cite: 3]
        }

        private string NormalizarTexto(string texto) //[cite: 3]
        {
            if (string.IsNullOrWhiteSpace(texto)) return ""; //[cite: 3]
            texto = texto.ToLower().Trim(); //[cite: 3]
            texto = texto.Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u"); //[cite: 3]
            return texto; //[cite: 3]
        }
    }
}