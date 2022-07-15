$(document).ready(function () {
    $.ajax({
        url: url + "Home/GetAllNameStations",
        dataType: "json",
        method: "GET",
        success: function (data) {
            placesList.push.apply(placesList, data.Stations)

        },
        error: function (requestObject, error, errorThrown) {
            console.log(requestObject.responseText)
        }
    });

})
var placesList = []

/** CARGA DEL MAPA **/
//Creamos mapa
//https://stackoverflow.com/questions/28599128/how-to-add-a-loading-screen-when-doing-a-ajax-call-in-phonegap
let northEast = L.latLng(41.177266365749176, -2.8682378181153627),
    southWest = L.latLng(39.759656431646974, -4.8669505750561175),
    bounds = L.latLngBounds(southWest, northEast);

let map = L.map('map',{maxBounds: bounds, minZoom: 9}).setView([40.4157390642727, -3.7071191753628954], 13); //mirando a sol

let url = ""
//Cargamos un layer para el mapa
L.tileLayer('https://{s}.tile.jawg.io/jawg-sunny/{z}/{x}/{y}{r}.png?access-token=68JcDnfmL9PaKsidgZYRsQGkslfD17uuGT4AsPT6zRDD7uMnBSb4W4lGkVfKI72Y', {
    attribution: 'Datos de los mapas: &copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
}).addTo(map);


var marker = L.icon({
    iconUrl: url + "Content/Images/icons8-marcador-32.png",
    iconSize: [32 * 1.3, 32 * 1.3],
    iconAnchor: [16 * 1.3, (32 - 6) * 1.3]
}); //Para el origen y destino

var layerGroup = L.layerGroup().addTo(map) //separa el mapa de las rutas

var waypoints = [] //origen y destino
var ptransport = [] //Tren, bus y cercanías
var otherTransports = ['driving', 'cycling', 'walking', 'driving-traffic'] //Coche, bici, andando, autobús (aprox)
var geometryOtherTransports = { car: [], bicycle: [], walking: [] }
var rwDistance = { metro: 0, cercanias: 0, ligero: 0 }
var rwTotalSpeed = 0;
var busCoords = []




//Variables colores metro https://colorswall.com/palette/106461
const metroColors = [
    {
        line: "1",
        color: "#30a3dc",
        weight: 3
    },
    {
        line: "2",
        color: "#e0292f",
        weight: 3
    },
    {
        line: "3",
        color: "#ffe114",
        weight: 3
    },
    {
        line: "4",
        color: "#814109",
        weight: 3
    },
    {
        line: "5",
        color: "#96bf0d",
        weight: 3
    },
    {
        line: "6-1",
        color: "#9a9999",
        weight: 3
    },
    {
        line: "6-2",
        color: "#9a9999",
        weight: 3
    },
    {
        line: "7",
        color: "#f96611",
        weight: 3
    },
    {
        line: "8",
        color: "#f373b7",
        weight: 3
    },
    {
        line: "9",
        color: "#990d66",
        weight: 3
    },
    {
        line: "10",
        color: "#1b0c80",
        weight: 3
    },
    {
        line: "11",
        color: "#136926",
        weight: 3
    },
    {
        line: "12-1",
        color: "#999933",
        weight: 3
    },
    {
        line: "12-2",
        color: "#999933",
        weight: 3
    },
    {
        line: "R",
        color: "#FFFFFF",
        weight: 3
    },
    {
        line: "C-1",
        color: "#66aede",
        weight: 4
    },
    {
        line: "C-2",
        color: "#008A29",
        weight: 4
    },
    {
        line: "C-3",
        color: "#BB29BB",
        weight: 4
    },
    {
        line: "C-3a",
        color: "#E45DBF",
        weight: 4
    },
    {
        line: "C-4a",
        color: "#0032A0",
        weight: 4
    },
    {
        line: "C-4b",
        color: "#0032A0",
        weight: 4
    },
    {
        line: "C-5",
        color: "#FFC72C",
        weight: 4
    }
    , {
        line: "C-7",
        color: "#EF3340",
        weight: 4
    }, {
        line: "C-8",
        color: "#777980",
        weight: 4
    },
    {
        line: "C-9",
        color: "#f46515",
        weight: 4
    }, {
        line: "C-10",
        color: "#bac12d",
        weight: 4
    }
]

//aqicn token: d3c7334343eeb2d447825c4528389846059368fd


//Funciones AUTOCOMPLETE
$('#start').autocomplete({
    source: placesList,
    change: function (event, ui) {
        if (ui.item == null) {
            this.value = "";
        }
    },
    minLength: 3
});

$('#end').autocomplete({
    source: placesList,
    minLength: 3,
    change: function (event, ui) {
        if (ui.item == null) {
            this.value = "";
        }
    }
});



async function getPTPollutionData(onComplete) {
    await onComplete //esperamos al ajax?

    //kwh/km por persona
    const metroEnergy = 0.97 / 100
    const ligeroEnergy = 1.46 / 100
    const cercaniasEnergy = 0.57 / 100

    //g de CO2/pasajero x km
    const metroEmissions = 0.26 * 10
    const cercaniasEmissions = 0.15 * 10
    const ligeroEmissions = 0.42 * 10
    const busEmissions = 125.52 

    var totalRW = ptransport.filter(elem => (elem.type == "METRO" || elem.type == "CERCAN&#205;AS" || elem.type == "METRO LIGERO")).length
    //datos para desplegar
    var avgRWSpeed = rwTotalSpeed / totalRW
    var RWDistance = rwDistance.metro + rwDistance.cercanias + rwDistance.ligero
    var totalDistance = RWDistance
    var totalDuration = 0;
    var rwKWH = metroEnergy * (rwDistance.metro) + cercaniasEnergy * (rwDistance.cercanias) + ligeroEnergy * (rwDistance.ligero)
    var rwCO2 = metroEmissions * (rwDistance.metro) + cercaniasEmissions * (rwDistance.cercanias) + ligeroEmissions * (rwDistance.ligero)


    //bus
    var busStations = ptransport.filter(station => station.type == "BUS URBANO"); //recogemos paradas de bus urbano
    var busLines = getLines(busStations, true); //recogemos líneas de bus
    var busDistance = 0;
    var busAccSpeed = 0;
    var busAvgSpeed = 0;
    var busStationsSpeed = 0;
    var busTotalEmissions = 0;
    if (busStations.length > 0) {

        for (let i = 0; i < busLines.length; i++) { //línea por línea
            var busStationsByLine = busStations.filter(station => station.linea == busLines[i])
            if (busStationsByLine.length > 1) {

                for (let i = 0; i < busStationsByLine.length; i++) {
                    if (i > 0 && i < busStationsByLine.length - 1) {
                        var busUrl = 'https://services5.arcgis.com/UxADft6QPcvFyDU1/arcgis/rest/services/M6_Red/FeatureServer/2/query?where=' + busStationsByLine[i].id + '=OBJECTID&objectIds=&time=&geometry=&geometryType=esriGeometryEnvelope&inSR=&spatialRel=esriSpatialRelIntersects&resultType=none&distance=0.0&units=esriSRUnit_Meter&relationParam=&returnGeodetic=false&outFields=*&returnGeometry=true&featureEncoding=esriDefault&multipatchOption=xyFootprint&maxAllowableOffset=&geometryPrecision=&outSR=&defaultSR=&datumTransformation=&applyVCSProjection=false&returnIdsOnly=false&returnUniqueIdsOnly=false&returnCountOnly=false&returnExtentOnly=false&returnQueryGeometry=false&returnDistinctValues=false&cacheHint=false&orderByFields=&groupByFieldsForStatistics=&outStatistics=&having=&resultOffset=&resultRecordCount=&returnZ=false&returnM=false&returnExceededLimitFeatures=true&quantizationParameters=&sqlFormat=none&f=pgeojson&token='
                        await fetch(busUrl) //recogemos ruta de línea
                            .then(response => response.json())
                            .then(data => {
                                var busData = data.features[0]

                                busData.geometry.coordinates.map(elem => { var aux = elem[0]; elem[0] = elem[1]; elem[1] = aux }); //cambio de variable
                                busCoords.push(busData.geometry.coordinates)

                                busDistance += parseFloat(busData.properties.LONGITUDTRAMOANTERIOR) / 1000
                                totalDistance += parseFloat(busData.properties.LONGITUDTRAMOANTERIOR) / 1000;

                                busAccSpeed += parseFloat(busData.properties.VELOCIDADTRAMOANTERIOR)
                                busStationsSpeed += 1;
                            });
                    } else { //mapbox
                        let busMBCoords;
                        let mapboxURL;
                        if (i == 0) {
                            busMBCoords = busStationsByLine[i].lon + ',' + busStationsByLine[i].lat + ";" + busStationsByLine[1].lon + "," + busStationsByLine[1].lat
                            mapboxURL = "https://api.mapbox.com/directions/v5/mapbox/driving/" + busMBCoords + "?geometries=geojson&access_token=pk.eyJ1IjoiZmVycnVhbm8iLCJhIjoiY2t2N3hsNmU3MXU4djJuczN2bnNmZmVkdSJ9.oWAph8BI1_CXIZxVZ9Q3wA";
                        
                        } else {
                            busMBCoords = busStationsByLine[i - 1].lon + ',' + busStationsByLine[i - 1].lat + ";" + busStationsByLine[i].lon + "," + busStationsByLine[i].lat
                            mapboxURL = "https://api.mapbox.com/directions/v5/mapbox/driving/" + busMBCoords + "?geometries=geojson&access_token=pk.eyJ1IjoiZmVycnVhbm8iLCJhIjoiY2t2N3hsNmU3MXU4djJuczN2bnNmZmVkdSJ9.oWAph8BI1_CXIZxVZ9Q3wA";

                        }
                        if (mapboxURL != "")
                            await fetch(mapboxURL)
                                .then(response => response.json())
                                .then(data => {
                                    if (data.code == "Ok") {
                                        let route = data.routes[0].geometry.coordinates
                                        route.map(elem => { var aux = elem[0]; elem[0] = elem[1]; elem[1] = aux })
                                        busCoords.push(route)
                                        busDistance += data.routes[0].distance / 1000
                                    }
                                });
                    }
                }
            } 
        }
        busAvgSpeed = busAccSpeed / busStationsSpeed;
        busTotalEmissions = busDistance * busEmissions;
        
    }
    if (busAvgSpeed > 0 && avgRWSpeed > 0) {
        var totalSpeed = rwTotalSpeed + busAccSpeed;
        var avgSpeed = totalSpeed / (busStationsSpeed + totalRW)
        totalDuration = totalDistance / avgSpeed
    }
    else if (avgRWSpeed > 0 && busAvgSpeed == 0)
        totalDuration = totalDistance / avgRWSpeed
    else {
        totalDuration = totalDistance / busAvgSpeed
    }
    var totalEmissions = busTotalEmissions + rwCO2;
    $("#info-pt").html(
        '<p><b>Tiempo (aprox.): </b>' + (totalDuration * 60).toFixed(2) + ' min</p>' +
        '<p><b>Distancia (aprox.): </b>' + totalDistance.toFixed(2) + ' km</p>' +
        '<div class="consumption"><h5><b>Impacto ambiental por pasajero</b></h5>' +
        '<p>' + rwKWH.toFixed(3) + ' <b>kW&middot;h</b></p></div>' +
        '<p>' + totalEmissions.toFixed(3) + ' <b>g de CO<sub>2</sub> por km'
    )
}


//Recogida de datos y llamada al back
$("#submit").on('click', function () {
    //comprobar que los datos están bien
    let startValue = $("#start").val()
    let endValue = $("#end").val()

    if (placesList.filter(x => x == startValue).length > 0 && placesList.filter(x => x == endValue).length > 0 && startValue != endValue) {

        //quitamos todas las layers de rutas y limpiamos arrays y datos
        $(".custom-accordion").removeClass('route-selected');
        layerGroup.clearLayers();
        waypoints = []
        ptransport = []
        geometryOtherTransports = { car: [], bicycle: [], walking: [] }
        rwDistance = { metro: 0, cercanias: 0, ligero: 0 }
        rwTotalSpeed = 0;
        busCoords = []

        var formDataPT = new FormData();
        //split para recoger por una parte el tipo de transporte y por otra el nombre de la estacion
        var start = $("#start").val().split(" - ")
        var end = $("#end").val().split(" - ")


        //llamada ajax para recoger coordenadas en caso de que se vaya en coche

        /*
         Llamar primero a A* y devolver la ruta en tranporte público y el origen y destino
         hacer llamada ajax con el origen y destino desde nominatim para que recoja la autovía más cercana
         llamar a ajax de itinero con ese origen y destino
         devolver todo
         */

        formDataPT.append("Start", start)
        formDataPT.append("End", end)

        $.ajax({
            url: url + "Home/FindPublicTransportPath",
            dataType: "json",
            data: formDataPT,
            processData: false,
            async: true,
            method: "POST",
            contentType: false,
            beforeSend: function () {
                $("#loading").removeClass('hide')

            },
            success: function (data) {
                if (data.Status == "Ok") {
                    data.PTransport.map(elem => {
                        METRO = 1,
                            CERCANIAS = 2,
                            LIGERO = 3,
                            INTERURBANO = 4,
                            URBANO = 5
                        switch (elem.type) {
                            case 1:
                                elem.type = "METRO";
                                break;
                            case 2:
                                elem.type = "CERCAN&#205;AS";
                                break;
                            case 3:
                                elem.type = "METRO LIGERO"
                                break;
                            case 4:
                                elem.type = "BUS INTERURBANO"
                                break;
                            case 5:
                                elem.type = "BUS URBANO"
                                break;
                        }
                    })
                    waypoints = data.Waypoints
                    ptransport = data.PTransport

                    var cercaniasList = ptransport.filter(elem => elem.type == "CERCAN&#205;AS")
                    var metroList = ptransport.filter(elem => elem.type == "METRO")
                    var ligeroList = ptransport.filter(elem => elem.type == "METRO LIGERO")


                    getPTPollutionData(getRailwayDistAndSpeed(metroList, cercaniasList, ligeroList))





                    //desplegar las infos
                    var coordsItinero = []
                    for (let i = 0; i < waypoints.length; i++) {
                        $.ajax({
                            url: "https://nominatim.openstreetmap.org/reverse?",
                            dataType: "json",
                            async: false,
                            data: {
                                lat: waypoints[i].lat,
                                lon: waypoints[i].lon,
                                format: "geojson",
                                zoom: 16
                            },
                            success: function (data) {
                                coordsItinero.push(data.features[0].geometry.coordinates)
                            },
                            error: function (requestObject, error, errorThrown) {
                                console.log(requestObject.responseText)
                            }
                        });
                    }
                    for (let i = 0; i < otherTransports.length; i++) {
                        var waypointsMB = ""

                        for (let j = 0; j < coordsItinero.length; j++) {
                            if (j == coordsItinero.length - 1)
                                waypointsMB += coordsItinero[j][0] + "," + coordsItinero[j][1]
                            else
                                waypointsMB += coordsItinero[j][0] + "," + coordsItinero[j][1] + ";"

                        }

                        let mapboxURL = "https://api.mapbox.com/directions/v5/mapbox/" + otherTransports[i] + "/" + waypointsMB + "?geometries=geojson&access_token=pk.eyJ1IjoiZmVycnVhbm8iLCJhIjoiY2t2N3hsNmU3MXU4djJuczN2bnNmZmVkdSJ9.oWAph8BI1_CXIZxVZ9Q3wA";

                        $.ajax({
                            url: mapboxURL,
                            async: false,
                            success: function (data) {
                                var route = data.routes[0]

                                switch (otherTransports[i]) {
                                    case "driving":
                                        //Cálculo emisiones CO2 ANEXO 3
                                        const gDieselBT2 = 229.07;
                                        const gDieselLT2 = 172.59;
                                        const gGasLT14 = 178.25;
                                        const gGasLT2 = 210.08;
                                        const gGasBT2 = 273.74;

                                        var distance = (route.distance / 1000)
                                        geometryOtherTransports.car = route.geometry;
                                        geometryOtherTransports.car.coordinates.map(elem => { var aux = elem[0]; elem[0] = elem[1]; elem[1] = aux })
                                        $("#info-car").html(
                                            '<p><b>Tiempo:</b> ' + (route.duration / 60).toFixed(2) + ' min</p>' +
                                            '<p><b>Distancia:</b> ' + distance.toPrecision(2) + ' km</p>' +

                                            '<h5><b>Impacto ambiental en base a la cilindrada</b></h5>' +
                                            '<p><b>Di&eacute;sel < 2 L: </b>' + (distance * gDieselLT2).toFixed(2) + ' <b>g CO<sub>2</sub> por km</p></b>' +
                                            '<p><b>Di&eacute;sel > 2 L: </b>' + (distance * gDieselBT2).toFixed(2) + ' <b>g CO<sub>2</sub> por km</p></b>' +
                                            '<p><b>Gasolina < 1,4 L: </b>' + (distance * gGasLT14).toFixed(2) + ' <b>g CO<sub>2</sub> por km</p></b>' +
                                            '<p><b>Gasolina  1,4 - 2,0 L: </b>' + (distance * gGasLT2).toFixed(2) + ' <b>g CO<sub>2</sub> por km</p></b>' +
                                            '<p><b>Gasolina > 2 L: </b>' + (distance * gGasBT2).toFixed(2) + ' <b>g CO<sub>2</sub> por km</p></b>')
                                        break;
                                    case "cycling":
                                        geometryOtherTransports.bicycle = route.geometry;
                                        geometryOtherTransports.bicycle.coordinates.map(elem => { var aux = elem[0]; elem[0] = elem[1]; elem[1] = aux })
                                        $("#info-bicycle").html(
                                            '<p><b>Tiempo:</b> ' + (route.duration / 60).toPrecision(3) + ' min</p>' +
                                            '<p><b>Distancia:</b> ' + (route.distance / 1000).toPrecision(3) + ' km</p>')
                                        break;
                                    case "walking":
                                        geometryOtherTransports.walking = route.geometry;
                                        geometryOtherTransports.walking.coordinates.map(elem => { var aux = elem[0]; elem[0] = elem[1]; elem[1] = aux })
                                        $("#info-pedestrian").html(
                                            '<p><b>Tiempo:</b> ' + (route.duration / 60).toPrecision(3) + ' min</p>' +
                                            '<p><b>Distancia:</b> ' + (route.distance / 1000).toPrecision(3) + ' km</p>')
                                        break;
                                }
                                $("#results").removeClass('hide');
                            },
                            error: function (requestObject, error, errorThrown) {
                                console.log(requestObject.responseJSON)
                            },
                            complete: function () {
                                $("#loading").addClass('hide')
                            },
                        });
                    }
                    //    var formDataItinero = new FormData()
                    //    formDataItinero.append("coords", coordsItinero)
                    //    $.ajax({
                    //        url: url + "Home/GetItineroRoutes",
                    //        dataType: "json",
                    //        data: formDataItinero,
                    //        processData: false,
                    //        async: true,
                    //        method: "POST",
                    //        contentType: false,
                    //        beforeSend: function () {
                    //            $("#loading").removeClass('hide')

                    //        },
                    //        success: function (data) {
                    //            otherTransports = data;
                    //            $("#info-car").html(
                    //                '<p><b>Tiempo:</b> ' + (data.Car.Time / 60).toPrecision(3) + ' min</p>' +
                    //                '<p><b>Distancia:</b> ' + (data.Car.Distance / 1000).toPrecision(3) + ' km</p>')
                    //            $("#info-bicycle").html(
                    //                '<p><b>Tiempo:</b> ' + (data.Bicycle.Time / 60).toPrecision(3) + ' min</p>' +
                    //                '<p><b>Distancia:</b> ' + (data.Bicycle.Distance / 1000).toPrecision(3) + ' km</p>')
                    //            $("#info-pedestrian").html(
                    //                '<p><b>Tiempo:</b> ' + (data.Pedestrian.Time / 60).toPrecision(3) + ' min</p>' +
                    //                '<p><b>Distancia:</b> ' + (data.Pedestrian.Distance / 1000).toPrecision(3) + ' km</p>')

                    //            $("#results").removeClass('hide');
                    //        },
                    //        complete: function () {
                    //            $("#loading").addClass('hide')
                    //        },
                    //        error: function (requestObject, error, errorThrown) {
                    //            console.log(requestObject.responseText)
                    //        }
                    //    });
                }
            },
            error: function (requestObject, error, errorThrown) {
                console.log(requestObject.responseText)
            }
        });
    } else {
        if (startValue != endValue) {
            Swal.fire({
                icon: 'error',
                title: 'Algo va mal...',
                text: 'El origen o el destino introducidos no son correctos'
            })
        } else {
            Swal.fire({
                icon: 'error',
                title: 'Algo va mal...',
                text: 'El origen o el destino introducidos son iguales'
            })
        }
    }

});

function getRailwayDistAndSpeed(metroList, cercaniasList, ligeroList) {
    return new Promise(resolve => {
        //Recoger distancias metro y cercanías (aproximado) porq solo va a un sentido y puede haber error
        for (let i = 0; i < metroList.length; i++) {
            fetch("https://services5.arcgis.com/UxADft6QPcvFyDU1/arcgis/rest/services/Red_Metro/FeatureServer/4/query?where=CODIGOESTACION=" + metroList[i].id + " AND NUMEROORDEN=" + metroList[i].ordenLinea + "&objectIds=&time=&geometry=&geometryType=esriGeometryEnvelope&inSR=&spatialRel=esriSpatialRelIntersects&resultType=none&distance=0.0&units=esriSRUnit_Meter&relationParam=&returnGeodetic=false&outFields=VELOCIDADTRAMOANTERIOR%2C+LONGITUDTRAMOANTERIOR%2C+CODIGOESTACION%2C+NUMEROORDEN&returnGeometry=true&featureEncoding=esriDefault&multipatchOption=xyFootprint&maxAllowableOffset=&geometryPrecision=&outSR=&defaultSR=&datumTransformation=&applyVCSProjection=false&returnIdsOnly=false&returnUniqueIdsOnly=false&returnCountOnly=false&returnExtentOnly=false&returnQueryGeometry=false&returnDistinctValues=false&cacheHint=false&orderByFields=&groupByFieldsForStatistics=&outStatistics=&having=&resultOffset=&resultRecordCount=&returnZ=false&returnM=false&returnExceededLimitFeatures=true&quantizationParameters=&sqlFormat=none&f=pgeojson&token=")
                .then(response => response.json())
                .then(data => {
                    var metroData = data.features[0].properties
                    rwTotalSpeed += metroData.VELOCIDADTRAMOANTERIOR
                    rwDistance.metro += metroData.LONGITUDTRAMOANTERIOR / 1000

                });
        }

        for (let i = 0; i < cercaniasList.length; i++) {
            fetch('https://services5.arcgis.com/UxADft6QPcvFyDU1/arcgis/rest/services/M5_Red/FeatureServer/4/query?where=objectid=' + cercaniasList[i].id + '&objectIds=&time=&geometry=&geometryType=esriGeometryEnvelope&inSR=&spatialRel=esriSpatialRelIntersects&resultType=none&distance=0.0&units=esriSRUnit_Meter&relationParam=&returnGeodetic=false&outFields=*&returnGeometry=true&featureEncoding=esriDefault&multipatchOption=xyFootprint&maxAllowableOffset=&geometryPrecision=&outSR=&defaultSR=&datumTransformation=&applyVCSProjection=false&returnIdsOnly=false&returnUniqueIdsOnly=false&returnCountOnly=false&returnExtentOnly=false&returnQueryGeometry=false&returnDistinctValues=false&cacheHint=false&orderByFields=&groupByFieldsForStatistics=&outStatistics=&having=&resultOffset=&resultRecordCount=&returnZ=false&returnM=false&returnExceededLimitFeatures=true&quantizationParameters=&sqlFormat=none&f=geojson&token=')
                .then(response => response.json())
                .then(data => {
                    var cercaniasData = data.features[0].properties
                    rwTotalSpeed += cercaniasData.VELOCIDADTRAMOANTERIOR
                    rwDistance.cercanias += cercaniasData.LONGITUDTRAMOANTERIOR / 1000
                })
        }

        for (let i = 0; i < ligeroList.length; i++) {
            fetch('https://services5.arcgis.com/UxADft6QPcvFyDU1/arcgis/rest/services/Red_MetroLigero/FeatureServer/4/query?where=CODIGOESTACION = ' + ligeroList[i].id + ' AND NUMEROORDEN =' + ligeroList[i].ordenLinea + '&objectIds=&time=&geometry=&geometryType=esriGeometryEnvelope&inSR=&spatialRel=esriSpatialRelIntersects&resultType=none&distance=0.0&units=esriSRUnit_Meter&relationParam=&returnGeodetic=false&outFields=VELOCIDADTRAMOANTERIOR%2C+LONGITUDTRAMOANTERIOR%2C+CODIGOESTACION%2C+NUMEROORDEN&returnGeometry=true&featureEncoding=esriDefault&multipatchOption=xyFootprint&maxAllowableOffset=&geometryPrecision=&outSR=&defaultSR=&datumTransformation=&applyVCSProjection=false&returnIdsOnly=false&returnUniqueIdsOnly=false&returnCountOnly=false&returnExtentOnly=false&returnQueryGeometry=false&returnDistinctValues=false&cacheHint=false&orderByFields=&groupByFieldsForStatistics=&outStatistics=&having=&resultOffset=&resultRecordCount=&returnZ=false&returnM=false&returnExceededLimitFeatures=true&quantizationParameters=&sqlFormat=none&f=pgeojson&token=')
                .then(response => response.json())
                .then(data => {
                    var ligeroData = data.features[0].properties
                    rwTotalSpeed += ligeroData.VELOCIDADTRAMOANTERIOR
                    rwDistance.ligero += ligeroData.LONGITUDTRAMOANTERIOR / 1000
                })
        }
        setTimeout(() => {

            resolve([rwDistance, rwTotalSpeed])
        }, 2000)

    })

}


$("#car-route").on('click', function () {
    $(".custom-accordion").removeClass('route-selected');
    $("#car").addClass('route-selected');

    layerGroup.clearLayers();

    let oriDestCoords = [geometryOtherTransports.car.coordinates[0], geometryOtherTransports.walking.coordinates.at(-1)];

    for (let i = 0; i < oriDestCoords.length; i++) {

        fetch("https://api.waqi.info/v2/feed/geo:" + oriDestCoords[i][0] + ";" + oriDestCoords[i][1] + "?token=d3c7334343eeb2d447825c4528389846059368fd")
            .then(response => response.json())
            .then(data => {
                var carMarker = L.marker(oriDestCoords[i], { icon: marker }).addTo(layerGroup);
                var pollutionInfo = addPollutionInfo(data);
                carMarker.bindPopup(pollutionInfo).addTo(layerGroup)
            })
    }


    map.flyTo(geometryOtherTransports.car.coordinates[parseInt(geometryOtherTransports.car.coordinates.length / 2)], 14);
    L.polyline(geometryOtherTransports.car.coordinates, { color: 'blue' }).addTo(layerGroup);

});

$("#foot-route").on('click', function () {
    $(".custom-accordion").removeClass('route-selected');
    $("#pedestrian").addClass('route-selected');

    layerGroup.clearLayers();

    let oriDestCoords = [geometryOtherTransports.walking.coordinates[0], geometryOtherTransports.walking.coordinates.at(-1)];

    for (let i = 0; i < oriDestCoords.length; i++) {

        fetch("https://api.waqi.info/v2/feed/geo:" + oriDestCoords[i][0] + ";" + oriDestCoords[i][1] + "?token=d3c7334343eeb2d447825c4528389846059368fd")
            .then(response => response.json())
            .then(data => {
                var walkingMarker = L.marker(oriDestCoords[i], { icon: marker }).addTo(layerGroup);
                var pollutionInfo = addPollutionInfo(data);
                walkingMarker.bindPopup(pollutionInfo).addTo(layerGroup)
            })
    }

    map.flyTo(geometryOtherTransports.walking.coordinates[parseInt(geometryOtherTransports.walking.coordinates.length / 2)], 14);
    L.polyline(geometryOtherTransports.walking.coordinates, { color: 'blue' }).addTo(layerGroup);

});

$("#bicycle-route").on('click', function () {
    $(".custom-accordion").removeClass('route-selected');
    $("#bicycle").addClass('route-selected');

    layerGroup.clearLayers(); //limpiamos

    let oriDestCoords = [geometryOtherTransports.bicycle.coordinates[0], geometryOtherTransports.bicycle.coordinates.at(-1)];

    for (let i = 0; i < oriDestCoords.length; i++) {

        fetch("https://api.waqi.info/v2/feed/geo:" + oriDestCoords[i][0] + ";" + oriDestCoords[i][1] + "?token=d3c7334343eeb2d447825c4528389846059368fd")
            .then(response => response.json())
            .then(data => {
                var bicycleMarker = L.marker(oriDestCoords[i], { icon: marker }).addTo(layerGroup);
                var pollutionInfo = addPollutionInfo(data);
                bicycleMarker.bindPopup(pollutionInfo).addTo(layerGroup)
            })
    }

    map.flyTo(geometryOtherTransports.bicycle.coordinates[parseInt(geometryOtherTransports.bicycle.coordinates.length / 2)], 14);
    L.polyline(geometryOtherTransports.bicycle.coordinates, { color: 'blue' }).addTo(layerGroup);

});

function addPollutionInfo(data) {
    var pollutionInfo = "";

    if (data.rxs.status == "ok") {
        var pollutionData = data.rxs.obs[0].msg.iaqi;
        var pollutants = [
            { key: "pm25", value: "<b>PM<sub>25</sub>: </b>" },
            { key: "pm10", value: "<b>PM<sub>10</sub>: </b>" },
            { key: "o3", value: "<b>O<sub>3</sub>: </b>" },
            { key: "no2", value: "<b>NO<sub>2</sub>: </b>" },
            { key: "so2", value: "<b>SO<sub>2</sub>: </b>" },
            { key: "co", value: "<b>CO: </b>" }];
        var weather = [
            { key: "p", value: '<i class="fa-solid fa-gauge"></i>', unit: "hPa" },
            { key: "t", value: '<i class="fa-solid fa-temperature-half"></i>', unit: "&#176;C" },
            { key: "w", value: '<i class="fa-solid fa-wind"></i>', unit: "m/s" },
            { key: "h", value: '<i class="fa-solid fa-droplet"></i>', unit: "&#37;" }]

        var pollutionString = ''
        var weatherString = ''



        for (data in pollutionData) {
            var pollutantData = pollutants.filter(elem => elem.key == data)
            if (pollutantData.length > 0)
                pollutionString += pollutantData[0].value + pollutionData[data].v + "<br/>";

            var weatherData = weather.filter(elem => elem.key == data)
            if (weatherData.length > 0) {
                weatherString += weatherData[0].value + pollutionData[data].v + " " + weatherData[0].unit + "<br/>"
            }

        }
        if (pollutionString.length > 0 && weatherString.length > 0)
            pollutionInfo = pollutionString + "<br/>" + weatherString + "</div>"
    }
    return pollutionInfo;
}

$("#publictransport-route").on('click', function () {
    $(".custom-accordion").removeClass('route-selected');
    $("#public-transport").addClass('route-selected');

    layerGroup.clearLayers(); //limpiamos
    map.flyTo(waypoints[waypoints.length / 2], 14); //cambiamos vista
    var markerIcon = L.icon({
        iconUrl: url + 'Content/Images/circle.png',
        iconSize: [24, 24],
    });
    var coords = []
    for (let i = 0; i < ptransport.length; i++) {

        var latlng = L.latLng(ptransport[i].lat, ptransport[i].lon)
        coords.push(latlng)
        var pollutionInfo = "";
        var stationsMarker;

        $.ajax({
            url: "https://api.waqi.info/v2/feed/geo:" + coords[i].lat + ";" + coords[i].lng + "?token=d3c7334343eeb2d447825c4528389846059368fd",
            dataType: "json",
            success: function (data) {
                if (i != 0 && i != ptransport.length - 1)
                    stationsMarker = L.marker([coords[i].lat, coords[i].lng], { icon: markerIcon })
                else {
                    stationsMarker = L.marker([coords[i].lat, coords[i].lng], { icon: marker })
                }
                pollutionInfo = addPollutionInfo(data)
                stationsMarker.bindPopup(
                    "<p><b>" + ptransport[i].type + "</b></p>" +
                    "<b>L&#237;nea</b> " + ptransport[i].linea + "<br/>" +
                    "<b>Estaci&#243;n</b> " + ptransport[i].denominacion + "<br/>" +
                    '<div id="pollution-' + i + '" style="display:none">' +
                    pollutionInfo +
                    '<p onclick="show(\'pollution-' + i + '\', this)" class="show-more">Mostrar m&aacute;s...</p>').addTo(layerGroup);

            },
            error: function (requestObject, error, errorThrown) {
                console.log(requestObject.responseJSON)
            }
        })

    }

    //colorear las líneas (excepto bus)
    var coordsWithoutBus = ptransport.filter(elem => elem.type != "BUS URBANO").map(elem => [elem.lat, elem.lon])
    L.polyline(coordsWithoutBus, { color: 'blue', dashArray: '20, 20', dashOffset: '0', width: 1, opacity: 0.3 }).addTo(layerGroup)

    for (let i = 0; i < busCoords.length; i++)
        L.polyline(busCoords[i], { color: 'blue' }).addTo(layerGroup) //pintamos


    var lines = getLines(ptransport, false);
    //ferroviarios
    for (let i = 0; i < lines.length; i++) {  //pintamos con líneas+
        var lineType = lines[i].split(",")
        var linesCoords = ptransport.filter(elem => elem.linea == lineType[0]).map(elem => [elem.lat, elem.lon]);

        //coloreamos para metro/cercanías
        var colorAndWeight = metroColors.filter(elem => elem.line == lineType[0] && (lineType[1] == "METRO" || lineType[1] == "CERCAN&#205;AS")).flatMap(elem => [elem.color, elem.weight])

        if (colorAndWeight.length != 0)
            L.polyline(linesCoords, { color: colorAndWeight[0], weight: colorAndWeight[1], opacity: 1 }).addTo(layerGroup)
        else
            L.polyline(linesCoords, { color: '#442c8c' }) //morado
    }
});


function show(id, element) {
    if ($("#" + id).is(':hidden')) {
        $("#" + id).show()
        element.innerHTML = "Mostrar menos..."
    }
    else {
        $("#" + id).hide()
        element.innerHTML = "Mostrar m&aacute;s..."
    }
}

function getLines(ptransport, isBus) {
    var linesList
    if (!isBus)
        linesList = ptransport.map(elem => elem.linea + ',' + elem.type);
    else
        linesList = ptransport.map(elem => elem.linea);
    return [...new Set(linesList)];
}


//Acordeones
var acc = document.getElementsByClassName("custom-accordion");


for (let i = 0; i < acc.length; i++) {
    acc[i].addEventListener("click", function () {
        this.classList.toggle("active");
        var panel = this.nextElementSibling;
        if (panel.style.maxHeight) {
            panel.style.maxHeight = null;
        } else {
            panel.style.maxHeight = panel.scrollHeight + "px";
        }
    });
}

