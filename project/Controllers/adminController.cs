using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApplication1.Models;
using System.Web.Helpers;
using Newtonsoft.Json.Linq;
using csgo.core;
using Microsoft.AspNetCore.SignalR;

namespace csgo.Controllers
{
    public class adminController : Controller
    {
       
        private readonly ILogger<adminController> _logger;
        public enum tokenType
        {
            login,
            register,
            admin,
            addaccount,
            payment,
            buyaccount,
            viewaccount,
            rate,
            openticket,
            opendispute,
            readticket,
            sendmessage,
            closeticket,
            changepass,
            resetpass,
            forgotpass,
            confirm,
            paysafe,
            createdispute,
            changeemail,
            requestseller,
            generateemail,
            twofactor,
            readnotifications,
            requestwithdraw,
            setpaypal,
            accountdata
        }
        public class tokenAccess
        {
        
            public string ip { get; set; }
            public int delay { get; set; }
            public DateTime last { get; set; }
            public string token { get; set; }
            public tokenType type { get; set; }
            public tokenAccess( HttpRequest request, tokenType type, string token = null, int delay = 0)
            {
                this.type = type;
                this.ip = request.getIPAddress();
                this.delay = delay;
                this.token = token;
                this.last = DateTime.Now;
            }
            public static void createToken( HttpRequest request, tokenType type, string token = null, int delay = 0)
            {
                if ( request == null )
                    return;
                try
                {
                    var index =  accesses.FindIndex(a=> a.ip == request.getIPAddress() && a.type == type);
                    if ( index != -1 )
                        accesses[ index ] = new tokenAccess( request, type, token, delay );
                    else
                        accesses.Add( new tokenAccess( request, type, token, delay ) );
                }
                catch
                {

                }
            }
            public static bool validateToken( HttpRequest request, tokenType type, string token = null )
            {
                tokenAccess tkn = null;
                if (token != null)
                    tkn =   accesses.Find(a=> a.ip ==  request.getIPAddress() && a.type == type && a.token == token);
                else
                    tkn = accesses.Find( a => a.ip == request.getIPAddress() && a.type == type );
                if ( tkn != null )
                {
                    if ( tkn.delay > 0 && (int)( DateTime.Now - tkn.last ).TotalSeconds < tkn.delay )
                        return false;
                   
                    accesses.Remove( tkn );
                    return true;
                }
                return false;
            }
        }
        public static List<tokenAccess> accesses = new List<tokenAccess>();
        public adminController(ILogger<adminController> logger)
        {
            _logger = logger;
        }
       
        public IActionResult Index()
        {
       
            return View();
        }
        public IActionResult Users()
        {
            tokenAccess.createToken( Request, tokenType.admin );
          
            return View();
        }
        public IActionResult sellerRequests()
        {
            tokenAccess.createToken(Request, tokenType.admin);

            return View();
        }
        public IActionResult viewSellerRequest(int userId)
        {
            tokenAccess.createToken(Request, tokenType.admin);
            ViewBag.userId = userId;
            return View();
        }
        public IActionResult Transactions( )
        {
            tokenAccess.createToken( Request, tokenType.admin );

            return View( );
        }
        public IActionResult AccountsTranfsers( )
        {
            tokenAccess.createToken( Request, tokenType.admin );

            return View( );
        }
        
        public IActionResult Accounts( )
        {
            tokenAccess.createToken( Request, tokenType.admin );

            return View( );
        }
        public IActionResult withdrawRequests( )
        {
            tokenAccess.createToken( Request, tokenType.admin );

            return View( );
        }
        [HttpGet]
        public async Task<IActionResult> sendAnno(string message)
        {
            await csgo.core.ChatHub.Current.Clients.All.SendAsync("notify", 0, message);
            return Ok();
        }
        public IActionResult Disputes( )
        {


            return View( );
        }
        [HttpGet]
        public async Task<JsonResult> getUserInfo(int id)
        {
         

            if (tokenAccess.validateToken(Request, tokenType.admin))
            {
                tokenAccess.createToken( Request, tokenType.admin );
                return Json( await usersManager.getUserDatabyId( id ) );
              
           }
          
             return Json( new { error = "You are not authorized." } );
        }
        [HttpGet]
        public async Task<JsonResult> saveUserInfo( int id, string data )
        {
          
            if ( tokenAccess.validateToken( Request, tokenType.admin ) )
            {
                dynamic parsed = JValue.Parse(data);

                await databaseManager.updateQuery( $"UPDATE users SET seller = '{parsed.seller}', email = '{parsed.email}', boughtAccounts = '{parsed.boughtAccounts}', soldAccounts = '{parsed.soldAccounts}', balance = '{parsed.balance}', banned = '{parsed.banned}', banReason = '{parsed.banReason}' WHERE id = '{id}' LIMIT 1" ).Execute( );
                if (!(bool)parsed.seller && ((string)parsed.sellerTakeReason).Length > 0)
                {
                    csgo.core.emailManager.sendNoSeller((string)parsed.email, (string)parsed.sellerTakeReason);
                    Console.WriteLine($"Seller removed for {(string)parsed.seller} with email {(string)parsed.email}  for reason  {parsed.sellerTakeReason}");
                }
                accountsManager.lastUpdate = DateTime.Now.AddDays( -1 );
                tokenAccess.createToken( Request, tokenType.admin );
                return Json( new { status = true, message = "Request succesfully sent." } );
            }

            return Json( new { error = "You are not authorized." } );
        }
        [HttpGet]
        public async Task<JsonResult> acceptWithdraw( int id )
        {

            if ( tokenAccess.validateToken( Request, tokenType.admin ) )
            {
                balanceManager.withdrawRequest withdrawRequest = await balanceManager.getWithdrawRequest(id);

                if (withdrawRequest == null)
                    return Json( new { status = true, message = "Request doesn't exist." } );
                if (withdrawRequest.accepted)
                    return Json( new { status = true, message = "This request is already accepted." } );


                await csgo.core.notifyManager.sendNotification( withdrawRequest.userId, "Your withdraw request was accepted. Check out your PayPal." );
                await databaseManager.updateQuery( $"UPDATE withdrawRequests SET accepted = True WHERE id = @id LIMIT 1" ).addValue("@id", id).Execute( );
                await databaseManager.updateQuery( $"UPDATE users SET holdBalance = 0 WHERE id = @id LIMIT 1" ).addValue( "@id", withdrawRequest.userId ).addValue("@money", withdrawRequest.amount).Execute( );

                await databaseManager.updateQuery( $"UPDATE transactions SET status = '{(int)core.logsManager.transactions.transactionStatus.complete}' WHERE type = '{(int)core.logsManager.transactions.transactionType.withdraw}' AND userId = @userId" ).addValue( "@userId", withdrawRequest.userId ).Execute( );
                  
                accountsManager.lastUpdate = DateTime.Now.AddDays( -1 );
                tokenAccess.createToken( Request, tokenType.admin );
                return Json( new { success = true, message = "Your  request was succesfully sent." } );
            }

            return Json( new { success = false, message = "Not authorized." } );
        }
        [HttpGet]
        public async Task<JsonResult> getWithdrawData( int id )
        {

            if ( tokenAccess.validateToken( Request, tokenType.admin ) )
            {
                balanceManager.withdrawRequest withdrawRequest = await balanceManager.getWithdrawRequest(id);

                if ( withdrawRequest == null )
                    return Json( new { status = true, message = "Request doesn't exist." } );

                tokenAccess.createToken( Request, tokenType.admin );
                return Json( new { status = true, data = withdrawRequest } );
            }

            return Json( new { error = "You are not authorized." } );
        }
        [HttpGet]
        public async Task<JsonResult> manageSellerRequest(int id, bool status, string phoneNumber)
        {
            if (tokenAccess.validateToken(Request, tokenType.admin))
            {
                if (status)
                {

                    var user = csgo.usersManager.users.Find(a => a.id == id);
                    if (user != null)
                    {
                        user.seller = true;
                        user.sellerDate = DateTime.Now;
                    }
                    await databaseManager.updateQuery($"UPDATE users SET seller = True, phoneNumber = @phoneNumber,  sellerDate = CURRENT_TIMESTAMP WHERE id = @id LIMIT 1").addValue("@id", id).addValue("@phoneNumber", phoneNumber).Execute();
                    await csgo.core.notifyManager.sendNotification(id, $"Your seller request was accepted. You cant start selling on this site.");
                    if (user != null)
                        await user.sendNotify(notifyManager.notifyType.success, $"Your seller request was accepted. You cant start selling on this site.");
                }
                else
                {
                    await csgo.core.notifyManager.sendNotification(id, $"Your seller request was rejected. You can try again.");
                    var user = csgo.usersManager.users.Find(a => a.id == id);
                    if (user != null)
                        await user.sendNotify(notifyManager.notifyType.success, $"Your seller request was rejected.");
                }

                await databaseManager.updateQuery($"DELETE FROM sellerrequests WHERE userId = @userId LIMIT 1").addValue("@userId", id).Execute();
                return Json(new { status = true, message = "Request succesfully sent." });
            }
            return Json(new { status = false, message = "You are not authorized." });
        }
        public class DataTableAjaxPostModel
        {
            // properties are not capital due to json mapping
            public int draw { get; set; }
            public int start { get; set; }
            public int length { get; set; }
            public List<Column> columns { get; set; }
            public Search search { get; set; }
            public List<Order> order { get; set; }
        }

        public class Column
        {
            public string data { get; set; }
            public string name { get; set; }
            public bool searchable { get; set; }
            public bool orderable { get; set; }
            public Search search { get; set; }
        }

        public class Search
        {
            public string value { get; set; }
            public string regex { get; set; }
        }

        public class Order
        {
            public int column { get; set; }
            public string dir { get; set; }
        }
        [HttpGet]
        public async Task<JsonResult> revokeAccount( int id, string reason )
        {
            var account =  csgo.accountsManager.csgoAccounts.Find( b => b.id == id );
            if ( account != null )
            {


                
                await accountsManager.addRevokedAccount(account, reason);
                await databaseManager.updateQuery( $"DELETE FROM csgoaccounts WHERE id = @id LIMIT 1" ).addValue( "@id", id ).Execute( );
                await csgo.core.notifyManager.sendNotification( account.sellerid, $"Your account with username {account.username} was revoked reason: {reason}" );
                csgo.accountsManager.csgoAccounts.Remove( csgo.accountsManager.csgoAccounts.Find( b => b.id == id ) );
            }
            else
            {
                return Json( new { status = true, message = "Account doesnt exist." } );
            }
            return Json( new { status = true, message = "Account was succesfully deleted." } );
        }
        [HttpGet]
        public async Task<JsonResult> saveAccountInfo( int id, string data)
        {
            

            dynamic parsed = JValue.Parse(data);

            await databaseManager.updateQuery( $"UPDATE csgoaccounts SET password = @password, rank =  @rank, wins = @wins, prime = @prime, hours = @hours, email = @email, emailPassword = @emailPassword, status = @status, description = @description, image = @image, price = @price, title = @title, gameList = @gameList, type = @type  WHERE id = @id LIMIT 1" )
                .addValue("@password", (string)parsed.password).addValue("@rank", (int)parsed.rank).addValue("@wins", (int)parsed.wins).addValue("@prime", (bool)parsed.prime).addValue("@hours", (int)parsed.hours).addValue("@email", (string)parsed.email).addValue("@emailPassword", (string)parsed.emailPassword).addValue("@status", (int)parsed.status).addValue("@description", (string)parsed.description).addValue("@image", (string)parsed.imageLink).addValue("@price", (decimal)parsed.price).addValue("@id", id)
                .addValue("@title", (string)parsed.title)
                .addValue("@gameList", (string)parsed.gameList)
                .addValue("@type", (int)parsed.type)
                .Execute( );
  



            var account = csgo.accountsManager.csgoAccounts.Find(b=> b.id == id );
          if (account != null)
            {
                // .addValue("@password", (string)parsed.password).addValue("@rank", (int)parsed.rank).addValue("@wins", (int)parsed.wins).addValue("@prime", (bool)parsed.prime).addValue("@hours", (int)parsed.hours).addValue("@email", (string)parsed.email).addValue("@emailPassword", (string)parsed.emailPassword).addValue("@status", (int)parsed.status).addValue("@description", (string)parsed.description).addValue("@image", (string)parsed.imageLink).addValue("@id", id)
                account.password = (string)parsed.password;
                account.wins = (int)parsed.wins;
                account.prime = (bool)parsed.prime;
                account.hours = (int)parsed.hours;
                account.email = (string)parsed.email;
                account.status = (status)(int)parsed.status;
                account.description = (string)parsed.description;
                account.price = (decimal)parsed.price;
                account.image = (string)parsed.imageLink;
                account.type = (type)(int)parsed.type;
                account.gameList = (string)parsed.gameList;
                account.title = (string)parsed.title;
                
                //sdadsdd112315fedasffgqagfdgfsddfgsbcvxj
            }
            var sellerUser = csgo.usersManager.users.Find(a=> a.id ==account.sellerid );
            if ( sellerUser != null && parsed.status == 2 )
              await  sellerUser.sendNotify( notifyManager.notifyType.success, $"Account with username {account.username} was accepted." );
            accountsManager.lastUpdate = DateTime.Now.AddHours(-1);
            return Json( new { status = true, message = "Request succesfully sent." } );
        }
        [HttpGet]
        public async Task<JsonResult> test( int id )
        {
            var user = csgo.usersManager.users.Find(a=> a.username == "mDenis16");

            await csgo.core.notifyManager.sendNotify( user, core.notifyManager.notifyType.success, "a mers" );
            return Json( new { status = true, message = "Request succesfully sent." } );
        }

         [HttpGet]
         public string generateEmail()
         {
            return csgo.accountsManager.tempMailGenerate();
         }
    
        [HttpGet]
        public async Task<JsonResult> getAccountInfo( int id )
        {
          
            if ( tokenAccess.validateToken( Request, tokenType.admin ) )
            {
                var account = await accountsManager.getAccountById(id);
                TempData["tempMailChange"] = true;
                TempData["tempMail"] = account.email;
                TempData["admin"] = true;
                tokenAccess.createToken( Request, tokenType.admin );
                return Json(account);

            }

            return Json( new { error = "You are not authorized." } );
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
