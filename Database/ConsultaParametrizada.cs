using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Text.RegularExpressions;

namespace ExportadorPersycom.Database;

// EL SQL SE ESCRIBE UNA SOLA VEZ, con parametros nombrados (@numero, @cliente...), y aqui se
// adapta al proveedor que haya debajo:
//   - SqlClient entiende @nombre tal cual: el texto viaja sin tocar y los parametros se añaden
//     por nombre.
//   - ODBC solo entiende marcadores posicionales ?: se sustituye cada @nombre por ? en el orden
//     en que aparece en el texto, y se añade un parametro por APARICION (si @texto sale cuatro
//     veces, van cuatro parametros con el mismo valor), que es como ODBC los casa.
// Asi ninguna consulta de DbFacade existe por duplicado, y el dia que cambie una query se cambia
// en un solo sitio para los dos caminos.
public static class ConsultaParametrizada
{
    // Un nombre de parametro: @ seguido de letras/numeros/guion bajo. El SQL embebido no usa @
    // para nada mas (ni variables ni @@funciones), asi que no hay falsos positivos que esquivar.
    private static readonly Regex Marcador = new(@"@([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);

    public static DbCommand Crear(DbConnection conn, string sqlConNombres, IReadOnlyDictionary<string, object?> parametros)
    {
        var cmd = conn.CreateCommand();
        if (conn is OdbcConnection)
        {
            cmd.CommandText = Marcador.Replace(sqlConNombres, m =>
            {
                cmd.Parameters.Add(Parametro(cmd, m.Groups[1].Value, Valor(parametros, m.Groups[1].Value)));
                return "?";
            });
        }
        else
        {
            cmd.CommandText = sqlConNombres;
            foreach (var nombre in Marcador.Matches(sqlConNombres).Select(m => m.Groups[1].Value).Distinct(StringComparer.Ordinal))
                cmd.Parameters.Add(Parametro(cmd, nombre, Valor(parametros, nombre)));
        }
        return cmd;
    }

    private static object? Valor(IReadOnlyDictionary<string, object?> parametros, string nombre) =>
        parametros.TryGetValue(nombre, out var v) ? v
        : throw new ArgumentException($"La consulta usa el parámetro @{nombre} y no se le ha dado valor.");

    private static DbParameter Parametro(DbCommand cmd, string nombre, object? valor)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = "@" + nombre;     // ODBC ignora el nombre (casa por posicion); SqlClient lo necesita
        p.Value = valor ?? DBNull.Value;
        p.DbType = valor switch
        {
            null => DbType.String,
            long => DbType.Int64,
            int => DbType.Int32,
            _ => DbType.String,
        };
        return p;
    }
}
