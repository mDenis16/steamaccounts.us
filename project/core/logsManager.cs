using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Helpers;

namespace csgo.core.logsManager
{
    public  class accounts
    {
        public class log
        {
            public int id { get; set; }
            public int csgoId { get; set; }
            public decimal price { get; set; }
            public string buyer { get; set; }
            public string seller { get; set; }
            public int sellerId { get; set; }
            public int buyerId { get; set; }
            public List<string> transactionId { get; set; }
            public DateTime date { get; set; }
        }
        public static async Task addLog(int csgoId, decimal price, string seller, string buyer, int sellerId, int buyerId, List<string> transactionId )
        {
            await databaseManager.updateQuery( $"INSERT INTO csgoaccountslogs (csgoId, price, seller, buyer, sellerId, buyerId, transactionId) VALUES ('{csgoId}', '{price}', '{seller}', '{buyer}', '{sellerId}', '{buyerId}', '{Newtonsoft.Json.JsonConvert.SerializeObject( transactionId)}')").Execute( );
        }
        public static async Task<List<log>> readAllLogs( )
        {
            List<log> logs = new List<log>();
            await databaseManager.selectQuery( $"SELECT * FROM csgoaccountslogs", delegate ( DbDataReader reader )
            {
                if ( reader.HasRows )
                {
                    log log = new log();
                    log.csgoId = ( int ) reader[ "csgoId" ];
                    log.price = ( decimal ) reader[ "price" ];
                    log.seller = ( string ) reader[ "seller" ];
                    log.buyer = ( string ) reader[ "buyer" ];
                    log.buyerId = ( int ) reader[ "buyerId" ];
                    log.sellerId = ( int ) reader[ "sellerId" ];
                    log.date = ( DateTime ) reader[ "date" ];
                    log.transactionId = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>( ( string ) reader[ "transactionId" ] ); ;
                    logs.Add( log );
                }
            } ).Execute( );
            return logs;
        }
        public static async Task<List<log>> readLogs( int userId )
        {
            List<log> logs = new List<log>();
            await databaseManager.selectQuery( $"SELECT * FROM csgoaccountslogs WHERE userId = '{userId}'", delegate ( DbDataReader reader )
            {
                if ( reader.HasRows )
                {
                    log log = new log();
                    log.csgoId = ( int ) reader[ "csgoId" ];
                    log.price = ( decimal ) reader[ "price" ];
                    log.seller = ( string ) reader[ "seller" ];
                    log.buyer = ( string ) reader[ "buyer" ];
                    log.buyerId = ( int ) reader[ "buyerId" ];
                    log.sellerId = ( int ) reader[ "sellerId" ];
                    log.date = ( DateTime ) reader[ "date" ];
                    log.transactionId = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>((string) reader[ "transactionId" ]);
                }
            } ).Execute( );
            return logs;
        }
    }

    public class transactions
    {
        public enum transactionType
        {
            deposit,
            withdraw,
            buy,
            sell
        }
        public enum methodType
        {
            balance,
            paypal,
            paysafe,
            mobile
        }
        public enum transactionStatus
        {
            complete,
            inpending
        }
        public static string[] status = {"Complete", "In Pending"};
        public static string[] type = { "Deposit", "Withdraw", "Buy", "Sell" };
        public static string[] method = { "Balance", "PayPal", "PaySafeCard", "Mobile"};
        public class transaction
        {
            public int userID { get; set; }
            public int id { get; set; }
            public string username { get; set; }
            public string email { get; set; }
            public decimal amount { get; set; }
            public transactionType type { get; set; }
            public transactionStatus status { get; set; }
            public methodType method { get; set; }
          
            public DateTime date { get; set; }
            public DateTime confirmDate { get; set; }

        }
        public static async Task<List<transaction>> readTransactionsLogs( int userId )
        {
            List<transaction> deposits = new List<transaction>();
            await databaseManager.selectQuery( $"SELECT * FROM transactions WHERE userId = '{userId}'", delegate ( DbDataReader reader )
             {
                 if ( reader.HasRows )
                 {
                     transaction log = new transaction();
                     log.userID = ( int ) reader[ "userId" ];
                     log.id = ( int ) reader[ "id" ];
                     log.username = ( string ) reader[ "username" ];
                     log.email = ( string ) reader[ "email" ];
                     log.method = ( methodType ) ( int ) reader[ "method" ];
                     log.amount = ( decimal ) reader[ "amount" ];
                     log.type = ( transactionType ) ( int ) reader[ "type" ];
                     log.status = ( transactionStatus ) ( int ) reader[ "status" ];
                     log.date = ( DateTime ) reader[ "date" ];
                     log.confirmDate = ( DateTime ) reader[ "confirmDate" ];
                     deposits.Add( log );
                 }
             } ).Execute( );
            return deposits;
        }
        public static async Task<List<transaction>> readAllTransactions(  )
        {
            List<transaction> deposits = new List<transaction>();
            await databaseManager.selectQuery( $"SELECT * FROM transactions", delegate ( DbDataReader reader )
            {
                if ( reader.HasRows )
                {
                    transaction log = new transaction();
                    log.userID = ( int ) reader[ "userId" ];
                    log.id = ( int ) reader[ "id" ];
                    log.username = ( string ) reader[ "username" ];
                    log.email = ( string ) reader[ "email" ];
                    log.method = ( methodType ) ( int ) reader[ "method" ];
                    log.amount = ( decimal ) reader[ "amount" ];
                    log.type = ( transactionType ) ( int ) reader[ "type" ];
                    log.status = ( transactionStatus ) ( int ) reader[ "status" ];
                    log.date = ( DateTime ) reader[ "date" ];
                    log.confirmDate = ( DateTime ) reader[ "confirmDate" ];
                    deposits.Add( log );
                }
            } ).Execute( );
            return deposits;
        }
        public static async Task<int> addTransactionLog( int userId, string username, transactionType type, transactionStatus status, methodType method, string email, decimal amount )
        {
            if (type == transactionType.sell)
             return await databaseManager.updateQuery( $"INSERT INTO transactions (userId, email, username, amount, type, method, status, confirmDate) VALUES ('{userId}', '{email}', '{username}', '{amount}', '{( int ) type}', '{( int ) method}', '{(int)status}',  DATE_ADD(now(), INTERVAL 10 DAY) ) " ).Execute( );
        else
                return await databaseManager.updateQuery( $"INSERT INTO transactions (userId, email, username, amount, type, method, status, confirmDate) VALUES ('{userId}', '{email}', '{username}', '{amount}', '{( int ) type}', '{( int ) method}', '{( int ) status}',  CURRENT_DATE() ) " ).Execute( );
        }
       
    }

    public class utilities
    {
        public static async Task addEmailLog( string email, string email_text )
        {
            await databaseManager.updateQuery( $"INSERT INTO emails (email, email_text) VALUES (@email, @email_text) " ).addValue( "@email", email ).addValue( "@email_text", email_text ).Execute( );
        }
        public static async Task addLoginLog( string username, int userId, string ip )
        {
            await databaseManager.updateQuery( $"INSERT INTO loginlogs (username, userId, ip) VALUES (@username, @userId, @ip) " ).addValue( "@username", username ).addValue( "@userId", userId ).addValue("@ip", ip).Execute( );
        }
    }
}