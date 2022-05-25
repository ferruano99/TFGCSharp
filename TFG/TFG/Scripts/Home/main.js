/** CARGA DEL MAPA **/
//Creamos mapa
//https://stackoverflow.com/questions/28599128/how-to-add-a-loading-screen-when-doing-a-ajax-call-in-phonegap

let map = L.map('map').setView([40.4157390642727, -3.7071191753628954], 13); //mirando a sol

let url = ""
//Cargamos un layer para el mapa
L.tileLayer('https://{s}.tile.jawg.io/jawg-sunny/{z}/{x}/{y}{r}.png?access-token=68JcDnfmL9PaKsidgZYRsQGkslfD17uuGT4AsPT6zRDD7uMnBSb4W4lGkVfKI72Y', {
    attribution: 'Datos de los mapas: &copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
}).addTo(map);


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
                response($.ui.autocomplete.filter(data.Stations, request.term))
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
    minLength: 1
});

$('#end').autocomplete({
    source: function (request, response) {
        $.ajax({
            url: url + "Home/GetAllNameStations",
            dataType: "json",
            method: "GET",
            success: function (data) {
                response($.ui.autocomplete.filter(data.Stations, request.term))
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
    formData.append("Start", $("#start").val())
    formData.append("End", $("#end").val())

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
                L.marker(latlng,
                    {
                        icon: markerIcon
                    }).addTo(map)
            }
            L.polyline(coords, {color: "red"}).addTo(map)
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



//acordeones
var allAccordions = $('.custom-accordion');
