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
let l1 = "#30a3dc"
let l2 = "#e0292f"
let l3 = "#ffe114"
let l4 = "#814109"
let l5 = "#96bf0d"
let l6 = "#9a9999"
let l7 = "#f96611"
let l8 = "#f373b7"
let l9 = "#990d66"
let l10 = "#1b0c80"
let l11 = "#136926"
let l12 = "#999933"