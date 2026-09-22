namespace PersycomPAF.Database.Model;

public class DatosPaf
{
    public string Numero { get; set; } = "";
    public string Version { get; set; } = "";
    public string Orden { get; set; } = "";
    public string Cantidad { get; set; } = "";
    public string Nomenclatura { get; set; } = "";
    public string XmlDescriptive { get; set; } = "";
    public string Cliente { get; set; } = "";
    public string NombreVersion { get; set; } = "";

    public string NombreFichero() =>
        $"{Numero}_{Version}_{Orden}_{Cantidad}_{Nomenclatura}_{Cliente}.xml";
}
