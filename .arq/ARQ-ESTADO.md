# Estado del proyecto
<!-- Se REESCRIBE cada turno. Maximo una pantalla. Las trampas viven en .arq/trampas.md, que
     es durable y NO se reescribe aqui. -->

## Ahora mismo (04-11-2026)

Repo `jbanon/ExportadorPersycom`, rama `main`, al día con `origin/main` (`a992796`). Clonado por
primera vez en la máquina de dev nueva (`persydesaubnt`, ver trampas.md de `PresupuestadorPersycom`
sobre el cambio de máquina del 03-11-2026) en `/home/persyadmin/proyectos/ExportadorPersycom`.
`dotnet build` verificado 0/0 ahí.

Tarea 0001 (pantalla de búsqueda avanzada) sigue cerrada, sin cambios desde la sesión anterior.

**Tarea 0002 CERRADA**: el campo "Referencia de obra" filtraba sobre `PAF.Obra` y ahora filtra
sobre `PAF.Referencia` (cambio de una línea en `DbFacade.BuscarAvanzado`). Hecha por Sonnet,
revisada por el arquitecto con `git diff` y además probada en vivo contra `Alugal_TNeta25` (la
consulta real generada se ejecutó sin error; la única fila de prueba tiene `Referencia` NULL, por
lo que da 0 resultados, comportamiento correcto). Commiteada y pusheada (`a992796`).

**Resuelto en esta sesión**: la base de pruebas `Alugal_TNeta25` ya está traída a esta máquina
(SQL Server nativo, no Docker) desde la máquina vieja por SSH. Verificado `ONLINE`, con los
mismos datos mínimos que ya tenía (1 fila en `PAF`/`ContenidoPAF`). El camino de "probar la
conexión manual a SQL Server desde Linux" vuelve a estar disponible. Detalle del procedimiento en
trampas.md.

**Sin resolver**: `SEG-1` (backlog) — Control Inteligente de Aplicaciones sigue bloqueando el
`.exe` sin firmar; no se ha tocado.

## Pendiente de decisión del gerente

- **SEG-1**: desactivar Control Inteligente de Aplicaciones en la máquina de pruebas, o firmar el
  ejecutable.

## Convenciones vigentes

- El arquitecto verifica cada entrega con `git diff`/build antes de aprobar.
- Credenciales (DSN, SQL Server manual): nunca en fichero versionado en git.
- Sin commit/push sin orden explicita del arquitecto.

## Trampas conocidas

En [`.arq/trampas.md`](trampas.md). Fichero durable: no se reescribe al cerrar turno.
