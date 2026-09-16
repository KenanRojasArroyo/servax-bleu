namespace Servax_bleu_unificacion.Models
{
    public class Especie
    {
        public int IdEspecie { get; set; }
        public string Nombre { get; set; }
        public string NombreCientifico { get; set; }
        public string Tipo { get; set; }
        public bool EsToxica { get; set; }
        public string Descripcion { get; set; }
        public bool Activo { get; set; }
    }
}