using System;

namespace Servax_bleu_unificacion.Models
{
    public class MuestreoAgua
    {
        public int IdMuestreo { get; set; }
        public int IdCorral { get; set; }
        public DateTime Fecha { get; set; }
        public double? Temperatura { get; set; }
        public double? Oxigeno { get; set; }
        public double? Profundidad { get; set; }
        public double? PH { get; set; }
        public double? Salinidad { get; set; }
        public string Nutrientes { get; set; }
        public string Irregularidad { get; set; }
        public string Observaciones { get; set; }

        // Solo para mostrar en listados/vistas (join), no se persiste directo.
        public string NombreCorral { get; set; }
    }
}