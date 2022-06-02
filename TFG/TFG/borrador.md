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
- comprobar que las líneas están ordenadas con la columna numeroordenlinea
- corregir los fallos de ordenación
- Borrar rows duplicadas (tienen sentido 1 y 2)
- hay algunas líneas con zonas de tarifa distinas (cambio de trenes). Reordenar de nuevo las líneas
- ídem para cercanías y metro lgiero pero quitando las zonas de tarifas
- caso especial con líneas circulares

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




# CERCANÍAS
## C1
Príncipe Pío
Pirámides
Delicias
Méndez Álvaro
Atocha
Nuevos
Chamartín
Fuente la mora (Ligero)
T4

## C2
Coslada
Vicálvaro
Vallecas

## C3
El casar
Sol
Mirasierra-Paco de Lucía
Pitis

## C4
Getafe Central

## C5
Móstoles Central
Alcorcón Central
Cuatro vientos
Aluche
Laguna
Embajadores
Villaverde Alto
Fuenlabrada Central

## C7
Coslada
Vicálvaro
Vallecas
Pozuelo (ligero)
Aravaca (ligero)

## C8
Recoletos

## C9
Pirámides
Delicias
