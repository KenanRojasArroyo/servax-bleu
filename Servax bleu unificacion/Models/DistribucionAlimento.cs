using System;

namespace Servax_bleu_unificacion.Models
{
    public class DistribucionAlimento
    {
        public int IdDistribucion { get; set; }
        public int IdBarco { get; set; }
        public int IdAlimento { get; set; }
        public int IdCorral { get; set; }
        public DateTime Fecha { get; set; }
        public decimal CantidadToneladas { get; set; }
        public string FormulaAplicada { get; set; }
        public string Observaciones { get; set; }

        public string NombreBarco { get; set; }
        public string NombreAlimento { get; set; }
        public string NombreCorral { get; set; }
    }
}
