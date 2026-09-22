using System.IO.Compression;
using ExportadorPersycom.Database.Model;

namespace ExportadorPersycom.Zip;

public static class EmpaquetadorPaf
{
    // El original dejaba el temporal fijo en C:\RolaTemp\zip\, escrito sin comprobar permisos
    // en la raiz de C:. Aqui va a la carpeta temporal del usuario, que siempre es escribible.
    public static string CrearZip(long numero, long version, List<DatosPaf> datos, string carpetaDestino)
    {
        string carpetaTemporal = Path.Combine(Path.GetTempPath(), "ExportadorPersycom", $"{numero}_v{version}");
        if (Directory.Exists(carpetaTemporal))
            Directory.Delete(carpetaTemporal, true);
        Directory.CreateDirectory(carpetaTemporal);

        try
        {
            foreach (var item in datos)
                File.WriteAllText(Path.Combine(carpetaTemporal, item.NombreFichero()), item.XmlDescriptive);

            string rutaZip = Path.Combine(carpetaDestino, $"PAF_{numero}_v{version}.zip");
            if (File.Exists(rutaZip))
                File.Delete(rutaZip);
            ZipFile.CreateFromDirectory(carpetaTemporal, rutaZip);
            return rutaZip;
        }
        finally
        {
            Directory.Delete(carpetaTemporal, true);
        }
    }
}
