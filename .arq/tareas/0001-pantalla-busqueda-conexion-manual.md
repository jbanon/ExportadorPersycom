# 0001 — Pantalla de búsqueda con conexión ODBC o SQL Server manual

Encargo directo del gerente (06-10-2026). Repo `jbanon/ExportadorPersycom`, rama `main`, al día
con `origin/main` (`9355f2c`): buscador simple, orden por `Numero DESC`, campo `Referencia` y
el icono embebido ya están hechos y probados (parcialmente — ver abajo). Esta tarea AÑADE una
pantalla nueva, sin romper el flujo actual de `FormPrincipal` (combo con filtro + Generar ZIP).

## Contexto necesario

- `FormPrincipal.cs` — pantalla actual completa, patrón visual a seguir (colores `RojoPersycom`,
  estilo de controles, `EjecutarConEstado` para loading/errores).
- `Database/DbFacade.cs` — las queries actuales (`ObtenerNumerosDisponibles`, `BuscarPresupuestos`,
  `ObtenerVersiones`, `ObtenerReferencia`, `ObtenerDatosParaExportar`), todas sobre `OdbcConnection`
  vía `DbConnectionFactory.Abrir()`.
- `Database/DbConnectionFactory.cs` — hoy solo sabe abrir ODBC con el DSN de `App.config`.
- `Database/ConsultaZzRolapDatosPaf.cs` — el SQL embebido (subconsulta), con marcadores `?` de
  ODBC. **No se toca el esquema del cliente** (sigue sin crear vistas ni nada en su base).
- `Database/Model/DatosPaf.cs` y `Zip/EmpaquetadorPaf.cs` — el modelo de línea y el empaquetado
  del ZIP; reutilizar, no duplicar.

## Qué pide el gerente (textual)

"Una pantalla con: configuración de acceso mediante ODBC o configuración [manual], unos cuadros
de búsquedas sobre todos los campos que te pedí antes [Presupuesto/Numero, Pedido de
compras/NumeroPedido, Cliente/Nombre, Referencia de obra/Obra], un panel de resultado decente."

**"Configuración manual" confirmada con el gerente**: conexión SQL Server directa (host, base de
datos, usuario, contraseña) vía `SqlClient` (paquete `Microsoft.Data.SqlClient` o
`System.Data.SqlClient`, a tu criterio — el primero es el mantenido activamente, prefiérelo salvo
que veas un motivo concreto para el otro), como alternativa al DSN ODBC `PrefSuite` actual. No es
otro DSN ni otra cadena de conexión ODBC: es un proveedor de BD distinto.

## Qué hacer

1. **Capa de conexión con dos proveedores.** Hoy `DbFacade`/`DbConnectionFactory` asumen ODBC en
   todas partes. Necesitas que las mismas queries funcionen contra ODBC (DSN) o SQL Server
   (host/BD/usuario/contraseña), sin duplicar la lógica de cada método de `DbFacade`. Plantea tú
   la solución (por ejemplo, trabajar contra `System.Data.Common.DbConnection`/`DbCommand`, que
   ambos proveedores implementan, con una fábrica que decide cuál crear según el modo elegido).
   **Reto concreto que tienes que resolver y explicar en el informe**: el SQL embebido de
   `ConsultaZzRolapDatosPaf.Origen` usa marcadores posicionales `?` (sintaxis ODBC) en las queries
   parametrizadas (`BuscarPresupuestos`); `SqlClient` necesita parámetros nombrados `@algo`. No
   vale con "si es SqlClient, reescribo la query entera a mano" en cada sitio — decide una forma
   de no duplicar el texto SQL completo por proveedor (podría ser una función que adapte
   marcadores, o generar el texto una vez con placeholders propios y sustituir según el proveedor
   activo, o lo que se te ocurra que sea razonable). Si después de intentarlo ves que no hay forma
   limpia sin duplicar, dilo en el informe con el motivo, no lo fuerces.
   La contraseña de la conexión manual **no se persiste en ningún fichero** (ni `App.config` ni
   otro): se pide en la propia pantalla cada vez que se usa ese modo.

2. **Pantalla nueva** (Form nuevo abierto desde un botón de `FormPrincipal`, o integrado de otra
   forma si te parece mejor — dilo en el informe y por qué) con:
   - Selector de modo de conexión arriba: ODBC (DSN, por defecto `PrefSuite`, editable) | SQL
     Server manual (host, puerto opcional, base de datos, usuario, contraseña).
   - Cuatro cuadros de búsqueda **separados**, uno por campo, con su etiqueta: Presupuesto
     (`Numero`), Pedido de compras (`NumeroPedido`), Cliente (`Nombre`/`Cliente`), Referencia de
     obra (`Obra`). No es el buscador único de texto libre que ya existe en `FormPrincipal`
     (ese se queda como está, sin tocar).
   - Al combinar varios cuadros rellenos: mi opinión es que debería ser AND (cada campo relleno
     acota más el resultado, no lo amplía) — pero decide tú si te convence y dilo explícitamente
     en el informe; si tienes dudas reales sobre esto, pregunta antes de implementar en vez de
     adivinar.
   - Panel de resultados en condiciones: una tabla/grid (no una lista de texto), con columnas
     Numero, Version, Cliente, Pedido de compras, Referencia de obra, Referencia. Elegir una fila
     debe permitir generar el ZIP para ese Numero+Version, reutilizando
     `DbFacade.ObtenerDatosParaExportar` + `EmpaquetadorPaf.CrearZip` — no reimplementes esa parte.

3. **No rompas `FormPrincipal`**: su flujo actual (combo con filtro simple + Generar ZIP) se queda
   tal cual, funcionando con ODBC como hoy. Esta es una pantalla ADICIONAL. Si al implementar ves
   que tiene más sentido que esta pantalla nueva sustituya a la actual en vez de convivir con
   ella, PARA y pregunta antes de decidirlo tú.

## Qué NO se pide

- No toques `EmpaquetadorPaf.cs` ni `Database/Model/DatosPaf.cs` salvo lo imprescindible para
  llevar los campos nuevos al grid de resultados.
- No persistas ninguna contraseña en disco, ni en código ni en fichero de configuración.
- No crees ni toques ningún objeto en el esquema del cliente (vistas, tablas): sigue siendo
  subconsulta embebida, como ya está.

## Verificación

- `dotnet build`: 0/0, como siempre.
- **Esto es importante y nuevo**: el camino de conexión manual a SQL Server SÍ se puede probar en
  vivo desde aquí (Linux), contra una copia real del esquema (no de los datos: hay 1 sola fila en
  `PAF`) en el contenedor `sqlserver` de este servidor, base `Alugal_TNeta25`, puerto `1433`
  expuesto en el host (`localhost,1433`). Pide las credenciales de un usuario de solo lectura al
  arquitecto antes de probar — no las inventes ni las pidas por el canal de mensajes entre
  sesiones (ahí se bloquean los mensajes con contraseñas en texto plano; pídelas y espera a que
  te las pasen por otra vía).
- El camino ODBC sigue sin poder probarse en vivo desde aquí: dilo explícitamente como "sin
  probar en vivo", no lo simules.
- `dotnet publish -r win-x64 --self-contained` también, para confirmar que el empaquetado sigue
  bien tras los cambios (ver la trampa del icono en `.arq/trampas.md` antes de tocar nada de
  recursos/ficheros cargados en tiempo de ejecución).

## Informe

`.arq/informes/0001-resultado.md`.
