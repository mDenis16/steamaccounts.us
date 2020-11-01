using csgo.core;
using csgo.postModels;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace csgo
{
    public class usersManager
    {
		public class recoveryPassword
		{
			public int userId { get; set; }
			public string token { get; set; }

			public recoveryPassword( ) { }
			public recoveryPassword( int id, string toekn )
			{
				this.userId = id;
				this.token = toekn;
			}
			public recoveryPassword( int id)
			{
				this.userId = id;

			}
			public recoveryPassword( string toekn )
			{
				var temp =  recoveryTokens.Find( a => a.token == toekn );
				this.token = temp.token;
				this.userId = temp.userId;
			}
			public string addToken( )
			{
				int index = recoveryTokens.FindIndex(a=> a.userId  == this.userId);
				byte[] time = BitConverter.GetBytes(DateTime.UtcNow.ToBinary());
				byte[] key = Guid.NewGuid().ToByteArray();
				this.token = Convert.ToBase64String(time.Concat(key).ToArray());
				
				if ( index != -1 )
					recoveryTokens[ index ].token = this.token;
				else
					recoveryTokens.Add( new recoveryPassword( this.userId, this.token ) );
				return this.token;
			}
			public bool verifyToken( )
			{
				int index = recoveryTokens.FindIndex(a=> a.token  == this.token);
				if ( index == -1 )
					return false;
				byte[] data = Convert.FromBase64String(this.token);
				DateTime when = DateTime.FromBinary(BitConverter.ToInt64(data, 0));
				if ( when < DateTime.UtcNow.AddMinutes( -5 ) )
				{
					this.deleteToken( );
					return false;
				}
				return true;
			}
			public void deleteToken( )
			{
				int index = recoveryTokens.FindIndex(a=> a.userId  == this.userId);
				if (index != -1)
				  recoveryTokens.RemoveAt( index );

			}
		}
		public class sellerRequest
		{
			public string username { get; set; }
			public int id { get; set; }
			public int userId { get; set; }
		    public DateTime date { get; set; }
			public string nationality { get; set; }
			public string description { get; set; }
			public string question { get; set; }
			public string phoneNumber { get; set; }
			public int age { get; set; }
		}
		public static List<recoveryPassword> recoveryTokens = new List<recoveryPassword>();
		public class userData
		{
			public string username { get; set; }
			public int id { get; set; }
			public decimal balance { get; set; }
			public string cookie { get; set; }
			public bool seller { get; set; }
			public int positiveRates { get; set; }
			public int negativeRates { get; set; }
			public string email { get; set; }
			public int boughtAccounts { get; set; }
			public bool admin { get; set; }
			public int soldAccounts { get; set; }
			public DateTime registerDate { get; set; }
			public DateTime lastConfirm { get; set; }
			public DateTime lastUpdate { get; set; }
			public DateTime lastBalanceCheck { get; set; }
			public string validateToken { get; set; }
			public bool confirmed { get; set; }
			public string connectionId { get; set; }
			public bool twofa {get; set;}
			public string twofaToken { get; set; }
			public DateTime lastRequestTime { get; set; }
			public DateTime lastMessageSent { get; set; }
			public bool banned { get; set; }
			public string banReason { get; set; }
			public Microsoft.AspNetCore.Http.HttpRequest lastRequest { get; set; }
		    public string loginIP { get; set; }
			public string phoneNumber { get; set; }
			public int unSeenNotifications { get; set; }
			public int sellingAccounts { get; set; }
			public bool lockedBuy { get; set; }
		     public decimal holdBalance { get; set; }
			public string paypalemail { get; set; }
			public DateTime lastLogin { get; set; }
			public DateTime lastEmailGenerate { get; set; }
			public DateTime sellerDate { get; set; }
			public DateTime balanceCheck { get; set; }
		}
		static bool debug = false;
		public static List<userData> users = new List<userData>();
		public static async Task<bool> loginUser(string username, string password)
		{
			bool exist = false;
			await databaseManager.selectQuery("SELECT username FROM users WHERE username = @username AND password = @password LIMIT 1", delegate (DbDataReader reader)
			{
				exist = reader.HasRows;
			}).addValue("@username", username).addValue("@password", password).Execute();
			
			return exist;
		}
		public static async Task<string> getUsernameById( int id )
		{
			string exist = "";
			await databaseManager.selectQuery( "SELECT username FROM users WHERE id = @id LIMIT 1", delegate ( DbDataReader reader )
			{
				exist = (string)reader[ "username" ];
			} ).addValue( "@id", id ).Execute( );

			return exist;
		}
		public static async Task<List<sellerRequest>> readAllSellerRequests()
		{
			List<sellerRequest> dataToReturn = new List<sellerRequest>();

			await databaseManager.selectQuery("SELECT * FROM sellerrequests", delegate (DbDataReader reader)
			{
				if (reader.HasRows)
				{
					sellerRequest request = new sellerRequest();
					request.username = (String)reader["username"];
					request.age = (int)reader["age"];
					request.nationality = (string)reader["nationality"];
					request.description = (string)reader["description"];
					request.userId = (int)reader["userId"];
					request.date = (DateTime)reader["date"];
					request.question = (string)reader["question"];

					dataToReturn.Add(request);
				}
			}).Execute();
			return dataToReturn;
		}
		public static async Task<bool> existSellerRequest(int id)
		{
			bool bolean = false;
			await databaseManager.selectQuery("SELECT * FROM sellerrequests WHERE userId = @id", delegate (DbDataReader reader)
			{
				bolean = reader.HasRows;
				
			}).addValue("@id", id).Execute();
			return bolean;
		}
		public static async Task<sellerRequest> readSellerRequest(int id)
		{
			sellerRequest request = new sellerRequest();
			await databaseManager.selectQuery("SELECT * FROM sellerrequests WHERE userId = @id", delegate (DbDataReader reader)
			{
				if (reader.HasRows)
				{
					
					request.username = (String)reader["username"];
					request.age = (int)reader["age"];
					request.date = (DateTime)reader["date"];
					request.userId = (int)reader["userId"];
					request.nationality = (string)reader["nationality"];
					request.description = (string)reader["description"];
					request.question = (string)reader["question"];
					request.phoneNumber = (string)reader["phoneNumber"];
					
				}
			}).addValue("@id", id).Execute();
			return request;
		}
		public static async Task<bool> registerUser(string username, string password, string email )
		{
			

			await databaseManager.updateQuery( $"INSERT INTO users (username, password, email) VALUES (@username, @password, @email)" ).addValue("@username", username).addValue("@password", password).addValue("@email", email).Execute( );
			return true;
		}
		public static async Task<bool> doesExist(string key, string data)
		{
			bool exist = false;
			await databaseManager.selectQuery( $"SELECT username FROM users WHERE {key} = @data", delegate ( DbDataReader reader )
			{
				exist = reader.HasRows;
			} ).addValue("@data", data).Execute( );
			return exist;
		
		}
		public static async Task<List<userData>> getAllUsers()
		{
			List<userData> dataToReturn = new List<userData>();
			
			await databaseManager.selectQuery("SELECT * FROM users", delegate (DbDataReader reader)
			{
				if (reader.HasRows)
				{
					userData userDetails = new userData();
					userDetails.balance = (decimal)reader["balance"];
					userDetails.username = (string)reader["username"];
					userDetails.id = (int)reader["id"];
					userDetails.cookie = (string)reader["cookie"];
					userDetails.positiveRates = (int)reader["positiveRates"];
					userDetails.negativeRates = (int)reader["negativeRates"];
					userDetails.registerDate = (DateTime)reader["registerDate"];
					userDetails.soldAccounts = (int)reader["soldAccounts"];
					userDetails.boughtAccounts = (int)reader["boughtAccounts"];
					userDetails.admin = ( bool ) reader[ "admin" ];
					userDetails.seller = (bool)reader["seller"];
					userDetails.lastUpdate = DateTime.Now;
					userDetails.loginIP = ( string ) reader[ "loginIP" ];
					userDetails.banReason = (string)reader["banReason"];
					userDetails.sellerDate = (DateTime)reader["sellerDate"];
					userDetails.phoneNumber = (string)reader["phoneNumber"];
					userDetails.lastLogin = (DateTime)reader["lastLogin"];
					userDetails.paypalemail = ( string ) reader[ "paypalemail" ];
					
					dataToReturn.Add(userDetails);
				}
			}).Execute();
			return dataToReturn;
		}
		//public static DateTime lastUsers3monthsUpdate = DateTime.Now.AddHours(-24);
		public static async Task<List<userData>> getAllUsers3Months( )

		{
			List<userData> dataToReturn = new List<userData>();

			await databaseManager.selectQuery( "select * from users where registerDate >= now( )-interval 3 month;", delegate ( DbDataReader reader )
			{
				if ( reader.HasRows )
				{
					userData userDetails = new userData();
					
					userDetails.registerDate = ( DateTime ) reader[ "registerDate" ];
				
					dataToReturn.Add( userDetails );
				}
			} ).Execute( );
			return dataToReturn;
		}
		
		public static async Task<userData> getUserData( Microsoft.AspNetCore.Http.HttpRequest Request )
		{
			string curIP = Request.getIPAddress();
			string cookie = Request.Cookies["sessionid"];
			if (debug)
			{
				userData userDetailsDbg = new userData();

				await databaseManager.selectQuery("SELECT * FROM users WHERE cookie = @cookie AND banned = False LIMIT 1", delegate (DbDataReader reader)
				{
					if (reader.HasRows)
					{
						userDetailsDbg.balance = ( decimal ) reader[ "balance" ];
						userDetailsDbg.username = ( string ) reader[ "username" ];
						userDetailsDbg.id = ( int ) reader[ "id" ];
						userDetailsDbg.cookie = ( string ) reader[ "cookie" ];
						userDetailsDbg.positiveRates = ( int ) reader[ "positiveRates" ];
						userDetailsDbg.negativeRates = ( int ) reader[ "negativeRates" ];
						userDetailsDbg.registerDate = ( DateTime ) reader[ "registerDate" ];
						userDetailsDbg.seller = ( bool ) reader[ "seller" ];
						userDetailsDbg.soldAccounts = ( int ) reader[ "soldAccounts" ];
						userDetailsDbg.admin = ( bool ) reader[ "admin" ];
						userDetailsDbg.email = ( string ) reader[ "email" ];
						userDetailsDbg.seller = ( bool ) reader[ "seller" ];
						userDetailsDbg.boughtAccounts = ( int ) reader[ "boughtAccounts" ];
						userDetailsDbg.positiveRates = ( int ) reader[ "positiveRates" ];
						userDetailsDbg.negativeRates = ( int ) reader[ "negativeRates" ];
						userDetailsDbg.lastUpdate = DateTime.Now;
						userDetailsDbg.banned = (bool)reader["banned"];
						userDetailsDbg.sellerDate = (DateTime)reader["sellerDate"];
						userDetailsDbg.lastLogin = (DateTime)reader["lastLogin"];
					}
				} ).addValue("@cookie", cookie).Execute();
				return userDetailsDbg;
			}
			var temp = users.Find(a => a.cookie == cookie && curIP == a.loginIP && a.banned == false ); 
			if (temp == null)
				return null;
			temp.lastRequest = Request;
			temp.lastRequestTime = DateTime.Now;
			if ((DateTime.Now - temp.lastUpdate).TotalSeconds < 3)
				return temp;
		

			userData userDetails = new userData();
			userDetails.lastRequest = temp.lastRequest;
			userDetails.lastRequestTime = DateTime.Now;
			await databaseManager.selectQuery("SELECT * FROM users WHERE cookie = @cookie AND banned = False LIMIT 1", delegate (DbDataReader reader)
			{
				if (reader.HasRows)
				{
					userDetails.balance = ( decimal ) reader[ "balance" ];
					userDetails.username = ( string ) reader[ "username" ];
					userDetails.id = ( int ) reader[ "id" ];
					userDetails.cookie = ( string ) reader[ "cookie" ];
					userDetails.positiveRates = ( int ) reader[ "positiveRates" ];
					userDetails.negativeRates = ( int ) reader[ "negativeRates" ];
					userDetails.registerDate = ( DateTime ) reader[ "registerDate" ];
					userDetails.seller = ( bool ) reader[ "seller" ];
					userDetails.soldAccounts = ( int ) reader[ "soldAccounts" ];
					userDetails.admin = ( bool ) reader[ "admin" ];
					userDetails.email = ( string ) reader[ "email" ];
					userDetails.seller = ( bool ) reader[ "seller" ];
					userDetails.boughtAccounts = ( int ) reader[ "boughtAccounts" ];
					userDetails.positiveRates = ( int ) reader[ "positiveRates" ];
					userDetails.negativeRates = ( int ) reader[ "negativeRates" ];
					userDetails.lastUpdate = DateTime.Now;
					userDetails.loginIP = ( string ) reader[ "loginIP" ];
					userDetails.banned = (bool)reader["banned"];
					userDetails.lastLogin = (DateTime)reader["lastLogin"];
					userDetails.sellerDate = (DateTime)reader["sellerDate"];
					userDetails.phoneNumber = (string)reader["phoneNumber"];
					userDetails.unSeenNotifications = 0;
					userDetails.lockedBuy = ( bool ) reader[ "lockedBuy" ];
					userDetails.holdBalance = ( decimal ) reader[ "holdBalance" ];
					userDetails.paypalemail = ( string ) reader[ "paypalemail" ];
					userDetails.lastBalanceCheck = ( DateTime ) reader[ "lastBalanceCheck" ];


				}
			}).addValue("@cookie", cookie).Execute();
			await csgo.core.balanceManager.checkForIncomingBalance( userDetails.id );
			await databaseManager.selectQuery("Select count(*) as countNotify from notifications WHERE userId = @userId AND seen = False", delegate (DbDataReader reader)
			{
				if (reader.HasRows)
				{
					userDetails.unSeenNotifications = (int)(Int64)reader["countNotify"];


				}
			}).addValue("@userId", userDetails.id).Execute();
		 
			if ( userDetails.loginIP != curIP )
				return null;
			users[users.IndexOf(temp)] = userDetails;
			return userDetails;
		}
		public static async Task<userData> getDataByUsername( string username )
		{

			userData userDetailsDbg = new userData();

			await databaseManager.selectQuery( "SELECT * FROM users WHERE username = @username LIMIT 1", delegate ( DbDataReader reader )
			{
				if ( reader.HasRows )
				{
					userDetailsDbg.balance = ( decimal ) reader[ "balance" ];
					userDetailsDbg.username = ( string ) reader[ "username" ];
					userDetailsDbg.id = ( int ) reader[ "id" ];
					userDetailsDbg.cookie = ( string ) reader[ "cookie" ];
					userDetailsDbg.positiveRates = ( int ) reader[ "positiveRates" ];
					userDetailsDbg.negativeRates = ( int ) reader[ "negativeRates" ];
					userDetailsDbg.registerDate = ( DateTime ) reader[ "registerDate" ];
					userDetailsDbg.seller = ( bool ) reader[ "seller" ];
					userDetailsDbg.soldAccounts = ( int ) reader[ "soldAccounts" ];
					userDetailsDbg.admin = ( bool ) reader[ "admin" ];
					userDetailsDbg.email = ( string ) reader[ "email" ];
					userDetailsDbg.seller = ( bool ) reader[ "seller" ];
					userDetailsDbg.boughtAccounts = ( int ) reader[ "boughtAccounts" ];
					userDetailsDbg.positiveRates = ( int ) reader[ "positiveRates" ];
					userDetailsDbg.negativeRates = ( int ) reader[ "negativeRates" ];
					userDetailsDbg.lastUpdate = DateTime.Now;
					userDetailsDbg.loginIP = ( string ) reader[ "loginIP" ];
					userDetailsDbg.banned = (bool)reader["banned"];
					userDetailsDbg.banReason = (string)reader["banReason"];
					userDetailsDbg.sellerDate = (DateTime)reader["sellerDate"];
					userDetailsDbg.lastLogin = (DateTime)reader["lastLogin"];

					var usr = csgo.usersManager.users.Find( a => a.username == username );
					if ( usr != null && usr.connectionId != null )
						userDetailsDbg.connectionId = usr.connectionId;
				}
			} ).addValue( "@username", username ).Execute( );
			return userDetailsDbg;

		}
		static Int64 usersCount = 0; static DateTime lastUpdateCount = DateTime.Now.AddDays(-24);	
		public static async Task<Int64> getUsersCount( )
		{
			if ( ( DateTime.Now - lastUpdateCount ).TotalSeconds < 30 )
				return usersCount;
			else
			{
				await databaseManager.selectQuery( "Select count(*) as usersCount from users", delegate ( DbDataReader reader )
				{
					if ( reader.HasRows )
					{
						usersCount = ( Int64 ) reader[ "usersCount" ];


					}
				} ).Execute( );
				lastUpdateCount = DateTime.Now;
				return usersCount;
			}
		}
		
		public static async Task<userData> getUserDatabyId(int sqlid, bool important = false)
		{
			if (debug)
			{
				userData userDetailsDbg = new userData();

				await databaseManager.selectQuery("SELECT * FROM users WHERE id = @sqlid LIMIT 1", delegate (DbDataReader reader)
				{
					if (reader.HasRows)
					{
						userDetailsDbg.balance = ( decimal ) reader[ "balance" ];
						userDetailsDbg.username = ( string ) reader[ "username" ];
						userDetailsDbg.id = ( int ) reader[ "id" ];
						userDetailsDbg.cookie = ( string ) reader[ "cookie" ];
						userDetailsDbg.positiveRates = ( int ) reader[ "positiveRates" ];
						userDetailsDbg.negativeRates = ( int ) reader[ "negativeRates" ];
						userDetailsDbg.registerDate = ( DateTime ) reader[ "registerDate" ];
						userDetailsDbg.seller = ( bool ) reader[ "seller" ];
						userDetailsDbg.soldAccounts = ( int ) reader[ "soldAccounts" ];
						userDetailsDbg.admin = ( bool ) reader[ "admin" ];
						userDetailsDbg.email = ( string ) reader[ "email" ];
						userDetailsDbg.seller = ( bool ) reader[ "seller" ];
						userDetailsDbg.boughtAccounts = ( int ) reader[ "boughtAccounts" ];
						userDetailsDbg.positiveRates = ( int ) reader[ "positiveRates" ];
						userDetailsDbg.negativeRates = ( int ) reader[ "negativeRates" ];
						userDetailsDbg.lastUpdate = DateTime.Now;
						userDetailsDbg.loginIP = ( string ) reader[ "loginIP" ];
						userDetailsDbg.banned = (bool)reader["banned"];
						userDetailsDbg.sellerDate = (DateTime)reader["sellerDate"];
						userDetailsDbg.banReason = (string)reader["banReason"];
						userDetailsDbg.lastLogin = (DateTime)reader["lastLogin"];
					}
				}).addValue("@sqlid", sqlid).Execute();
				return userDetailsDbg;
			}
			var temp = users.Find(a => a.id == sqlid);
			if ( !important )
			{
				if ( temp != null )
				{

					if ( ( DateTime.Now - temp.lastUpdate ).TotalSeconds < 3 )
						return temp;
				}
			}
			userData userDetails = new userData();

			await databaseManager.selectQuery("SELECT * FROM users WHERE id = @sqlid LIMIT 1", delegate (DbDataReader reader)
			{
				if (reader.HasRows)
				{
					userDetails.balance = (decimal)reader["balance"];
					userDetails.username = (string)reader["username"];
			
					userDetails.id = (int)reader["id"];
					userDetails.cookie = (string)reader["cookie"];
					userDetails.positiveRates = (int)reader["positiveRates"];
					userDetails.negativeRates = (int)reader["negativeRates"];
					userDetails.registerDate = (DateTime)reader["registerDate"];
					userDetails.seller = (bool)reader["seller"];
					userDetails.soldAccounts = (int)reader["soldAccounts"];
					userDetails.admin = ( bool ) reader[ "admin" ];
					userDetails.email = ( string ) reader[ "email" ];
					userDetails.seller = ( bool ) reader[ "seller" ];
					var asd = csgo.usersManager.users.Find( a => a.id == sqlid );
					if ( asd != null && asd.connectionId != null )
				    userDetails.connectionId = asd.connectionId;
					userDetails.boughtAccounts = (int)reader["boughtAccounts"];
					userDetails.positiveRates = ( int ) reader[ "positiveRates" ];
					userDetails.negativeRates = ( int ) reader[ "negativeRates" ];
					userDetails.loginIP = ( string ) reader[ "loginIP" ];
					userDetails.lastUpdate = DateTime.Now;
					userDetails.banned = (bool)reader["banned"];
					userDetails.sellerDate = (DateTime)reader["sellerDate"];
					userDetails.banReason = (string)reader["banReason"];
					userDetails.lastLogin = (DateTime)reader["lastLogin"];
					userDetails.phoneNumber = (string)reader["phoneNumber"];
					userDetails.sellingAccounts = csgo.accountsManager.csgoAccounts.FindAll(a => a.sellerid == sqlid && a.status == status.selling).Count;
					userDetails.holdBalance = ( decimal ) reader[ "holdBalance" ];
					userDetails.paypalemail = ( string ) reader[ "paypalemail" ];
				

				}
			} ).addValue("@sqlid", sqlid).Execute();
			int index = users.IndexOf(temp);
			if ( index  != -1)
			users[ index ] = userDetails;
			return userDetails;
		}
	}
}
