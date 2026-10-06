namespace ExportadorPersycom.Database.Model;

// Una fila del panel de resultados de la busqueda avanzada: un presupuesto+version (cabecera de
// PAF), no una linea. Lo que hace falta para reconocerlo en el grid y para generar su ZIP.
public sealed class ResultadoBusqueda
{
    public long Numero { get; init; }
    public long Version { get; init; }
    public string NombreVersion { get; init; } = "";
    public string Cliente { get; init; } = "";
    public string PedidoCompras { get; init; } = "";
    public string ReferenciaObra { get; init; } = "";
    public string Referencia { get; init; } = "";
}
