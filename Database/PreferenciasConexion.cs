using System.Text.Json;

namespace ExportadorPersycom.Database;

// Lo que la pantalla de busqueda recuerda entre ejecuciones para no teclearlo cada vez: modo,
// DSN, servidor, puerto, base, usuario y la casilla de certificado. LA CONTRASEÑA NO: ni aqui ni
// en ningun otro fichero (decision de la tarea 0001). Va a %AppData%\ExportadorPersycom como
// JSON; si no se puede leer o escribir, la pantalla funciona igual con los valores por defecto.
public sealed class PreferenciasConexion
{
    public ModoConexion Modo { get; set; } = ModoConexion.Odbc;
    public string Dsn { get; set; } = ConfiguracionConexion.DsnPorDefecto;
    public string Servidor { get; set; } = "";
    public int? Puerto { get; set; }
    public string BaseDatos { get; set; } = "";
    public string Usuario { get; set; } = "";
    public bool ExigirCertificadoValido { get; set; }

    private static string Ruta => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ExportadorPersycom", "conexion.json");

    public static PreferenciasConexion Cargar()
    {
        try
        {
            if (!File.Exists(Ruta)) return new PreferenciasConexion();
            return JsonSerializer.Deserialize<PreferenciasConexion>(File.ReadAllText(Ruta)) ?? new PreferenciasConexion();
        }
        catch (Exception)
        {
            return new PreferenciasConexion();
        }
    }

    public void Guardar()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Ruta)!);
            File.WriteAllText(Ruta, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception)
        {
            // Recordar la conexion es una comodidad: si el perfil no deja escribir, no se rompe nada.
        }
    }

    public static PreferenciasConexion De(ConfiguracionConexion c) => new()
    {
        Modo = c.Modo, Dsn = c.Dsn, Servidor = c.Servidor, Puerto = c.Puerto,
        BaseDatos = c.BaseDatos, Usuario = c.Usuario, ExigirCertificadoValido = c.ExigirCertificadoValido,
    };
}
