# Instrucciones del proyecto (Exportador Persycom)

## Qué es esto

App de escritorio Windows (.NET 8, WinForms, self-contained, un único `.exe`) que consulta la
base de datos de Preference/Alugal del cliente (hoy vía ODBC, DSN `PrefSuite`) para generar un ZIP
de un presupuesto+versión, destinado a fábrica. Compila (no ejecuta) desde Linux gracias a
`EnableWindowsTargeting=true` en el `.csproj`; **ejecutar y probar en vivo el camino ODBC necesita
una máquina Windows real** con el DSN configurado — no se puede desde aquí. El camino de conexión
manual a SQL Server (cuando exista) sí se puede probar desde Linux contra una copia del esquema en
el contenedor `sqlserver` de este servidor.

## Comunicación con el arquitecto

Mismo criterio que en los demás proyectos de este servidor: sin narrar pasos intermedios, informe
final conciso (qué se hizo, ficheros tocados, build 0/0, cómo se probó — o por qué no se pudo
probar). Si hay una decisión de diseño, seguridad, modelo de datos o ambigüedad real, parar y
preguntar antes de implementar, no asumir. Sin emojis.

## Protocolo arquitecto / programador

### Ficheros de memoria
- `.arq/ARQ-ESTADO.md` — estado vivo. Léelo siempre al empezar. Se reescribe entero cada turno
  del arquitecto. No contiene las trampas: solo enlaza a `.arq/trampas.md`.
- `.arq/trampas.md` — trampas conocidas, aprendidas por las malas. Durable: no se reescribe, se
  añade a lo que ya hay.
- `.arq/decisiones/ADR-NNNN-*.md` — decisiones cerradas y vinculantes.
- `.arq/bitacora.md` — histórico append-only, una entrada por turno del arquitecto.
- `.arq/backlog.md` — pendientes con id y estado.
- `.arq/tareas/NNNN-slug.md` — spec de una tarea concreta.
- `.arq/informes/NNNN-resultado.md` — lo que reporta el programador.

### Reparto
- **Arquitecto**: no escribe código de producción. Solo escribe dentro de `.arq/`. Verifica el
  trabajo leyendo `git diff` y el código, no solo el informe.
- **Programador**: implementa la tarea del fichero que se le indique, nada más. Escribe
  únicamente en `.arq/informes/`; no toca el resto de `.arq/`.

### Verificación

Sin capturas de pantalla en los informes. Como la app es de Windows y este servidor es Linux:
- `dotnet build` (y `dotnet publish -r win-x64 --self-contained` cuando el cambio afecte al
  empaquetado) es la verificación base, siempre.
- Si el cambio toca la conexión manual a SQL Server, se puede probar en vivo contra el contenedor
  `sqlserver` de este servidor (pedir credenciales al arquitecto si hacen falta).
- Si el cambio solo afecta al camino ODBC/UI de Windows, decirlo explícitamente como "sin probar
  en vivo" — no simularlo ni darlo por bueno sin más.

### Git

Repo `jbanon/ExportadorPersycom`, rama **`main`** (no `master`). No hacer commit/push sin que el
arquitecto dé la orden explícita.
