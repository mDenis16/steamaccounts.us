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

namespace csgo.Controllers
{
    public class registerController : Controller
    {
        

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Register( [Bind( Prefix = "Item2" )] Register objUser )
		{
			
			if ( ModelState.IsValid )
			{
				if (objUser.email.Contains("yahoo"))
				{
					TempData["toast"] = "{type:'error',message:'Yahoo email is currently disabled..'}";
					return this.RedirectToAction("index", "login");
				}
				if ( objUser.username.Contains( " " ) )
				{
					TempData[ "toast" ] = "{type:'error',message:'Username can't contain spaces.'}";
					return this.RedirectToAction("index", "login");
				}
				if ( objUser.username.Length > 30 )
				{
					TempData[ "toast" ] = "{type:'error',message:'Username is too long.'}";
					return this.RedirectToAction("index", "login");
				}
				if ( objUser.password.Length > 30 )
				{
					TempData[ "toast" ] = "{type:'error',message:'Password is too long.'}";
					return this.RedirectToAction("index", "login");
				}
				if ( objUser.password.Contains( " " ) )
				{
					TempData[ "toast" ] = "{type:'error',message:'Password can't contain spaces.'}";
					return this.RedirectToAction("index", "login");
				}
				if ( objUser.username.Length < 4 )
				{
					TempData[ "toast" ] = "{type:'error',message:'Username is too short.'}";
					return this.RedirectToAction("index", "login");
				}
				if ( objUser.password.Length < 4 )
				{
					TempData[ "toast" ] = "{type:'error',message:'Username is too short.'}";
					return this.RedirectToAction("index", "login");
				}
				if ( await doesExist( "email", objUser.email ) )
				{
					TempData[ "toast" ] = "{type:'error',message:'This email is already in use.'}";
					return this.RedirectToAction( "index", "login" );
				}
				if ( await doesExist( "username", objUser.username ) )
				{
					TempData["toast"] = "{type:'error',message:'This username is already in use.'}";
					return this.RedirectToAction( "index", "login" );
				}
				if ( await usersManager.registerUser( objUser.username, objUser.password, objUser.email ) )
				{
					/*	if ( await usersManager.loginUser( objUser.username, objUser.password ) )
						{
							string cookiegenerated = Request.Host.GetHashCode().ToString() + DateTime.Now.GetHashCode().ToString();
							CookieOptions option = new CookieOptions();



							option.Expires = new DateTimeOffset?( DateTime.Now.AddMinutes( 120.0 ) );

							Response.Cookies.Append( "sessionid", cookiegenerated, option );
							await databaseManager.updateQuery( $"UPDATE users SET cookie = '{cookiegenerated}' WHERE username = '{objUser.username}' LIMIT 1" ).Execute( );

							userData userDetails = new userData();
							await databaseManager.selectQuery( "SELECT * FROM users WHERE username = @username LIMIT 1", delegate ( DbDataReader reader )
							{
								if ( reader.HasRows )
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
									userDetails.seller = ( bool ) reader[ "seller" ];
									userDetails.email = ( string ) reader[ "email" ];
									userDetails.admin = ( bool ) reader[ "admin" ];
									userDetails.lastUpdate = DateTime.Now;
								}
							} ).addValue( "@username", objUser.username ).Execute( );
							var a = csgo.usersManager.users.Find(a => a.id == userDetails.id);

							if ( a != null )
								a = userDetails;
							else
								csgo.usersManager.users.Add( userDetails );
							TempData[ "toast" ] = "{type:'success',message:'You succesfully logged in.'}";
							return this.Redirect( @Url.Action( "index", "home" ) );
						}
						*/
					string token = csgo.core.emailManager.randomToken(new Random().Next(10,30));
					await databaseManager.updateQuery( $"UPDATE users SET validateToken = '{token}', lastConfirm = CURRENT_TIMESTAMP WHERE username = @username LIMIT 1" ).addValue("@username", objUser.username).Execute( );
				Console.WriteLine(csgo.core.emailManager.sendConfirmationEmail( objUser.email, token ).Content);
					TempData[ "toast" ] = "{type:'success',message:'An confirmation email was sent to your email.'}";
				}
				
			}
			return this.RedirectToAction( "index", "login" );
		}

    }
}
