namespace PersycomPAF;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Application.ThreadException += (_, e) => MostrarErrorInesperado(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            MostrarErrorInesperado(e.ExceptionObject as Exception ?? new Exception("Error desconocido."));

        Application.Run(new FormPrincipal());
    }

    private static void MostrarErrorInesperado(Exception ex)
    {
        MessageBox.Show(
            "Ha ocurrido un error inesperado y hay que cerrar la aplicacion:\n\n" + ex.Message,
            "Persycom PAF", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
