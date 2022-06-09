/** CARGA DEL MAPA **/
//Creamos mapa
//https://stackoverflow.com/questions/28599128/how-to-add-a-loading-screen-when-doing-a-ajax-call-in-phonegap

let map = L.map('map').setView([40.4157390642727, -3.7071191753628954], 13); //mirando a sol

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
var otherTransports = [] //Coche, bici, andando




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
    source: function (request, response) {
        $.ajax({
            url: url + "Home/GetAllNameStations",
            dataType: "json",
            method: "GET",
            success: function (data) {
                var results = $.ui.autocomplete.filter(data.Stations, request.term)
                response(results.slice(0, 15))
            },
            error: function (requestObject, error, errorThrown) {
                console.log(requestObject.responseText)
            }
        })
    },
    change: function (event, ui) {
        if (ui.item == null) {
            this.value = "";
        }
    },
    minLength: 2
});

$('#end').autocomplete({
    source: function (request, response) {


        $.ajax({
            url: url + "Home/GetAllNameStations",
            dataType: "json",
            method: "GET",
            success: function (data) {
                var results = $.ui.autocomplete.filter(data.Stations, request.term)
                response(results.slice(0, 15))
            },
            error: function (requestObject, error, errorThrown) {
                console.log(requestObject.responseText)
            }
        })
    },
    minLength: 1,
    change: function (event, ui) {
        if (ui.item == null) {
            this.value = "";
        }
    }
});


//Recogida de datos y llamada al back
$("#submit").on('click', function () {
    //comprobar que los datos están bien


    //quitamos todas las layers de rutas
    layerGroup.clearLayers();

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
                        elem.type = "CERCANÍAS";
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

            var formDataItinero = new FormData()
            formDataItinero.append("coords", coordsItinero)
            $.ajax({
                url: url + "Home/GetItineroRoutes",
                dataType: "json",
                data: formDataItinero,
                processData: false,
                async: true,
                method: "POST",
                contentType: false,
                beforeSend: function () {
                    $("#loading").removeClass('hide')

                },
                success: function (data) {
                    otherTransports = data;
                    $("#info-car").html(
                        '<p><b>Tiempo:</b> ' + (data.Car.Time / 60).toPrecision(3) + ' min</p>' +
                        '<p><b>Distancia:</b> ' + (data.Car.Distance / 1000).toPrecision(3) + ' km</p>')
                    $("#info-bicycle").html(
                        '<p><b>Tiempo:</b> ' + (data.Bicycle.Time / 60).toPrecision(3) + ' min</p>' +
                        '<p><b>Distancia:</b> ' + (data.Bicycle.Distance / 1000).toPrecision(3) + ' km</p>')
                    $("#info-pedestrian").html(
                        '<p><b>Tiempo:</b> ' + (data.Pedestrian.Time / 60).toPrecision(3) + ' min</p>' +
                        '<p><b>Distancia:</b> ' + (data.Pedestrian.Distance / 1000).toPrecision(3) + ' km</p>')

                    $("#results").removeClass('hide');
                },
                complete: function () {
                    $("#loading").addClass('hide')
                },
                error: function (requestObject, error, errorThrown) {
                    console.log(requestObject.responseText)
                }
            });
        },

        error: function (requestObject, error, errorThrown) {
            console.log(requestObject.responseText)
        }
    });


});



$("#car-route").on('click', function () {
    $(".custom-accordion").removeClass('route-selected');
    $("#car").addClass('route-selected');

    layerGroup.clearLayers();
    var carCoords = []
    otherTransports.Car.Shape.map(coord => {
        var newCoord = [coord.Latitude, coord.Longitude]
        carCoords.push(newCoord)
    });

    for (let i = 0; i < waypoints.length; i++)
        L.marker(waypoints[i], { icon: marker }).addTo(layerGroup);
    map.flyTo(carCoords[parseInt(carCoords.length / 2)], 14);
    L.polyline(carCoords, { color: 'blue' }).addTo(layerGroup);

});

$("#foot-route").on('click', function () {
    $(".custom-accordion").removeClass('route-selected');
    $("#pedestrian").addClass('route-selected');

    layerGroup.clearLayers();

    var pedestrianCoords = []
    otherTransports.Car.Shape.map(coord => {
        var newCoord = [coord.Latitude, coord.Longitude]
        pedestrianCoords.push(newCoord)
    });

    for (let i = 0; i < waypoints.length; i++)
        L.marker(waypoints[i], { icon: marker }).addTo(layerGroup);
    map.flyTo(pedestrianCoords[parseInt(pedestrianCoords.length / 2)], 14);
    L.polyline(pedestrianCoords, { color: 'blue' }).addTo(layerGroup);

});

$("#bicycle-route").on('click', function () {
    $(".custom-accordion").removeClass('route-selected');
    $("#bicycle").addClass('route-selected');

    layerGroup.clearLayers(); //limpiamos

    var bicycleCoords = []
    otherTransports.Bicycle.Shape.map(coord => {
        var newCoord = [coord.Latitude, coord.Longitude]
        bicycleCoords.push(newCoord) //reestrucutramos coordenadas para leaflet
    });

    for (let i = 0; i < waypoints.length; i++)
        L.marker(waypoints[i], { icon: marker }).addTo(layerGroup);

    map.flyTo(bicycleCoords[parseInt(bicycleCoords.length / 2)], 14);
    L.polyline(bicycleCoords, { color: 'blue' }).addTo(layerGroup); //pintamos

});

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
        if (i != 0 && i != ptransport.length - 1)
            L.marker(latlng,
                {
                    icon: markerIcon
                })
                .bindPopup(
                    "<p><b>" + ptransport[i].type + "</b></p>" +
                    "<b>L&#237;nea</b> " + ptransport[i].linea + "<br/>" +
                    "<b>Estaci&#243;n</b> " + ptransport[i].denominacion + "<br/>")
                .addTo(layerGroup)
    }

    //colorear las líneas
    var lines = getLines(ptransport);

    for (let i = 0; i < waypoints.length; i++)
        L.marker(waypoints[i], { icon: marker }).addTo(layerGroup);


    for (let i = 0; i < lines.length; i++) {  //pintamos con líneas
        var linesCoords = ptransport.filter(elem => elem.linea == lines[i]).map(elem => [elem.lat, elem.lon]);

        var colorAndWeight = metroColors.filter(elem => elem.line == lines[i]).flatMap(elem => [elem.color, elem.weight])

        L.polyline(linesCoords, { color: colorAndWeight[0], weight: colorAndWeight[1] }).addTo(layerGroup)
    }

});


function getLines(ptransport) {
    var linesList = ptransport.map(elem => elem.linea);
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

