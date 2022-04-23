/** CARGA DEL MAPA **/
//Creamos mapa
//https://stackoverflow.com/questions/28599128/how-to-add-a-loading-screen-when-doing-a-ajax-call-in-phonegap
let map = L.map('map').setView([40.4157390642727, -3.7071191753628954], 13);
let url = ""

//Cargamos un layer para el mapa
L.tileLayer('https://{s}.tile.jawg.io/jawg-sunny/{z}/{x}/{y}{r}.png?access-token=68JcDnfmL9PaKsidgZYRsQGkslfD17uuGT4AsPT6zRDD7uMnBSb4W4lGkVfKI72Y', {
    attribution: 'Datos de los mapas: &copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
}).addTo(map);


/** PARSEO DE LAS CALLES **/
//Funciones para escoger la address
var coords = []
var origen = document.getElementById("start");
var destino = document.getElementById("destination");


//IDA
$("#submit-ida").click(function () {
    if ($("#start").val() == "") {
        Swal.fire({
            title: 'Origen',
            text: 'Por favor, escriba un punto de partida',
            icon: 'error',
            confirmButtonText: 'Aceptar',
            confirmButtonColor: '#dd4646'
        });
    } else {
        busquedaViajes("resultados-ida", origen)
    }
});

origen.addEventListener("keyup", (event) => {
    if (event.key == "Enter") {
        if ($("#start").val() == "") {
            Swal.fire({
                title: 'Origen',
                text: 'Por favor, escriba un punto de partida',
                icon: 'error',
                confirmButtonText: 'Aceptar',
                confirmButtonColor: '#dd4646'
            });
        } else
            busquedaViajes("resultados-ida", origen);
    }
})

//VUELTA
$("#submit-vuelta").click(function () {
    if ($("#destination").val() == "") {
        Swal.fire({
            title: 'Escriba el destino',
            icon: 'warning',
            confirmButtonText: 'Aceptar',
            confirmButtonColor: '#dd4646'
        });
    } else
        busquedaViajes("resultados-vuelta", destino)
});

destino.addEventListener("keyup", (event) => {
    if (event.key == "Enter") {
        if ($("#destination").val() == "") {
            Swal.fire({
                title: 'Escriba el origen',
                icon: 'warning',
                confirmButtonText: 'Aceptar',
                confirmButtonColor: '#dd4646'
            });
        } else
            busquedaViajes("resultados-vuelta", destino);
    }
});


function add_valores(sitios, id) {
    //RECOGER EL DIV
    if (id == "resultados-ida") {
        //Mostramos el destino
        $("#" + id).css("display", "block");
        //quitamos los resultados
        $("#write-origen").css("display", "none");

    } else { //Caso de la vuelta
        $("#write-destino").css("display", "none");
        $("#resultados-vuelta").css("display", "block");
    }
    if (sitios.length > 0) {
        //Metemos en el HTML la respuesta
        for (let i = 0; i < sitios.length; i++) { //display_name contiene el nombre del sitio
            //Quiero coger la información relevante
            var limite = sitios[i].display_name.indexOf(", Comunidad de Madrid");
            var nuevoDato = sitios[i].display_name.substring(0, limite) + "."; //string[]

            $("#" + id).append('<div class="sitio" onclick="add_coords(' + sitios[i].lat + ',' + sitios[i].lon + ',\'' + id + '\')">' + nuevoDato + '</div>')

        }
    } else {
        document.getElementById(id).innerHTML = "<p class=\"error\"> No hay sitios disponibles</p>"
    }

}


function add_coords(lat, lng, id) {
    let sitio = [lat, lng];
    coords.push(sitio);

    if (id == "resultados-ida") {
        //Ponemos el destino
        $("#destino").css("display", "block");
        $("#origen").css("display", "none");
    } else {
        $(".table").css("display", "none");
        $("#map").css("display", "block");
        map.invalidateSize();


        // console.log(JSON.stringify({
        //     origenLat: coords[0][0],
        //     origenLon: coords[0][1],
        //     destinoLat: coords[1][0],
        //     destinoLon: coords[1][1]
        // }))
        var form = new FormData();
        form.append("latOrig", coords[0][0])
        form.append("lonOrig", coords[0][1])
        form.append("latDest", coords[1][0])
        form.append("lonDest", coords[1][1])

        //$.getJSON(url, {
        //    origenLat: coords[0][0],
        //    origenLon: coords[0][1],
        //    destinoLat: coords[1][0],
        //    destinoLon: coords[1][1]
        //}, function (data) {
        //    console.log(data)
        //})

        $.ajax({
            url: url + "Home/FindPath",
            dataType: "json",
            data: form,
            processData: false,
            async: false,
            method: "POST",
            contentType: false,
            success: function (data) {
                console.log(data)
            },
            error: function (requestObject, error, errorThrown) {
                console.log(requestObject.responseText)
            }
        });
    }
}


function busquedaViajes(id, input) {
    var xmlhttp = new XMLHttpRequest(), //para recoger la información
        method = "GET",
        url = "https://nominatim.openstreetmap.org/search?format=json&limit=10&q=" + input.value;
    if (!input.value.includes("Comunidad de Madrid")) {
        url += ", Comunidad de Madrid"
    }
    xmlhttp.onreadystatechange = function () {
        if (this.readyState === 4 && this.status === 200) {
            var myArr = JSON.parse(this.responseText);
            add_valores(myArr, id);
            if (!$("#return").is(':visible')) {
                $("#return").css('display', 'inline-block')
            }
        }
    };
    xmlhttp.open(method, url, true);
    xmlhttp.send();
}

function returnButton() {
    //Vaciamos el array
    let coordsAux = []
    coords = coordsAux
    //Los resultados los dejamos en blanco
    $("#resultados-ida").empty()
    $("#resultados-ida").append("<h1>ELIJA UN SITIO</h1>")
    $("#resultados-vuelta").empty()
    $("#resultados-vuelta").append("<h1>ELIJA UN SITIO</h1>")

    //Activamos el textbox de ida y desactivamos el de vuelta
    $("#origen").css("display", "block");
    $("#write-origen").css("display", "block");
    $("#resultados-ida").css("display", "none");
    $("#destino").css("display", "none");
    $("#write-destino").css("display", "block");
    $("#resultados-vuelta").css("display", "none");
    $("#return").css("display", "none");
}
