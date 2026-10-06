using System.Data.Odbc;
using ExportadorPersycom.Database.Model;

namespace ExportadorPersycom.Database;

// Mismo contrato de datos que el original de KoUtilities en cuanto a columnas y semantica,
// pero SIN depender de que exista la vista [ZZ-Rolap-DatosPAF] en la base del cliente: el
// SELECT que la definia se usa como subconsulta embebida (ConsultaZzRolapDatosPaf.Origen),
// por decision expresa de no crear ni tocar objetos de esquema ahi.
// Aqui tambien cambia como se organiza el codigo C# alrededor: conexion por llamada (using)
// en vez de una conexion estatica compartida, y las excepciones SUBEN al llamador en vez
// de tragarse en silencio (el original llamaba a un ILogger que nunca se inicializaba,
// asi que cualquier error real de BD lanzaba NullReferenceException dentro del propio
// catch y desaparecia sin dejar rastro).
public class DbFacade
{
    public List<string> ObtenerNumerosDisponibles()
    {
        var claves = new List<string>();
        using var conn = DbConnectionFactory.Abrir();
        using var cmd = new OdbcCommand(
            $"SELECT Numero, Cliente FROM {ConsultaZzRolapDatosPaf.Origen} ORDER BY Numero DESC", conn);
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

    // Texto libre escrito por el usuario: SIEMPRE parametrizado (marcador ? de ODBC, en el
    // orden de aparicion). A diferencia de Numero/Version en el resto de la clase (long ya
    // parseados, sin riesgo de inyeccion), este texto no se interpola nunca en el SQL.
    public List<string> BuscarPresupuestos(string texto)
    {
        var claves = new List<string>();
        string patron = $"%{EscaparComodinesLike(texto)}%";
        using var conn = DbConnectionFactory.Abrir();
        using var cmd = new OdbcCommand(
            $"SELECT Numero, Cliente FROM {ConsultaZzRolapDatosPaf.Origen} " +
            "WHERE UPPER(CAST(Numero AS VARCHAR(50))) LIKE UPPER(?) " +
            "OR UPPER(NumeroPedido) LIKE UPPER(?) " +
            "OR UPPER(Cliente) LIKE UPPER(?) " +
            "OR UPPER(Obra) LIKE UPPER(?) " +
            "ORDER BY Numero DESC", conn);
        cmd.Parameters.AddWithValue("texto_numero", patron);
        cmd.Parameters.AddWithValue("texto_pedido", patron);
        cmd.Parameters.AddWithValue("texto_cliente", patron);
        cmd.Parameters.AddWithValue("texto_obra", patron);
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

    // Escapa los comodines propios de LIKE en T-SQL (sintaxis de corchetes, sin necesitar
    // ESCAPE) para que el texto del usuario se busque como subcadena literal.
    private static string EscaparComodinesLike(string texto) =>
        texto.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");

    // PAF.Referencia es un dato de cabecera (Numero+Version), no de linea: por eso no vive
    // en DatosPaf (que es por linea de ContenidoPAF, para el ZIP) sino en su propia consulta.
    public string? ObtenerReferencia(long numero, long version)
    {
        using var conn = DbConnectionFactory.Abrir();
        using var cmd = new OdbcCommand(
            $"SELECT Referencia FROM {ConsultaZzRolapDatosPaf.Origen} " +
            $"WHERE Numero = {numero} AND Version = {version}", conn);
        using var rd = cmd.ExecuteReader();
        return rd.Read() && !rd.IsDBNull(0) ? rd[0].ToString() : null;
    }

    public List<string> ObtenerVersiones(long numero)
    {
        var versiones = new List<string>();
        using var conn = DbConnectionFactory.Abrir();
        using var cmd = new OdbcCommand(
            $"SELECT Version, NombreVersion FROM {ConsultaZzRolapDatosPaf.Origen} " +
            $"WHERE Numero = {numero} ORDER BY Version", conn);
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
            "SELECT Numero, Version, Orden, Cantidad, Nomenclatura, Zlib.unzipxml(XMLDescriptive), Cliente, NombreVersion " +
            $"FROM {ConsultaZzRolapDatosPaf.Origen} WHERE Numero = {numero} AND Version = {version}" +
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
