namespace ExportadorPersycom.Database;

// Ni se consulta la vista [dbo].[ZZ-Rolap-DatosPAF] ni se crea en la base del cliente
// (decision expresa: no tocar el esquema del cliente). En su lugar, el SELECT que la
// definia (dado por el gerente) se guarda aqui y se usa como subconsulta en el FROM de
// cada consulta de DbFacade, con el mismo resultado sin necesitar la vista.
public static class ConsultaZzRolapDatosPaf
{
    public const string Origen = """
        (
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
        ) AS ZzRolapDatosPaf
        """;
}
