namespace Servax_bleu_unificacion.Models
{
    public class Alimento
    {
        public int IdAlimento { get; set; }
        public string Nombre { get; set; }
        public string TipoAlimento { get; set; }
        public string UnidadMedida { get; set; }
        public decimal StockActual { get; set; }
        public decimal CostoUnitario { get; set; }
        public string Observaciones { get; set; }
    }
}
