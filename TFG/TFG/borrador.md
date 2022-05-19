# Cosas realizadas
- Usar .NET 5.0 con MVC y la librería Itinero
- En el front usar Leaflet con mapbox con una API Key para mostrar las rutas
- En SQL Server importar los CSV de los datos abiertos de CRTM y modificarlos al gusto
- En la modificación, quitado X columnas y modificado ciertas rows para mejor lectura
- Realizar algoritmo A*
- Realizar las consultas de líneas y transbordos en metro
- Explicar los cambios en el compilador a la hora de usar itinero (x64 y más cosas)
- Algoritmo Backtracking para recoger líneas directas



# Pasos SQL
- Importar CSV desde página Datos Abiertos
- Añadir PKs y FK para relacionarla con metro_estacion
- Borrar columnas innecesarias
- Borrar rows duplicadas (tienen sentido 1 y 2)

## Línea 10
- Tiene tarifas A y B. El órden va de 1 a N en ambas tarifas
- En la tarifa A, incrementarle +10 a cada uno (ya q hay 10 previas en la tarifa B)
- Cambiar los 10a y 10b por 10.
## Línea 7
- Ídem con línea 10
## Línea 9
- Ídem con 10 y 7
## Líneas 6 y 12
- Recoger línea que va de un sentido a otro ya q es circular
