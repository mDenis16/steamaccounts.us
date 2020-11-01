using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace csgo.core
{
    public class balanceManager
    {
        public class withdrawRequest
        {
            public int id { get; set; }
            public string username { get; set; }
            public int userId { get; set; }
            public DateTime date { get; set; }
            public decimal amount { get; set; }
            public bool accepted { get; set; }
            public string paypalemail { get; set; }
        }
        public static async Task<int> createWithdrawRequest(int userId, string username, decimal amount, string paypalemail )
        {
            return await databaseManager.updateQuery( $"INSERT INTO withdrawRequests (username, userId, amount, paypalemail) VALUES ( '{username}', '{userId}', '{amount}', '{paypalemail}') " ).Execute( );
        }
        public static async Task<withdrawRequest> getWithdrawRequest( int id )
        {
            withdrawRequest request =  new withdrawRequest();
            await databaseManager.selectQuery( $"SELECT * FROM withdrawRequests WHERE id = '{id}'", delegate ( DbDataReader reader )
            {
                if ( reader.HasRows )
                {
                    request.id = ( int ) reader[ "id" ];
                    request.username = ( string ) reader[ "username" ];
                    request.paypalemail = ( string ) reader[ "paypalemail" ];
                    request.userId = ( int ) reader[ "userId" ];
                    request.date = ( DateTime ) reader[ "date" ];
                    request.amount = ( decimal ) reader[ "amount" ];
                    request.accepted = ( bool ) reader[ "accepted" ];
                }
            } ).Execute( );
            return request;
        }
        public static async Task<List<withdrawRequest>> getAllWithdrawRequests(  )
        {
            List<withdrawRequest> requests =  new List<withdrawRequest>();
            await databaseManager.selectQuery( $"SELECT * FROM withdrawRequests", delegate ( DbDataReader reader )
            {
                if ( reader.HasRows )
                {
                    withdrawRequest request = new withdrawRequest();
                    request.id = ( int ) reader[ "id" ];
                    request.username = ( string ) reader[ "username" ];
                    request.userId = ( int ) reader[ "userId" ];
                    request.date = ( DateTime ) reader[ "date" ];
                    request.amount = ( decimal ) reader[ "amount" ];
                    request.accepted = ( bool ) reader[ "accepted" ];
                    request.paypalemail = ( string ) reader[ "paypalemail" ];
                    requests.Add( request );
                }
            } ).Execute( );
            return requests;
        }
        public class balanceInfo
        {
            public decimal inHold { get; set; }
            public decimal withdrawable { get; set; }
        }
        public static async Task checkForIncomingBalance( int userId )
        {
            var account = csgo.usersManager.users.Find(a=> a.id == userId);
            if ( account != null &&  (( DateTime.Now - account.lastBalanceCheck ).TotalMinutes > 30 )) 
            {
                List<csgo.core.logsManager.transactions.transaction> deposits = new List<csgo.core.logsManager.transactions.transaction>();

                _ = databaseManager.selectQuery( $"SELECT * FROM transactions WHERE userId = '{userId}' AND status = '{(int)csgo.core.logsManager.transactions.transactionStatus.inpending}'  AND type = '{(int)csgo.core.logsManager.transactions.transactionType.sell}' AND CURRENT_DATE() >= confirmDate ", delegate ( DbDataReader reader )
                  {
                      if ( reader.HasRows )
                      {
                          csgo.core.logsManager.transactions.transaction log = new csgo.core.logsManager.transactions.transaction();
                          log.userID = ( int ) reader[ "userId" ];
                          log.id = ( int ) reader[ "id" ];
                          log.username = ( string ) reader[ "username" ];
                          log.email = ( string ) reader[ "email" ];
                          log.method = ( csgo.core.logsManager.transactions.methodType ) ( int ) reader[ "method" ];
                          log.amount = ( decimal ) reader[ "amount" ];
                          log.type = ( csgo.core.logsManager.transactions.transactionType ) ( int ) reader[ "type" ];
                          log.status = ( csgo.core.logsManager.transactions.transactionStatus ) ( int ) reader[ "status" ];
                      
                          log.date = ( DateTime ) reader[ "date" ];
                          deposits.Add( log );
                      }
                  } ).Execute( ).Result;

                if ( deposits.Count > 0 )
                {
                    decimal moneyPlus = 0.0m;
                    deposits.ForEach( a =>
                    {
                   //  if (a.confirmDate >= DateTime.Now)
                          moneyPlus += a.amount;
                    } );

                    await csgo.core.balanceManager.addUserBalance( account.id, moneyPlus );
                    await databaseManager.updateQuery( $"UPDATE transactions SET status = '{( int ) csgo.core.logsManager.transactions.transactionStatus.complete}' WHERE   CURRENT_DATE() >= confirmDate AND type = '{( int ) csgo.core.logsManager.transactions.transactionType.sell}' AND  status = '{( int ) csgo.core.logsManager.transactions.transactionStatus.inpending}' AND userId = '{userId}'" ).Execute( );
                   await csgo.core.notifyManager.sendNotification( userId, $"You received {moneyPlus} from transactions. " );
                }
                Console.WriteLine( ( DateTime.Now - account.balanceCheck ).TotalMinutes );
                await databaseManager.updateQuery( $"UPDATE users SET lastBalanceCheck = CURRENT_TIMESTAMP WHERE id = '{userId}'" ).Execute( );
                Console.WriteLine( csgo.usersManager.users.Find( a => a.id == userId ).lastBalanceCheck.ToString( ) );
                account.lastBalanceCheck = DateTime.Now;
            }


        }
       
        public static async Task<bool> addUserBalance(int userId, decimal amount)
        {
            try
            {
                var siteUser = usersManager.users.Find(a => a.id == userId);
                if ( siteUser != null )
                {
                    siteUser.balance += amount;
                    siteUser.lastUpdate = DateTime.Now;
                }
                await databaseManager.updateQuery( $"UPDATE users SET balance = balance + '{amount}' WHERE id = '{userId}' LIMIT 1" ).Execute( );

            }
      
            catch
            {
                return false;
            }
            return true;
        }
        public static async Task<bool> takeUserBalance(int userId, decimal amount)
        {
            var siteUser = usersManager.users.Find(a => a.id == userId);
            if (siteUser != null)
            {

                if (siteUser.balance < amount)
                    return false;
  
                siteUser.balance -= amount;
                siteUser.lastUpdate = DateTime.Now;

              
              
            }
            await databaseManager.updateQuery( $"UPDATE users SET balance = balance - '{amount}' WHERE id = '{userId}' LIMIT 1" ).Execute( );
            return true;
        }

    }
}
