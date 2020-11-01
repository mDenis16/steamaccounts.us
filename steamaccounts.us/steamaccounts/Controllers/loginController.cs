using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using csgo.postModels;
using System.Data.Common;
using static csgo.usersManager;
using System.Buffers.Text;
using csgo.core;
using Microsoft.AspNetCore.SignalR;
using RestSharp;
using RestSharp.Authenticators;
using TwoFactorAuthNet;
using PayPal;

namespace csgo.Controllers
{
    public class loginController : Controller
    {

		public static string Base64Encode( string plainText )
		{
			var plainTextBytes = System.Text.Encoding.ASCII.GetBytes(plainText);
			return System.Convert.ToBase64String( plainTextBytes );
		}
		public IActionResult Index()
        {
			adminController.tokenAccess.createToken( Request, adminController.tokenType.login );
			return View();
        }
		[Route( "lostPassword" )]
		public IActionResult lostPassword( )
		{
			if ( csgo.core.requestsHelper.processRequest( Request ) )
				return Json( new { success = "false", message = "You are sending to many requests. Blacklist will expire in 30 seconds." } );
			adminController.tokenAccess.createToken( Request, adminController.tokenType.forgotpass );
			return View( );
		}
	//	public static TwoFactorAuth twoFactorAuthentificator = new TwoFactorAuth("localhost");
		public static TwoFactorAuth tfa = new TwoFactorAuth("SteamAccounts");
		[Route("2fa")]
		public IActionResult twoFa()
		{
			if (csgo.Controllers.adminController.tokenAccess.validateToken(Request, adminController.tokenType.twofactor))
			{
				if (csgo.core.requestsHelper.processRequest(Request))
					return Json(new { success = "false", message = "You are sending to many requests. Blacklist will expire in 30 seconds." });
				
				var userId = TempData["userId"];
				if (userId == null)
				{
					TempData["toast"] = "{type:'error',message:'You are not authorized. An error occured try again later 2.'}";
					return this.Redirect(@Url.Action("index", "home"));
				}
				if (TempData["mobile"] != null)
					ViewBag.isMobile = true;
				var token2FA = (string)TempData["token2FA"];
				if (token2FA != null && token2FA.Length < 3 )
				{
					Console.WriteLine("Need to setup authnetificator. curent token " + token2FA);
					TempData["userId"] = (int)userId;
					ViewBag.userId = (int)userId; string temp = "";
					if (TempData["temp2FAToken"] != null)				
						temp = (string)TempData["temp2FAToken"];
					else
					    temp = tfa.CreateSecret(160);
					ViewBag.temp2FAToken = temp;
				
					TempData["temp2FAToken"] = temp;
					TempData["toast"] = "{type:'warning',message:'You need to setup your 2FA Authentification to continue using this site.'}";
					csgo.Controllers.adminController.tokenAccess.createToken(Request, adminController.tokenType.twofactor);
					return View();
				}
				TempData.Remove("temp2FAToken");
				Console.WriteLine("Need to login with authnetificator. curent token " + token2FA);
				TempData["userId"] = (int)userId;
			
				TempData["token2FA"] = token2FA;
				csgo.Controllers.adminController.tokenAccess.createToken(Request, adminController.tokenType.twofactor);
				return View();
			}
			TempData["toast"] = "{type:'error',message:'You are not authorized. An error occured try again later 3.'}";
			return this.Redirect(@Url.Action("index", "home"));
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> twoFaAuth(csgo.postModels.twoFactor obj)
		{
			if (csgo.Controllers.adminController.tokenAccess.validateToken(Request, adminController.tokenType.twofactor))
			{
				if (csgo.core.requestsHelper.processRequest(Request))
					return Json(new { success = "false", message = "You are sending to many requests. Blacklist will expire in 30 seconds." });
				var a = TempData["userId"];
				if (a == null)
				{

					TempData["toast"] = "{type:'error',message:'You are not authorized. An error occured try again later 1.'}";
					return this.Redirect(@Url.Action("index", "home"));
				}
				int userId = (int)a;
				var temp2FAToken = TempData["temp2FAToken"];
				if (temp2FAToken != null)
				{
					if (obj.code != null && tfa.VerifyCode((string)temp2FAToken, obj.code.Replace(" ", "")))
					{
						await databaseManager.updateQuery($"UPDATE users SET twofaToken = '{temp2FAToken}', loginIP = '' WHERE id = @id LIMIT 1").addValue("@id", TempData["userId"]).Execute();
						TempData["toast"] = "{type:'success',message:'You successully configured the authentificator.'}";
						return this.Redirect(@Url.Action("index", "login"));
					}
					else {
						TempData["userId"] = (int)userId;


						TempData["temp2FAToken"] = (string)temp2FAToken;
						TempData["token2FA"] = "";
						csgo.Controllers.adminController.tokenAccess.createToken(Request, adminController.tokenType.twofactor);
						return this.Redirect("https://localhost/2fa");
					}
				}
				var token2FA = TempData["token2FA"];
				if (token2FA == null)
				{
					TempData["toast"] = "{type:'error',message:'You are not authorized.'}";
					return this.Redirect(@Url.Action("index", "home"));
				}
				Console.WriteLine($"TOKEN FROM DATABASE {(string)token2FA} AND CODE {obj.code} TIME ON THE SERVER {DateTime.Now}");
				Console.WriteLine($"TOKEN SERVERSIDE {tfa.GetCode((string)token2FA, tfa.TimeProvider.GetTimeAsync().Result)}");
			Console.WriteLine(tfa.VerifyCode((string)token2FA, obj.code, 1, DateTime.UtcNow));
				Console.WriteLine($"UTC NOW {DateTime.UtcNow.ToString()}");
				Console.WriteLine($"GENERATED 2FA CODE {tfa.GetCode((string)token2FA)}");
				if (tfa.VerifyCode((string)token2FA, obj.code.Replace(" ", "")))
				{

					Guid g = Guid.NewGuid();
					string GuidString = Convert.ToBase64String(g.ToByteArray());
					GuidString = GuidString.Replace("=", "");
					GuidString = GuidString.Replace("+", "");

					string cookiegenerated = GuidString;
					string ip = Request.getIPAddress();
					
					userData userDetails = new userData();
					userDetails.loginIP = ip;
					await databaseManager.selectQuery("SELECT * FROM users WHERE id = @id LIMIT 1", delegate (DbDataReader reader)
					{
						if (reader.HasRows)
						{
							userDetails.balance = (decimal)reader["balance"];
							userDetails.username = (string)reader["username"];
							userDetails.id = (int)reader["id"];
							userDetails.cookie = cookiegenerated;
							userDetails.registerDate = (DateTime)reader["registerDate"];
							userDetails.seller = (bool)reader["seller"];
							userDetails.negativeRates = (int)reader["negativeRates"];
							userDetails.positiveRates = (int)reader["positiveRates"];
							userDetails.soldAccounts = (int)reader["soldAccounts"];
							userDetails.boughtAccounts = (int)reader["boughtAccounts"];
							userDetails.confirmed = (bool)reader["confirmed"];
							userDetails.admin = (bool)reader["admin"];
							userDetails.email = (string)reader["email"];
							userDetails.lastConfirm = (DateTime)reader["lastConfirm"];
							userDetails.validateToken = (string)reader["validateToken"];
							userDetails.lastUpdate = DateTime.Now;
							userDetails.banned = (bool)reader["banned"];
							userDetails.banReason = (string)reader["banReason"];
							userDetails.twofa = (bool)reader["twofa"];
							userDetails.twofaToken = (string)reader["twofaToken"];
							userDetails.lastLogin = (DateTime)reader["lastLogin"];
						}
					}).addValue("@id", userId).Execute();
					if (userDetails.banned)
					{
						TempData["toast"] = "{type:'error',message:'" + $"Your account is banned on this site. Reason: {userDetails.banReason}" + "'}";
						return this.Redirect(@Url.Action("index", "home"));
					}
					
					if (!userDetails.confirmed)
					{
						if (userDetails.email.Contains("yahoo"))
						{
							TempData["toast"] = "{type:'warning',message:'Yahoo isn't fully supported. Please change your email in order to use this site.'}";
							TempData["userId"] = userDetails.id;
							Console.WriteLine("email recovery");
							csgo.Controllers.adminController.tokenAccess.createToken(Request, adminController.tokenType.changeemail);
							return this.Redirect(@Url.Action("changeEmail"));
						}
						else
						{
							if ((int)(DateTime.Now - userDetails.lastConfirm).TotalMinutes > 30)
							{
								if (userDetails.validateToken.Length < 3)
									userDetails.validateToken = csgo.core.emailManager.randomToken(new Random().Next(10, 30));
								Console.WriteLine(csgo.core.emailManager.sendConfirmationEmail(userDetails.email, userDetails.validateToken).Content);
								await databaseManager.updateQuery($"UPDATE users SET lastConfirm = CURRENT_TIMESTAMP, validateToken = @validateToken WHERE id = @id LIMIT 1").addValue("@validateToken", userDetails.validateToken).addValue("@id", userId).Execute();
								TempData["toast"] = "{type:'success',message:'And confirmation code was send to your email.'}";
							}
							else
							{
								TempData["toast"] = "{type:'success',message:'Account isn`t confirmed yet. Check your email.'}";
							}
						}
						return this.Redirect(@Url.Action("index", "home"));
					}

					CookieOptions option = new CookieOptions();



					option.Expires = new DateTimeOffset?(DateTime.Now.AddDays(5));

					Response.Cookies.Append("sessionid", cookiegenerated, option);

					await databaseManager.updateQuery($"UPDATE users SET cookie = '{cookiegenerated}', loginIP = '{ip}', lastLogin = CURRENT_TIMESTAMP WHERE id = @id LIMIT 1").addValue("@id", userId).Execute();
					var index = csgo.usersManager.users.FindIndex(a => a.id == userId);

					if (index != -1)
					{
						var b = csgo.usersManager.users[index];
						if (b.connectionId != null)
						{
							await b.sendNotify(core.notifyManager.notifyType.warning, $"Someone just connected on your account using 2FA. IP: {ip}. You will be log out.");
							await csgo.core.ChatHub.Current.Clients.Client(b.connectionId).SendAsync("logout");
						}
						csgo.usersManager.users[index] = userDetails;

					}
					else
						csgo.usersManager.users.Add(userDetails);

					var s = TempData["loginRequest"];

					TempData.Remove("loginRequest");
					TempData["toast"] = "{type:'success',message:'You succesfully logged in using 2FA.'}";

					return this.Redirect(@Url.Action("index", "home"));

				}
				else
				{
					TempData["userId"] = (int)userId;
					TempData["token2FA"] = (string)token2FA;
					TempData["toast"] = "{type:'error',message:'Your 2FA code is invalid. You have more 2 chances.'}";
					csgo.Controllers.adminController.tokenAccess.createToken(Request, adminController.tokenType.twofactor);
					return this.Redirect("https://localhost/2fa/");
				}


				
				TempData["toast"] = "{type:'success',message:'And confirmation code was send to your new email.'}";
				return this.Redirect(@Url.Action("index", "home"));
			}
			TempData["toast"] = "{type:'error',message:'You are not authorized 2.'}";
			return this.Redirect(@Url.Action("index", "home"));
		}
		[Route("changeEmail")]
		public IActionResult changeEmail()
		{
			if (csgo.Controllers.adminController.tokenAccess.validateToken(Request, adminController.tokenType.changeemail))
			{
				if (csgo.core.requestsHelper.processRequest(Request))
					return Json(new { success = "false", message = "You are sending to many requests. Blacklist will expire in 30 seconds." });
				
				var userId = TempData["userId"];
				if (userId == null)
				{
					TempData["toast"] = "{type:'error',message:'You are not authorized. An error occured try again later 2.'}";
					return this.Redirect(@Url.Action("index", "home"));
				}
				
				TempData["userId"] = (int)userId;
				csgo.Controllers.adminController.tokenAccess.createToken(Request, adminController.tokenType.changeemail);
				return View();
			}
			TempData["toast"] = "{type:'error',message:'You are not authorized. An error occured try again later 3.'}";
			return this.Redirect(@Url.Action("index", "home"));
		}
	

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> changeEmailPost(csgo.postModels.forgotPassword obj)
		{
			if (csgo.Controllers.adminController.tokenAccess.validateToken(Request, adminController.tokenType.changeemail))
			{
				if (csgo.core.requestsHelper.processRequest(Request))
					return Json(new { success = "false", message = "You are sending to many requests. Blacklist will expire in 30 seconds." });
				var userId = TempData["userId"];
				if (userId == null)
				{

					TempData["toast"] = "{type:'error',message:'You are not authorized. An error occured try again later 1.'}";
					return this.Redirect(@Url.Action("index", "home"));
				}
				if (obj.email.Contains("yahoo"))
				{
					csgo.Controllers.adminController.tokenAccess.createToken(Request, adminController.tokenType.changeemail);
					TempData["toast"] = "{type:'error',message:'You can not use yahoo. Please use another mail service.'}";
					TempData["userId"] = (int)userId;
					return this.Redirect(@Url.Action("changeEmail"));
				}
				if (await doesExist("email", obj.email))
				{
					csgo.Controllers.adminController.tokenAccess.createToken(Request, adminController.tokenType.changeemail);
					TempData["toast"] = "{type:'error',message:'This email is already in use.'}";
					TempData["userId"] = (int)userId;
					return this.Redirect(@Url.Action("changeEmail"));
				}


				string validateToken = csgo.core.emailManager.randomToken(new Random().Next(10, 30));
				Console.WriteLine(csgo.core.emailManager.sendConfirmationEmail(obj.email, validateToken).Content);
				await databaseManager.updateQuery($"UPDATE users SET lastConfirm = CURRENT_TIMESTAMP, email = @email, validateToken = @validateToken WHERE id = @id LIMIT 1").addValue("@validateToken", validateToken).addValue("@id", (int)userId).addValue("@email", obj.email).Execute();
				TempData["toast"] = "{type:'success',message:'And confirmation code was send to your new email.'}";
				return this.Redirect(@Url.Action("index", "home"));
			}
			TempData["toast"] = "{type:'error',message:'You are not authorized 2.'}";
			return this.Redirect(@Url.Action("index", "home"));
		}

		public JsonResult resetPassApi( int userId )
		{
			string generatedToken = new csgo.usersManager.recoveryPassword(userId).addToken();
			return Json( new {token = generatedToken } );
		}
		[Route( "recoveryPassword" )]
		public  IActionResult recoveryPassword( string token )
		{
			if ( csgo.core.requestsHelper.processRequest( Request ) )
				return Json( new { success = "false", message = "You are sending to many requests. Blacklist will expire in 30 seconds." } );
			var tkn = new csgo.usersManager.recoveryPassword(token);
			if ( tkn.verifyToken( ) )
			{
				TempData[ "userId" ] = tkn.userId;
				ViewBag.exist = true;
			}
			return View( );
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> resetPassword( csgo.postModels.recoveryPassword obj )
		{
			if ( csgo.Controllers.adminController.tokenAccess.validateToken(Request, adminController.tokenType.resetpass ) )
			{
				if ( csgo.core.requestsHelper.processRequest( Request ) )
					return Json( new { success = "false", message = "You are sending to many requests. Blacklist will expire in 30 seconds." } );
				var data = TempData[ "userId" ];
				if ( data != null )
				{



					TempData[ "toast" ] = "{type:'success',message:'Your password was succesfully changed.'}";
					//Console.WriteLine( "a meres" + TempData[ "userId" ] );
					new csgo.usersManager.recoveryPassword( ( int ) data ).deleteToken( );
					await databaseManager.updateQuery( $"UPDATE users SET lastChangedPassword = CURRENT_TIMESTAMP, password = @newPassword WHERE id = @id LIMIT 1" ).addValue( "@id", ( int ) data ).addValue( "@newPassword", obj.password ).Execute( );

				}
				return this.Redirect( @Url.Action( "index", "home" ) );
			}
			else
			{
				TempData[ "toast" ] = "{type:'success',message:'Invalid token.'}";
				return this.Redirect( @Url.Action( "index", "home" ) );
			}
		
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> forgotPassword( csgo.postModels.forgotPassword obj )
		{
			if ( csgo.Controllers.adminController.tokenAccess.validateToken( Request, adminController.tokenType.forgotpass ) )
			{
				if ( csgo.core.requestsHelper.processRequest( Request ) )
					return Json( new { success = "false", message = "You are sending to many requests. Blacklist will expire in 30 seconds." } );

				bool exist = false; int id = -1; DateTime lastPasswordChange = DateTime.Now;
				await databaseManager.selectQuery( "SELECT * FROM users WHERE email = @email LIMIT 1", delegate ( DbDataReader reader )
				{
					if ( reader.HasRows )
					{
						id = ( int ) reader[ "id" ];
						lastPasswordChange = ( DateTime ) reader[ "lastChangedPassword" ];
						exist = true;
					}
				} ).addValue( "@email", obj.email ).Execute( );
				if ( exist )
				{
					if ((int)(DateTime.Now - lastPasswordChange).TotalHours < 2 )
					{
						TempData[ "toast" ] = "{type:'error',message:'You can reset your password once every 2 hours.'}";
						return this.Redirect( @Url.Action( "index", "home" ) );
					}
					string token = new csgo.usersManager.recoveryPassword(id).addToken();
					csgo.core.emailManager.sendRecoveryEmail( obj.email, token );
					TempData[ "toast" ] = "{type:'success',message:'An recovery link was sent to your email.'}";
					return this.Redirect( @Url.Action("index", "home" ) );
				}
				else
				{
					TempData[ "toast" ] = "{type:'error',message:'Email isn`t asocied to any account.'}";
					return this.Redirect( @Url.Action( "lostPassword", "login" ) );
				}
			}
			
			TempData[ "toast" ] = "{type:'error',message:'You are not authorized.'}";
			return this.Redirect( @Url.Action( "forgotPassword", "login" ) );
		}
		[HttpGet]
		public async Task<IActionResult> changePassword( string oldPassword, string newPassword )
		{
			
		

			if ( adminController.tokenAccess.validateToken( Request, adminController.tokenType.changepass ) )
			{
				if ( csgo.core.requestsHelper.processRequest( Request ) )
					return Json( new { success = "false", message = "You are sending to many requests. Blacklist will expire in 30 seconds." } );
				if ( Request.Cookies[ "sessionid" ] == null )
					return Json( new { success = false, message = "You are not logged in." } );

				var account = csgo.usersManager.users.Find(a=> a.cookie == Request.Cookies[ "sessionid" ] );
				if ( account == null )
					return Json( new { success = false, message = "You are not logged in." } );
				bool isGood = false;
				await databaseManager.selectQuery( "SELECT * FROM users WHERE id = @id AND password = @oldPassword LIMIT 1", delegate ( DbDataReader reader )
			   {
				   isGood = reader.HasRows;
			   } ).addValue( "@id", account.id ).addValue( "@oldPassword", oldPassword ).Execute( );
				if ( !isGood )
					return Json( new { success = false, message = "Current password is incorect. Try again later." } );


		        await databaseManager.updateQuery( $"UPDATE users SET lastChangedPassword = CURRENT_TIMESTAMP, password = @newPassword WHERE id = @id LIMIT 1" ).addValue( "@id", account.id ).addValue( "@newPassword", newPassword ).Execute( );
				return Json( new { success = true, message = "Password was succesfully changed." } );
			}
			return Json( new { success = false, message = "You are not authorized." } );
		}
		[Route("confirm")]
		public ActionResult Confirm(string token )
		{
			if ( csgo.core.requestsHelper.processRequest( Request ) )
				return Json( new { success = "false", message = "You are sending to many requests. Blacklist will expire in 30 seconds." } );
			ViewBag.token = token;
			csgo.Controllers.adminController.tokenAccess.createToken( Request, adminController.tokenType.confirm, null, 30 );
			return View( );
		}

		
		[ HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login( [Bind( Prefix = "Item1" )] Login objUser )
		{

			if ( adminController.tokenAccess.validateToken( Request, adminController.tokenType.login ) && ModelState.IsValid )
			{
				if ( csgo.core.requestsHelper.processRequest( Request ) )
					return Json( new { success = "false", message = "You are sending to many requests. Blacklist will expire in 30 seconds." } );
		   
				
	 /*	if (HttpContext.Request.getIPAddress() != "93.118.232.220" )
		        {
		      TempData["toast"] = "{type:'error',message:'Site is under maintenance. Come back in 30 minutes.'}";
					
		     	return this.Redirect(@Url.Action("index", "home"));
			
			}*/
				
			if (await usersManager.loginUser(objUser.username, objUser.password))
				 {
					Guid g = Guid.NewGuid();
					string GuidString = Convert.ToBase64String(g.ToByteArray());
					GuidString = GuidString.Replace("=", "");
					GuidString = GuidString.Replace("+", "");

					string cookiegenerated = GuidString;
					string ip = Request.getIPAddress();

					userData userDetails = new userData();
					userDetails.loginIP = ip;
					await databaseManager.selectQuery("SELECT * FROM users WHERE username = @username LIMIT 1", delegate (DbDataReader reader)
					{
						if (reader.HasRows)
						{
							userDetails.balance = ( decimal ) reader[ "balance" ];
							userDetails.username = ( string ) reader[ "username" ];
							userDetails.id = ( int ) reader[ "id" ];
							userDetails.cookie = cookiegenerated;
							userDetails.registerDate = ( DateTime ) reader[ "registerDate" ];
							userDetails.seller = ( bool ) reader[ "seller" ];
							userDetails.negativeRates = ( int ) reader[ "negativeRates" ];
							userDetails.positiveRates = ( int ) reader[ "positiveRates" ];
							userDetails.soldAccounts = ( int ) reader[ "soldAccounts" ];
							userDetails.boughtAccounts = ( int ) reader[ "boughtAccounts" ];
							userDetails.confirmed = ( bool ) reader[ "confirmed" ];
							userDetails.admin = ( bool ) reader[ "admin" ];
							userDetails.email = ( string ) reader[ "email" ];
							userDetails.lastConfirm = ( DateTime ) reader[ "lastConfirm" ];
							userDetails.validateToken = ( string ) reader[ "validateToken" ];
							userDetails.lastUpdate = DateTime.Now;
							userDetails.banned = (bool)reader["banned"];
							userDetails.banReason = (string)reader["banReason"];
							userDetails.twofa = (bool)reader["twofa"];
							userDetails.twofaToken = (string)reader["twofaToken"];
							userDetails.loginIP = (string)reader["loginIP"];
							userDetails.lastLogin = (DateTime)reader["lastLogin"];
							userDetails.paypalemail = ( string ) reader[ "paypalemail" ];
							userDetails.holdBalance = ( decimal ) reader[ "holdBalance" ];

						}
					}).addValue("@username", objUser.username).Execute();
					if (userDetails.banned)
					{
						TempData["toast"] = "{type:'error',message:'" + $"Your account is banned on this site. Reason: {userDetails.banReason}" + "'}";
						return this.Redirect(@Url.Action("index", "home"));
					}
					if (!userDetails.confirmed )
					{
						if (userDetails.email.Contains("yahoo"))
						{
							TempData["toast"] = "{type:'warning',message:'Yahoo isn't fully supported. Please change your email in order to use this site.'}";
							TempData["userId"] = userDetails.id;
							Console.WriteLine("email recovery");
							csgo.Controllers.adminController.tokenAccess.createToken(Request, adminController.tokenType.changeemail);
							return this.Redirect(@Url.Action("changeEmail"));
						}
						else
						{
							if ((int)(DateTime.Now - userDetails.lastConfirm).TotalMinutes > 30)
							{
								if (userDetails.validateToken.Length < 3)
									userDetails.validateToken = csgo.core.emailManager.randomToken(new Random().Next(10, 30));
								Console.WriteLine(csgo.core.emailManager.sendConfirmationEmail(userDetails.email, userDetails.validateToken).Content);
								await databaseManager.updateQuery($"UPDATE users SET lastConfirm = CURRENT_TIMESTAMP, validateToken = @validateToken WHERE username = @username LIMIT 1").addValue("@validateToken", userDetails.validateToken).addValue("@username", userDetails.username).Execute();
								TempData["toast"] = "{type:'success',message:'And confirmation code was send to your email.'}";
							}
							else
							{
								TempData["toast"] = "{type:'success',message:'Account isn`t confirmed yet. Check your email.'}";
							}
						}
						return this.Redirect( @Url.Action( "index", "home" ) );
					}
				/*	if ((userDetails.soldAccounts > 2) && (userDetails.twofaToken.Length < 2 || userDetails.loginIP != ip))
					{
						if (Request.IsMobileBrowser())
							TempData["mobile"] = true;

						TempData["userId"] = userDetails.id;
						TempData["token2FA"] = userDetails.twofaToken;
	
						csgo.Controllers.adminController.tokenAccess.createToken(Request, adminController.tokenType.twofactor);
						return this.Redirect("https://localhost/2fa");
					}*/
					

					CookieOptions option = new CookieOptions();



					option.Expires = new DateTimeOffset?( DateTime.Now.AddMinutes( 120.0 ) );

					Response.Cookies.Append( "sessionid", cookiegenerated, option );
		
					await databaseManager.updateQuery( $"UPDATE users SET cookie = '{cookiegenerated}', loginIP = '{ip}', lastLogin = CURRENT_TIMESTAMP WHERE username = @username LIMIT 1" ).addValue("@username", objUser.username).Execute( );
					var index = csgo.usersManager.users.FindIndex(a => a.id == userDetails.id);
					await csgo.core.logsManager.utilities.addLoginLog( userDetails.username, userDetails.id, ip );
					if ( index != -1 )
					{
						var a = csgo.usersManager.users[ index ];
						if ( a.connectionId != null )
						{
							await a.sendNotify( core.notifyManager.notifyType.warning, $"Someone just connected on your account. IP: {ip}. You will be log out." );
							await csgo.core.ChatHub.Current.Clients.Client( a.connectionId ).SendAsync( "logout" );
						}
						csgo.usersManager.users[ index ] = userDetails;
						
					}
					else
						csgo.usersManager.users.Add( userDetails );
					
					var s = TempData["loginRequest"];
					
					TempData.Remove("loginRequest");
					TempData[ "toast" ] = "{type:'success',message:'You succesfully logged in.'}";

					return this.Redirect(@Url.Action("index", "home"));
				}
			}
			TempData[ "toast" ] = "{type:'error',message:'You entered a wrong password.'}";

			return this.Redirect( @Url.Action( "index", "login" ) );
		}
		[HttpGet]
		public async Task<JsonResult> phoneLogin(string username, string password )
		{

				if ( await usersManager.loginUser( username, password ) )
				{


				return Json( new { success = true, message = "You succesfully logged in." } );
				}



			return Json( new { success = true, message = "You entered an invalid password." } );
		}
	}
}
