# 0002 — El campo "Referencia de obra" debe buscar por `PAF.Referencia`, no por `PAF.Obra`

## Objetivo
El cuadro de búsqueda "Referencia de obra" (`_txtObra` en `FormPrincipal.cs`) filtra hoy sobre la
columna `Obra` (`PAF.Obra`, vía la subconsulta `ConsultaZzRolapDatosPaf`). Tiene que filtrar sobre
`PAF.Referencia` en su lugar.

## Causa (ya diagnosticada, no investigar de nuevo)
`Database/DbFacade.cs:68`, dentro de `BuscarAvanzado`:
```
Filtro("obra", "Obra", referenciaObra);
```
La columna `"Obra"` viene de `dbo.PAF.Obra` (ver `Database/ConsultaZzRolapDatosPaf.cs:21`). La
columna `dbo.PAF.Referencia` (línea 22 del mismo fichero) ya se trae en el SELECT y ya se muestra
como columna propia en el grid ("Referencia", `FormPrincipal.cs:297`) — son dos columnas reales
distintas de `PAF`, no la misma con otro nombre.

## Decisión ya resuelta (no preguntar)
El textbox y su etiqueta ("Referencia de obra") no cambian. Solo cambia la columna contra la que
filtra: de `Obra` a `Referencia`. No se añade un filtro OR sobre ambas columnas — se sustituye.

## Pasos
1. En `DbFacade.BuscarAvanzado`, cambia `Filtro("obra", "Obra", referenciaObra)` para que filtre
   sobre la columna `Referencia` en vez de `Obra`.
2. Revisa que no quede ninguna otra referencia a buscar por `Obra` en el mismo sentido (solo se
   toca el filtro de búsqueda; las columnas que YA se muestran en el grid, `ReferenciaObra` y
   `Referencia`, no se tocan ni se renombran).
3. Verificación — ver la nota de abajo sobre el entorno antes de intentarlo.

## Verificación (lee esto antes de intentar probar en vivo)
La base de pruebas `Alugal_TNeta25` que usaba la sesión anterior para probar contra SQL Server en
vivo vivía en un contenedor Docker de la máquina de dev VIEJA (ver `.arq/trampas.md`, bloque
"Entorno / pruebas desde Linux"). Esta es una máquina de dev NUEVA desde el 03-11-2026: no hay
Docker instalado y esa base no existe en el SQL Server nativo de aquí (confirmado,
`sys.databases` solo lista `PersycomPresupuestador`). **No la recrees ni instales Docker por tu
cuenta** — si hace falta para esta tarea, para y pregunta. Para esta tarea concreta, con
`dotnet build` (0 errores, 0 avisos) y una lectura cuidadosa del cambio (una sola línea, un solo
nombre de columna) debería bastar; dilo explícitamente en el informe si crees que hace falta algo
más.

## Fuera de alcance
- Recrear la base de pruebas `Alugal_TNeta25` o instalar Docker en esta máquina.
- Cualquier otro campo de búsqueda o columna del grid.
- `SEG-1` (backlog, firma del ejecutable / Smart App Control): no relacionado.

## Criterios de aceptación
- El filtro de "Referencia de obra" genera SQL contra la columna `Referencia`, no `Obra`.
- `dotnet build`: 0 errores, 0 avisos nuevos.

## Informe
`.arq/informes/0002-resultado.md`.
