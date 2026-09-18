using System;

namespace Servax_bleu_unificacion.Models
{
    public class InventarioPez
    {
        public int IdInventario { get; set; }
        public int IdCorral { get; set; }
        public int IdEspecie { get; set; }
        public int Cantidad { get; set; }
        public decimal? PesoPromedioKg { get; set; }
        public string Estado { get; set; }
        public DateTime? FechaRegistro { get; set; }
        public string Observaciones { get; set; }

        // Solo para mostrar en listados/vistas (join), no se persisten directo.
        public string NombreCorral { get; set; }
        public string NombreEspecie { get; set; }
    }
}
