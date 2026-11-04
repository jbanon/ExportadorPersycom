# Trampas conocidas
<!-- Durable: NO se reescribe cada turno. Se añade a lo que ya hay. -->

## Despliegue / empaquetado Windows

- **`dotnet publish -r win-x64 --self-contained` NO copia automáticamente ficheros marcados solo
  con `CopyToOutputDirectory` (sin `CopyToPublishDirectory`) a la carpeta `publish/`.** El build
  normal (`dotnet build`) sí los copia a `bin/.../win-x64/`, lo que engaña: parece que "ya
  funciona" hasta que alguien distribuye el `.exe` publicado solo, sin el resto de la carpeta de
  build. Pasó el 06-10-2026 con `Recursos/persycom.ico`, cargado desde disco en tiempo de
  ejecución: crash al arrancar en la primera prueba real en Windows. Arreglo aplicado: incrustarlo
  como `EmbeddedResource` (igual que ya se hacía con el logo) en vez de depender de un fichero al
  lado del `.exe`. **Antes de dar por bueno cualquier fichero que el código cargue desde disco en
  tiempo de ejecución, comprobar que sobrevive a un `dotnet publish -r win-x64
  --self-contained` real, no solo a un `dotnet build`.**
- **El `ExportadorPersycom.dll.config` (App.config) NO hace falta para que la app funcione hoy**:
  el único valor que lee (`Dsn`) tiene un fallback hardcodeado (`"PrefSuite"`) que coincide con el
  único valor que trae ese fichero. No avisar de que hace falta copiarlo salvo que se añada una
  configuración real que SÍ cambie el comportamiento por defecto.
- **"Control Inteligente de Aplicaciones" (Smart App Control) de Windows 11 bloquea el `.exe` sin
  firmar, y `Unblock-File`/el checkbox de "Desbloquear" en Propiedades NO sirve contra esto** (eso
  solo quita la marca de "descargado de Internet", un mecanismo distinto de SmartScreen). El
  diálogo de bloqueo ni siquiera ofrece "ejecutar de todas formas". Descubierto el 06-10-2026 en
  la primera prueba real en una máquina Windows. Sin arreglo de código posible: hace falta
  desactivar esa protección en la máquina (en muchas versiones de Windows 11 es un interruptor de
  un solo sentido, solo se puede reactivar reinstalando Windows) o firmar el ejecutable con un
  certificado de código reconocido.

## Entorno / pruebas desde Linux

- No hay ningún Windows real disponible desde esta sesión: `EnableWindowsTargeting=true` permite
  **compilar** el target `net8.0-windows` desde Linux, pero NO ejecutar la app ni probar el
  camino ODBC (DSN `PrefSuite` solo existe en Windows). Cualquier verificación de UI/WinForms real
  necesita que alguien la pruebe en una máquina Windows.
- El contenedor `sqlserver` de este servidor (compartido con PresupuestadorPersycom) tiene una
  base `Alugal_TNeta25` con el esquema real de `dbo.PAF`/`ContenidoPAF`/`ContenidoPAFBlob`, pero
  casi sin filas (1 sola fila en `PAF` a 06-10-2026) — sirve para comprobar que las queries no
  fallan, NO para validar resultados con volumen real. Puerto 1433 expuesto en el host
  (`0.0.0.0:1433`), alcanzable como `localhost,1433` desde cualquier proceso de este mismo Linux.
- `/tmp/KoUtilities` (si sigue existiendo) es un directorio prácticamente vacío (solo subcarpetas
  sin ficheros dentro): NO sirve como referencia del código original de KoUtilities pese al
  nombre. No perder tiempo mirándolo de nuevo sin comprobar antes que tiene contenido real.
- **Corrección 04-11-2026**: la máquina de dev cambió el 03-11-2026 (misma máquina física que
  `PresupuestadorPersycom`, ver el `trampas.md` de ese repo). La máquina nueva (`persydesaubnt`)
  NO tiene Docker instalado (`docker` no existe en el PATH); su SQL Server es una instalación
  nativa. Repo clonado por primera vez en esta máquina el 04-11-2026, en
  `/home/persyadmin/proyectos/ExportadorPersycom`; `dotnet build` (target `net8.0-windows`) ya
  verificado 0/0 ahí.
- **`Alugal_TNeta25` ya está en el SQL Server nativo de esta máquina (04-11-2026)**: traída de la
  máquina vieja por SSH (contenedor Docker `sqlserver` de `presupuestador-dev.winsoft.es`,
  `BACKUP DATABASE` dentro del contenedor, `docker cp` + `scp` + `RESTORE DATABASE` aquí, con
  `MOVE` porque las rutas físicas del backup ya coincidían con `/var/opt/mssql/data/` del
  install nativo). Confirmado tras restaurar: sigue casi vacía, 1 fila en `PAF` y 1 en
  `ContenidoPAF` — el `.bak` pesa 400 MB por el fichero de datos preasignado, no por volumen real;
  sigue sirviendo solo para comprobar que las queries no fallan, NO para validar resultados con
  volumen real (igual que decía la nota de abajo de la máquina vieja). El `.bak` queda en
  `/var/opt/mssql/backup/Alugal_TNeta25.bak` (propiedad `mssql:mssql`) por si hace falta
  rehacer la restauración.
