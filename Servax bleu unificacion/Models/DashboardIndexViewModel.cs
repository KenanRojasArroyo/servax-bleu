using System.Data;

namespace Servax_bleu_unificacion.Controllers
{
    public class DashboardIndexViewModel
    {
        public DataTable CalidadMuestreo { get; set; } = new DataTable();
        public DataTable InventarioCorralEspecie { get; set; } = new DataTable();
        public DataTable CrecimientoEspecie { get; set; } = new DataTable();
        public DataTable AlimentacionInventario { get; set; } = new DataTable();
    }
}