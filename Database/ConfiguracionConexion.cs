using System.Configuration;
using System.Data.Common;
using System.Data.Odbc;
using Microsoft.Data.SqlClient;

namespace ExportadorPersycom.Database;

public enum ModoConexion
{
    /// <summary>DSN ODBC del sistema (el camino original, "PrefSuite").</summary>
    Odbc = 0,

    /// <summary>SQL Server directo: host, base de datos, usuario y contrasena (SqlClient).</summary>
    SqlServer = 1,
}

// Como se llega a la base de Preference/Alugal. Dos proveedores distintos (ODBC y SqlClient)
// detras de una sola clase: DbFacade no sabe cual tiene debajo, trabaja contra
// System.Data.Common y pide aqui la conexion abierta. La contrasena vive SOLO en memoria,
// mientras dura la pantalla: no se guarda en App.config ni en ningun otro fichero.
public sealed class ConfiguracionConexion
{
    public ModoConexion Modo { get; init; } = ModoConexion.Odbc;

    // ODBC
    public string Dsn { get; init; } = DsnPorDefecto;

    // SQL Server
    public string Servidor { get; init; } = "";
    public int? Puerto { get; init; }
    public string BaseDatos { get; init; } = "";
    /// <summary>Vacio = autenticacion integrada de Windows (sin usuario ni contrasena).</summary>
    public string Usuario { get; init; } = "";
    public string Contrasena { get; init; } = "";
    /// <summary>
    /// Exigir cifrado con certificado valido. Apagado por defecto: SqlClient lo exige de serie y
    /// contra un servidor con certificado autofirmado (lo habitual en una red local) la conexion
    /// falla sin mas; apagado se cifra si el servidor puede y se confia en su certificado, que es
    /// el mismo nivel que da hoy el camino ODBC.
    /// </summary>
    public bool ExigirCertificadoValido { get; init; }

    // Nombre del DSN ODBC tal como lo tenia el original ("PrefSuite"); configurable en
    // App.config para no depender de recompilar si cambia en la maquina del cliente.
    public static string DsnPorDefecto =>
        ConfigurationManager.AppSettings["Dsn"] is string dsn && dsn.Length > 0 ? dsn : "PrefSuite";

    /// <summary>La configuracion de siempre: ODBC con el DSN de App.config. La usa FormPrincipal.</summary>
    public static ConfiguracionConexion OdbcPorDefecto() => new() { Modo = ModoConexion.Odbc, Dsn = DsnPorDefecto };

    public bool EsOdbc => Modo == ModoConexion.Odbc;

    /// <summary>Texto corto para la barra de estado: a que se esta conectando, sin la contrasena.</summary>
    public string Describir() => EsOdbc
        ? $"ODBC · DSN {Dsn}"
        : $"SQL Server · {Servidor}{(Puerto is int p ? $",{p}" : "")} · {BaseDatos}" + (Usuario.Length > 0 ? $" · {Usuario}" : " · Windows");

    /// <summary>Comprueba lo que la pantalla no puede dejar pasar antes de intentar conectar. Null = todo bien.</summary>
    public string? Validar()
    {
        if (EsOdbc) return Dsn.Trim().Length == 0 ? "Indica el nombre del DSN ODBC." : null;
        if (Servidor.Trim().Length == 0) return "Indica el servidor SQL Server.";
        if (BaseDatos.Trim().Length == 0) return "Indica la base de datos.";
        if (Usuario.Trim().Length > 0 && Contrasena.Length == 0) return "Indica la contraseña del usuario (o deja el usuario vacío para entrar con la cuenta de Windows).";
        return null;
    }

    public DbConnection Abrir()
    {
        var conn = Crear();
        conn.Open();
        return conn;
    }

    private DbConnection Crear()
    {
        if (EsOdbc) return new OdbcConnection($"DSN={Dsn.Trim()}");

        var b = new SqlConnectionStringBuilder
        {
            DataSource = Puerto is int p ? $"{Servidor.Trim()},{p}" : Servidor.Trim(),
            InitialCatalog = BaseDatos.Trim(),
            ConnectTimeout = 10,
            ApplicationName = "Exportador Persycom",
            Encrypt = ExigirCertificadoValido ? SqlConnectionEncryptOption.Mandatory : SqlConnectionEncryptOption.Optional,
            TrustServerCertificate = !ExigirCertificadoValido,
        };
        if (Usuario.Trim().Length > 0)
        {
            b.UserID = Usuario.Trim();
            b.Password = Contrasena;
        }
        else
        {
            b.IntegratedSecurity = true;
        }
        return new SqlConnection(b.ConnectionString);
    }
}
