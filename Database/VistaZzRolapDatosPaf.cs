namespace ExportadorPersycom.Database;

// Definicion real de la vista que necesita esta aplicacion, tal como la dio el gerente
// (CREATE VIEW de la base de origen). Se embebe aqui para que la app pueda comprobar si
// existe en la base del cliente y crearla si falta, en vez de exigir un paso manual previo.
public static class VistaZzRolapDatosPaf
{
    public const string Nombre = "[dbo].[ZZ-Rolap-DatosPAF]";

    public const string SqlCreacion = """
        CREATE VIEW [dbo].[ZZ-Rolap-DatosPAF] AS
        SELECT
            dbo.ContenidoPAF.Numero,
            dbo.ContenidoPAF.Version,
            dbo.ContenidoPAF.Orden,
            dbo.ContenidoPAF.Cantidad,
            dbo.ContenidoPAF.Nomenclatura,
            dbo.ContenidoPAFBlob.XMLDescriptive,
            dbo.PAF.Nombre AS Cliente,
            dbo.PAF.NombreVersion
        FROM dbo.ContenidoPAF
        INNER JOIN dbo.ContenidoPAFBlob
            ON dbo.ContenidoPAF.Numero = dbo.ContenidoPAFBlob.Numero
            AND dbo.ContenidoPAF.Version = dbo.ContenidoPAFBlob.Version
            AND dbo.ContenidoPAF.Orden = dbo.ContenidoPAFBlob.Orden
        INNER JOIN dbo.PAF
            ON dbo.ContenidoPAF.Numero = dbo.PAF.Numero
            AND dbo.ContenidoPAFBlob.Version = dbo.PAF.Version
            AND dbo.ContenidoPAF.Version = dbo.PAF.Version
        """;
}
