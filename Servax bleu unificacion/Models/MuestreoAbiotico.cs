using System;
using System.Collections.Generic;

namespace Servax_bleu_unificacion.Models
{
    public class MuestreoAbiotico
    {
        public int IdMuestreo { get; set; }
        public int IdSitio { get; set; }
        public DateTime Fecha { get; set; }
        public string Turno { get; set; } // 'Mañana' | 'Tarde' | 'Noche' — supuesto a confirmar con Arian
        public double? OxigenoDisueltoMgL { get; set; }
        public double? TurbidezM { get; set; }
        public string Observaciones { get; set; }

        // Nutrientes capturados en este muestreo: clave = Nombre del catálogo Nutriente
        // (Nitritos, Nitratos, Silicatos, Hierro, Amonio, Fosfatos), valor = medición o null ("-").
        public Dictionary<string, double?> Nutrientes { get; set; } = new Dictionary<string, double?>
        {
            { "Nitritos", null }, { "Nitratos", null }, { "Silicatos", null },
            { "Hierro", null }, { "Amonio", null }, { "Fosfatos", null }
        };

        // Solo para mostrar en listados (join), no se persiste directo.
        public string NombreSitio { get; set; }
    }
}
