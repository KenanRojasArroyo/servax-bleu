using System;

namespace Servax_bleu_unificacion.Models
{
    public class RegistroAlimentacion
    {
        public int IdRegistro { get; set; }
        public int IdCorral { get; set; }
        public int IdAlimento { get; set; }
        public DateTime Fecha { get; set; }
        public decimal CantidadKg { get; set; }
        public string Responsable { get; set; }
        public int Mortalidad { get; set; }
        public string Observaciones { get; set; }

        public string NombreCorral { get; set; }
        public string NombreAlimento { get; set; }
    }
}
