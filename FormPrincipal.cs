using System.Diagnostics;
using System.Reflection;
using ExportadorPersycom.Database;
using ExportadorPersycom.Zip;

namespace ExportadorPersycom;

public class FormPrincipal : Form
{
    private static readonly Color RojoPersycom = Color.FromArgb(0xE3, 0x2B, 0x36);
    private static readonly Color RojoPersycomOscuro = Color.FromArgb(0xC1, 0x22, 0x2C);
    private static readonly Color GrisTexto = Color.FromArgb(0x1F, 0x29, 0x37);
    private static readonly Color GrisSuave = Color.FromArgb(0xF4, 0xF5, 0xF7);

    private readonly DbFacade _dbFacade = new();

    private readonly ComboBox _cbPresupuesto = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _cbVersion = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _txtCarpeta = new() { ReadOnly = true };
    private readonly Button _btnCarpeta = new() { Text = "..." };
    private readonly Button _btnGenerar = new() { Text = "Generar ZIP" };
    private readonly ProgressBar _progreso = new() { Style = ProgressBarStyle.Marquee, Visible = false };
    private readonly Label _lblEstado = new() { AutoSize = false, ForeColor = GrisTexto };

    private string _carpetaDestino = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

    public FormPrincipal()
    {
        Text = "Exportador Persycom";
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Color.White;
        ClientSize = new Size(480, 400);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Recursos", "persycom.ico"), 32, 32);

        ConstruirLayout();

        Load += async (_, _) =>
        {
            if (await AsegurarVistaAsync())
                await CargarPresupuestosAsync();
        };
        _cbPresupuesto.SelectedIndexChanged += async (_, _) => await CargarVersionesAsync();
        _btnCarpeta.Click += (_, _) => ElegirCarpeta();
        _btnGenerar.Click += async (_, _) => await GenerarZipAsync();

        _txtCarpeta.Text = _carpetaDestino;
        ActualizarEstado("");
    }

    private void ConstruirLayout()
    {
        var cabecera = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = Color.White };
        var logo = new PictureBox
        {
            Image = CargarLogoEmbebido(),
            SizeMode = PictureBoxSizeMode.Zoom,
            Bounds = new Rectangle(20, 15, 220, 60),
        };
        var barraRoja = new Panel { Dock = DockStyle.Bottom, Height = 4, BackColor = RojoPersycom };
        cabecera.Controls.Add(logo);
        cabecera.Controls.Add(barraRoja);

        var cuerpo = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(24, 16, 24, 16),
            AutoSize = true,
        };
        cuerpo.Controls.Add(NuevaEtiqueta("Presupuesto"));
        _cbPresupuesto.Dock = DockStyle.Top;
        cuerpo.Controls.Add(_cbPresupuesto);
        cuerpo.Controls.Add(Espaciador());

        cuerpo.Controls.Add(NuevaEtiqueta("Versión"));
        _cbVersion.Dock = DockStyle.Top;
        cuerpo.Controls.Add(_cbVersion);
        cuerpo.Controls.Add(Espaciador());

        cuerpo.Controls.Add(NuevaEtiqueta("Carpeta destino"));
        var filaCarpeta = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, Height = 30 };
        filaCarpeta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        filaCarpeta.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));
        _txtCarpeta.Dock = DockStyle.Fill;
        _btnCarpeta.Dock = DockStyle.Fill;
        filaCarpeta.Controls.Add(_txtCarpeta, 0, 0);
        filaCarpeta.Controls.Add(_btnCarpeta, 1, 0);
        cuerpo.Controls.Add(filaCarpeta);
        cuerpo.Controls.Add(Espaciador(20));

        _btnGenerar.Dock = DockStyle.Top;
        _btnGenerar.Height = 38;
        _btnGenerar.FlatStyle = FlatStyle.Flat;
        _btnGenerar.FlatAppearance.BorderSize = 0;
        _btnGenerar.BackColor = RojoPersycom;
        _btnGenerar.ForeColor = Color.White;
        _btnGenerar.Font = new Font(Font, FontStyle.Bold);
        _btnGenerar.MouseEnter += (_, _) => _btnGenerar.BackColor = RojoPersycomOscuro;
        _btnGenerar.MouseLeave += (_, _) => _btnGenerar.BackColor = RojoPersycom;
        cuerpo.Controls.Add(_btnGenerar);
        cuerpo.Controls.Add(Espaciador());

        _progreso.Dock = DockStyle.Top;
        cuerpo.Controls.Add(_progreso);

        _lblEstado.Dock = DockStyle.Top;
        _lblEstado.Height = 60;
        cuerpo.Controls.Add(_lblEstado);

        Controls.Add(cuerpo);
        Controls.Add(cabecera);
    }

    private static Label NuevaEtiqueta(string texto) =>
        new() { Text = texto, Dock = DockStyle.Top, AutoSize = false, Height = 22, ForeColor = GrisTexto };

    private static Control Espaciador(int alto = 10) => new Panel { Dock = DockStyle.Top, Height = alto };

    private static Image? CargarLogoEmbebido()
    {
        var asm = Assembly.GetExecutingAssembly();
        string? recurso = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("logo-persycom.png"));
        if (recurso is null) return null;
        using var stream = asm.GetManifestResourceStream(recurso);
        return stream is null ? null : Image.FromStream(stream);
    }

    // Antes de cargar nada, comprueba que exista la vista [ZZ-Rolap-DatosPAF] de la que
    // depende toda la app; si falta (base de cliente recien conectada, nunca usada con esta
    // herramienta), ofrece crearla con el CREATE VIEW embebido en VistaZzRolapDatosPaf, pero
    // pidiendo confirmacion primero: es un cambio de esquema en la base del cliente, no algo
    // para hacer en silencio.
    private async Task<bool> AsegurarVistaAsync()
    {
        _progreso.Visible = true;
        ActualizarEstado("Comprobando la vista de datos...");
        try
        {
            bool existe = await Task.Run(() => _dbFacade.VistaExiste());
            if (existe) return true;

            var respuesta = MessageBox.Show(this,
                $"La vista {VistaZzRolapDatosPaf.Nombre} no existe en esta base de datos.\n\n¿Crearla ahora?",
                "Exportador Persycom", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (respuesta != DialogResult.Yes)
            {
                ActualizarEstado("No se puede continuar sin la vista de datos.", esError: true);
                return false;
            }

            ActualizarEstado("Creando la vista de datos...");
            await Task.Run(() => _dbFacade.CrearVista());
            return true;
        }
        catch (Exception ex)
        {
            ActualizarEstado("No se ha podido comprobar/crear la vista: " + ex.Message, esError: true);
            return false;
        }
        finally
        {
            _progreso.Visible = false;
        }
    }

    private async Task CargarPresupuestosAsync()
    {
        await EjecutarConEstado("Cargando presupuestos...", async () =>
        {
            var claves = await Task.Run(() => _dbFacade.ObtenerNumerosDisponibles());
            _cbPresupuesto.DataSource = claves;
        });
    }

    private async Task CargarVersionesAsync()
    {
        long numero = ExtraerNumero(_cbPresupuesto.SelectedItem?.ToString());
        if (numero <= 0) return;

        await EjecutarConEstado("Cargando versiones...", async () =>
        {
            var versiones = await Task.Run(() => _dbFacade.ObtenerVersiones(numero));
            _cbVersion.DataSource = versiones;
        });
    }

    private void ElegirCarpeta()
    {
        using var dialogo = new FolderBrowserDialog { SelectedPath = _carpetaDestino };
        if (dialogo.ShowDialog(this) == DialogResult.OK)
        {
            _carpetaDestino = dialogo.SelectedPath;
            _txtCarpeta.Text = _carpetaDestino;
        }
    }

    private async Task GenerarZipAsync()
    {
        long numero = ExtraerNumero(_cbPresupuesto.SelectedItem?.ToString());
        long version = ExtraerNumero(_cbVersion.SelectedItem?.ToString());
        if (numero <= 0 || version <= 0)
        {
            ActualizarEstado("Elegí un presupuesto y una versión antes de generar el ZIP.", esError: true);
            return;
        }

        await EjecutarConEstado("Generando el ZIP...", async () =>
        {
            string ruta = await Task.Run(() =>
            {
                var datos = _dbFacade.ObtenerDatosParaExportar(numero, version);
                return EmpaquetadorPaf.CrearZip(numero, version, datos, _carpetaDestino);
            });

            ActualizarEstado($"Listo: {Path.GetFileName(ruta)}");
            if (MessageBox.Show(this, $"ZIP generado en:\n{ruta}\n\n¿Abrir la carpeta?", "Exportador Persycom",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                Process.Start("explorer.exe", $"/select,\"{ruta}\"");
            }
        });
    }

    private async Task EjecutarConEstado(string mensajeEnCurso, Func<Task> accion)
    {
        _btnGenerar.Enabled = false;
        _progreso.Visible = true;
        ActualizarEstado(mensajeEnCurso);
        try
        {
            await accion();
        }
        catch (Exception ex)
        {
            ActualizarEstado("No se ha podido completar la operación: " + ex.Message, esError: true);
        }
        finally
        {
            _progreso.Visible = false;
            _btnGenerar.Enabled = true;
        }
    }

    private void ActualizarEstado(string mensaje, bool esError = false)
    {
        _lblEstado.Text = mensaje;
        _lblEstado.ForeColor = esError ? RojoPersycomOscuro : GrisTexto;
    }

    private static long ExtraerNumero(string? seleccion)
    {
        if (string.IsNullOrEmpty(seleccion)) return -1;
        string parteNumero = seleccion.Contains(" - ") ? seleccion[..seleccion.IndexOf(" - ")] : seleccion;
        return long.TryParse(parteNumero, out long numero) ? numero : -1;
    }
}
