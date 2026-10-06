# 0001 — Resultado: pantalla de búsqueda con conexión ODBC o SQL Server manual

Fecha: 06-10-2026. Rama `main`. Dos entregas el mismo día:

1. **Commit `ecb2e7a`** — lo que pedía la spec: pantalla ADICIONAL (`FormBusqueda`) abierta desde
   un botón de la pantalla antigua, que seguía intacta.
2. **Commit siguiente (este informe)** — **cambio de alcance ordenado por el gerente** tras probar
   la primera entrega en Windows: la pantalla nueva pasa a ser LA pantalla de la aplicación y la
   antigua se elimina. Detalle en la sección "Cambio de alcance".

`dotnet build` 0 errores / 0 avisos. `dotnet publish -c Release -r win-x64 --self-contained`
correcto: un único `ExportadorPersycom.exe` (72 MB) más el `.dll.config` de siempre; no se ha
añadido nada que se cargue desde disco (ver trampa del icono en `.arq/trampas.md`).

## Cómo queda la aplicación (estado final, para quien la retome)

Una sola pantalla, `FormPrincipal.cs`, con tres bloques de arriba abajo en el orden de uso:

1. **Conexión a la base de datos.** Dos modos, por botón de opción:
   - **ODBC**: nombre del DSN del sistema (por defecto `PrefSuite`, el de `App.config`, editable).
     Es el camino que usa el cliente hoy.
   - **SQL Server (conexión manual)**: servidor, puerto (opcional), base de datos, usuario y
     contraseña. Usuario vacío = autenticación integrada de Windows. Casilla "Exigir certificado
     válido": apagada, se cifra si el servidor puede y se confía en su certificado (mismo nivel que
     el ODBC actual); encendida, cifrado estricto.
   - Botón **Probar conexión** con estado "Conectado · servidor · versión" o "No conecta".
   - El bloque **se pliega** con un clic en su título y se pliega solo al conectar bien; plegado,
     el título enseña el resumen (modo, servidor, base, usuario y estado). Si una búsqueda falla
     por error de base de datos, se despliega solo para que se vea dónde arreglarlo.
   - Se recuerdan entre ejecuciones modo, DSN, servidor, puerto, base, usuario y la casilla, en
     `%AppData%\ExportadorPersycom\conexion.json`. **La contraseña no se guarda nunca**: hay que
     teclearla cada vez que se abre la aplicación en modo SQL Server.
2. **Búsqueda.** Cuatro cuadros con etiqueta: Presupuesto (`Numero`), Pedido de compras
   (`NumeroPedido`), Cliente (`PAF.Nombre`) y Referencia de obra (`Obra`). Cada cuadro busca como
   subcadena sin distinguir mayúsculas. **Los cuadros rellenos se combinan con AND** (cada uno
   acota más). Sin ningún cuadro relleno se listan todos los presupuestos. Busca en vivo con 400 ms
   de retardo, con Enter al momento, y con el botón Buscar; Limpiar vacía cuadros y resultados.
3. **Resultados.** Tabla de solo lectura, una fila por presupuesto+versión, columnas Presupuesto,
   Versión, Nombre de versión, Cliente, Pedido de compras, Referencia de obra y Referencia; orden
   presupuesto descendente y versión descendente; contador de filas; texto explicativo cuando no
   hay filas. Elegida una fila, **Generar ZIP** (botón o doble clic) con la carpeta destino
   elegible (por defecto el Escritorio), igual que antes: mismo `EmpaquetadorPaf.CrearZip` y mismo
   nombre `PAF_{numero}_v{version}.zip`.

Lo que NO cambia: la consulta de datos sigue siendo la subconsulta embebida de
`ConsultaZzRolapDatosPaf` (no se crea ni toca nada en la base del cliente); el ZIP se genera
exactamente igual que antes.

## Cambio de alcance (06-10-2026, orden directa del gerente)

La spec pedía una pantalla ADICIONAL y decía: "si ves que tiene más sentido que sustituya a la
actual, PARA y pregunta". No fue el programador quien lo decidió: el gerente, tras probar la
primera entrega en su Windows, ordenó en su sesión: "La pantalla que se queda es esta. Eliminamos
la otra. La parte de configuración a la base de datos que sea ocultable. Y hacemos la tabla de
resultados más atractiva", y después: "lo de 'búsqueda avanzada' debe desaparecer también". El
arquitecto fue informado del cambio y revisó el diff antes del commit.

**Qué se quitó y por qué no se pierde nada:**

- La pantalla antigua (`FormPrincipal.cs` original: combo con filtro de texto libre en OR, lista
  de versiones, referencia, carpeta y Generar ZIP). Todo lo que hacía lo hace la nueva: el filtro
  libre en OR se sustituye por los cuatro cuadros en AND (quien quiera "lo de antes" rellena solo
  un cuadro); versiones y referencia salen como columnas del grid en vez de en dos controles
  aparte; el ZIP se genera igual.
- Con ella, las consultas de `DbFacade` que solo usaba esa pantalla: `ObtenerNumerosDisponibles`,
  `BuscarPresupuestos` (texto libre en OR), `ObtenerVersiones` y `ObtenerReferencia`, y el
  constructor sin parámetros (ODBC por defecto) y `ConfiguracionConexion.OdbcPorDefecto()`.
  Si hiciera falta recuperar alguna, están en el commit `ecb2e7a`.
- El rótulo "Búsqueda avanzada" (título de ventana y cabecera): ya no es una búsqueda "avanzada"
  frente a otra, es la pantalla. Ahora: "Exportador Persycom" y "Exportación de presupuestos a
  fábrica".

**Si el cliente echa de menos algo:** lo único de la pantalla antigua sin equivalente literal es
el desplegable que listaba TODOS los presupuestos nada más abrir; ahora se obtiene lo mismo
pulsando Buscar con los cuadros vacíos (o Enter en cualquier cuadro vacío).

## Decisiones técnicas (confirmadas con el arquitecto el 06-10-2026)

- **Marcadores `?` (ODBC) frente a `@nombre` (SqlClient)**: el SQL se escribe UNA vez con
  `@nombre`. `Database/ConsultaParametrizada.Crear(conn, sql, parametros)` lo adapta: con
  `OdbcConnection` sustituye cada `@nombre` por `?` en orden de aparición y añade un parámetro por
  APARICIÓN (si `@texto` sale cuatro veces, cuatro parámetros con el mismo valor, que es como ODBC
  los casa); con cualquier otro proveedor manda el texto tal cual y añade los parámetros por
  nombre. Ninguna consulta existe duplicada. El SQL embebido no usa `@` para nada más, así que la
  sustitución no tiene falsos positivos.
- **Capa de datos sobre `System.Data.Common`**: `DbFacade` recibe una `ConfiguracionConexion`
  por constructor y no sabe qué proveedor tiene debajo. `ConfiguracionConexion.Abrir()` crea
  `OdbcConnection` o `SqlConnection` según el modo. `DbConnectionFactory` queda como paso único.
- **AND al combinar cuadros**: cada campo acota. Confirmado por el arquitecto.
- **Cifrado**: opcional por defecto, confiando en el certificado del servidor, con casilla para
  exigirlo. SqlClient exige cifrado estricto de serie y contra un certificado autofirmado (lo
  habitual en una red local) la conexión fallaría sin más; el ODBC actual tampoco valida
  certificado, así que no es una regresión.
- **Preferencias en `%AppData%` sin contraseña**: aprobado por el arquitecto.
- Paquete `Microsoft.Data.SqlClient` 6.1.4 (el mantenido, en vez de `System.Data.SqlClient`).
- Comodines de LIKE escapados con corchetes (`[%]`, `[_]`, `[[]`): el texto del usuario se busca
  literal. Texto del usuario SIEMPRE parametrizado; `Numero`/`Version` van interpolados solo
  porque ya son `long` parseados.

## Ficheros (estado final respecto a `9355f2c`, antes de la tarea)

- Nuevos: `Database/ConfiguracionConexion.cs`, `Database/ConsultaParametrizada.cs`,
  `Database/PreferenciasConexion.cs`, `Database/Model/ResultadoBusqueda.cs`, `RecursosEmbebidos.cs`
  (logo, icono y paleta compartidos; antes privados de la pantalla antigua).
- Reescritos: `FormPrincipal.cs` (la pantalla nueva; la antigua se borró), `Database/DbFacade.cs`,
  `Database/DbConnectionFactory.cs`.
- `ExportadorPersycom.csproj`: paquete `Microsoft.Data.SqlClient`.
- Sin tocar: `Program.cs`, `Zip/EmpaquetadorPaf.cs`, `Database/Model/DatosPaf.cs`,
  `Database/ConsultaZzRolapDatosPaf.cs`, `App.config`, esquema del cliente.

## Qué se probó y qué no

- **SqlClient en vivo (desde Linux)** contra `localhost,1433` / `Alugal_TNeta25` (copia del esquema
  real de Preference/Alugal, 1 sola fila en `PAF`) con el login de solo lectura autorizado por el
  arquitecto, mediante un harness de consola temporal que compiló la capa `Database/` y `Zip/`
  REAL del proyecto (sin WinForms; ya borrado): probar conexión (SQL Server 16.00.4255), búsqueda
  avanzada sin filtros (1 fila: presupuesto 1 v1, Profine Iberia), por número, por cliente, AND
  con cliente inexistente (0 filas), comodines `%[_` literales (0 filas, sin error), y la rama
  ODBC del adaptador de marcadores (texto `... LIKE UPPER(?) OR b LIKE ? AND c = ?` con tres
  parámetros, sin abrir conexión).
- **Exportación completa por SqlClient: NO probada**. `ObtenerDatosParaExportar` falla en ESE
  contenedor con "Execution of user code in the .NET Framework is disabled. Enable clr enabled":
  `Zlib.unzipxml` es una función SQLCLR y el servidor de pruebas tiene el CLR apagado. No es un
  fallo del código (la consulta es la de siempre, la que funciona en el cliente por ODBC).
  Activarlo es `sp_configure` a nivel de servidor y afecta a otras bases: queda para el gerente,
  fuera de esta tarea.
- **Camino ODBC: sin probar en vivo** (no hay Windows ni DSN desde aquí).
- **Pantalla en Windows**: probada por el gerente en su máquina el 06-10-2026 (dos veces: la
  primera motivó el cambio de alcance; la segunda, "de momento está ok"). No se ha probado con
  volumen de datos real en el grid: la base de pruebas tiene una fila.

## Fuera de alcance / notas

- El bloqueo del `.exe` por Control Inteligente de Aplicaciones (SEG-1) sigue igual.
- `App.config` sigue igual; el DSN por defecto se lee de ahí.
- Columnas del grid repartidas por peso y formulario redimensionable (980×720, mínimo 820×600):
  sin ajustar a ojo con muchas filas.
