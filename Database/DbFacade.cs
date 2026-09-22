using System.Data.Odbc;
using ExportadorPersycom.Database.Model;

namespace ExportadorPersycom.Database;

// Mismo contrato de datos que el original de KoUtilities (no se toca): la vista
// [ZZ-Rolap-DatosPAF] y sus 8 columnas, y la funcion SQL Zlib.unzipxml para descomprimir.
// Aqui solo cambia como se organiza el codigo C# alrededor: conexion por llamada (using)
// en vez de una conexion estatica compartida, y las excepciones SUBEN al llamador en vez
// de tragarse en silencio (el original llamaba a un ILogger que nunca se inicializaba,
// asi que cualquier error real de BD lanzaba NullReferenceException dentro del propio
// catch y desaparecia sin dejar rastro).
public class DbFacade
{
    public bool VistaExiste()
    {
        using var conn = DbConnectionFactory.Abrir();
        using var cmd = new OdbcCommand($"SELECT OBJECT_ID('{VistaZzRolapDatosPaf.Nombre}', 'V')", conn);
        object? resultado = cmd.ExecuteScalar();
        return resultado is not null and not DBNull;
    }

    public void CrearVista()
    {
        using var conn = DbConnectionFactory.Abrir();
        using var cmd = new OdbcCommand(VistaZzRolapDatosPaf.SqlCreacion, conn);
        cmd.ExecuteNonQuery();
    }

    public List<string> ObtenerNumerosDisponibles()
    {
        var claves = new List<string>();
        using var conn = DbConnectionFactory.Abrir();
        using var cmd = new OdbcCommand(
            "SELECT Numero, cliente FROM [ZZ-Rolap-DatosPAF] ORDER BY Orden", conn);
        using var rd = cmd.ExecuteReader();

        while (rd.Read())
        {
            string numero = rd.IsDBNull(0) ? "NN" : rd[0].ToString()!;
            string cliente = rd.IsDBNull(1) ? "XX" : rd[1].ToString()!;
            string clave = cliente != "XX" ? $"{numero} - {cliente}" : numero;
            if (!claves.Contains(clave)) claves.Add(clave);
        }
        return claves;
    }

    public List<string> ObtenerVersiones(long numero)
    {
        var versiones = new List<string>();
        using var conn = DbConnectionFactory.Abrir();
        using var cmd = new OdbcCommand(
            "SELECT Version, NombreVersion FROM [ZZ-Rolap-DatosPAF] WHERE Numero = " +
            numero + " ORDER BY Version", conn);
        using var rd = cmd.ExecuteReader();

        while (rd.Read())
        {
            string version = rd.IsDBNull(0) ? "NN" : rd[0].ToString()!;
            string nombreVersion = rd.IsDBNull(1) ? "XX" : rd[1].ToString()!;
            string clave = nombreVersion != "XX" ? $"{version} - {nombreVersion}" : version;
            if (!versiones.Contains(clave)) versiones.Add(clave);
        }
        return versiones;
    }

    public List<DatosPaf> ObtenerDatosParaExportar(long numero, long version)
    {
        var resultado = new List<DatosPaf>();
        using var conn = DbConnectionFactory.Abrir();
        using var cmd = new OdbcCommand(
            "SELECT Numero, Version, Orden, Cantidad, Nomenclatura, Zlib.unzipxml(XMLDescriptive), cliente, NombreVersion " +
            "FROM [ZZ-Rolap-DatosPAF] WHERE Numero = " + numero + " AND Version = " + version +
            " ORDER BY Orden", conn);
        using var rd = cmd.ExecuteReader();

        while (rd.Read())
        {
            resultado.Add(new DatosPaf
            {
                Numero = rd.IsDBNull(0) ? "NN" : rd[0].ToString()!,
                Version = rd.IsDBNull(1) ? "NN" : rd[1].ToString()!,
                Orden = rd.IsDBNull(2) ? "NN" : rd[2].ToString()!,
                Cantidad = rd.IsDBNull(3) ? "NN" : rd[3].ToString()!,
                Nomenclatura = rd.IsDBNull(4) ? "XX" : rd[4].ToString()!,
                XmlDescriptive = rd.IsDBNull(5) ? "" : rd[5].ToString()!,
                Cliente = rd.IsDBNull(6) ? "XX" : rd[6].ToString()!,
                NombreVersion = rd.IsDBNull(7) ? "XX" : rd[7].ToString()!,
            });
        }
        return resultado;
    }
}
