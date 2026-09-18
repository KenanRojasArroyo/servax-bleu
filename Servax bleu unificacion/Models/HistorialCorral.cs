using System;

namespace Servax_bleu_unificacion.Models
{
    public class HistorialCorral
    {
        public int IdHistorial { get; set; }
        public int IdCorral { get; set; }
        public DateTime Fecha { get; set; }
        public int CantidadPeces { get; set; }
        public string EstadoGeneral { get; set; }
        public string ResumenCalidadAgua { get; set; }
        public string ResumenNutrientes { get; set; }
        public string Observaciones { get; set; }

        public string NombreCorral { get; set; }
    }
}
