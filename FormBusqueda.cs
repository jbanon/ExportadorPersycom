using System.Diagnostics;
using ExportadorPersycom.Database;
using ExportadorPersycom.Database.Model;
using ExportadorPersycom.Zip;
using static ExportadorPersycom.RecursosEmbebidos;

namespace ExportadorPersycom;

// BUSQUEDA AVANZADA (tarea 0001). Pantalla ADICIONAL a FormPrincipal, que sigue igual.
//
// Tres bloques de arriba abajo, en el orden en que se usan: CONEXION (ODBC con DSN, o SQL
// Server directo), BUSQUEDA (un cuadro por campo; los rellenos se combinan con AND: cada campo
// acota) y RESULTADOS (un grid, una fila por presupuesto+version; elegir una fila y generar su
// ZIP con el mismo empaquetado de siempre).
//
// La contrasena de SQL Server vive en el cuadro de texto y en la ConfiguracionConexion en
// memoria mientras la pantalla esta abierta; no se guarda en ningun sitio. El resto de la
// conexion (modo, DSN, servidor, puerto, base, usuario) se recuerda en %AppData%.
public class FormBusqueda : Form
{
    private DbFacade? _db;

    // Conexion
    private readonly RadioButton _rbOdbc = new() { Text = "ODBC (DSN del sistema)", AutoSize = true, Checked = true };
    private readonly RadioButton _rbSql = new() { Text = "SQL Server (conexión manual)", AutoSize = true };
    private readonly TextBox _txtDsn = new();
    private readonly TextBox _txtServidor = new();
    private readonly TextBox _txtPuerto = new() { Width = 70 };
    private readonly TextBox _txtBase = new();
    private readonly TextBox _txtUsuario = new();
    private readonly TextBox _txtContrasena = new() { UseSystemPasswordChar = true };
    private readonly CheckBox _chkCertificado = new() { Text = "Exigir certificado válido (cifrado estricto)", AutoSize = true };
    private readonly Button _btnProbar = new() { Text = "Probar conexión" };
    private readonly Label _lblConexion = new() { AutoSize = false, Height = 20, ForeColor = GrisApagado };
    private readonly Panel _panelOdbc = new() { Dock = DockStyle.Top, Height = 32 };
    private readonly Panel _panelSql = new() { Dock = DockStyle.Top, Height = 96 };

    // Busqueda
    private readonly TextBox _txtNumero = new();
    private readonly TextBox _txtPedido = new();
    private readonly TextBox _txtCliente = new();
    private readonly TextBox _txtObra = new();
    private readonly Button _btnBuscar = new() { Text = "Buscar" };
    private readonly Button _btnLimpiar = new() { Text = "Limpiar" };
    private readonly System.Windows.Forms.Timer _debounce = new() { Interval = 400 };

    // Resultados
    private readonly DataGridView _grid = new();
    private readonly Label _lblResultados = new() { AutoSize = true, ForeColor = GrisApagado };
    private readonly TextBox _txtCarpeta = new() { ReadOnly = true };
    private readonly Button _btnCarpeta = new() { Text = "..." };
    private readonly Button _btnGenerar = new() { Text = "Generar ZIP de la fila elegida" };
    private readonly ProgressBar _progreso = new() { Style = ProgressBarStyle.Marquee, Visible = false };
    private readonly Label _lblEstado = new() { AutoSize = false, ForeColor = GrisTexto };

    private string _carpetaDestino = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    private List<ResultadoBusqueda> _resultados = new();

    public FormBusqueda()
    {
        Text = "Exportador Persycom · Búsqueda avanzada";
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Color.White;
        ClientSize = new Size(980, 720);
        MinimumSize = new Size(820, 600);
        StartPosition = FormStartPosition.CenterParent;
        Icon = Icono();

        ConstruirLayout();
        CargarPreferencias();
        _txtCarpeta.Text = _carpetaDestino;

        _rbOdbc.CheckedChanged += (_, _) => CambioDeModo();
        _rbSql.CheckedChanged += (_, _) => CambioDeModo();
        _btnProbar.Click += async (_, _) => await ProbarConexionAsync();
        foreach (var t in new[] { _txtNumero, _txtPedido, _txtCliente, _txtObra })
        {
            t.TextChanged += (_, _) => { _debounce.Stop(); _debounce.Start(); };
            t.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await BuscarAsync(); } };
        }
        // Cambiar la conexion invalida la busqueda y la conexion comprobada.
        foreach (var t in new[] { _txtDsn, _txtServidor, _txtPuerto, _txtBase, _txtUsuario, _txtContrasena })
            t.TextChanged += (_, _) => ConexionCambiada();
        _chkCertificado.CheckedChanged += (_, _) => ConexionCambiada();
        _debounce.Tick += async (_, _) => await BuscarAsync();
        _btnBuscar.Click += async (_, _) => await BuscarAsync();
        _btnLimpiar.Click += (_, _) => Limpiar();
        _btnCarpeta.Click += (_, _) => ElegirCarpeta();
        _btnGenerar.Click += async (_, _) => await GenerarZipAsync();
        _grid.CellDoubleClick += async (_, e) => { if (e.RowIndex >= 0) await GenerarZipAsync(); };
        _grid.SelectionChanged += (_, _) => _btnGenerar.Enabled = _grid.SelectedRows.Count > 0 && !_progreso.Visible;
        FormClosed += (_, _) => GuardarPreferencias();

        CambioDeModo();
        ActualizarEstado("Comprueba la conexión y busca. Sin ningún campo relleno se listan todos los presupuestos.");
    }

    // ── Layout ────────────────────────────────────────────────────────────────

    private void ConstruirLayout()
    {
        var cabecera = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = Color.White };
        var logo = new PictureBox { Image = Logo(), SizeMode = PictureBoxSizeMode.Zoom, Bounds = new Rectangle(20, 10, 190, 52) };
        var titulo = new Label
        {
            Text = "Búsqueda avanzada", AutoSize = true, ForeColor = GrisTexto,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold), Location = new Point(230, 24),
        };
        var barraRoja = new Panel { Dock = DockStyle.Bottom, Height = 4, BackColor = RojoPersycom };
        cabecera.Controls.Add(logo);
        cabecera.Controls.Add(titulo);
        cabecera.Controls.Add(barraRoja);

        var cuerpo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(20, 12, 20, 12) };
        cuerpo.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // conexion
        cuerpo.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // busqueda
        cuerpo.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // resultados
        cuerpo.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // pie
        cuerpo.Controls.Add(BloqueConexion(), 0, 0);
        cuerpo.Controls.Add(BloqueBusqueda(), 0, 1);
        cuerpo.Controls.Add(BloqueResultados(), 0, 2);
        cuerpo.Controls.Add(BloquePie(), 0, 3);

        Controls.Add(cuerpo);
        Controls.Add(cabecera);
    }

    private Control BloqueConexion()
    {
        var bloque = Bloque("1 · Conexión a la base de datos");

        var modos = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 28, WrapContents = false };
        _rbSql.Margin = new Padding(24, 3, 0, 0);
        _rbOdbc.Margin = new Padding(0, 3, 0, 0);
        modos.Controls.Add(_rbOdbc);
        modos.Controls.Add(_rbSql);

        // ODBC: una sola fila.
        var filaDsn = Fila(("DSN", _txtDsn, 220));
        filaDsn.Dock = DockStyle.Fill;
        _panelOdbc.Controls.Add(filaDsn);

        // SQL Server: dos filas de campos y la casilla.
        var filaServidor = Fila(("Servidor", _txtServidor, 260), ("Puerto", _txtPuerto, 70), ("Base de datos", _txtBase, 200));
        var filaUsuario = Fila(("Usuario", _txtUsuario, 200), ("Contraseña", _txtContrasena, 200));
        _chkCertificado.Margin = new Padding(0, 4, 0, 0);
        var filaCert = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 26 };
        filaCert.Controls.Add(_chkCertificado);
        var ayuda = new Label
        {
            Text = "Usuario vacío = entrar con la cuenta de Windows. La contraseña no se guarda.",
            AutoSize = true, ForeColor = GrisApagado, Margin = new Padding(16, 7, 0, 0), Font = new Font(Font.FontFamily, 8.5f),
        };
        filaCert.Controls.Add(ayuda);
        _panelSql.Controls.Add(filaCert);
        _panelSql.Controls.Add(filaUsuario);
        _panelSql.Controls.Add(filaServidor);

        var accion = new TableLayoutPanel { Dock = DockStyle.Top, Height = 34, ColumnCount = 2 };
        accion.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        accion.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        EstiloSecundario(_btnProbar);
        _btnProbar.Dock = DockStyle.Fill;
        _btnProbar.Margin = new Padding(0, 4, 10, 2);
        _lblConexion.Dock = DockStyle.Fill;
        _lblConexion.TextAlign = ContentAlignment.MiddleLeft;
        accion.Controls.Add(_btnProbar, 0, 0);
        accion.Controls.Add(_lblConexion, 1, 0);

        // Orden de apilado Top: el ultimo añadido queda arriba.
        bloque.Controls.Add(accion);
        bloque.Controls.Add(_panelSql);
        bloque.Controls.Add(_panelOdbc);
        bloque.Controls.Add(modos);
        return bloque;
    }

    private Control BloqueBusqueda()
    {
        var bloque = Bloque("2 · Búsqueda (los campos rellenos se combinan: cada uno acota más)");

        var rejilla = new TableLayoutPanel { Dock = DockStyle.Top, Height = 62, ColumnCount = 4 };
        for (int i = 0; i < 4; i++) rejilla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        rejilla.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        rejilla.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        var campos = new (string, TextBox)[] { ("Presupuesto", _txtNumero), ("Pedido de compras", _txtPedido), ("Cliente", _txtCliente), ("Referencia de obra", _txtObra) };
        for (int i = 0; i < campos.Length; i++)
        {
            var (etiqueta, caja) = campos[i];
            rejilla.Controls.Add(Etiqueta(etiqueta), i, 0);
            caja.Dock = DockStyle.Fill;
            caja.Margin = new Padding(0, 0, i == 3 ? 0 : 12, 0);
            rejilla.Controls.Add(caja, i, 1);
        }

        var botones = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, FlowDirection = FlowDirection.LeftToRight };
        EstiloPrimario(_btnBuscar);
        _btnBuscar.Size = new Size(120, 30);
        _btnBuscar.Margin = new Padding(0, 6, 8, 0);
        EstiloSecundario(_btnLimpiar);
        _btnLimpiar.Size = new Size(90, 30);
        _btnLimpiar.Margin = new Padding(0, 6, 0, 0);
        botones.Controls.Add(_btnBuscar);
        botones.Controls.Add(_btnLimpiar);

        bloque.Controls.Add(botones);
        bloque.Controls.Add(rejilla);
        return bloque;
    }

    private Control BloqueResultados()
    {
        var bloque = Bloque("3 · Resultados", relleno: true);

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.RowHeadersVisible = false;
        _grid.MultiSelect = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoGenerateColumns = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.BackgroundColor = Color.White;
        _grid.BorderStyle = BorderStyle.FixedSingle;
        _grid.GridColor = GrisLinea;
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = GrisSuave;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = GrisTexto;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
        _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = GrisSuave;
        _grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        _grid.ColumnHeadersHeight = 32;
        _grid.RowTemplate.Height = 28;
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0xFC, 0xE4, 0xE6);
        _grid.DefaultCellStyle.SelectionForeColor = GrisTexto;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(0xFA, 0xFA, 0xFB);
        _grid.Columns.AddRange(
            Columna("Numero", "Presupuesto", 11, DataGridViewContentAlignment.MiddleRight),
            Columna("Version", "Versión", 8, DataGridViewContentAlignment.MiddleRight),
            Columna("NombreVersion", "Nombre de versión", 16),
            Columna("Cliente", "Cliente", 24),
            Columna("PedidoCompras", "Pedido de compras", 13),
            Columna("ReferenciaObra", "Referencia de obra", 16),
            Columna("Referencia", "Referencia", 12));

        var pieGrid = new Panel { Dock = DockStyle.Bottom, Height = 22 };
        _lblResultados.Location = new Point(0, 4);
        pieGrid.Controls.Add(_lblResultados);

        bloque.Controls.Add(_grid);
        bloque.Controls.Add(pieGrid);
        return bloque;
    }

    private Control BloquePie()
    {
        var pie = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3, Margin = new Padding(0) };
        pie.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pie.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));
        pie.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        pie.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        pie.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        pie.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
        pie.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        pie.Controls.Add(Etiqueta("Carpeta destino del ZIP"), 0, 0);
        _txtCarpeta.Dock = DockStyle.Fill;
        _txtCarpeta.Margin = new Padding(0, 4, 4, 4);
        _btnCarpeta.Dock = DockStyle.Fill;
        _btnCarpeta.Margin = new Padding(0, 4, 8, 4);
        EstiloPrimario(_btnGenerar);
        _btnGenerar.Dock = DockStyle.Fill;
        _btnGenerar.Margin = new Padding(0, 2, 0, 2);
        _btnGenerar.Enabled = false;
        pie.Controls.Add(_txtCarpeta, 0, 1);
        pie.Controls.Add(_btnCarpeta, 1, 1);
        pie.Controls.Add(_btnGenerar, 2, 1);

        _progreso.Dock = DockStyle.Fill;
        _progreso.Margin = new Padding(0);
        pie.Controls.Add(_progreso, 0, 2);
        pie.SetColumnSpan(_progreso, 3);

        _lblEstado.Dock = DockStyle.Fill;
        _lblEstado.Margin = new Padding(0, 6, 0, 0);
        pie.Controls.Add(_lblEstado, 0, 3);
        pie.SetColumnSpan(_lblEstado, 3);
        return pie;
    }

    // Un bloque con titulo pequeño en mayusculas y filete: la pantalla se lee en tres pasos.
    private Panel Bloque(string titulo, bool relleno = false)
    {
        var panel = new Panel { Dock = relleno ? DockStyle.Fill : DockStyle.Top, AutoSize = !relleno, Padding = new Padding(0, 0, 0, 10), Margin = new Padding(0, 0, 0, 6) };
        var cab = new Label
        {
            Text = titulo.ToUpperInvariant(), Dock = DockStyle.Top, Height = 26, AutoSize = false,
            ForeColor = RojoPersycomOscuro, Font = new Font(Font.FontFamily, 8.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(0, 0, 0, 4),
        };
        var filete = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = GrisLinea, Margin = new Padding(0) };
        // Dock apila al reves: el ULTIMO de la coleccion queda arriba. Para que el titulo y el
        // filete esten siempre arriba, cada vez que el llamador añade un hijo se recolocan al final.
        // Asi el llamador añade sus hijos en orden de abajo hacia arriba, como con cualquier Dock=Top.
        panel.Controls.Add(filete);
        panel.Controls.Add(cab);
        panel.ControlAdded += (_, e) =>
        {
            if (e.Control == cab || e.Control == filete) return;
            panel.Controls.SetChildIndex(filete, panel.Controls.Count - 1);
            panel.Controls.SetChildIndex(cab, panel.Controls.Count - 1);
        };
        return panel;
    }

    private static TableLayoutPanel Fila(params (string Etiqueta, TextBox Caja, int Ancho)[] campos)
    {
        var fila = new TableLayoutPanel { Dock = DockStyle.Top, Height = 32, ColumnCount = campos.Length * 2 + 1, AutoSize = false };
        for (int i = 0; i < campos.Length; i++)
        {
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, campos[i].Ancho + 16));
            var et = Etiqueta(campos[i].Etiqueta);
            et.Dock = DockStyle.None;
            et.AutoSize = true;
            et.Margin = new Padding(i == 0 ? 0 : 14, 8, 6, 0);
            fila.Controls.Add(et, i * 2, 0);
            campos[i].Caja.Width = campos[i].Ancho;
            campos[i].Caja.Margin = new Padding(0, 4, 0, 0);
            fila.Controls.Add(campos[i].Caja, i * 2 + 1, 0);
        }
        fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return fila;
    }

    private static Label Etiqueta(string texto) =>
        new() { Text = texto, Dock = DockStyle.Top, AutoSize = false, Height = 22, ForeColor = GrisTexto, TextAlign = ContentAlignment.BottomLeft };

    private static DataGridViewTextBoxColumn Columna(string propiedad, string titulo, float peso, DataGridViewContentAlignment alineacion = DataGridViewContentAlignment.MiddleLeft) =>
        new() { DataPropertyName = propiedad, HeaderText = titulo, FillWeight = peso, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = { Alignment = alineacion } };

    private void EstiloPrimario(Button b)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.BackColor = RojoPersycom;
        b.ForeColor = Color.White;
        b.Font = new Font(Font, FontStyle.Bold);
        b.MouseEnter += (_, _) => b.BackColor = RojoPersycomOscuro;
        b.MouseLeave += (_, _) => b.BackColor = RojoPersycom;
        b.EnabledChanged += (_, _) => b.BackColor = b.Enabled ? RojoPersycom : Color.FromArgb(0xD1, 0xD5, 0xDB);
    }

    private void EstiloSecundario(Button b)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderColor = GrisLinea;
        b.BackColor = GrisSuave;
        b.ForeColor = GrisTexto;
    }

    // ── Conexion ──────────────────────────────────────────────────────────────

    private void CambioDeModo()
    {
        bool sql = _rbSql.Checked;
        _panelOdbc.Visible = !sql;
        _panelSql.Visible = sql;
        ConexionCambiada();
    }

    /// <summary>Cualquier cambio en los datos de conexion: la comprobacion anterior ya no vale y la facade se rehace al usarla.</summary>
    private void ConexionCambiada()
    {
        _db = null;
        _lblConexion.Text = "Sin comprobar";
        _lblConexion.ForeColor = GrisApagado;
    }

    private ConfiguracionConexion LeerConfiguracion() => new()
    {
        Modo = _rbSql.Checked ? ModoConexion.SqlServer : ModoConexion.Odbc,
        Dsn = _txtDsn.Text.Trim(),
        Servidor = _txtServidor.Text.Trim(),
        Puerto = int.TryParse(_txtPuerto.Text.Trim(), out int p) && p > 0 ? p : null,
        BaseDatos = _txtBase.Text.Trim(),
        Usuario = _txtUsuario.Text.Trim(),
        Contrasena = _txtContrasena.Text,
        ExigirCertificadoValido = _chkCertificado.Checked,
    };

    /// <summary>La facade con la configuracion actual, o null (con el error ya en pantalla) si falta algo.</summary>
    private DbFacade? Facade()
    {
        if (_db != null) return _db;
        var config = LeerConfiguracion();
        if (config.Validar() is string error)
        {
            ActualizarEstado(error, esError: true);
            return null;
        }
        return _db = new DbFacade(config);
    }

    private async Task ProbarConexionAsync()
    {
        var db = Facade();
        if (db == null) return;
        await EjecutarConEstado("Conectando...", async () =>
        {
            string servidor = await Task.Run(db.ProbarConexion);
            _lblConexion.Text = $"Conectado · {servidor}";
            _lblConexion.ForeColor = VerdeOk;
            ActualizarEstado($"Conexión correcta: {db.Conexion.Describir()}.");
            GuardarPreferencias();
        }, alFallar: () => { _lblConexion.Text = "No conecta"; _lblConexion.ForeColor = RojoPersycomOscuro; });
    }

    private void CargarPreferencias()
    {
        var p = PreferenciasConexion.Cargar();
        _rbSql.Checked = p.Modo == ModoConexion.SqlServer;
        _rbOdbc.Checked = !_rbSql.Checked;
        _txtDsn.Text = p.Dsn;
        _txtServidor.Text = p.Servidor;
        _txtPuerto.Text = p.Puerto?.ToString() ?? "";
        _txtBase.Text = p.BaseDatos;
        _txtUsuario.Text = p.Usuario;
        _chkCertificado.Checked = p.ExigirCertificadoValido;
    }

    private void GuardarPreferencias() => PreferenciasConexion.De(LeerConfiguracion()).Guardar();

    // ── Busqueda ──────────────────────────────────────────────────────────────

    private async Task BuscarAsync()
    {
        _debounce.Stop();
        var db = Facade();
        if (db == null) return;
        string numero = _txtNumero.Text, pedido = _txtPedido.Text, cliente = _txtCliente.Text, obra = _txtObra.Text;
        await EjecutarConEstado("Buscando...", async () =>
        {
            _resultados = await Task.Run(() => db.BuscarAvanzado(numero, pedido, cliente, obra));
            _grid.DataSource = _resultados;
            _grid.ClearSelection();
            bool filtrado = new[] { numero, pedido, cliente, obra }.Any(t => !string.IsNullOrWhiteSpace(t));
            _lblResultados.Text = _resultados.Count switch
            {
                0 => filtrado ? "Ningún presupuesto cumple todos los campos rellenos." : "La base no tiene presupuestos.",
                1 => "1 presupuesto",
                var n => $"{n} presupuestos (una fila por versión)",
            };
            ActualizarEstado(_resultados.Count == 0 ? "" : "Elige una fila y genera el ZIP (o doble clic en la fila).");
            if (_lblConexion.ForeColor != VerdeOk) { _lblConexion.Text = "Conectado"; _lblConexion.ForeColor = VerdeOk; GuardarPreferencias(); }
        });
    }

    private void Limpiar()
    {
        _debounce.Stop();
        foreach (var t in new[] { _txtNumero, _txtPedido, _txtCliente, _txtObra }) t.Text = "";
        _grid.DataSource = null;
        _resultados = new();
        _lblResultados.Text = "";
        ActualizarEstado("");
        _txtNumero.Focus();
    }

    // ── ZIP ───────────────────────────────────────────────────────────────────

    private ResultadoBusqueda? FilaElegida() =>
        _grid.SelectedRows.Count > 0 ? _grid.SelectedRows[0].DataBoundItem as ResultadoBusqueda : null;

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
        var fila = FilaElegida();
        if (fila == null)
        {
            ActualizarEstado("Elige una fila de los resultados antes de generar el ZIP.", esError: true);
            return;
        }
        var db = Facade();
        if (db == null) return;

        await EjecutarConEstado($"Generando el ZIP del presupuesto {fila.Numero} versión {fila.Version}...", async () =>
        {
            string ruta = await Task.Run(() =>
            {
                var datos = db.ObtenerDatosParaExportar(fila.Numero, fila.Version);
                if (datos.Count == 0) throw new InvalidOperationException("Ese presupuesto y versión no tienen líneas que exportar.");
                return EmpaquetadorPaf.CrearZip(fila.Numero, fila.Version, datos, _carpetaDestino);
            });

            ActualizarEstado($"Listo: {Path.GetFileName(ruta)}");
            if (MessageBox.Show(this, $"ZIP generado en:\n{ruta}\n\n¿Abrir la carpeta?", "Exportador Persycom",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                Process.Start("explorer.exe", $"/select,\"{ruta}\"");
            }
        });
    }

    // ── Estado ────────────────────────────────────────────────────────────────

    private async Task EjecutarConEstado(string mensajeEnCurso, Func<Task> accion, Action? alFallar = null)
    {
        _btnGenerar.Enabled = false;
        _btnBuscar.Enabled = false;
        _btnProbar.Enabled = false;
        _progreso.Visible = true;
        ActualizarEstado(mensajeEnCurso);
        try
        {
            await accion();
        }
        catch (Exception ex)
        {
            alFallar?.Invoke();
            ActualizarEstado("No se ha podido completar la operación: " + ex.Message, esError: true);
        }
        finally
        {
            _progreso.Visible = false;
            _btnBuscar.Enabled = true;
            _btnProbar.Enabled = true;
            _btnGenerar.Enabled = _grid.SelectedRows.Count > 0;
        }
    }

    private void ActualizarEstado(string mensaje, bool esError = false)
    {
        _lblEstado.Text = mensaje;
        _lblEstado.ForeColor = esError ? RojoPersycomOscuro : GrisTexto;
    }
}
