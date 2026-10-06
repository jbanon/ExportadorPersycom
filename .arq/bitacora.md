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
