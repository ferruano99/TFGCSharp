/** CARGA DEL MAPA **/
//Creamos mapa
//https://stackoverflow.com/questions/28599128/how-to-add-a-loading-screen-when-doing-a-ajax-call-in-phonegap

let map = L.map('map').setView([40.4157390642727, -3.7071191753628954], 13); //mirando a sol

let url = ""
//Cargamos un layer para el mapa
L.tileLayer('https://{s}.tile.jawg.io/jawg-sunny/{z}/{x}/{y}{r}.png?access-token=68JcDnfmL9PaKsidgZYRsQGkslfD17uuGT4AsPT6zRDD7uMnBSb4W4lGkVfKI72Y', {
    attribution: 'Datos de los mapas: &copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
}).addTo(map);

var waypoints = [] //origen y destino

//Variables colores metro https://colorswall.com/palette/106461
const metroColors = [
{
        line: "1",
        color: "#30a3dc"
    },
    {
        line: "2",
        color: "#e0292f"
    },
    {
        line: "3",
        color: "#ffe114"
    },
    {
        line: "4",
        color: "#814109"
    },
    {
        line: "5",
        color: "#96bf0d"
    },
    {
        line: "6",
        color: "#9a9999"
    },
    {
        line: "6-2",
        color: "#9a9999"
    },
    {
        line: "7",
        color: "#f96611"
    },
    {
        line: "8",
        color: "#f373b7"
    },
    {
        line: "9",
        color: "#990d66"
    },
    {
        line: "10",
        color: "#1b0c80"
    },
    {
        line: "11",
        color: "#136926"
    },
    {
        line: "12",
        color: "#999933"
    },
    {
        line: "12-2",
        color: "#999933"
    },
    {
        line: "R",
        color: "#FFFFFF"
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
                response(results.slice(0,15))
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


$("#submit").on('click', function () {
    var formData = new FormData();
    //split para recoger por una parte el tipo de transporte y por otra el nombre de la estacion
    var start = $("#start").val().split(" - ")
    var end = $("#end").val().split(" - ")


    formData.append("Start", start)
    formData.append("End", end)

    $.ajax({
        url: url + "Home/FindPath",
        dataType: "json",
        data: formData,
        processData: false,
        async: true,
        method: "POST",
        contentType: false,
        beforeSend: function () {
            $("#loading").removeClass('hide')

        },
        success: function (data) {
            waypoints = data.Waypoints


            var path = data.Path
            console.log(data.Car)
            var markerIcon = L.icon({
                iconUrl: 'Content/Images/circle.png',
                iconSize: [24, 24],
            });
            var coords = []
            for (let i = 0; i < path.length; i++) {
                var latlng = L.latLng(path[i].lat, path[i].lon)
                coords.push(latlng)
                var marker = L.marker(latlng,
                    {
                        icon: markerIcon
                    }).addTo(map)
            }
            var line = L.polyline(coords, { color: "red" }).addTo(map)
            map.addLayer(marker)
            map.addLayer(line)
            console.log(data)
        },
        complete: function () {
            $("#loading").addClass('hide')
        },
        error: function (requestObject, error, errorThrown) {
            console.log(requestObject.responseText)
        }
    });
});

$("#car").on('click', function () {
    console.log(waypoints)
})





//Acordeones
var acc = document.getElementsByClassName("custom-accordion");
var i;

for (i = 0; i < acc.length; i++) {
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

