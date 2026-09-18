using System;

namespace Servax_bleu_unificacion.Models
{
    public class CrecimientoAnual
    {
        public int IdCrecimiento { get; set; }
        public int IdCorral { get; set; }
        public int IdEspecie { get; set; }
        public int Anio { get; set; }
        public decimal? PesoPromedioInicial { get; set; }
        public decimal? PesoPromedioFinal { get; set; }
        public float? TasaCrecimiento { get; set; }
        public string Observaciones { get; set; }

        // Solo para mostrar en listados/vistas (join), no se persisten directo.
        public string NombreCorral { get; set; }
        public string NombreEspecie { get; set; }
    }
}
