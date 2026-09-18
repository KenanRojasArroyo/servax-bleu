using System;

namespace Servax_bleu_unificacion.Models
{
    public class Barco
    {
        public int IdBarco { get; set; }
        public string Nombre { get; set; }
        public decimal CapacidadToneladas { get; set; }
        public string Estado { get; set; }
        public DateTime? FechaAlta { get; set; }
        public string Observaciones { get; set; }
    }
}
