using System.Configuration;
using System.Data.Odbc;

namespace ExportadorPersycom.Database;

public static class DbConnectionFactory
{
    // Nombre del DSN ODBC tal como lo tenia el original ("PrefSuite"); configurable en
    // App.config para no depender de recompilar si cambia en la maquina del cliente.
    private static string DsnName =>
        ConfigurationManager.AppSettings["Dsn"] is string dsn && dsn.Length > 0 ? dsn : "PrefSuite";

    public static OdbcConnection Abrir()
    {
        var conn = new OdbcConnection($"DSN={DsnName}");
        conn.Open();
        return conn;
    }
}
