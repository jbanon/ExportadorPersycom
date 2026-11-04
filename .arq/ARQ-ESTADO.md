# Estado del proyecto
<!-- Se REESCRIBE cada turno. Maximo una pantalla. Las trampas viven en .arq/trampas.md, que
     es durable y NO se reescribe aqui. -->

## Ahora mismo (04-11-2026)

Repo `jbanon/ExportadorPersycom`, rama `main`, al día con `origin/main` (`9525582`). Clonado por
primera vez en la máquina de dev nueva (`persydesaubnt`, ver trampas.md de `PresupuestadorPersycom`
sobre el cambio de máquina del 03-11-2026) en `/home/persyadmin/proyectos/ExportadorPersycom`.
`dotnet build` verificado 0/0 ahí.

Tarea 0001 (pantalla de búsqueda avanzada) sigue cerrada, sin cambios desde la sesión anterior.

**Tarea 0002 abierta** (el campo "Referencia de obra" filtra sobre `PAF.Obra` y debe filtrar
sobre `PAF.Referencia`): escrita en `.arq/tareas/0002-busqueda-obra-por-referencia.md`, pendiente
de asignar a un programador — aún sin despachar.

**Hallazgo de esta sesión**: la base de pruebas `Alugal_TNeta25` (contenedor Docker de la máquina
vieja) no existe en la máquina nueva, que no tiene Docker y tiene SQL Server nativo con solo
`PersycomPresupuestador`. El camino de verificación en vivo contra SQL Server está roto hasta que
se decida recrear el esquema de pruebas o montar Docker. Anotado en trampas.md.

**Sin resolver**: `SEG-1` (backlog) — Control Inteligente de Aplicaciones sigue bloqueando el
`.exe` sin firmar; no se ha tocado.

## Pendiente de decisión del gerente

- **SEG-1**: desactivar Control Inteligente de Aplicaciones en la máquina de pruebas, o firmar el
  ejecutable.
- Recrear la base de pruebas `Alugal_TNeta25` en la máquina nueva (y con qué datos), o instalar
  Docker de nuevo — sin esto, ninguna tarea puede probarse en vivo contra SQL Server desde Linux.

## Convenciones vigentes

- El arquitecto verifica cada entrega con `git diff`/build antes de aprobar.
- Credenciales (DSN, SQL Server manual): nunca en fichero versionado en git.
- Sin commit/push sin orden explicita del arquitecto.

## Trampas conocidas

En [`.arq/trampas.md`](trampas.md). Fichero durable: no se reescribe al cerrar turno.
