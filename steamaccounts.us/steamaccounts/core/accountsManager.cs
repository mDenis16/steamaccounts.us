using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace csgo {
    public enum status {
        pending,
        sold,
        selling
    }
    public enum type {
        csgo,
        steam,
        offline
    }
    public class offlineProduct {
        public int id { get; set; }
        public string title { get; set; }
        public int stock { get; set; }
        public int maxStock { get; set; }
        public string description { get; set; }
        public string image { get; set; }
        public decimal price {get;set;}

    }
    public class csgoAccount {
        public string username { get; set; }
        public string password { get; set; }
        public string seller { get; set; }
        public decimal price { get; set; }
        public int rank { get; set; }
        public bool prime { get; set; }
        public int hours { get; set; }
        public int negativeRates { get; set; }
        public int positiveRates { get; set; }
        public int wins { get; set; }
        public status status { get; set; }
        public DateTime date { get; set; }
        public string image { get; set; }
        public int id { get; set; }
        public int ticketId { get; set; }
        public int sellerid { get; set; }
        public string buyer { get; set; }
        public int buyerid { get; set; }
        public string description { get; set; }
        public DateTime lastUpdate { get; set; }
        public string email { get; set; }
        public string emailPassword { get; set; }
        public string steamId64 { get; set; }
        public bool disputed { get; set; }
        public DateTime lastRequestTime { get; set; }
        public type type { get; set; }
        public string title { get; set; }
        public string gameList { get; set; }
    }
    public class revokedAccount {
        public int id { get; set; }
        public int sellerId { get; set; }
        public string seller { get; set; }
        public string email { get; set; }
        public string reason { get; set; }
        public DateTime date { get; set; }
        public string username { get; set; }
    }

    public class accountsManager {

        public static List<string> statusName = new List<string> () {
            "Pending",
            "Sold",
            "Selling"
        };

        public static bool firstTime = true;
        public static bool firstTime2 = true;
        public static DateTime lastUpdate = DateTime.Now;
        public static DateTime lastUpdateRevoked = DateTime.Now;
        public static List<csgoAccount> csgoAccounts = new List<csgoAccount> ();
        public static List<revokedAccount> revokedAccounts = new List<revokedAccount> ();
        public static string[] categories = { "Silver", "Gold Nova", "Master Guardian", "Legendary Eagle", "Supreme", "Global Elite" };
        public static string[] ranks = { "Unranked", "Silver 1", "Silver 2", "Silver 3", "Silver 4", "Silver Elite", "Silver Elite Master", "Gold Nova 1", "Gold Nova 2", "Gold Nova 3", "Gold Nova Master", "Master Guardian 1", "Master Guardian 2", "Master Guardian Elite", "Distinguished Master Guardian", "Legendary Eagle", "Legendary Eagle Master", "Supreme Master", "Global Elite" };
        public static string[] emails = { "provlst.com", "powerl.com", "whowlft.com", "ximtyl.com" };
        public static List<offlineProduct> offlineProducts = new List<offlineProduct> ();
        public static DateTime lastProductsUpdate = DateTime.Now.AddDays (-1);
        static Random rdn = new Random ();
        public static string tempMailGenerate () {
            Guid g = Guid.NewGuid ();
            string GuidString = Convert.ToBase64String (g.ToByteArray ());
            GuidString = GuidString.Replace ("=", "");
            GuidString = GuidString.Replace ("+", "");
            if (GuidString.Length > 18)
                GuidString = GuidString.Substring (0, 18);
            return GuidString + $"@{emails[rdn.Next(emails.Length - 1)]}";
        }
        public static async Task addAccount (csgoAccount account) {
            await databaseManager.updateQuery ($"INSERT INTO csgoaccounts (type, seller, sellerid, username, password, rank, price,prime,email, emailPassword, image, steamid64, description, hours,wins)" +
                $"VALUES (@type, @seller," +
                $" @sellerid," +
                $" @username," +
                $" @password," +
                $" @rank," +
                $" @price," +
                $" @prime," +
                $" @email ," +
                $" @emailPassword," +
                $" @image, " +
                $" @steamid64, " +
                $" @description, " +
                $" @hours," +
                $" @wins)").
            addValue ("@seller", account.seller)
                .addValue ("@sellerid", account.sellerid)
                .addValue ("@username", account.username)
                .addValue ("@password", account.password)
                .addValue ("@rank", account.rank)
                .addValue ("@price", account.price)
                .addValue ("@prime", account.prime)
                .addValue ("@email", account.email)
                .addValue ("@emailPassword", account.emailPassword)
                .addValue ("@image", account.image)
                .addValue ("@steamid64", account.steamId64)
                .addValue ("@description", account.description)
                .addValue ("@hours", account.hours)
                .addValue ("@wins", account.wins)
                .addValue ("@negativeRates", account.negativeRates)
                .addValue ("@positiveRates", account.positiveRates)
                .addValue ("@type", (int) account.type)

                .Execute ();
            accountsManager.csgoAccounts.Add (account);
        }
        public static List<csgoAccount> searchAccounts (List<csgoAccount> list, string input) {
            var toReturn = new List<csgoAccount> ();
            input = input.ToLower ();
            toReturn = list.FindAll (a => csgo.accountsManager.ranks[a.rank].ToLower () == input);
            if (toReturn.Count == 0)
                toReturn = list.FindAll (a => a.seller.ToLower ().Contains (input));

            return toReturn;
        }

        public static async Task<List<offlineProduct>> fetchOfflineProducts () {
            if ((DateTime.Now - lastProductsUpdate).TotalMinutes > 5) {
                offlineProducts.Clear ();
                await databaseManager.selectQuery ($"SELECT * FROM offlineproducts ", delegate (DbDataReader reader) {
                    offlineProduct product = new offlineProduct ();
                    product.id = (int) reader["id"];
                    product.title = (string) reader["description"];
                    product.stock = (int) reader["stock"];
                    product.maxStock = (int) reader["maxStock"];
                    product.description = (string) reader["description"];
                    product.image = (string) reader["image"];
                    product.price = (decimal)reader["price"];
                    offlineProducts.Add (product);
                }).Execute ();
                lastProductsUpdate = DateTime.Now;
            }
            return offlineProducts;
        }

        public static async Task<csgoAccount> getAccountById (int id) {
            if (csgo.accountsManager.csgoAccounts.Count == 0)
                csgo.accountsManager.csgoAccounts = await csgo.accountsManager.loadAccounts ();
            return csgo.accountsManager.csgoAccounts.Find (a => a.id == id);
            csgoAccount account = new csgoAccount ();
            await databaseManager.selectQuery ($"SELECT * FROM csgoaccounts WHERE id = @id LIMIT 1", delegate (DbDataReader reader) {

                account.id = (int) reader["id"];
                account.username = (string) reader["username"];
                account.password = (string) reader["password"];
                account.seller = (string) reader["seller"];
                account.date = (DateTime) reader["date"];
                account.status = (status) (int) reader["status"];
                account.rank = (int) reader["rank"];
                account.prime = (bool) reader["prime"];
                account.email = (string) reader["email"];
                account.emailPassword = (string) reader["emailPassword"];
                account.price = (decimal) reader["price"];
                account.image = (string) reader["image"];
                account.hours = (int) reader["hours"];
                account.wins = (int) reader["wins"];
                account.description = (string) reader["description"];
                account.sellerid = (int) reader["sellerid"];
                account.buyer = (string) reader["buyer"];
                account.buyerid = (int) reader["buyerid"];
                account.image = (string) reader["image"];
                account.disputed = (bool) reader["disputed"];
                account.ticketId = (int) reader["ticketId"];

            }).addValue ("@id", id).Execute ();
            return account;
        }
        public static async Task addRevokedAccount (csgoAccount account, string reason) {
            revokedAccount revokedAccount = new revokedAccount ();
            revokedAccount.sellerId = account.sellerid;
            revokedAccount.seller = account.seller;
            revokedAccount.email = account.email;
            revokedAccount.username = account.username;
            revokedAccount.date = DateTime.Now;
            revokedAccount.reason = reason;

            await databaseManager.updateQuery ($"INSERT INTO revokedaccounts (sellerId, seller, username, email, reason)" +
                    $"VALUES (@sellerId, @seller, @username, @email, @reason )")
                .addValue ("@sellerId", account.sellerid)
                .addValue ("@seller", account.seller)
                .addValue ("@username", account.username)
                .addValue ("@email", account.email)
                .addValue ("@reason", reason)
                .Execute ();
            revokedAccounts.Add (revokedAccount);
            lastUpdateRevoked = DateTime.Now;
        }
        public static async Task<List<revokedAccount>> GetRevokedAccounts (int sellerId) {
            if ((DateTime.Now - lastUpdateRevoked).TotalSeconds < 15 && !firstTime2) {
                return revokedAccounts;
            }

            List<revokedAccount> accounts = new List<revokedAccount> ();

            await databaseManager.selectQuery ($"SELECT * FROM revokedaccounts WHERE sellerId = @sellerId LIMIT 1", delegate (DbDataReader reader) {
                revokedAccount account = new revokedAccount ();
                account.id = (int) reader["id"];

                account.seller = (string) reader["seller"];
                account.date = (DateTime) reader["date"];
                account.sellerId = (int) reader["sellerId"];
                account.email = (string) reader["email"];
                account.reason = (string) reader["reason"];
                account.username = (string) reader["username"];

                var index = revokedAccounts.FindIndex (a => a.id == account.id);
                if (index != -1)
                    revokedAccounts[index] = account;
                else
                    revokedAccounts.Add (account);

            }).addValue ("@sellerId", sellerId).Execute ();
            lastUpdateRevoked = DateTime.Now;
            firstTime2 = false;
            return revokedAccounts;
        }
        public static async Task<List<csgoAccount>> loadAccounts (bool shop = false, bool admin = false) {
            if (admin) {
                var temp = new List<csgoAccount> ();
                await databaseManager.selectQuery ($"SELECT * FROM csgoaccounts", delegate (DbDataReader reader) {
                    csgoAccount account = new csgoAccount ();
                    account.id = (int) reader["id"];
                    account.username = (string) reader["username"];
                    account.password = (string) reader["password"];
                    account.seller = (string) reader["seller"];
                    account.date = (DateTime) reader["date"];
                    account.status = (status) (int) reader["status"];
                    account.rank = (int) reader["rank"];
                    account.image = (string) reader["image"];
                    account.prime = (bool) reader["prime"];
                    account.price = (decimal) reader["price"];
                    account.sellerid = (int) reader["sellerid"];
                    account.buyer = (string) reader["buyer"];
                    account.disputed = (bool) reader["disputed"];
                    account.ticketId = (int) reader["ticketId"];
                    account.buyerid = (int) reader["buyerid"];
                    account.image = (string) reader["image"];
                    account.steamId64 = (string) reader["steamid64"];
                    account.description = (string) reader["description"];

                    temp.Add (account);

                }).Execute ();
                return temp;
            }
            if ((DateTime.Now - lastUpdate).TotalSeconds < 15 && !firstTime) {
                return csgoAccounts;
            }

            csgoAccounts.Clear ();
            await databaseManager.selectQuery ($"SELECT * FROM csgoaccounts", delegate (DbDataReader reader) {
                csgoAccount account = new csgoAccount ();
                account.id = (int) reader["id"];
                account.username = (string) reader["username"];
                account.password = (string) reader["password"];
                account.seller = (string) reader["seller"];
                account.sellerid = (int) reader["sellerid"];
                account.date = (DateTime) reader["date"];
                account.status = (status) (int) reader["status"];
                account.rank = (int) reader["rank"];
                account.hours = (int) reader["hours"];
                account.wins = (int) reader["wins"];
                account.prime = (bool) reader["prime"];
                account.price = (decimal) reader["price"];
                account.description = (string) reader["description"];
                account.image = (string) reader["image"];
                account.email = (string) reader["email"];
                account.emailPassword = (string) reader["emailPassword"];
                account.steamId64 = (string) reader["steamid64"];
                account.lastUpdate = DateTime.Now;
                account.buyer = (string) reader["buyer"];
                account.buyerid = (int) reader["buyerid"];
                account.disputed = (bool) reader["disputed"];
                account.image = (string) reader["image"];
                account.positiveRates = (int) reader["positiveRates"];
                account.negativeRates = (int) reader["negativeRates"];
                account.gameList = (string) reader["gameList"];
                account.title = (string) reader["title"];
                account.type = (csgo.type) (int) reader["type"];
                account.ticketId = (int) reader["ticketId"];
                var index = csgoAccounts.FindIndex (a => a.id == account.id);
                if (index != -1)
                    csgoAccounts[index] = account;
                else
                    csgoAccounts.Add (account);
            }).Execute ();
            lastUpdate = DateTime.Now;
            firstTime = false;

            return csgoAccounts;
        }
        public static async Task<bool> existAccount (string username) {
            bool _return = false;
            await databaseManager.selectQuery ($"SELECT * FROM csgoaccounts WHERE username = @username", delegate (DbDataReader reader) {
                _return = reader.HasRows;
            }).addValue ("@username", username).Execute ();

            return _return;
        }
        public static async Task<List<csgoAccount>> loadUserAccounts (string username) {

            if ((DateTime.Now - lastUpdate).TotalSeconds < 15 && !firstTime) {
                return csgoAccounts.FindAll (account => account.seller == username);
            }

            await databaseManager.selectQuery ($"SELECT * FROM csgoaccounts WHERE seller = @seller", delegate (DbDataReader reader) {
                csgoAccount account = new csgoAccount ();
                account.id = (int) reader["id"];
                account.username = (string) reader["username"];
                account.password = (string) reader["password"];
                account.seller = (string) reader["seller"];
                account.date = (DateTime) reader["date"];
                account.status = (status) (int) reader["status"];
                account.rank = (int) reader["rank"];
                account.prime = (bool) reader["prime"];
                account.email = (string) reader["email"];
                account.emailPassword = (string) reader["emailPassword"];
                account.price = (decimal) reader["price"];
                account.hours = (int) reader["hours"];
                account.disputed = (bool) reader["disputed"];
                account.wins = (int) reader["wins"];
                account.sellerid = (int) reader["sellerid"];
                account.buyer = (string) reader["buyer"];
                account.disputed = (bool) reader["disputed"];
                account.ticketId = (int) reader["ticketId"];
                account.buyerid = (int) reader["buyerid"];

                var index = csgoAccounts.FindIndex (a => a.id == account.id);
                if (index != -1)
                    csgoAccounts[index] = account;
                else
                    csgoAccounts.Add (account);
            }).addValue ("@seller", username).Execute ();
            lastUpdate = DateTime.Now;
            firstTime = false;

            return csgoAccounts.FindAll (account => account.seller == username);
        }
        public static async Task<List<csgoAccount>> loadBoughtAccounts (string username) {

            if ((DateTime.Now - lastUpdate).TotalSeconds < 15 && !firstTime) {
                return csgoAccounts.FindAll (account => account.buyer == username);
            }

            await databaseManager.selectQuery ($"SELECT * FROM csgoaccounts", delegate (DbDataReader reader) {
                csgoAccount account = new csgoAccount ();
                account.id = (int) reader["id"];
                account.username = (string) reader["username"];
                account.password = (string) reader["password"];
                account.seller = (string) reader["seller"];
                account.date = (DateTime) reader["date"];
                account.status = (status) (int) reader["status"];
                account.rank = (int) reader["rank"];
                account.prime = (bool) reader["prime"];
                account.email = (string) reader["email"];
                account.emailPassword = (string) reader["emailPassword"];
                account.price = (decimal) reader["price"];
                account.hours = (int) reader["hours"];
                account.wins = (int) reader["wins"];
                account.sellerid = (int) reader["sellerid"];
                account.buyer = (string) reader["buyer"];
                account.buyerid = (int) reader["buyerid"];
                account.disputed = (bool) reader["disputed"];
                account.ticketId = (int) reader["ticketId"];
                var index = csgoAccounts.FindIndex (a => a.id == account.id);
                if (index != -1)
                    csgoAccounts[index] = account;
                else
                    csgoAccounts.Add (account);
            }).Execute ();
            lastUpdate = DateTime.Now;
            firstTime = false;

            return csgoAccounts.FindAll (account => account.buyer == username);
        }
        public static string getImageByCategory (int category) {
            return $"https://raw.githubusercontent.com/SteamDatabase/GameTracking-CSGO/0e457516ba13817a45b6c2a1d262fe7d0599bcbc/csgo/pak01_dir/resource/flash/econ/status_icons/skillgroup" + category + ".png";

        }
    }
}