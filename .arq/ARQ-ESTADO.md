# Estado del proyecto
<!-- Se REESCRIBE cada turno. Maximo una pantalla. Las trampas viven en .arq/trampas.md, que
     es durable y NO se reescribe aqui. -->

## Ahora mismo (06-10-2026)

Repo `jbanon/ExportadorPersycom`, rama `main`, al día con `origin/main` (`09b434e`). **Tarea 0001
CERRADA del todo**, incluido un cambio de alcance pedido directamente por el gerente tras probarla
en Windows: la pantalla de búsqueda avanzada **pasa a ser LA pantalla de la aplicación**
(`FormPrincipal`); la antigua (combo + filtro simple) se elimina por completo, junto con las
consultas de `DbFacade` que solo ella usaba.

- Bloque de conexión plegable: se pliega solo al conectar bien o al buscar, se despliega solo si
  falla una consulta con error de base de datos.
- Grid con cabecera oscura, filas más altas, presupuesto en negrita, texto de "sin resultados"
  sobre la tabla vacía.
- Cabecera con título "Exportación de presupuestos a fábrica" y subtítulo.
- Único hueco sin equivalente literal, documentado en el informe: el desplegable con TODOS los
  presupuestos al abrir la pantalla antigua — ahora equivale a pulsar "Buscar" con los cuatro
  campos vacíos.
- Confirmado por el gerente en Windows ("de momento está ok") antes de commitear.
- Informe `.arq/informes/0001-resultado.md` reescrito para que alguien sin contexto pueda
  retomarlo si el cliente final (quien usa la herramienta) reporta una sugerencia o un error: qué
  se probó y qué no (SqlClient en vivo sí; exportación completa NO por el CLR apagado en el
  servidor de pruebas; ODBC y la prueba visual, solo en Windows), decisiones técnicas y motivo de
  cada una.

**Sin resolver**: `SEG-1` (backlog) — Control Inteligente de Aplicaciones sigue bloqueando el
`.exe` sin firmar; no se ha tocado.

## Pendiente de decisión del gerente

- **SEG-1**: desactivar Control Inteligente de Aplicaciones en la máquina de pruebas, o firmar el
  ejecutable.
- Activar o no el CLR de SQL Server en el contenedor compartido con el Presupuestador, para poder
  probar la exportación completa desde Linux (hoy falla solo ahí, `Zlib.unzipxml` es SQLCLR).

## Convenciones vigentes

- El arquitecto verifica cada entrega con `git diff`/build antes de aprobar.
- Credenciales (DSN, SQL Server manual): nunca en fichero versionado en git.
- Sin commit/push sin orden explicita del arquitecto.

## Trampas conocidas

En [`.arq/trampas.md`](trampas.md). Fichero durable: no se reescribe al cerrar turno.
