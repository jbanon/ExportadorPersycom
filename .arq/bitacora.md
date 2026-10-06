# Bitácora (arquitecto)
<!-- Histórico append-only. Una entrada por turno del arquitecto. No se borra ni se resume. -->

**06-10-2026 — Primera sesión con protocolo `.arq/` en este proyecto.** Hasta hoy el trabajo se
hizo sin esta estructura (buscador simple, orden por `Numero`, campo `Referencia`, fix del icono
embebido — ver commits `c820940` y `9355f2c`, y el detalle en `ARQ-ESTADO.md`). Primera prueba
real en Windows hecha hoy por el gerente: encontró el crash del icono (arreglado en el momento) y
después el bloqueo de Control Inteligente de Aplicaciones (sin arreglo de código posible, backlog
`SEG-1`).

Encargo nuevo del gerente: según él, una versión anterior de esta herramienta tenía dos formas de
conexión (ODBC `PrefSuite`, la actual, o "configuración manual") y varios cuadros de búsqueda, no
solo el buscador único que se construyó hoy. Investigado `/tmp/KoUtilities` buscando esa versión
original (mencionada en un comentario del propio código, `DbFacade.cs:6`, como "el original de
KoUtilities") — resultó estar vacío, sin ficheros reales dentro de sus subcarpetas, así que no
sirvió de referencia. Preguntado directamente al gerente qué significaba "configuración manual":
**confirmado que es una conexión SQL Server directa (host, base de datos, usuario, contraseña) vía
`SqlClient`, alternativa al DSN ODBC**, no otro DSN ni otra cadena ODBC.

Esto cambia el panorama de pruebas: hoy el camino ODBC es intestable desde Linux, pero el camino
manual a SQL Server, una vez implementado, sí se podrá probar en vivo contra la copia del esquema
de `Alugal_TNeta25` en el contenedor `sqlserver` de este servidor (puerto 1433 expuesto al host).

Escrita la tarea `0001`: pantalla nueva (no sustituye la actual) con selector de conexión
ODBC/manual, cuatro cuadros de búsqueda separados (Presupuesto/Pedido de compras/Cliente/
Referencia de obra) y un panel de resultados en grid que reutiliza la generación de ZIP ya
existente. Señalado como reto técnico explícito en la propia tarea: el SQL embebido usa
marcadores `?` (ODBC); `SqlClient` necesita `@nombre` — hay que resolver esa diferencia sin
duplicar las queries, y se deja a criterio del programador cómo, con instrucción de explicarlo en
el informe. Despachada a `PRESUP-ProgFable`.

**06-10-2026 — Tarea 0001 CERRADA, revisada y aprobada.** Revisado `git diff` completo antes de
aprobar: `ConfiguracionConexion`/`ConsultaParametrizada` correctos (ninguna query de usuario sin
parametrizar, contraseña nunca persistida, invalidación de conexión cacheada al cambiar cualquier
campo), grid de solo lectura. Verificado en vivo por primera vez algo real de este proyecto desde
Linux: el camino SqlClient contra `Alugal_TNeta25` funciona; la exportación completa
(`ObtenerDatosParaExportar`) falla en ese contenedor concreto por tener el CLR de SQL Server
desactivado (afecta a `Zlib.unzipxml`) — no es un fallo del código, y no se ha tocado esa
configuración del servidor sin que lo decida el gerente (es a nivel de instancia, compartida con
PresupuestadorPersycom). Commit `ecb2e7a` en `origin/main`, confirmado.

Queda sin probar en vivo el camino ODBC real y la pantalla WinForms (necesitan una máquina Windows
real) y sin resolver el bloqueo de Control Inteligente de Aplicaciones sobre el `.exe` sin firmar
(`SEG-1`).

**06-10-2026 — 0001 cerrada del todo: la pantalla de búsqueda pasa a ser la principal.** El
gerente probó la primera versión en Windows y pidió, directamente a Fable (sin pasar por el
arquitecto, igual que el cambio del icono antes), que la pantalla nueva sustituyera a la antigua
en vez de convivir con ella: conexión plegable, grid más cuidado, cabecera con título/subtítulo.
Revisado el `git diff` completo (fusión de `FormBusqueda` en `FormPrincipal`, limpieza de
`DbFacade`/`ConfiguracionConexion`, sin clases duplicadas, `Program.cs` sin cambios necesarios):
correcto. Confirmado por el gerente en Windows ("de momento está ok") antes de dar la orden de
commitear. Push `09b434e`. Informe reescrito a petición expresa del gerente para que, si el
cliente final reporta algo más adelante, cualquiera pueda retomarlo sin preguntar — Fable separó
claramente qué se probó en vivo (SqlClient contra Alugal_TNeta25) de lo que no (exportación
completa, bloqueada por el CLR apagado en el contenedor de pruebas; ODBC; la pantalla en sí, solo
verificada por el gerente en Windows).
