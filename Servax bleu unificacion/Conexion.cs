using System;
using System.Data.SqlClient;
using System.Configuration;

namespace Servax_bleu_unificacion
{
    public class Conexion
    {
        // Lee la cadena 'ConexioBD' configurada en el Web.config
        private readonly string cadena = ConfigurationManager.ConnectionStrings["ConexioBD"].ConnectionString;

        public SqlConnection ObtenerConexion()
        {
            return new SqlConnection(cadena);
        }
    }
}