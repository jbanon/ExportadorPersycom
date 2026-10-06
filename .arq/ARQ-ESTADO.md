# Estado del proyecto
<!-- Se REESCRIBE cada turno. Maximo una pantalla. Las trampas viven en .arq/trampas.md, que
     es durable y NO se reescribe aqui. -->

## Ahora mismo (06-10-2026)

Repo `jbanon/ExportadorPersycom`, rama `main` (ojo, no `master`), al dia con `origin/main`
(`ecb2e7a`). **Tarea 0001 CERRADA**: pantalla nueva `FormBusqueda` (boton "Busqueda avanzada..."
en `FormPrincipal`, no sustituye su flujo), con:

- Conexion ODBC (DSN, como siempre) o SQL Server manual (host/puerto/BD/usuario/contrasena via
  `Microsoft.Data.SqlClient`). Contrasena NUNCA se persiste; el resto de la configuracion si, en
  `%AppData%\ExportadorPersycom\conexion.json`.
- Cuatro cuadros de busqueda separados (Presupuesto/Pedido de compras/Cliente/Referencia de obra),
  combinados con AND, busqueda en vivo (debounce) + Enter.
- Grid de resultados de solo lectura (Numero, Version, Cliente, Pedido de compras, Referencia de
  obra, Referencia); doble clic o boton genera el ZIP reutilizando el codigo existente.
- `Database/ConsultaParametrizada.cs`: el SQL se escribe UNA vez con `@nombre`; se adapta a `?`
  posicional para ODBC o se manda tal cual a SqlClient. Ninguna query duplicada entre proveedores.

**Verificado en vivo** (unica vez hasta ahora que se ha podido probar algo real de este proyecto
desde Linux): camino SqlClient contra `Alugal_TNeta25` (contenedor `sqlserver`, login de solo
lectura), `ProbarConexion`/`BuscarAvanzado`/`BuscarPresupuestos`/`ObtenerVersiones`/
`ObtenerReferencia` todos correctos. `ObtenerDatosParaExportar` (la exportacion real) falla ahi
porque el CLR de SQL Server esta desactivado en ese contenedor (`Zlib.unzipxml` lo necesita) —
**pendiente de decision del gerente**: activarlo (`sp_configure 'clr enabled'`, a nivel de
SERVIDOR, afecta tambien a la base del Presupuestador) o dejarlo, la rama ODBC real en Windows no
tiene este problema.

**Sin probar en vivo todavia**: el camino ODBC completo, y la pantalla WinForms (maquetacion,
tamanos, comportamiento del grid) — necesitan una maquina Windows real.

## Pendiente de decision del gerente

- **SEG-1** (backlog): Control Inteligente de Aplicaciones bloqueando el `.exe` sin firmar en la
  maquina de pruebas. Sin resolver.
- Activar o no el CLR en el SQL Server de este servidor, para poder probar la exportacion completa
  desde aqui (ver arriba).
- Pendiente de que alguien pruebe la pantalla nueva en una maquina Windows real.

## Convenciones vigentes

- El arquitecto verifica cada entrega con `git diff`/build antes de aprobar.
- Credenciales (DSN, SQL Server manual): nunca en fichero versionado en git.
- Sin commit/push sin orden explicita del arquitecto.

## Trampas conocidas

En [`.arq/trampas.md`](trampas.md). Fichero durable: no se reescribe al cerrar turno.
