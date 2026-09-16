using System;
using System.Data.SqlClient;
using System.Configuration;

namespace Servax_bleu_unificacion
{
    public class Conexion
    {
        private readonly string cadena = ConfigurationManager.ConnectionStrings["ConexioBD"].ConnectionString;

        public SqlConnection ObtenerConexion()
        {
            return new SqlConnection(cadena);
        }
    }
}