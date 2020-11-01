using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SteamKit.CSGO;
using SteamKit2;
using WebApplication1.Models;
using Newtonsoft;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using PayPal.Api;
using System.Net.Http;
using System.Net.Http.Headers;
using csgo.postModels;

using System.Data;

using Microsoft.AspNetCore.Authorization;
using csgo.core;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Encodings.Web;
using Newtonsoft.Json;
using System.Web;
using System.Text.RegularExpressions;

namespace csgo.Controllers
{
    
    public class ticketController : Controller
    {
      
        public async Task<IActionResult> viewDispute( )
        {
            ViewBag.id = TempData[ "ticketId" ];
            if ( ViewBag.id == null )
            {
                TempData[ "toast" ] = "{type:'error',message:'You are not authorized.'}";
                return Redirect( Url.Action( "notFound", "home" ) );
            }
            TempData[ "ticketId" ] = ViewBag.id;



            return View( );
        }
        [HttpGet]
        public async Task<IActionResult> requestViewDispute( int id )
        {
         
            TempData[ "ticketId" ] = id;
           return Redirect( Url.Action( "viewDispute", "ticket" ) );
         
        }
        [HttpGet]
        public async Task<JsonResult> createDispute( int csgoId )
        {

            if ( csgo.core.requestsHelper.processRequest( Request ) )
                return Json( new { success = false, message = "Please stop spamming the api. You can try again in 30 seconds." } );
            if ( adminController.tokenAccess.validateToken( Request, adminController.tokenType.createdispute ) )
            { 
                var csgoAccount = csgo.accountsManager.csgoAccounts.Find( a => a.id == csgoId);
            if ( csgoAccount == null )
                return Json( new { success = false, message = "This account doesn't exist." } );
            if ( csgoAccount.disputed )
                return Json( new { success = false, message = "This account already have a dispute." } );

            var account = csgo.usersManager.users.Find(a=> a.cookie == Request.Cookies[ "sessionid" ] );
            if ( account == null )
                return Json( new { success = false, message = "You are not logged in." } );

            if ( csgoAccount.buyerid != account.id )
                return Json( new { success = false, message = "You didn't bought this account." } );

            if ( csgoAccount.status != csgo.status.sold )
                return Json( new { success = false, message = "This account isn't sold yet." } );

            var id = await csgo.core.ticketsManager.createDispute( csgoAccount.id, csgoAccount.seller, csgoAccount.sellerid, csgoAccount.buyer, csgoAccount.buyerid, ticketsManager.type.dispute );

            return Json( new { success = false, id = id, message = "Your dispute was succesfully created. Redirecting in 3 seconds." } );
           }
            return Json( new { success = false, message = "You are not authorized." } );
        }

        [HttpGet]
        public async Task<JsonResult> getTicketData( )
        {

            int ticketId =  (int)TempData[ "ticketId" ];
            if ( ticketId == null )
                return Json( new { success = false, message = "Invalid ticket id." } );
            var account = csgo.usersManager.users.Find(a=> a.cookie == Request.Cookies[ "sessionid" ] );
            if ( account == null )
                return Json( new { success = false, message = "You are not logged in." } );

          var ticket =   await csgo.core.ticketsManager.getTicketData( ticketId );
            if (!(ticket.againstId == account.id || ticket.fromUserId == account.id || account.admin))
                  return Json( new { success = false, message = "Unauthorized access." } );

            TempData[ "ticketId" ] = ticketId;
            return Json(ticket);
        }
        
      [ HttpGet ]
        public async Task<JsonResult> readMessages(string cached )
        {
            if ( adminController.tokenAccess.validateToken( Request, adminController.tokenType.readticket ) )
            {
                var ticketId =  TempData[ "ticketId" ];



                if ( ticketId == null )
                    return Json( new { success = false, message = "Invalid ticket id." } );
                var account = csgo.usersManager.users.Find(a=> a.cookie == Request.Cookies[ "sessionid" ] );
                if ( account == null )
                    return Json( new { success = false, message = "You are not logged in." } );
                var cachedMessages = Newtonsoft.Json.JsonConvert.DeserializeObject<int[]>(cached);

                var ticketData = await csgo.core.ticketsManager.getTicketData((int)ticketId);

                if ( ticketData == null )
                    return Json( new { success = false, message = "Ticket doesn't exist." } );

                var messagesFinal = new List<csgo.core.ticketsManager.message>();
                foreach ( var item in ticketData.messages )
                {
                    if ( !cachedMessages.Contains( item.id ) )
                    {
                   /*     item.messageData = $@"<li class=\'d-flex justify-content-between mb-4 pt-1' style='width: 100%'>" +
                            "<div class='chat-body white p-3 ml-2 z-depth-1 mr-2' style='width: 100%'>" +
                            " <div class='header'>" +
                            $"<strong class='primary-font'> {item.fromUsername}</strong>" +
                            $"<small class='pull-right text-muted'><i class='far fa-clock'></i>{item.date.ToString()}</small>" +
                            "</div>" +
                            " <hr class='w-100'>" +
                            $"    <p class='pr-2'> {item.messageData} </p>" +
                            "</div>" +
                            "</li>";
                            */
                        //item.messageData = HttpUtility.HtmlEncode( item.messageData );

                        messagesFinal.Add( item );

                    }
                }

                TempData[ "ticketId" ] = ticketId;
             

            

                return Json( ( messagesFinal ) );
            }
            return Json( new { status = false, message = "Unauthorized access." } );
        }
       


         [ HttpGet ]
        public async Task<JsonResult> closeTicket( )
        {
            if ( !adminController.tokenAccess.validateToken( Request, adminController.tokenType.admin ) )
                return Json( new { success = false, message = "You are not authorized." } );
           var ticketId = TempData[ "ticketId" ];
            if ( ticketId == null )
                return Json( new { success = false, message = "Invalid ticket id." } );

            var account = csgo.usersManager.users.Find(a=> a.cookie == Request.Cookies[ "sessionid" ] );
            
            if ( account == null )
                return Json( new { success = false, message = "You are not logged in." } );
            if (!account.admin)
                return Json( new { success = false, message = "You are not an admin" } );
            await csgo.core.ticketsManager.setTicketStatus( ( int ) ticketId, account.id, account.username, true );
            TempData[ "ticketId" ] = ticketId;
            return Json( new { success = true, message = "Ticket succesfully closed." } );
        }
        [HttpGet]
        public async Task<JsonResult> openTicket( )
        {
            if ( !adminController.tokenAccess.validateToken( Request, adminController.tokenType.admin ) )
                return Json( new { success = false, message = "You are not authorized." } );
            var ticketId = TempData[ "ticketId" ];
            if ( ticketId == null )
                return Json( new { success = false, message = "Invalid ticket id." } );

            var account = csgo.usersManager.users.Find(a=> a.cookie == Request.Cookies[ "sessionid" ] );

            if ( account == null )
                return Json( new { success = false, message = "You are not logged in." } );
            if ( !account.admin )
                return Json( new { success = false, message = "You are not an admin" } );
            await csgo.core.ticketsManager.setTicketStatus( ( int ) ticketId, account.id, account.username, false );
            TempData[ "ticketId" ] = ticketId;
            return Json( new { success = true, message = "Ticket succesfully opened." } );
        }
        public static bool ValidateAntiXSS(string inputParameter)
        {
            if (string.IsNullOrEmpty(inputParameter))
                return true;

            // Following regex convers all the js events and html tags mentioned in followng links.
            //https://www.owasp.org/index.php/XSS_Filter_Evasion_Cheat_Sheet                 
            //https://msdn.microsoft.com/en-us/library/ff649310.aspx

            var pattren = new StringBuilder();

            //Checks any js events i.e. onKeyUp(), onBlur(), alerts and custom js functions etc.             
            pattren.Append(@"((alert|on\w+|function\s+\w+)\s*\(\s*(['+\d\w](,?\s*['+\d\w]*)*)*\s*\))");

            //Checks any html tags i.e. <script, <embed, <object etc.
            pattren.Append(@"|(<(script|iframe|embed|frame|frameset|object|img|applet|body|html|style|layer|link|ilayer|meta|bgsound|p|h))");

            return !Regex.IsMatch(System.Web.HttpUtility.UrlDecode(inputParameter), pattren.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
        [ HttpGet]
        public async Task<JsonResult> sendMessage( string token, string message )
        {
          
             if ( csgo.core.requestsHelper.processRequest(Request))
                return Json( new { success = false, message = "Please stop spamming the api. You can send again in 30 seconds." } ); 

            if ( !adminController.tokenAccess.validateToken( Request, adminController.tokenType.sendmessage, token ) )
                return Json( new { success = false, message = "Your token is invalid" } );
            
          if (!ValidateAntiXSS(message))
                return Json(new { success = false, message = "Your message contains malicious code." });
            var ticketId =  TempData[ "ticketId" ];
            if ( ticketId == null)
               return Json( new { success = false,  message = "Invalid ticket id." } );
            var account = csgo.usersManager.users.Find(a=> a.cookie == Request.Cookies[ "sessionid" ] );
            if ( account == null )
                return Json( new { success = false, message = "You are not logged in." } );
            if ((DateTime.Now - account.lastMessageSent).TotalMinutes < 5.00 && !account.admin )
                return Json( new { success = false, message = "You need to wait 5 minutes to send a message gain." } );
         

            int messageID = await csgo.core.ticketsManager.addMessageToTicket((int)ticketId, account.id, account.username, message );
            if ( messageID  == -1)
                return Json( new { success = false, message = "You can't reply this ticket. Maybe is closed." } );
            var newTkn = Convert.ToBase64String( Guid.NewGuid( ).ToByteArray( ) );
           
            TempData[ "ticketId" ] = ticketId;
            csgo.Controllers.adminController.tokenAccess.createToken( Request, adminController.tokenType.sendmessage, newTkn, 15 );
            return Json( new { success = true, response = new { msg = message, token = newTkn, date = DateTime.Now.ToString(), username = account.username, id = messageID }, message = "Message succesfully sent." } );
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
