using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace csgo.core
{
    public class ticketsManager
    {
        public class ticket
        {
            public int csgoId { get; set; }
            public string against { get; set; }
            public int againstId { get; set; }
            public string fromUser { get; set; }
            public int fromUserId { get; set; }
            public List<string> participants {get;set; }
            public type type { get; set; }
            public bool closed { get; set; }
            public DateTime creationTime { get; set; }
            public int id { get; set; }
            public List<message> messages { get; set; }
        }

        public class message
        {
            public int id { get; set; }
            public int ticketId { get; set; }
            public int fromId { get; set; }
            public string fromUsername { get; set; }
            public string messageData { get; set; }
            public DateTime date { get; set; }
        }
        public enum type
        {
            ticket,
            dispute
        }


        public static async Task<List<ticket>> readAllTickets()
        {
            List<ticket> ticketList = new List<ticket>();
            await databaseManager.selectQuery($"SELECT * FROM tickets", delegate (DbDataReader reader)
            {
                if (reader.HasRows)
                {
                    ticket ticket = new ticket();
                    ticket.csgoId = ( int ) reader[ "csgoId" ];
                    ticket.creationTime = ( DateTime ) reader[ "creationTime" ];
                    ticket.against = ( string ) reader[ "against" ];
                    ticket.againstId = ( int ) reader[ "againstId" ];
                    ticket.closed = ( bool ) reader[ "closed" ];
                    ticket.fromUser = ( string ) reader[ "fromUser" ];
                    ticket.fromUserId = ( int ) reader[ "fromUserId" ];
                    ticket.id = ( int ) reader[ "id" ];
                    ticket.type = ( type ) ( int ) reader[ "type" ];
                    ticketList.Add( ticket );
                }
            }).Execute();
            return ticketList;
        }
        public static async Task<ticket> getTicketData( int id )
        {
            var ticket = new ticket();
            ticket.messages = new List<message>( );
            await databaseManager.selectQuery( $"SELECT * FROM tickets WHERE id = '{id}'", delegate ( DbDataReader reader )
            {
                if ( reader.HasRows )
                {
          
                    ticket.csgoId = ( int ) reader[ "csgoId" ];
                    ticket.creationTime = ( DateTime ) reader[ "creationTime" ];
                    ticket.against = ( string ) reader[ "against" ];
                    ticket.againstId = ( int ) reader[ "againstId" ];
                    ticket.fromUser = ( string ) reader[ "fromUser" ];
                    ticket.fromUserId = ( int ) reader[ "fromUserId" ];
                    ticket.id = ( int ) reader[ "id" ];
                    ticket.closed = ( bool ) reader[ "closed" ];
                    ticket.type = ( type ) ( int ) reader[ "type" ];

                }
            } ).Execute( );
            await databaseManager.selectQuery( $"SELECT * FROM ticketsdata WHERE ticketId  = '{id}'", delegate ( DbDataReader reader )
            {
                if ( reader.HasRows )
                {
                    var message = new message();
                    message.id = ( int ) reader[ "id" ];
                    message.ticketId = ( int ) reader[ "ticketId" ];
                    message.fromId = ( int ) reader[ "fromId" ];
                    message.date = ( DateTime ) reader[ "date" ];
                    message.fromUsername = ( string ) reader[ "fromUsername" ];
                    message.messageData = ( string ) reader[ "message" ];
                    ticket.messages.Add( message );
                }
            } ).Execute( );
            return ticket;
         }
        
        public static async Task<bool> existTicket(int ticketId )
        {
            var exist = false;
            await databaseManager.selectQuery( $"SELECT * FROM tickets WHERE id = '{ticketId}' LIMIT 1", delegate ( DbDataReader reader )
            {
                exist = reader.HasRows;
            } ).Execute( );
            return exist;
        }
       
        public static async Task<int> addMessageToTicket( int ticketId, int fromID, string fromUsername, string message)
        {
            if ( !await existTicket( ticketId ) )
                return -1;

            

           var ticketData = await  csgo.core.ticketsManager.getTicketData( ticketId );
            if ( ticketData.closed )
                return -1;
            ticketData.messages.Select( a => a.fromId ).Distinct( ).ToList().ForEach(async a =>
            {
                var user = csgo.usersManager.users.Find(b=> b.id == a && b.id != fromID);
                if (a != fromID)
                   await csgo.core.notifyManager.sendNotification(ticketData.fromUserId, $"{fromUsername} responded to your dispute with id #{ticketId}.");
                if (user != null && user.connectionId != null )
                {
                    if ( user.lastRequest != null )
                        csgo.Controllers.adminController.tokenAccess.createToken( user.lastRequest, csgo.Controllers.adminController.tokenType.readticket );

                    await ChatHub.Current.Clients.Client(user.connectionId).SendAsync("refreshTicket", fromUsername, ticketId);
                }
            } );
          

            return  await databaseManager.updateQuery( $"INSERT INTO ticketsdata (ticketId, fromId, fromUsername, message) VALUES (@ticketId, @fromID, @fromUsername, @message)" )
                .addValue("@ticketId", ticketId).addValue("@fromID", fromID).addValue("@fromUsername", fromUsername).addValue("@message", message).Execute( );  
        }
        public static async Task<bool> setTicketStatus( int ticketId, int fromID, string fromUsername, bool open)
        {
            if ( !await existTicket( ticketId ) )
                return false;

            var ticketData = await  csgo.core.ticketsManager.getTicketData( ticketId );
            if (open)
            {
                await csgo.core.notifyManager.sendNotification(ticketData.fromUserId, $"Your dispute against {ticketData.against} with id #{ticketId} was reopened by admin {fromUsername}.");
                await csgo.core.notifyManager.sendNotification(ticketData.fromUserId, $"Your dispute from {ticketData.fromUser} with id #{ticketId} was reopened by admin {fromUsername}.");
            }
            else
            {
                await csgo.core.notifyManager.sendNotification(ticketData.fromUserId, $"Your dispute against {ticketData.against} with id #{ticketId} was closed by admin {fromUsername}.");
                await csgo.core.notifyManager.sendNotification(ticketData.fromUserId, $"Your dispute from {ticketData.fromUser} with id #{ticketId} was closed by admin {fromUsername}.");
            }

                ticketData.messages.Select(a => a.fromId).Distinct().ToList().ForEach(async a =>
           {
               var user = csgo.usersManager.users.Find(b => b.id == a && b.id != fromID);
               if (user != null && user.connectionId != null)
               {
                   if (user.lastRequest != null)
                   {
                       if (open)
                           await csgo.core.notifyManager.sendNotify(user, notifyManager.notifyType.warning, $"Your ticket with id #{ticketId} was reopened by admin {fromUsername}.");
                       else
                       {
                           await csgo.core.notifyManager.sendNotify(user, notifyManager.notifyType.warning, $"Your ticket with id #{ticketId} was closed by admin {fromUsername}.");

                       }
                       await ChatHub.Current.Clients.Client(user.connectionId).SendAsync("refreshTicket", fromUsername, ticketId);
                   }
               }
           });
            await databaseManager.updateQuery( $"UPDATE tickets SET closed = {open} WHERE id = '{ticketId}' LIMIT 1" ).Execute( );
            return true;

        }
        public static async Task<int> createDispute(int csgoId, string against, int againstId, string fromUser, int fromUserId, type type)
        {
            

            var id = await databaseManager.updateQuery($"INSERT INTO tickets (csgoId, against, againstId, fromUser, fromUserId, type) VALUES ('{csgoId}', '{against}', '{againstId}', '{fromUser}', '{fromUserId}', '{(int)type}')").Execute();
            await databaseManager.updateQuery( $"UPDATE csgoaccounts SET ticketId = '{id}' WHERE id = '{csgoId}' LIMIT 1" ).Execute( );
            await csgo.core.notifyManager.sendNotification(id, $"{fromUser} created a dispute against  you for account with id {csgoId}. Check your profile.");
            var account = csgo.accountsManager.csgoAccounts.Find(a=> a.id == csgoId);
            if ( account != null )
                account.ticketId = id;
            return id;
        }

    }
}
