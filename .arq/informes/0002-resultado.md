# 0002 - resultado

Hecho: `Database/DbFacade.cs:68`, `Filtro("obra", "Obra", referenciaObra)` pasa a
`Filtro("obra", "Referencia", referenciaObra)`. El filtro genera `UPPER(Referencia) LIKE UPPER(@obra)`.
Sustituye, no suma un OR. Textbox, etiqueta y columnas del grid (`ReferenciaObra`, `Referencia`)
sin tocar. No queda otro filtro de busqueda sobre `Obra`; las unicas apariciones restantes son el
SELECT y el mapeo de la columna del grid.

`dotnet build`: 0 errores, 0 avisos.

Sin probar en vivo: no tengo la cadena de conexion manual a SQL Server (la tarea dice pedirla al
arquitecto) y la base solo tiene 1 fila en PAF. La columna `Referencia` ya se selecciona en la
misma consulta, asi que el SQL es valido por construccion, pero no se ha ejecutado.
No commiteado (el CLAUDE.md del repo exige orden explicita).
