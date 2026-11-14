// Venue map page: Leaflet 1.9.4 with OpenStreetMap tiles, hosted in a .NET MAUI HybridWebView.
// Messages are raw JSON strings in both directions (see Docs design § 2.6).
// JS -> C#: {"type":"ready"} once the map exists; {"type":"tap","lat":n,"lon":n} on a map click.
// C# -> JS: {"type":"view","lat":n,"lon":n,"zoom":n,"marker":bool} centres the map and places
// (marker true) or removes (marker false) the single marker.

(function () {
    "use strict";

    var TILE_URL = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
    var ATTRIBUTION = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap contributors</a>';
    var MAX_ZOOM = 19;
    var WORLD_CENTER = [0, 0];
    var WORLD_ZOOM = 2;

    var map = L.map("map", { zoomControl: true, attributionControl: true })
        .setView(WORLD_CENTER, WORLD_ZOOM);

    L.tileLayer(TILE_URL, {
        maxZoom: MAX_ZOOM,
        attribution: ATTRIBUTION
    }).addTo(map);

    var marker = null;

    function placeMarker(lat, lon) {
        if (marker === null) {
            marker = L.marker([lat, lon]).addTo(map);
        } else {
            marker.setLatLng([lat, lon]);
        }
    }

    function removeMarker() {
        if (marker !== null) {
            map.removeLayer(marker);
            marker = null;
        }
    }

    function send(message) {
        window.HybridWebView.SendRawMessage(JSON.stringify(message));
    }

    map.on("click", function (e) {
        placeMarker(e.latlng.lat, e.latlng.lng);
        send({ type: "tap", lat: e.latlng.lat, lon: e.latlng.lng });
    });

    window.addEventListener("HybridWebViewMessageReceived", function (e) {
        var msg = JSON.parse(e.detail.message);
        if (msg.type !== "view") {
            return;
        }
        map.setView([msg.lat, msg.lon], msg.zoom);
        if (msg.marker) {
            placeMarker(msg.lat, msg.lon);
        } else {
            removeMarker();
        }
    });

    send({ type: "ready" });
})();
