using System.Data.Common;

namespace ExportadorPersycom.Database;

// Queda como punto unico por el que DbFacade pide conexiones. La decision de proveedor (ODBC
// o SQL Server) vive en ConfiguracionConexion; esto solo la aplica.
public static class DbConnectionFactory
{
    public static DbConnection Abrir(ConfiguracionConexion configuracion) => configuracion.Abrir();
}
