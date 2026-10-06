using System.Data.Common;
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
//
// DOS PROVEEDORES (tarea 0001): todo va contra System.Data.Common, y la conexion la decide
// la ConfiguracionConexion que se recibe al construir (ODBC con DSN, o SQL Server directo).
// Las consultas con texto del usuario se escriben con @nombre y pasan por
// ConsultaParametrizada, que las adapta al proveedor: ninguna query existe dos veces.
// Al retirarse la pantalla antigua (orden del gerente, 06-10-2026) se fueron con ella sus
// consultas (lista completa, buscador OR, versiones y referencia sueltas): la busqueda
// avanzada ya devuelve todo eso en una sola fila por presupuesto+version.
public class DbFacade
{
    private readonly ConfiguracionConexion _conexion;

    public DbFacade(ConfiguracionConexion conexion) => _conexion = conexion;

    public ConfiguracionConexion Conexion => _conexion;

    private DbConnection Abrir() => DbConnectionFactory.Abrir(_conexion);

    private static DbCommand Comando(DbConnection conn, string sql)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return cmd;
    }

    /// <summary>Abre y cierra una conexion y pregunta la version del servidor: para el boton "Probar conexion".</summary>
    public string ProbarConexion()
    {
        using var conn = Abrir();
        using var cmd = Comando(conn, "SELECT 1");
        cmd.ExecuteScalar();
        return $"{conn.DataSource} · {conn.ServerVersion}";
    }

    /// <summary>
    /// La busqueda avanzada (tarea 0001): un cuadro por campo y los rellenos se combinan con AND
    /// (cada campo acota). Cada campo busca como subcadena, sin distinguir mayusculas. Devuelve
    /// una fila por presupuesto+version (cabecera), no por linea. Sin ningun campo relleno
    /// devuelve todo, lo mas reciente primero.
    /// </summary>
    public List<ResultadoBusqueda> BuscarAvanzado(string? numero, string? pedidoCompras, string? cliente, string? referenciaObra)
    {
        var condiciones = new List<string>();
        var parametros = new Dictionary<string, object?>();
        void Filtro(string nombre, string columna, string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return;
            condiciones.Add($"UPPER({columna}) LIKE UPPER(@{nombre})");
            parametros[nombre] = $"%{EscaparComodinesLike(valor.Trim())}%";
        }
        Filtro("numero", "CAST(Numero AS VARCHAR(50))", numero);
        Filtro("pedido", "CAST(NumeroPedido AS VARCHAR(50))", pedidoCompras);
        Filtro("cliente", "Cliente", cliente);
        Filtro("obra", "Obra", referenciaObra);

        string where = condiciones.Count == 0 ? "" : " WHERE " + string.Join(" AND ", condiciones);
        var resultado = new List<ResultadoBusqueda>();
        using var conn = Abrir();
        using var cmd = ConsultaParametrizada.Crear(conn,
            "SELECT DISTINCT Numero, Version, NombreVersion, Cliente, NumeroPedido, Obra, Referencia " +
            $"FROM {ConsultaZzRolapDatosPaf.Origen}{where} ORDER BY Numero DESC, Version DESC",
            parametros);
        using var rd = cmd.ExecuteReader();

        while (rd.Read())
        {
            resultado.Add(new ResultadoBusqueda
            {
                Numero = rd.IsDBNull(0) ? 0 : Convert.ToInt64(rd[0]),
                Version = rd.IsDBNull(1) ? 0 : Convert.ToInt64(rd[1]),
                NombreVersion = Texto(rd, 2),
                Cliente = Texto(rd, 3),
                PedidoCompras = Texto(rd, 4),
                ReferenciaObra = Texto(rd, 5),
                Referencia = Texto(rd, 6),
            });
        }
        return resultado;
    }

    private static string Texto(DbDataReader rd, int i) => rd.IsDBNull(i) ? "" : rd[i].ToString()!.Trim();

    // Escapa los comodines propios de LIKE en T-SQL (sintaxis de corchetes, sin necesitar
    // ESCAPE) para que el texto del usuario se busque como subcadena literal.
    private static string EscaparComodinesLike(string texto) =>
        texto.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");

    public List<DatosPaf> ObtenerDatosParaExportar(long numero, long version)
    {
        var resultado = new List<DatosPaf>();
        using var conn = Abrir();
        using var cmd = Comando(conn,
            "SELECT Numero, Version, Orden, Cantidad, Nomenclatura, Zlib.unzipxml(XMLDescriptive), Cliente, NombreVersion " +
            $"FROM {ConsultaZzRolapDatosPaf.Origen} WHERE Numero = {numero} AND Version = {version}" +
            " ORDER BY Orden");
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
