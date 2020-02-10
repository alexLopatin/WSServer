let socket = new WebSocket("ws://127.0.0.1/");

document.getElementById("button").onclick = function () {
    var inp = document.getElementById('input');
    socket.send(inp.value);
}
document.getElementById("close").onclick = function () {
    socket.close(1000, "clientClose");
}

socket.onopen = function (e) {
    alert("[open] Соединение установлено");
};

socket.onmessage = function (event) {
    var div = document.getElementById('chat');
    div.innerHTML += `<br>${event.data}`;
};


socket.onclose = function (event) {
    if (event.wasClean) {
        alert(`[close] Соединение закрыто чисто, код=${event.code} причина=${event.reason}`);
    } else {
        alert(`[close] Соединение прервано ${event.code}`);
    }
};

socket.onerror = function (error) {
    alert(`[error] ${error.message}`);
};