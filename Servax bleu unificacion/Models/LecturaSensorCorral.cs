using System;

namespace Servax_bleu_unificacion.Models
{
    public class LecturaSensorCorral
    {
        public int IdLectura { get; set; }
        public int IdCorral { get; set; }
        public DateTime Fecha { get; set; }
        public double Profundidad { get; set; } // 3.0 o 20.0 (m)
        public string MetodoCaptura { get; set; } // 'Sensor' | 'Manual'
        public int? IdSensor { get; set; } // NULL si MetodoCaptura = 'Manual'
        public double? Temperatura { get; set; }
        public double? OxigenoMgL { get; set; }
        public double? SaturacionOxigenoPct { get; set; }

        // Solo para mostrar en listados (join), no se persisten directo.
        public string NombreCorral { get; set; }
        public string NumeroSensor { get; set; }
        public string MarcaSensor { get; set; }
    }
}
