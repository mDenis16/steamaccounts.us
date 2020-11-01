using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace csgo.core
{
    public static class notifyManager
    {
        public class notification
        {
            public int id { get; set; }
            public int userId { get; set; }
            public string username { get; set; }
            public string message { get; set; }
            public DateTime date { get; set; }
            public bool seen { get; set; }
        }
        public static async Task<List<notification>> readNotifications(int userId, bool seen = false)
        {
            List<notification> notifications = new List<notification>();
            await databaseManager.selectQuery($"SELECT * FROM notifications WHERE userId = @userId", delegate (DbDataReader reader)
            {
                if (reader.HasRows)
                {
                    notification notification = new notification();
                    notification.date = (DateTime)reader["date"];
                    notification.userId = (int)reader["userId"];
                    notification.username = (string)reader["username"];
                    notification.message = (string)reader["message"];
                    notification.seen = (bool)reader["seen"];
                    notification.id = (int)reader["id"];
                    notifications.Add(notification);
                }
            }).addValue("@userId", userId).Execute();

            if (seen)
                await databaseManager.updateQuery($"UPDATE notifications SET seen = True WHERE userId = @userId AND seen = False").addValue("@userId", userId).Execute();

            return notifications.OrderByDescending(a=> a.date).ToList();
        }
        public static async Task sendNotification(int userId, string message)
        {

            await databaseManager.updateQuery($"INSERT INTO notifications (message, userId) VALUES (@message, @userId)").addValue("@userId", userId).addValue("@message", message).Execute();
        
        }
        public enum notifyType
        {
            error,
            success,
            warning,
            message
        }
 
            public static async Task sendNotify( this csgo.usersManager.userData userData, notifyType type, string message )
           {
        
            if ( userData.connectionId == null )
                return;
            await csgo.core.ChatHub.Current.Clients.Client( userData.connectionId ).SendAsync( "notify", ( int ) type, message );
          
           }
    }
}
