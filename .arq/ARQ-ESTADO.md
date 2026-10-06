# Estado del proyecto
<!-- Se REESCRIBE cada turno. Maximo una pantalla. Las trampas viven en .arq/trampas.md, que
     es durable y NO se reescribe aqui. -->

## Ahora mismo (06-10-2026)

Repo `jbanon/ExportadorPersycom`, rama `main` (ojo, no `master`), al dia con `origin/main`
(`9355f2c`). Protocolo `.arq/` creado hoy, primera sesion de este proyecto con este montaje.

**Trabajo de hoy (sesion unica, antes de este fichero) resumido**:
- Buscador simple (un cuadro, filtra en vivo por Numero/NumeroPedido/Cliente/Obra, parametrizado),
  orden por `Numero DESC` (antes `ORDER BY Orden`, de ahi el desorden que reporto el gerente), y
  campo `PAF.Referencia` mostrado en pantalla. Implementado por PRESUP-ProgSonnet. Commit `c820940`.
- Bug encontrado en la primera prueba real en Windows: el `.exe` publicado como single-file no
  llevaba la carpeta `Recursos/` (el icono se cargaba desde disco) y crasheaba al arrancar.
  Arreglado incrustando el `.ico` como recurso embebido, igual que ya se hacia con el logo.
  Commit `9355f2c`.
- Probado en Windows: bloqueado por **Control Inteligente de Aplicaciones** (Smart App Control),
  no por SmartScreen — `Unblock-File` no sirve contra esto. Sin resolver: pendiente de que el
  gerente decida entre desactivar esa proteccion en la maquina de pruebas o firmar el ejecutable.
  Ver `.arq/backlog.md`.

**Tarea 0001 en marcha**: pantalla nueva (ADEMAS de la actual, sin reemplazarla) con selector de
conexion ODBC/PrefSuite o **SQL Server manual** (host/BD/usuario/contrasena, confirmado con el
gerente, via `SqlClient`), cuatro cuadros de busqueda separados (uno por campo) y un panel de
resultados en grid. Ver `.arq/tareas/0001-pantalla-busqueda-conexion-manual.md`. Despachada a
PRESUP-ProgFable.

**Hallazgo importante para pruebas futuras**: el camino de conexion manual a SQL Server, una vez
implementado, permite probar ESTE PROYECTO en vivo desde Linux contra el contenedor `sqlserver`
de este servidor (base `Alugal_TNeta25`, copia del esquema real de Preference/Alugal, casi sin
filas). El camino ODBC sigue sin poder probarse mas que en una maquina Windows real.

## Pendiente de decision del gerente

- **Control Inteligente de Aplicaciones** bloqueando el `.exe` en la maquina de pruebas: ¿se
  desactiva (ojo, en muchas versiones de Windows 11 es dificil de reactivar sin reinstalar) o se
  firma el ejecutable?

## Convenciones vigentes

- El arquitecto verifica cada entrega con `git diff`/build antes de aprobar, igual que en
  PresupuestadorPersycom.
- Credenciales (DSN, SQL Server manual): nunca en fichero versionado en git.
- Sin commit/push sin orden explicita del arquitecto.

## Trampas conocidas

En [`.arq/trampas.md`](trampas.md). Fichero durable: no se reescribe al cerrar turno.
