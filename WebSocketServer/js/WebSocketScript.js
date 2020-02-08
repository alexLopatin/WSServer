let socket = new WebSocket("ws://192.168.1.45/");

socket.onopen = function (e) {
    alert("[open] Соединение установлено");
    //alert("Отправляем данные на сервер");
    socket.send("Меня зовут Джон");
};

socket.onmessage = function (event) {
    alert(`[message] Данные получены с сервера: ${event.data}`);
};

socket.onclose = function (event) {
    if (event.wasClean) {
        alert(`[close] Соединение закрыто чисто, код=${event.code} причина=${event.reason}`);
    } else {
        // например, сервер убил процесс или сеть недоступна
        // обычно в этом случае event.code 1006
        alert(`[close] Соединение прервано ${event.code}`);
    }
};

socket.onerror = function (error) {
    alert(`[error] ${error.message}`);
};