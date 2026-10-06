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

    private readonly ComboBox _cbPresupuesto = new() { DropDownStyle = ComboBoxStyle.DropDown, AutoCompleteMode = AutoCompleteMode.None };
    private readonly ComboBox _cbVersion = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _txtReferencia = new() { ReadOnly = true };
    private readonly TextBox _txtCarpeta = new() { ReadOnly = true };
    private readonly Button _btnCarpeta = new() { Text = "..." };
    private readonly Button _btnGenerar = new() { Text = "Generar ZIP" };
    private readonly ProgressBar _progreso = new() { Style = ProgressBarStyle.Marquee, Visible = false };
    private readonly Label _lblEstado = new() { AutoSize = false, ForeColor = GrisTexto };

    // Debounce del buscador de presupuestos: evita una consulta por cada tecla pulsada.
    private readonly System.Windows.Forms.Timer _debounceBusqueda = new() { Interval = 300 };

    private string _carpetaDestino = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

    public FormPrincipal()
    {
        Text = "Exportador Persycom";
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Color.White;
        ClientSize = new Size(480, 460);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Icon = CargarIconoEmbebido();

        ConstruirLayout();

        Load += async (_, _) => await CargarPresupuestosAsync();
        _cbPresupuesto.SelectedIndexChanged += async (_, _) => await CargarVersionesAsync();
        _cbPresupuesto.TextChanged += (_, _) => ReiniciarDebounceBusqueda();
        _debounceBusqueda.Tick += async (_, _) => await EjecutarBusquedaAsync();
        _cbVersion.SelectedIndexChanged += async (_, _) => await CargarReferenciaAsync();
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

        cuerpo.Controls.Add(NuevaEtiqueta("Referencia"));
        _txtReferencia.Dock = DockStyle.Top;
        cuerpo.Controls.Add(_txtReferencia);
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

    // Igual que CargarLogoEmbebido: evita depender de que Recursos/ viaje al lado del
    // .exe publicado (dotnet publish -r win-x64 --self-contained no copia ese directorio
    // al single-file, solo al build normal), para que el unico fichero distribuido baste.
    private static Icon? CargarIconoEmbebido()
    {
        var asm = Assembly.GetExecutingAssembly();
        string? recurso = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("persycom.ico"));
        if (recurso is null) return null;
        using var stream = asm.GetManifestResourceStream(recurso);
        return stream is null ? null : new Icon(new Icon(stream), 32, 32);
    }

    private async Task CargarPresupuestosAsync()
    {
        await EjecutarConEstado("Cargando presupuestos...", async () =>
        {
            var claves = await Task.Run(() => _dbFacade.ObtenerNumerosDisponibles());
            RepoblarPresupuestos(claves);
        });
    }

    private void ReiniciarDebounceBusqueda()
    {
        // Elegir un item de la lista (click, o flechas+Enter) tambien dispara TextChanged,
        // porque el combo pone Text = la etiqueta del item elegido; en ese caso SelectedIndex
        // ya no es -1. Sin este filtro, 300ms despues de un acierto se relanzaria la busqueda
        // con la etiqueta completa como patron, sin coincidencias, vaciando la lista.
        if (_cbPresupuesto.SelectedIndex != -1) return;

        _debounceBusqueda.Stop();
        _debounceBusqueda.Start();
    }

    private async Task EjecutarBusquedaAsync()
    {
        _debounceBusqueda.Stop();
        string texto = _cbPresupuesto.Text.Trim();
        if (texto.Length == 0)
        {
            await CargarPresupuestosAsync();
            return;
        }

        await EjecutarConEstado("Buscando...", async () =>
        {
            var claves = await Task.Run(() => _dbFacade.BuscarPresupuestos(texto));
            RepoblarPresupuestos(claves, conservarTexto: true);
            ActualizarEstado(claves.Count == 0 ? "Sin resultados para esa búsqueda." : "");
            if (claves.Count > 0) _cbPresupuesto.DroppedDown = true;
        });
    }

    private void RepoblarPresupuestos(List<string> claves, bool conservarTexto = false)
    {
        string textoActual = _cbPresupuesto.Text;
        _cbPresupuesto.Items.Clear();
        _cbPresupuesto.Items.AddRange(claves.Cast<object>().ToArray());
        if (conservarTexto)
        {
            _cbPresupuesto.Text = textoActual;
            _cbPresupuesto.SelectionStart = textoActual.Length;
        }
    }

    private async Task CargarVersionesAsync()
    {
        long numero = ExtraerNumero(_cbPresupuesto.Text);
        if (numero <= 0) return;

        await EjecutarConEstado("Cargando versiones...", async () =>
        {
            var versiones = await Task.Run(() => _dbFacade.ObtenerVersiones(numero));
            _cbVersion.DataSource = versiones;
        });
    }

    private async Task CargarReferenciaAsync()
    {
        long numero = ExtraerNumero(_cbPresupuesto.Text);
        long version = ExtraerNumero(_cbVersion.SelectedItem?.ToString());
        if (numero <= 0 || version <= 0)
        {
            _txtReferencia.Text = "";
            return;
        }

        await EjecutarConEstado("Cargando referencia...", async () =>
        {
            string? referencia = await Task.Run(() => _dbFacade.ObtenerReferencia(numero, version));
            _txtReferencia.Text = referencia ?? "";
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
        long numero = ExtraerNumero(_cbPresupuesto.Text);
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
