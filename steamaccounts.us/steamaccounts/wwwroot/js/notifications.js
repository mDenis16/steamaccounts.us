

document.addEventListener('DOMContentLoaded', function () {

   
    var sessionid = readCookie('sessionid');
    if (sessionid) {
        var connection = new signalR.HubConnectionBuilder()
            .withUrl('/ws')
            .build();

        // Create a function that the hub can call to broadcast messages.
        connection.on('notify', function (type, message) {
            // Html encode display name and message.
            switch (type) {
                case 0:
                    toastr.error(message);
                    break;
                case 1:
                    toastr.success(message);
                    break;
                case 2:
                    toastr.warning(message);
                    break;
                case 3:
                    toastr.info(message);
                    break;

            }
        });

      
        connection.on('refreshTicket', (fromUsername, ticketId) => {
            if (window.location.pathname == "/ticket/viewDispute") {
                reloadTicket();
            }
            else {
                toastr.info(`${fromUsername} respond to your ticket #${ticketId}.`);
            }
            console.log('refreshgTicketsada');
        });
        connection.on('logout', function () {
          
            document.cookie = "sessionid=; expires=Thu, 01 Jan 1970 00:00:01 GMT;";
            
            setTimeout(() => {
                location.reload();
            }, 1000);
        });

        
        // Transport fallback functionality is now built into start.
        connection.start()
            .then(function () {
                console.log('connection started');

                connection.invoke('connectedUser', encodeURI(sessionid));
            
            })
            .catch(error => {
                console.error(error.message);
            });
    }
});
function readCookie(name) {
    var nameEQ = name + "=";
    var ca = document.cookie.split(';');
    for (var i = 0; i < ca.length; i++) {
        var c = ca[i];
        while (c.charAt(0) == ' ') c = c.substring(1, c.length);
        if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length, c.length);
    }
    return null;
}
