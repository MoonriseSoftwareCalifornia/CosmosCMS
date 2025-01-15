"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/ArticleHub").build();
var editConnections;

connection.on("ReceiveMessage",
    function(message) {
        editConnections = $.parseJSON(message);
        if (typeof editConnections === "undefined" || editConnections === null) {
            $("#articleChat").html("1 person editing");
        } else {
            var articleId = parseInt($("#Id").val());
            var count = 0;
            $.each(editConnections,
                function(index, connection) {
                    if (connection.ArticleId === articleId) {
                        count++;
                    }
                });
            if (count > 1) {
                $("#articleChat").html(count + " edit sessions");
            } else {
                $("#articleChat").html(count + " edit session");
            }
        }
    });

connection.on("PingMessage",
    function(message) {
        $("#articleChat").html(message);
        connection.invoke("SendMessage", articleId, "Editing HTML");
    });

connection.start().then(function() {
    //document.getElementById("sendButton").disabled = false;
    var articleId = parseInt($("#Id").val());
    connection.invoke("SendMessage", articleId, "Editing HTML");
}).catch(function(err) {
    return console.error(err.toString());
});

/*
document.getElementById("sendButton").addEventListener("click", function (event) {
    var user = document.getElementById("userName").value;
    var message = "Hello Word";
    connection.invoke("SendMessage", user, message).catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});
*/