using System;

namespace Servax_bleu_unificacion.Models
{
    public class Corral
    {
        public int IdCorral { get; set; }
        public string Nombre { get; set; }
        public string Ubicacion { get; set; }
        public decimal? CapacidadMaxima { get; set; }
        public DateTime? FechaInstalacion { get; set; }
        public string Estado { get; set; }
        public string Observaciones { get; set; }
    }
}