using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace csgo.core
{
    public class ChatHub : Hub
    {
        public static IHubContext<ChatHub> Current { get; set; }
        public class user
        {
           public string connectionId { get; set; }
           public string sessionid { get; set; }
            public int userId { get; set; }
            public user( ) { }
            public user(string connectionId, string sessionId, int userId )
            {
                this.connectionId = connectionId;
                this.sessionid = sessionid;
                this.userId = userId;
            }
        }
        public static List<user> connections = new List<user>();

        public override async Task OnDisconnectedAsync( Exception exception )
        {
            var user = csgo.usersManager.users.Find(a=> a.connectionId == Context.ConnectionId);

            if ( user != null )
                user.connectionId = null;
            var index = donationConnections.FindIndex(a => a == Context.ConnectionId);
            if (index != -1)
                donationConnections.RemoveAt(index);

            await base.OnDisconnectedAsync( exception );
        }
        public static List<string> donationConnections = new List<string>();
        public async Task connectedDonation()
        {
           
            donationConnections.Add(Context.ConnectionId);

        }
        public async Task connectedUser( string sessionid )
        {
            string ip = Context.GetHttpContext().Request.getIPAddress();
            var user = csgo.usersManager.users.Find(a=> a.cookie == sessionid && a.loginIP == ip);
           
            if ( user != null )
            {
                
      
                    user.connectionId = Context.ConnectionId;
                    user.lastRequestTime = DateTime.Now;
                    //user.lastIP = Context.GetHttpContext( ).Connection.RemoteIpAddress.ToString( );
                    user.lastRequest = Context.GetHttpContext( ).Request;
                   // csgo.usersManager.users.Find( a => a.cookie == sessionid && a.loginIP == ip ).connectionId = Context.ConnectionId;
                
                Console.WriteLine( "conected user " + user.username + "connection id " + user.connectionId );
            }
            else
                Console.WriteLine( "User is'nt connected." );

        }
    }
    
}