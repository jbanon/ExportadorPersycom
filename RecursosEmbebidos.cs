using System.Reflection;

namespace ExportadorPersycom;

// El logo y el icono van incrustados en el ensamblado, no al lado del .exe: el publish single-file
// no se lleva la carpeta Recursos/ (ver .arq/trampas.md). Compartido por las dos pantallas para
// que no haya dos copias de la misma carga.
public static class RecursosEmbebidos
{
    public static readonly Color RojoPersycom = Color.FromArgb(0xE3, 0x2B, 0x36);
    public static readonly Color RojoPersycomOscuro = Color.FromArgb(0xC1, 0x22, 0x2C);
    public static readonly Color GrisTexto = Color.FromArgb(0x1F, 0x29, 0x37);
    public static readonly Color GrisSuave = Color.FromArgb(0xF4, 0xF5, 0xF7);
    public static readonly Color GrisLinea = Color.FromArgb(0xE5, 0xE7, 0xEB);
    public static readonly Color GrisApagado = Color.FromArgb(0x6B, 0x72, 0x80);
    public static readonly Color VerdeOk = Color.FromArgb(0x15, 0x80, 0x3D);

    public static Image? Logo()
    {
        using var stream = Abrir("logo-persycom.png");
        return stream is null ? null : Image.FromStream(stream);
    }

    public static Icon? Icono()
    {
        using var stream = Abrir("persycom.ico");
        return stream is null ? null : new Icon(new Icon(stream), 32, 32);
    }

    private static Stream? Abrir(string sufijo)
    {
        var asm = Assembly.GetExecutingAssembly();
        string? recurso = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(sufijo));
        return recurso is null ? null : asm.GetManifestResourceStream(recurso);
    }
}
