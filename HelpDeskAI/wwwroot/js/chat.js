var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();

// Local variable to store the room information
var currentRoom;

//Disable the send button until connection is established.
document.getElementById("send-button").disabled = true;

connection.on("ReceiveMessage", function (user, message) {
    var messageContainer = document.getElementById("chat-messages");
    var messageElement = document.createElement("div");
    var currentUser = document.getElementById("user").value;
    // Check if the received message is from the current room
    if (user === currentUser) {
        messageElement.classList.add("message", "user-message");
    } else {
        messageElement.classList.add("message", "other-message");
    }

    messageElement.innerHTML = `<strong>${user}</strong>: ${message}`;
    messageContainer.appendChild(messageElement);
    scrollToBottom();
});

connection.start().then(function () {
    document.getElementById("send-button").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

document.getElementById("send-button").addEventListener("click", function (event) {
    var user = document.getElementById("user").value;
    var message = document.getElementById("message-input").value;

    console.log("Message:", message); // Debug log
    if (message.trim() != "") {

        connection.invoke("SendMessage", user, message, currentRoom).catch(function (err) {
            return console.error(err.toString());
        });
    }

        event.preventDefault();
        document.getElementById("message-input").value = "";
});
connection.on("SetCurrentRoom", function (room) {
    currentRoom = room;
});

connection.on("RenderOldMessages", function (oldMessages) {
    var messageContainer = document.getElementById("chat-messages");

    // Render each old message in the container
    oldMessages.forEach(function (message) {
        var messageContainer = document.getElementById("chat-messages");
        var messageElement = document.createElement("div");
        var currentUser = document.getElementById("user").value;
        // Check if the received message is from the current room
        if (message.senderUsername === currentUser) {
            messageElement.classList.add("message", "user-message");
        } else {
            messageElement.classList.add("message", "other-message");
        }

        messageElement.innerHTML = `<strong>${message.senderUsername}</strong>: ${message.message}`;
        messageContainer.appendChild(messageElement);
        scrollToBottom();
    });
});

function scrollToBottom() {
    var chatContainer = document.getElementById("chat-messages");
    chatContainer.scrollTop = chatContainer.scrollHeight;
}



