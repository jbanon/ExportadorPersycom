# 0001 — Resultado: pantalla de búsqueda con conexión ODBC o SQL Server manual

Fecha: 06-10-2026. Sin commit ni push (pendiente de la orden del arquitecto). Rama `main`.
`dotnet build` 0 errores / 0 avisos. `dotnet publish -c Release -r win-x64 --self-contained` correcto:
un único `ExportadorPersycom.exe` de 72 MB más el `.dll.config` de siempre (sin ficheros nuevos al
lado del exe; no se ha añadido nada que se cargue desde disco).

## Decisiones (confirmadas con el arquitecto en el hilo, 06-10-2026)

- **AND al combinar cuadros**: cada campo relleno acota. OR es lo que ya hace el buscador único de
  `FormPrincipal`, que sigue intacto. El rótulo del bloque de búsqueda lo dice en pantalla.
- **Marcadores `?` (ODBC) vs `@nombre` (SqlClient)**: el SQL se escribe UNA vez con `@nombre`.
  `Database/ConsultaParametrizada.Crear(conn, sql, parametros)` lo adapta: con `OdbcConnection`
  sustituye cada `@nombre` por `?` en orden de aparición y añade un parámetro por APARICIÓN (si
  `@texto` sale cuatro veces, cuatro parámetros con el mismo valor, que es como ODBC los casa);
  con cualquier otro proveedor manda el texto tal cual y añade los parámetros por nombre, una
  vez cada uno. Ninguna consulta de `DbFacade` existe duplicada. Probado: la rama ODBC genera
  `... LIKE UPPER(?) OR b LIKE ? AND c = ?` con tres parámetros, y la rama SqlClient devuelve los
  valores correctos en vivo.
- **Cifrado SQL Server**: cifrado opcional y confiar en el certificado del servidor por defecto
  (mismo nivel que el ODBC actual); casilla "Exigir certificado válido" para subirlo.
- **Preferencias**: modo, DSN, servidor, puerto, base, usuario y la casilla se recuerdan en
  `%AppData%\ExportadorPersycom\conexion.json`. **La contraseña no se guarda en ningún sitio**: vive
  en el cuadro de texto mientras la pantalla está abierta.
- Usuario vacío en modo SQL Server = autenticación integrada de Windows (lo dice la ayuda en pantalla).
- Paquete: `Microsoft.Data.SqlClient` 6.1.4 (el mantenido; estaba en la caché de NuGet del servidor).

## Qué se hizo

1. **Capa de conexión con dos proveedores.** `Database/ConfiguracionConexion.cs` (modo ODBC o
   SQL Server, campos, `Validar()`, `Describir()` sin contraseña, `Abrir()` que crea
   `OdbcConnection` o `SqlConnection` según el modo; `OdbcPorDefecto()` lee el DSN de App.config
   como hasta ahora). `DbConnectionFactory` queda como paso único a `configuracion.Abrir()`.
2. **`DbFacade` sobre `System.Data.Common`.** Recibe la `ConfiguracionConexion` por constructor
   (el constructor sin parámetros sigue siendo ODBC por defecto). Todos los métodos existentes
   igual, sobre `DbCommand`. `BuscarPresupuestos` pasa a `@texto` por `ConsultaParametrizada`
   (misma semántica OR). Nuevos: `ProbarConexion()` y `BuscarAvanzado(numero, pedido, cliente,
   obra)`: `SELECT DISTINCT` de cabecera (Numero, Version, NombreVersion, Cliente, NumeroPedido,
   Obra, Referencia), una condición `LIKE` por campo relleno unidas con AND, comodines escapados,
   orden `Numero DESC, Version DESC`; sin campos devuelve todo. Modelo nuevo
   `Database/Model/ResultadoBusqueda.cs`.
3. **Pantalla nueva `FormBusqueda.cs`**, Form aparte abierto desde el botón secundario "Búsqueda
   avanzada..." de `FormPrincipal` (una sola instancia, no modal, para tener las dos a la vista).
   Tres bloques numerados de arriba abajo, en el orden de uso: **1 Conexión** (radios ODBC / SQL
   Server; DSN editable, o servidor, puerto, base, usuario, contraseña con máscara y la casilla de
   certificado; botón "Probar conexión" con estado Conectado / No conecta; cualquier cambio en los
   datos invalida la comprobación), **2 Búsqueda** (cuatro cuadros con etiqueta en una fila:
   Presupuesto, Pedido de compras, Cliente, Referencia de obra; búsqueda en vivo con 400 ms de
   retardo, Enter inmediato, botones Buscar y Limpiar), **3 Resultados** (`DataGridView` de solo
   lectura, fila completa, cabecera gris, filas alternas, columnas Presupuesto, Versión, Nombre de
   versión, Cliente, Pedido de compras, Referencia de obra, Referencia; contador de filas). Pie:
   carpeta destino, botón rojo "Generar ZIP de la fila elegida" (también doble clic en la fila),
   barra de progreso y estado, con el mismo `EjecutarConEstado` que la principal. Reutiliza
   `DbFacade.ObtenerDatosParaExportar` + `EmpaquetadorPaf.CrearZip` sin tocarlos. Redimensionable
   (980×720, mínimo 820×600). Paleta y estilo de `FormPrincipal` (rojo Persycom, grises).
4. **`RecursosEmbebidos.cs`**: logo, icono y colores compartidos por las dos pantallas (antes
   privados de `FormPrincipal`); `FormPrincipal` pasa a usarlos. Nada se carga desde disco.
5. **`FormPrincipal`**: sin cambios de comportamiento. Construye su `DbFacade` con
   `ConfiguracionConexion.OdbcPorDefecto()`; botón nuevo bajo "Generar ZIP"; ventana 40 px más
   alta para que quepa.

## Ficheros

- Nuevos: `Database/ConfiguracionConexion.cs`, `Database/ConsultaParametrizada.cs`,
  `Database/PreferenciasConexion.cs`, `Database/Model/ResultadoBusqueda.cs`, `FormBusqueda.cs`,
  `RecursosEmbebidos.cs`.
- Tocados: `Database/DbFacade.cs`, `Database/DbConnectionFactory.cs`, `FormPrincipal.cs`,
  `ExportadorPersycom.csproj` (paquete `Microsoft.Data.SqlClient`).
- Sin tocar: `Zip/EmpaquetadorPaf.cs`, `Database/Model/DatosPaf.cs`,
  `Database/ConsultaZzRolapDatosPaf.cs`, `App.config`, esquema del cliente.

## Verificación

- **SqlClient en vivo** contra `localhost,1433` / `Alugal_TNeta25` con el login de solo lectura
  que ya uso para dev del Presupuestador (autorizado por el arquitecto), mediante un harness de
  consola temporal que compiló la capa `Database/` y `Zip/` REAL del proyecto (sin WinForms; ya
  borrado): `ProbarConexion` (SQL Server 16.00.4255), `BuscarAvanzado` sin filtros (1 fila:
  presupuesto 1 v1, Profine Iberia), por número (1), por cliente (1), AND con cliente inexistente
  (0), comodines `%[_` literales (0, sin error), `BuscarPresupuestos` (1), `ObtenerVersiones`,
  `ObtenerReferencia`.
- `ObtenerDatosParaExportar` por SqlClient falla en ESTE contenedor con "Execution of user code in
  the .NET Framework is disabled. Enable clr enabled": `Zlib.unzipxml` es una función SQLCLR y el
  servidor de pruebas tiene el CLR apagado. No es un fallo del código (la consulta es la misma de
  siempre, que funciona en el cliente por ODBC); si queréis probar la exportación completa desde
  aquí, hay que activar `clr enabled` en ese SQL Server, decisión del arquitecto/gerente.
- **Camino ODBC: sin probar en vivo** (no hay Windows ni DSN aquí). Lo único comprobado es la
  transformación de marcadores de la rama ODBC. La pantalla WinForms tampoco se ha ejecutado:
  queda la prueba visual en Windows (maquetación, tamaños, comportamiento del grid).
- La base tiene una sola fila en `PAF`: las consultas no fallan, pero no se ha podido ver el grid
  con volumen.

## Fuera de alcance / notas

- No se ha cambiado el `.exe` respecto al bloqueo de Control Inteligente de Aplicaciones (SEG-1).
- `App.config` sigue igual; el DSN por defecto se lee de ahí también en la pantalla nueva.
- Si en Windows el grid se ve apretado con pantallas pequeñas, el formulario es redimensionable y
  las columnas reparten por peso; no se ha podido ajustar a ojo.
