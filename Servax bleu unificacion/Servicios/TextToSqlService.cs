using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Servax_bleu_unificacion.Servicios
{
    /// <summary>
    /// Servicio Text-to-SQL usando un modelo local via Ollama (ej. qwen2.5-coder:7b).
    /// Reemplaza la dependencia anterior de la API de Gemini: corre 100% local,
    /// sin costo por consulta y sin exponer datos de la empresa a un tercero.
    /// </summary>
    public class TextToSqlService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private readonly string _ollamaUrl = ConfigurationManager.AppSettings["OllamaUrl"] ?? "http://localhost:11434/api/generate";
        private readonly string _ollamaModel = ConfigurationManager.AppSettings["OllamaModel"] ?? "qwen2.5-coder:7b";

        // Las 11 tablas originales de ServaxBleu están confirmadas contra INFORMATION_SCHEMA.COLUMNS (16/09/2026).
        // Las tablas/vistas de Calidad de Agua y Alimentación v2 (Sitio, Sensor, LecturaSensorCorral,
        // MuestreoAbiotico, fitoplancton, carga de barcos...) salen de unified_schema.sql:
        // ⚠️ pendiente de verificar contra INFORMATION_SCHEMA de la BD real.
        private const string EsquemaTablas = @"
 - Tabla Calidad: id (INT, PK), Fecha (DATE), Temperatura (FLOAT), Oxigeno (FLOAT), Profundidad (FLOAT)
 - Tabla Corral: IdCorral (INT, PK), IdSitio (INT, FK -> Sitio.IdSitio, nullable), Nombre (VARCHAR), Ubicacion (VARCHAR), CapacidadMaxima (DECIMAL), FechaInstalacion (DATE), Estado (VARCHAR), Observaciones (VARCHAR)
 - Tabla Especie: IdEspecie (INT, PK), Nombre (VARCHAR), NombreCientifico (VARCHAR), Tipo (VARCHAR), EsToxica (BIT), Descripcion (VARCHAR), Activo (BIT)
 - Tabla InventarioPez: IdInventario (INT, PK), IdCorral (INT, FK -> Corral.IdCorral), IdEspecie (INT, FK -> Especie.IdEspecie), Cantidad (INT), PesoPromedioKg (DECIMAL), Estado (VARCHAR), FechaRegistro (DATE), Observaciones (VARCHAR)
 - Tabla MuestreoAgua: IdMuestreo (INT, PK), IdCorral (INT, FK -> Corral.IdCorral), Fecha (DATE), Temperatura (FLOAT), Oxigeno (FLOAT), Profundidad (FLOAT), PH (FLOAT), Salinidad (FLOAT), Nutrientes (VARCHAR), Irregularidad (VARCHAR), Observaciones (VARCHAR)
 - Tabla CrecimientoAnual: IdCrecimiento (INT, PK), IdCorral (INT, FK -> Corral.IdCorral), IdEspecie (INT, FK -> Especie.IdEspecie), Anio (INT), PesoPromedioInicial (DECIMAL), PesoPromedioFinal (DECIMAL), TasaCrecimiento (FLOAT), Observaciones (VARCHAR)
 - Tabla RegistroAlimentacion: IdRegistro (INT, PK), IdCorral (INT, FK -> Corral.IdCorral), IdAlimento (INT, FK -> Alimento.IdAlimento), IdPresentacion (INT, FK -> PresentacionAlimento.IdPresentacion, nullable), Fecha (DATE), CantidadKg (DECIMAL), Responsable (VARCHAR), Mortalidad (INT), Observaciones (VARCHAR)
 - Tabla Alimento: IdAlimento (INT, PK), IdPresentacionDefault (INT, FK -> PresentacionAlimento.IdPresentacion, nullable), Nombre (VARCHAR), TipoAlimento (VARCHAR), UnidadMedida (VARCHAR), StockActual (DECIMAL), CostoUnitario (DECIMAL), Observaciones (VARCHAR)
 - Tabla Barco: IdBarco (INT, PK), Nombre (VARCHAR), CapacidadToneladas (DECIMAL), Estado (VARCHAR), FechaAlta (DATE), Observaciones (VARCHAR)
 - Tabla DistribucionAlimento: IdDistribucion (INT, PK), IdBarco (INT, FK -> Barco.IdBarco), IdAlimento (INT, FK -> Alimento.IdAlimento), IdCorral (INT, FK -> Corral.IdCorral), Fecha (DATETIME), CantidadToneladas (DECIMAL), FormulaAplicada (VARCHAR), Observaciones (VARCHAR)
 - Tabla HistorialCorral: IdHistorial (INT, PK), IdCorral (INT, FK -> Corral.IdCorral), Fecha (DATETIME), CantidadPeces (INT), EstadoGeneral (VARCHAR), ResumenCalidadAgua (VARCHAR), ResumenNutrientes (VARCHAR), Observaciones (VARCHAR)
 - Tabla Sitio: IdSitio (INT, PK), Nombre (VARCHAR)
 - Tabla Nutriente: IdNutriente (INT, PK), Nombre (VARCHAR: Nitritos, Nitratos, Silicatos, Hierro, Amonio, Fosfatos), UnidadMedida (VARCHAR)
 - Tabla Sensor: IdSensor (INT, PK), NumeroSensor (VARCHAR), Marca (VARCHAR), IdCorral (INT, FK -> Corral.IdCorral), ProfundidadInstalacion (DECIMAL), Activo (BIT)
 - Tabla LecturaSensorCorral: IdLectura (INT, PK), IdCorral (INT, FK -> Corral.IdCorral), Fecha (DATE), Profundidad (DECIMAL), MetodoCaptura (VARCHAR: 'Sensor' o 'Manual'), IdSensor (INT, FK -> Sensor.IdSensor, NULL si es Manual), Temperatura (FLOAT), OxigenoMgL (FLOAT), SaturacionOxigenoPct (FLOAT)
 - Tabla MuestreoAbiotico: IdMuestreo (INT, PK), IdSitio (INT, FK -> Sitio.IdSitio), Fecha (DATE), Turno (VARCHAR: 'Mañana', 'Tarde' o 'Noche'), OxigenoDisueltoMgL (DECIMAL), TurbidezM (DECIMAL), Observaciones (VARCHAR)
 - Tabla MuestreoNutriente: IdMuestreo (INT, PK, FK -> MuestreoAbiotico.IdMuestreo), IdNutriente (INT, PK, FK -> Nutriente.IdNutriente), Valor (DECIMAL)
 - Tabla EstacionMonitoreo: IdEstacion (INT, PK), Nombre (VARCHAR: '1' a '5' y 'El Sauzal'; son puntos físicos de muestreo, no temporadas)
 - Tabla CategoriaFitoplancton: IdCategoria (INT, PK), Nombre (VARCHAR: Dinoflagelados, Silicoflagelados, Diatomeas)
 - Tabla MuestreoFitoplancton: IdMuestreoFito (INT, PK), IdSitio (INT, FK -> Sitio.IdSitio), IdEstacion (INT, FK -> EstacionMonitoreo.IdEstacion), Fecha (DATE), AbundanciaTotalCelulasL (DECIMAL), Especie (VARCHAR), Observaciones (VARCHAR)
 - Tabla MuestreoFitoplanctonCategoria: IdMuestreoFito (INT, PK, FK -> MuestreoFitoplancton.IdMuestreoFito), IdCategoria (INT, PK, FK -> CategoriaFitoplancton.IdCategoria), AbundanciaCelulasL (DECIMAL)
 - Tabla PresentacionAlimento: IdPresentacion (INT, PK), Nombre (VARCHAR: 'Fresco' o 'Congelado'), PesoPromedioPorBolsaKg (DECIMAL)
 - Tabla CalculoCargaBarco: IdCalculo (INT, PK), IdBarco (INT, FK -> Barco.IdBarco), IdAlimento (INT, FK -> Alimento.IdAlimento), IdPresentacion (INT, FK -> PresentacionAlimento.IdPresentacion), Fecha (DATE), NumeroBolsas (INT), Observaciones (VARCHAR)
 - Tabla CalculoCargaBarcoCorral: IdCalculo (INT, PK, FK -> CalculoCargaBarco.IdCalculo), IdCorral (INT, PK, FK -> Corral.IdCorral), ToneladasAsignadas (DECIMAL), ReporteBuceoAjuste (VARCHAR: 'Subir', 'Bajar' o 'Sin cambio'), Observaciones (VARCHAR)
 - Vista vw_CalidadAguaSitio (ya pivoteada, preferirla para calidad de agua por sitio): Sitio (VARCHAR), Fecha, Turno, OxigenoDisueltoMgL, TurbidezM, Nitritos, Nitratos, Silicatos, Hierro, Amonio, Fosfatos
 - Vista vw_LecturaSensorVsManual: Corral (VARCHAR), Fecha, Profundidad, Temp_Sensor, Temp_Manual, O2_Sensor, O2_Manual
 - Vista vw_AbioticoFitoplancton: Sitio (VARCHAR), Fecha, OxigenoPromedioDia, TurbidezPromedioDia, AbundanciaTotalCelulasL, Estacion (VARCHAR, nombre de la estación de monitoreo)
 - Vista vw_ConsumoAlimentoCorral: Corral (VARCHAR), Alimento (VARCHAR), Presentacion (VARCHAR), Fecha, CantidadKg, Mortalidad
 - Vista vw_CargaBarcoCalculada: IdCalculo, Barco (VARCHAR), Alimento (VARCHAR), Presentacion (VARCHAR), Fecha, NumeroBolsas, PesoTotalKgEstimado";

        /// <summary>
        /// 1. Envía la pregunta en lenguaje natural al modelo local (Ollama) y regresa el SQL generado.
        /// </summary>
        public async Task<string> GenerarConsultaSqlAsync(string preguntaUsuario)
        {
            string prompt = $@"Eres un generador estricto de SQL para Microsoft SQL Server (T-SQL). Devuelve ÚNICAMENTE la consulta SQL. No incluyas explicaciones, 
            saludos ni bloques de código tipo ```sql. Solo puedes generar consultas de lectura (SELECT). Nunca generes DELETE, DROP, UPDATE, INSERT ni TRUNCATE.
            Esquema de las tablas disponibles:
            {EsquemaTablas}

            Pregunta del usuario: {preguntaUsuario}
            SQL:";

            var body = new
            {
                model = _ollamaModel,
                prompt = prompt,
                stream = false,
                options = new { temperature = 0.0 }
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsync(_ollamaUrl, content);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception(
                    "No se pudo conectar con Ollama. Verifica que esté corriendo (`ollama serve`) " +
                    $"y que el modelo '{_ollamaModel}' esté descargado (`ollama list`). Detalle: {ex.Message}");
            }

            // BUG CORREGIDO: antes decía "response.ContentReadAsStringAsync()" (sin el punto) y no compilaba.
            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Ollama respondió con error ({response.StatusCode}): {responseJson}");
            }

            using (var doc = JsonDocument.Parse(responseJson))
            {
                string sqlGenerado = doc.RootElement.GetProperty("response").GetString()?.Trim();

                if (string.IsNullOrWhiteSpace(sqlGenerado))
                {
                    throw new Exception("El modelo no devolvió una consulta SQL.");
                }

                // Limpieza básica por si el modelo incluye etiquetas markdown de todas formas.
                sqlGenerado = sqlGenerado.Replace("```sql", "").Replace("```", "").Trim();

                ValidarSoloLectura(sqlGenerado);

                return sqlGenerado;
            }

        }

        /// <summary>
        /// 2. Ejecuta el SQL generado contra la base de datos real (usa la misma
        /// conexión configurada en Web.config que el resto de la aplicación).
        /// </summary>
        public async Task<DataTable> EjecutarConsultaAsync(string sqlQuery)
        {
            ValidarSoloLectura(sqlQuery);

            var dataTable = new DataTable();
            var cn = new Conexion();

            using (SqlConnection connection = cn.ObtenerConexion())
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(sqlQuery, connection))
                {
                    using (var adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }

            return dataTable;
        }

        /// <summary>
        /// Bloquea cualquier instrucción que no sea de solo lectura, como red de
        /// seguridad extra ante posibles "alucinaciones" del modelo.
        /// </summary>
        private static void ValidarSoloLectura(string sql)
        {
            string sqlUpper = sql?.ToUpperInvariant() ?? "";
            if (sqlUpper.Contains("DELETE") || sqlUpper.Contains("DROP") ||
                sqlUpper.Contains("UPDATE") || sqlUpper.Contains("INSERT") ||
                sqlUpper.Contains("TRUNCATE") || sqlUpper.Contains("ALTER") ||
                sqlUpper.Contains("EXEC"))
            {
                throw new Exception("Por seguridad solo se permiten consultas de lectura (SELECT).");
            }
        }
    }
}