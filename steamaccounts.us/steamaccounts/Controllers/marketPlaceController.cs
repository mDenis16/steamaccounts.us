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
using System.Net;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Serialization;
using System.Web;
using RestSharp;
using Microsoft.AspNetCore.Identity;

namespace csgo.Controllers
{

    public class marketPlaceController : Controller
    {
        public class addAccountApiPostModel
        {
            public string email { get; set; }
            public int hours { get; set; }
            public string imageLink { get; set; }
            public string password { get; set; }
            public bool prime { get; set; }
            public string rank { get; set; }
            public string emailPassword { get; set; }
            public string username { get; set; }
            public int wins { get; set; }
            public decimal price { get; set; }
            public string description { get; set; }
            public int type { get; set; }
            public string gameList { get; set; }
            public string title { get; set; }
        }

        public interface IPagedList<T> : IList<T>
        {
            int PageCount { get; }
            int TotalItemCount { get; }
            int PageIndex { get; }
            int PageNumber { get; }
            int PageSize { get; }
            bool HasPreviousPage { get; }
            bool HasNextPage { get; }
            bool IsFirstPage { get; }
            bool IsLastPage { get; }
        }

        private readonly ILogger<marketPlaceController> _logger;
        public static List<accountClient> sellersAccounts = new List<accountClient>();

        private static SteamClient _steamClient;
        private static CallbackManager _manager;

        private static SteamUser _steamUser;

        private static bool _isRunning;
        public static string email = "";
        private static string _authCode, _twoFactorAuth;
        static string user = "";
        static string pass = "";
        static int curUserId = -1;
        static bool steamGuarded = false;
        static addAccountApiPostModel curAccount = null;
        static object resultAccount = null;
        public static bool _steamGuarded = false;
        public static bool _checkTemp = false;
        public class accountClient
        {
            public SteamClient _steamClient { get; set; }
            public CallbackManager _manager { get; set; }

            public SteamUser _steamUser { get; set; }

            public bool _isRunning { get; set; }

            public string _authCode { get; set; }
            public string _twoFactorAuth { get; set; }

            public Thread processThread { get; set; }
        }
        public marketPlaceController( ILogger<marketPlaceController> logger )
        {
            _logger = logger;
        }



        string GetPayPalResponse( bool useSandbox )
        {
            string responseState = "INVALID";
            // Parse the variables
            // Choose whether to use sandbox or live environment
            string paypalUrl = useSandbox ? "https://www.sandbox.paypal.com/"
            : "https://www.paypal.com/";

            using ( var client = new HttpClient( ) )
            {
                client.BaseAddress = new Uri( paypalUrl );
                client.DefaultRequestHeaders.Accept.Clear( );
                client.DefaultRequestHeaders.Accept.Add( new MediaTypeWithQualityHeaderValue( "application/x-www-form-urlencoded" ) );

                //STEP 2 in the paypal protocol
                //Send HTTP CODE 200
                HttpResponseMessage response = client.PostAsync("cgi-bin/webscr", new StringContent(string.Empty)).Result;

                if ( response.IsSuccessStatusCode )
                {
                    //STEP 3
                    //Send the paypal request back with _notify-validate
                    string rawRequest = response.Content.ReadAsStringAsync().Result;
                    rawRequest += "&cmd=_notify-validate";

                    HttpContent content = new StringContent(rawRequest);

                    response = client.PostAsync( "cgi-bin/webscr", content ).Result;

                    if ( response.IsSuccessStatusCode )
                    {
                        responseState = response.Content.ReadAsStringAsync( ).Result;
                    }
                }
            }

            return responseState;
        }

        public IActionResult csgoMarket( int pag, string search, string rank )
        {
            if ( csgo.core.requestsHelper.processRequest( Request ) )
                return Redirect( Url.Action( "warning", "home" ) );
            ViewBag.pag = pag;
            ViewBag.search = search;
            ViewBag.rank = rank;
            adminController.tokenAccess.createToken( Request, adminController.tokenType.buyaccount, null, 0 );
            return View( );
        }

        public IActionResult steamMarket( int pag, string search )
        {
            if ( csgo.core.requestsHelper.processRequest( Request ) )
                return Redirect( Url.Action( "warning", "home" ) );
            ViewBag.pag = pag;
            ViewBag.search = search;

            adminController.tokenAccess.createToken( Request, adminController.tokenType.buyaccount, null, 0 );
            return View( );
        }
        public static bool ValidateAntiXSS( string inputParameter )
        {
            if ( string.IsNullOrEmpty( inputParameter ) )
                return true;

            // Following regex convers all the js events and html tags mentioned in followng links.
            //https://www.owasp.org/index.php/XSS_Filter_Evasion_Cheat_Sheet                 
            //https://msdn.microsoft.com/en-us/library/ff649310.aspx

            var pattren = new StringBuilder();

            //Checks any js events i.e. onKeyUp(), onBlur(), alerts and custom js functions etc.             
            pattren.Append( @"((alert|on\w+|function\s+\w+)\s*\(\s*(['+\d\w](,?\s*['+\d\w]*)*)*\s*\))" );

            //Checks any html tags i.e. <script, <embed, <object etc.
            pattren.Append( @"|(<(script|iframe|embed|frame|frameset|object|img|applet|body|html|style|layer|link|ilayer|meta|bgsound|p|h))" );

            return !Regex.IsMatch( System.Web.HttpUtility.UrlDecode( inputParameter ), pattren.ToString( ), RegexOptions.IgnoreCase | RegexOptions.Compiled );
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> requestSellerPost( csgo.postModels.sellerRequest obj )
        {
            if ( csgo.Controllers.adminController.tokenAccess.validateToken( Request, adminController.tokenType.requestseller ) )
            {
                if ( csgo.core.requestsHelper.processRequest( Request ) )
                    return Json( new { success = "false", message = "You are sending to many requests. Blacklist will expire in 30 seconds." } );

                if ( Request.Cookies[ "sessionid" ] == null )
                    TempData[ "toast" ] = "{type:'error',message:'You are not logged in.'}";

                if ( obj.description.Length > 700 )
                {
                    TempData[ "toast" ] = "{type:'error',message:'Your description is too long.'}";
                    return this.Redirect( @Url.Action( "requestSeller" ) );
                }
                if ( obj.question.Length > 1000 )
                {
                    TempData[ "toast" ] = "{type:'error',message:'Your question response is too long.'}";
                    return this.Redirect( @Url.Action( "requestSeller" ) );
                }
                if ( !ValidateAntiXSS( obj.question ) )
                {
                    TempData[ "toast" ] = "{type:'error',message:'Your response contains malicious code.'}";
                    return this.Redirect( @Url.Action( "requestSeller" ) );
                }
                if ( !ValidateAntiXSS( obj.description ) )
                {
                    TempData[ "toast" ] = "{type:'error',message:'Your description contains malicious code.'}";
                    return this.Redirect( @Url.Action( "requestSeller" ) );
                }
                if ( !ValidateAntiXSS( obj.age ) )
                {
                    TempData[ "toast" ] = "{type:'error',message:'Your response contains malicious code.'}";
                    return this.Redirect( @Url.Action( "requestSeller" ) );
                }
                if ( !ValidateAntiXSS( obj.nationality ) )
                {
                    TempData[ "toast" ] = "{type:'error',message:'Your response contains malicious code.'}";
                    return this.Redirect( @Url.Action( "requestSeller" ) );
                }
                var userData = await usersManager.getUserData(Request);

                if ( userData == null )
                {
                    TempData[ "toast" ] = "{type:'error',message:'You session expired or you are logged from another location.'}";
                    return this.Redirect( @Url.Action( "index", "home" ) );
                }
                if ( await csgo.usersManager.existSellerRequest( userData.id ) )
                {
                    TempData[ "toast" ] = "{type:'error',message:'You already have a request in pending.'}";
                    return this.Redirect( @Url.Action( "index", "home" ) );
                }
                await databaseManager.updateQuery( $"INSERT INTO sellerrequests (username, userId, age,nationality, description,question, phoneNumber) VALUES (@username, @userId, @age, @nationality, @description, @question, @phoneNumber)" ).addValue( "@username", userData.username ).addValue( "@userId", userData.id ).addValue( "@age", obj.age ).addValue( "@nationality", obj.nationality ).addValue( "@description", obj.description ).addValue( "@question", obj.question ).addValue( "@phoneNumber", obj.phoneNumber ).Execute( );
                TempData[ "toast" ] = "{type:'success',message:'Your request was succesfully send. This process can take from 24 hours to 3 days.'}";
                return this.Redirect( @Url.Action( "index", "home" ) );


            }

            return this.Redirect( @Url.Action( "index", "home" ) );
        }
        [HttpGet]
        public async Task<IActionResult> requestEmailService( string username, string password )
        {
            if ( csgo.core.requestsHelper.processRequest( Request ) )
                return Json( new { success = "false", message = "You are sending to many requests. Blacklist will expire in 30 seconds." } );
            if ( adminController.tokenAccess.validateToken( Request, adminController.tokenType.generateemail ) )
            {


                //    try
                {


                    if ( Request.Cookies[ "sessionid" ] == null )
                        return Json( new { success = false, message = "You are not logged in." } );

                    var userData = await usersManager.getUserData(Request);

                    if ( userData == null )
                        return Json( new { success = false, message = "You session expired or you are logged from another location." } );
                    DateTime lastEmailGenerate = DateTime.Now.AddHours(-24);
                    if ( TempData[ "lastEmailGenerate" ] != null )
                        lastEmailGenerate = ( DateTime ) TempData[ "lastEmailGenerate" ];

                    if ( ( int ) ( DateTime.Now - lastEmailGenerate ).TotalMinutes < 3 )
                    {
                        adminController.tokenAccess.createToken( Request, adminController.tokenType.generateemail );
                        return Json( new { success = false, message = "You can generate an email once in 3 minutes." } );

                    }
                    TempData[ "lastEmailGenerate" ] = DateTime.Now;
                    if ( TempData[ "addAccount" ] != null )
                    {
                        if ( _isRunning )
                        {
                            adminController.tokenAccess.createToken( Request, adminController.tokenType.generateemail );
                            adminController.tokenAccess.createToken( Request, adminController.tokenType.generateemail, null, 3 );
                            return Json( new { success = false, message = "Another user is already running it." } );
                        }
                        if ( username == null || password == null )
                        {
                            adminController.tokenAccess.createToken( Request, adminController.tokenType.generateemail );
                            return Json( new { success = false, message = "Invalid steam account." } );
                        }
                        if ( username.Length < 3 )
                        {
                            adminController.tokenAccess.createToken( Request, adminController.tokenType.generateemail );

                            return Json( new { success = false, message = "Invalid steam account." } );
                        }
                        if ( password.Length < 3 )
                        {

                            adminController.tokenAccess.createToken( Request, adminController.tokenType.generateemail );
                            return Json( new { success = false, message = "Invalid steam account." } );
                        }
                        curAccount = new addAccountApiPostModel( );
                        curAccount.username = username;
                        curAccount.password = password;
                        if ( await csgo.accountsManager.existAccount( username ) )
                        {
                            adminController.tokenAccess.createToken( Request, adminController.tokenType.generateemail );
                            return Json( new { success = false, message = "This steam account is already added." } );
                        }



                        _steamClient = new SteamClient( );
                        // create the callback manager which will route callbacks to function calls
                        _manager = new CallbackManager( _steamClient );

                        // get the steamuser handler, which is used for logging on after successfully connecting
                        _steamUser = _steamClient.GetHandler<SteamUser>( );
                        _steamClient.GetHandler<SteamGameCoordinator>( );

                        // register a few callbacks we're interested in
                        // these are registered upon creation to a callback manager, which will then route the callbacks
                        // to the functions specified
                        _manager.Subscribe<SteamClient.ConnectedCallback>( OnConnected );
                        _manager.Subscribe<SteamClient.DisconnectedCallback>( OnDisconnected );

                        _manager.Subscribe<SteamUser.LoggedOnCallback>( OnLoggedOn );
                        _manager.Subscribe<SteamUser.LoggedOffCallback>( OnLoggedOff );

                        // this callback is triggered when the steam servers wish for the client to store the sentry file
                        _manager.Subscribe<SteamUser.UpdateMachineAuthCallback>( OnMachineAuth );

                        _isRunning = true;


                        Console.WriteLine( "Connecting to Steam..." );

                        // initiate the connection
                        _steamClient.Connect( );

                        // create our callback handling loop
                        while ( _isRunning )
                        {
                            // in order for the callbacks to get routed, they need to be handled by the manager
                            _manager.RunWaitCallbacks( TimeSpan.FromSeconds( 1 ) );
                        }
                        if ( _steamGuarded )
                        {

                            _steamGuarded = false;
                            return Json( new { success = false, message = "This steam account is steamguard protected." } );
                        }
                    }



                    string email = csgo.core.emailManager.getEmail(userData.username);
                    TempData[ "tempMail" ] = email;
                    TempData[ "tempMailChange" ] = false;
                    return Json( new { success = true, email = email } );
                }


            }
            return Json( new { success = false, message = "You session expired or you are logged from another location." } );
        }
        public static string GetMd5Hash( MD5 md5Hash, string input )
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for ( int i = 0; i < data.Length; i++ )
            {
                sBuilder.Append( data[ i ].ToString( "x2" ) );
            }

            // Return the hexadecimal string.
            return sBuilder.ToString( );
        }
        [HttpGet]
        public async Task<IActionResult> readEmailCodeAsync( )
        {
            var email = TempData["tempMail"];
            if ( email != null )
            {
                //  Console.WriteLine("READING EMAILS" + (String)email);
                dynamic lastEmailCheck = TempData["lastEmailCheck"];
                if ( lastEmailCheck == null )
                    lastEmailCheck = DateTime.Now.AddHours( -24 );
                Console.WriteLine( "LAST CHECK " + ( DateTime.Now - ( DateTime ) lastEmailCheck ).TotalSeconds );


                if ( ( DateTime.Now - ( DateTime ) lastEmailCheck ).TotalSeconds < 40 && TempData[ "admin" ] == null )
                    return Json( new { success = false, message = "You can check email once at 40 seconds." } );
                using ( MD5 md5Hash = MD5.Create( ) )
                {
                    string hash = GetMd5Hash(md5Hash, (string)email);

                    var client = new RestClient("https://privatix-temp-mail-v1.p.rapidapi.com/request/mail/id/" + hash + "/");
                    var request = new RestRequest(Method.GET);
                    request.AddHeader( "x-rapidapi-host", "privatix-temp-mail-v1.p.rapidapi.com" );
                    request.AddHeader( "x-rapidapi-key", "16e3b38e2amsh824c1935741225ep18aa41jsn41d3f2260d26" );
                    IRestResponse a = client.Execute(request);
                    Console.WriteLine( a.Content );
                    Console.WriteLine( email );
                    Console.WriteLine( "hash" );
                    dynamic obj = JValue.Parse(a.Content);
                    try
                    {
                        if ( obj[ "error" ] != null )
                        {
                            TempData[ "tempMail" ] = ( string ) email;
                            TempData[ "lastEmailCheck" ] = DateTime.Now;

                            Console.WriteLine( "ERROR " + obj[ "error" ] );
                            return Json( new { success = false, message = obj[ "error" ] } );
                        }
                    }
                    catch ( System.ArgumentException )
                    {

                        string steam = obj[obj.Count - 1].mail_text;

                        bool change = (bool)TempData["tempMailChange"];
                        /*   steam = steam.Replace("este:", "is:");
                           steam = steam.Replace("Dacă", "If");
                           steam = steam.Replace("Tu", "You");
                           steam = steam.Replace("adresă:", "address:");*/
                        //e-mailului tau\n\nT78BY\n\nPrime?ti
                       await csgo.core.logsManager.utilities.addEmailLog( (string)email, steam );
                        if ( change )
                        {
                            string code = "not yet";
                            try
                            {
                                if ( steam.Contains( "e-mailului tau" ) )
                                {


                                    int start = steam.IndexOf("e-mailului tau");
                                    int stop = steam.IndexOf("Prime?ti");
                                    code = steam.Substring( start + "e-mailului tau".Length, stop - start - "Prime?ti".Length ).Replace( " ", "" );

                                    TempData[ "tempMailChange" ] = true;
                                }
                                else
                                {
                                    int start = steam.IndexOf("is:");
                                    int stop = steam.IndexOf("If");
                                    code = steam.Substring( start + "is:".Length, stop - start - "is:".Length ).Replace( " ", "" );
                                    TempData[ "tempMailChange" ] = true;
                                }
                            }
                            catch ( Exception ex ) {
                                code = steam;
                                TempData[ "tempMail" ] = ( string ) email;
                                TempData[ "lastEmailCheck" ] = DateTime.Now;
                                TempData[ "tempMailChange" ] = true;
                            }
                            return Json( new { success = true, code = code } );
                        }
                        else
                        {
                            string code = "not yet";
                            try
                            {
                                if ( steam.Contains( "e-mailului tau" ) )
                                {


                                    int start = steam.IndexOf("e-mailului tau");
                                    int stop = steam.IndexOf("Prime?ti");
                                    code = steam.Substring( start + "e-mailului tau".Length, stop - start - "Prime?ti".Length ).Replace( " ", "" );

                                }
                                else
                                {
                                    int start = steam.IndexOf("address:");
                                    int stop = steam.IndexOf("You");
                                    code = steam.Substring( start + "address:".Length, stop - start - "address:".Length ).Replace( " ", "" );
                                }
                            }
                            catch
                            {
                                code = steam;
                            }
                            TempData[ "tempMail" ] = ( string ) email;
                            TempData[ "lastEmailCheck" ] = DateTime.Now;
                            TempData[ "tempMailChange" ] = false;
                            return Json( new { success = true, code = code } );
                        }


                        return Json( new { success = false, message = "failed" } );
                    }
                }


            }
            return Json( new { success = false, mesage = "You are not authorized." } );
        }
        public IActionResult addAccount( )
        {
            if ( csgo.core.requestsHelper.processRequest( Request ) )
                return Redirect( Url.Action( "warning", "home" ) );

            TempData[ "addAccount" ] = true;
            adminController.tokenAccess.createToken( Request, adminController.tokenType.addaccount, null, 3 );
            return View( );
        }
        [HttpGet]
        public async Task<JsonResult> sendRate( string data )
        {
            if ( csgo.core.requestsHelper.processRequest( Request ) )
                return Json( new { success = "false", message = "You are sending to many requests. Blacklist will expire in 30 seconds." } );
            if ( adminController.tokenAccess.validateToken( Request, adminController.tokenType.rate ) )
            {

                try
                {
                    dynamic parsed = JValue.Parse(data);

                    if ( Request.Cookies[ "sessionid" ] == null )
                        return Json( new { success = false, message = "You are not logged in." } );

                    var userData = await usersManager.getUserData(Request);

                    if ( userData == null )
                        return Json( new { success = false, message = "You session expired or you are logged from another location." } );

                    var account = csgo.accountsManager.csgoAccounts.Find(a=> a.id == (int)parsed.csgoId);
                    if ( account == null )
                        return Json( new { success = false, message = "Account doesn't exist." } );

                    if ( ( ( string ) parsed.message ).Length > 51 )
                        return Json( new { success = false, message = "Your message is too long." } );
                    if ( await csgo.core.ratesManager.existRate( userData.id, account.id ) )
                        return Json( new { success = false, message = "You already added a rate to this." } );

                    await csgo.core.ratesManager.addRate( account.sellerid, userData.id, ( bool ) parsed.rate, ( string ) parsed.message, account.id, userData.username, account.seller );

                    return Json( new { success = false, message = "Your rate was succesfully sent." } );
                }
                catch
                {
                    return Json( new { success = false, message = "Your request is invalid." } );
                }


            }
            adminController.tokenAccess.createToken( Request, adminController.tokenType.rate );
            return Json( new { success = false, message = "Your are not authorized." } );
        }


        [HttpGet]
        public async Task<JsonResult> getAccountInfo( int id )
        {
       //     if ( csgo.Controllers.adminController.tokenAccess.validateToken( HttpContext.Request, csgo.Controllers.adminController.tokenType.accountdata ) )
            {
                var account = csgo.accountsManager.csgoAccounts.Find(a=> a.id == id);
                if ( account == null )
                    return Json( new { success = false, message = "Account doesn't exist." } );
                try
                {

                    using ( var client = new HttpClient( ) )
                    {
                        var content = await client.GetStringAsync("https://api.steampowered.com/ISteamUser/GetPlayerBans/v1/?key=12A1D1DE83F9932934EDD6DF2BA00463&steamids=" + account.steamId64);
                        return Json( content );
                    }
                }
                catch
                {
                    return Json( new { success = false, message = "An error occured." } );
                }
            }
            return Json( new { success = false, message = "Your are not authorized." } );
        }
    
        [HttpGet]
        public async Task<JsonResult> buyAccount( int id )
        {
            if ( adminController.tokenAccess.validateToken( Request, adminController.tokenType.buyaccount ) )
            {
                adminController.tokenAccess.createToken( Request, adminController.tokenType.buyaccount, null, 3 );

                if ( Request.Cookies[ "sessionid" ] == null )
                    return Json( new { success = false, message = "You are not logged in." } );

                var userData = await usersManager.getUserData(Request);
               
                if ( userData == null )
                    return Json( new { success = false, message = "You session expired or you are logged from another location." } );

                var account = csgo.accountsManager.csgoAccounts.Find(a=> a.id == id);
                if ( account == null )
                    return Json( new { success = false, message = "Account doesn't exist." } );
                if (account.sellerid == userData.id)
                    return Json(new { success = false, message = "You can`t buy your account." });
                if ( account.status == status.pending )
                    return Json( new { success = false, message = "Account is not confirmed yet ." } );
                if ( account.status == status.sold )
                    return Json( new { success = false, message = "Account is already sold." } );
                if ( userData.balance < account.price )
                    return Json( new { success = false, message = "You don't have enough money." } );
              
                account.status = status.sold;
                decimal fee = 9.00m;
     
                if ( account.price > 15.00m )
                    fee = 10.00m;

                if ( account.price > 30.00m )
                    fee = 11.00m;

                if ( account.price > 45.00m )
                    fee = 13.00m;

                var moneyReceive = account.price - (fee / 100m * account.price);
             
                if ( await csgo.core.balanceManager.takeUserBalance( userData.id, account.price ) )
                 {
              //       await csgo.core.balanceManager.addUserBalance( account.sellerid, moneyReceive );
                     account.buyerid = userData.id;
                     account.buyer = userData.username;
                    var sellerData = await csgo.usersManager.getUserDatabyId(account.sellerid, true);
                    var buyerData = await csgo.usersManager.getUserDatabyId(account.buyerid, true);
                    await databaseManager.updateQuery( $"UPDATE csgoaccounts SET status = '{( int ) csgo.status.sold}', buyer = '{account.buyer}', buyerid = '{account.buyerid}' WHERE id = '{id}' LIMIT 1" ).Execute( );
                   
                    accountsManager.lastUpdate = DateTime.Now.AddDays( -1 );
                    int buyerTransactionId = await csgo.core.logsManager.transactions.addTransactionLog( account.buyerid, account.buyer, csgo.core.logsManager.transactions.transactionType.buy, csgo.core.logsManager.transactions.transactionStatus.complete, csgo.core.logsManager.transactions.methodType.balance, sellerData.email, account.price  );
                    int sellerTransactionId = await csgo.core.logsManager.transactions.addTransactionLog( account.sellerid, account.seller, csgo.core.logsManager.transactions.transactionType.sell, csgo.core.logsManager.transactions.transactionStatus.inpending, csgo.core.logsManager.transactions.methodType.balance, buyerData.email, moneyReceive  );
                    await csgo.core.logsManager.accounts.addLog( account.id, account.price, account.seller, account.buyer, account.sellerid, account.buyerid, new List<string> { buyerTransactionId.ToString(), sellerTransactionId.ToString() } );
                    await databaseManager.updateQuery( $"UPDATE users SET boughtAccounts = boughtAccounts + 1 WHERE id = '{account.buyerid}' LIMIT 1" ).Execute( );
                    await databaseManager.updateQuery( $"UPDATE users SET soldAccounts = soldAccounts + 1 WHERE id = '{account.sellerid}' LIMIT 1" ).Execute( );

                   
                    var sellerUser = csgo.usersManager.users.Find(a=> a.id == sellerData.id);
              
                   
                 
                    try
                    {
                        if (sellerUser != null)
                        {
                            await csgo.core.notifyManager.sendNotify(sellerUser, core.notifyManager.notifyType.success, $"One of your account was sold. Check your profile.");
                            await csgo.core.notifyManager.sendNotification(sellerUser.id, $"Account with id {account.id} was sold for {moneyReceive}.");
                            
                        }
                    }
                    catch
                    {

                    }
                    return Json( new { success = true, message = "You succesfully bought the account. You can see details on your profile." } );
                 }

            }
               
            
            return Json( new { success = false, message = "Your request is invalid." } );
        }
        [HttpGet]
        public async Task<JsonResult> viewAccount( int id )
        {
            try
            {
                if ( csgo.core.requestsHelper.processRequest( Request ) )    
                   return Json( new { success = "false", message = "You are sending to many requests. Timeout will expire in 5 seconds." } );

                if ( adminController.tokenAccess.validateToken( Request, adminController.tokenType.viewaccount ) )
                {


                    if ( Request.Cookies[ "sessionid" ] == null )
                        return Json( new { success = false, message = "You are not logged in." } );

                    var userData = await usersManager.getUserData(Request);

                    if ( userData == null )
                        return Json( new { success = false, message = "You session expired or you are logged from another location." } );

                    var account = csgo.accountsManager.csgoAccounts.Find(a=> a.id == id && a.buyerid ==userData.id);
                    if ( account == null )
                        return Json( new { success = false, message = "Account doesn't exist." } );

                    adminController.tokenAccess.createToken( Request, adminController.tokenType.rate );
                    adminController.tokenAccess.createToken( Request, adminController.tokenType.viewaccount );
                    TempData["tempMail"] = account.email;
                    TempData["tempMailChange"] = true;
                    return Json( new { success = true, message = "Access guaranted." , response = account } );

                }
            }
            catch
            {
                return Json( new { success = false, message = "Please try again later." } );
            }


            return Json( new { success = false, message = "Your request is invalid." } );
        }
         [HttpGet]
        public async Task<JsonResult> viewEditAccount( int id )
        {
            try
            {
                if ( csgo.core.requestsHelper.processRequest( Request ) )    
                   return Json( new { success = "false", message = "You are sending to many requests. Timeout will expire in 5 seconds." } );

                if ( adminController.tokenAccess.validateToken( Request, adminController.tokenType.viewaccount ) )
                {


                    if ( Request.Cookies[ "sessionid" ] == null )
                        return Json( new { success = false, message = "You are not logged in." } );

                    var userData = await usersManager.getUserData(Request);

                    if ( userData == null )
                        return Json( new { success = false, message = "You session expired or you are logged from another location." } );

                    var account = csgo.accountsManager.csgoAccounts.Find(a=> a.id == id && a.buyerid ==userData.id);
                    if ( account == null )
                        return Json( new { success = false, message = "Account doesn't exist." } );
                    string password = "protected";
                   if (account.status == csgo.status.pending )
                      password = account.password ;
                                 

                    adminController.tokenAccess.createToken( Request, adminController.tokenType.viewaccount );
                    TempData["tempMail"] = account.email;
                    TempData["tempMailChange"] = true;
                    return Json(new { success = false, message = "Access authorized.", response = new {username =  account.username, password = password, hours = account.hours, gameList= account.gameList, wins = account.wins, status = account.status, title = account.title} });
                }
            }
            catch
            {
                return Json( new { success = false, message = "Please try again later." } );
            }


            return Json( new { success = false, message = "Your request is invalid." } );
        }
        [HttpPost]

        public async Task<JsonResult> addAccountApi(string receive)
        {
            if ( csgo.core.requestsHelper.processRequest( Request ) )
                return Json( new { success = "false", message = "You are sending to many requests. Timeout will expire in 15 seconds." } );

        //   if (!adminController.tokenAccess.validateToken( Request, adminController.tokenType.addaccount ) )
          //     return Json( new { success = false, message = "Your request is invalid." } );

            try
            {
                if (Request.Cookies["sessionid"] == null)
                    return Json(new { success = false, message = "You are not logged in." });
              
              var userData = await usersManager.getUserData(Request);
                if (userData == null)
                    return Json(new { success = false, message = "You session expired or you are logged from another location." });

               // if (!ValidateAntiXSS(receive))
                 //   return Json(new { success = false, message = "You request contains malicious code." });

                curAccount = Newtonsoft.Json.JsonConvert.DeserializeObject<addAccountApiPostModel>(receive);

                if (_isRunning)
                {
                    adminController.tokenAccess.createToken(Request, adminController.tokenType.addaccount, null, 3);
                    return Json(new { success = false, message = "Another user is already running it." });
                }


                if ( await csgo.accountsManager.existAccount( curAccount.username ) )
                {
                    adminController.tokenAccess.createToken( Request, adminController.tokenType.addaccount, null, 3 );
                    return Json( new { success = false, message = "This steam account is already added." } );
                }
                adminController.tokenAccess.createToken(Request, adminController.tokenType.addaccount, null, 3);

                curUserId = userData.id;
          
            _steamClient = new SteamClient();
            // create the callback manager which will route callbacks to function calls
            _manager = new CallbackManager(_steamClient);

            // get the steamuser handler, which is used for logging on after successfully connecting
            _steamUser = _steamClient.GetHandler<SteamUser>();
            _steamClient.GetHandler<SteamGameCoordinator>();

            // register a few callbacks we're interested in
            // these are registered upon creation to a callback manager, which will then route the callbacks
            // to the functions specified
            _manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            _manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

            _manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            _manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);

            // this callback is triggered when the steam servers wish for the client to store the sentry file
            _manager.Subscribe<SteamUser.UpdateMachineAuthCallback>(OnMachineAuth);

            _isRunning = true;


            Console.WriteLine("Connecting to Steam...");

            // initiate the connection
            _steamClient.Connect();
             
            // create our callback handling loop
            while (_isRunning)
            {
                // in order for the callbacks to get routed, they need to be handled by the manager
                _manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
           
            return Json(resultAccount);
        }
            catch
            {
                return Json(new { success = false, message = "Your request is invalid" });
            }

        }

        public IActionResult Privacy()
        {
            return View();
        }
        [Route("sellerRequest")]
        public IActionResult sellerRequest()
        {
            return View();
        }
        private string GetHtmlPage( string URL )
        {
            String strResult;
            WebResponse objResponse;
            WebRequest objRequest = HttpWebRequest.Create(URL);
            objResponse = objRequest.GetResponse( );
            using ( StreamReader sr = new StreamReader( objResponse.GetResponseStream( ) ) )
            {
                strResult = sr.ReadToEnd( );
                sr.Close( );
            }
            return strResult;
        }
        private string ePayOutRequest(string body, string referer )
        {
            HttpWebResponse response;
            string responseText = "";

            if ( Request_paymentbox_e_payouts_com(body, referer, out response ) )
            {
                responseText = ReadResponse( response );
               
                response.Close( );
                
            }
            return responseText;
        }

        private static string ReadResponse( HttpWebResponse response )
        {
            using ( Stream responseStream = response.GetResponseStream( ) )
            {
                Stream streamToRead = responseStream;
                if ( response.ContentEncoding.ToLower( ).Contains( "gzip" ) )
                {
                    streamToRead = new GZipStream( streamToRead, CompressionMode.Decompress );
                }
                else if ( response.ContentEncoding.ToLower( ).Contains( "deflate" ) )
                {
                    streamToRead = new DeflateStream( streamToRead, CompressionMode.Decompress );
                }

                using ( StreamReader streamReader = new StreamReader( streamToRead, Encoding.UTF8 ) )
                {
                    return streamReader.ReadToEnd( );
                }
            }
        }

        private bool Request_paymentbox_e_payouts_com( string body, string referer, out HttpWebResponse response )
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://paymentbox.e-payouts.com/inc/form.inc.php");

                request.KeepAlive = true;
                request.Accept = "application/json, text/javascript, */*; q=0.01";
                request.Headers.Add( "X-Requested-With", @"XMLHttpRequest" );
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4044.138 Safari/537.36";
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.Headers.Add( "Origin", @"https://paymentbox.e-payouts.com" );
                request.Headers.Add( "Sec-Fetch-Site", @"same-origin" );
                request.Headers.Add( "Sec-Fetch-Mode", @"cors" );
                request.Headers.Add( "Sec-Fetch-Dest", @"empty" );
                request.Referer = referer;
                Console.WriteLine("REFERERERR" + referer);
                request.Headers.Set( HttpRequestHeader.AcceptEncoding, "gzip, deflate, br" );
                request.Headers.Set( HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9,ro;q=0.8,es;q=0.7" );
                request.Headers.Set( HttpRequestHeader.Cookie, @"_ga=GA1.2.599210882.1589203919; _gid=GA1.2.43732562.1589827822; PHPSESSID=0k4353srngsknp8b1i3eo84pp2; _gat_gtag_UA_108855300_2=1" );

                request.Method = "POST";
                request.ServicePoint.Expect100Continue = false;

                byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(body);
                request.ContentLength = postBytes.Length;
                Stream stream = request.GetRequestStream();
                stream.Write( postBytes, 0, postBytes.Length );
                stream.Close( );

                response = ( HttpWebResponse ) request.GetResponse( );
            }
            catch ( WebException e )
            {
                if ( e.Status == WebExceptionStatus.ProtocolError ) response = ( HttpWebResponse ) e.Response;
                else return false;
            }
            catch ( Exception )
            {
                if ( response != null ) response.Close( );
                return false;
            }

            return true;
        }
        [Route( "paySafeProcessor" )]
        public async Task<IActionResult> paySafeProcessor( decimal amount )
        {
            if ( csgo.core.requestsHelper.processRequest( Request ) )
               return Json( new { success = "false", message = "You are sending to many requests. Blacklist will expire in 30 seconds." } );

            if ( adminController.tokenAccess.validateToken( Request, adminController.tokenType.paysafe ) )
            {


                if ( Request.Cookies[ "sessionid" ] == null )
                    return Json( new { success = false, message = "You are not logged in." } );

                var userData = await usersManager.getUserData(Request);

                if ( userData == null )
                    return Json( new { success = false, message = "You session expired or you are logged from another location." } );


                amount = amount + ( 16m / 100m * amount );

                string result =  ePayOutRequest( $"payment-method-type=paysafecard&email={userData.email}&uid=5377&mid=2290&lang=ro&price={amount}&currency=EUR&ucode={userData.id}&country=RO&rec=", $"https://paymentbox.e-payouts.com/?uid=5377&mid=2290&price=" + amount + "&pm=paysafecard&title=steamaccounts.us&name=" + userData.username + "&ucode=" +  userData.id +"&shrink=false" );
                Console.WriteLine( result );
                dynamic ePay = JValue.Parse(result );
                if ( ePay.status != 1 )
                    return Json( new { message = "Paysafecard api failed." } );

                return Redirect( "https://paymentbox.e-payouts.com/" + ePay.data.url );
            }
            return Json( new { success = "false", message = "Your are not authorized." } );
        }
        [Route("fortumoProcessor")]
        public async Task<IActionResult> fortumoProcessor(decimal amount)
        {
            if (csgo.core.requestsHelper.processRequest(Request))
                return Json(new { success = "false", message = "You are sending to many requests. Blacklist will expire in 30 seconds." });

            if (adminController.tokenAccess.validateToken(Request, adminController.tokenType.paysafe))
            {


                if (Request.Cookies["sessionid"] == null)
                    return Json(new { success = false, message = "You are not logged in." });

                var userData = await usersManager.getUserData(Request);

                if (userData == null)
                    return Json(new { success = false, message = "You session expired or you are logged from another location." });


                amount = amount + (16m / 100m * amount);
                Console.WriteLine( amount );
                string result = ePayOutRequest($"payment-method-type=fortumo&email={userData.email}&uid=5377&mid=2290&lang=ro&price={amount}&currency=EUR&ucode={userData.id}&country=RO&rec=", $"https://paymentbox.e-payouts.com/?uid=5377&mid=2290&price=" + amount + "&pm=fortumo&title=steamaccounts.us&name=" + userData.username + "&ucode=" + userData.id + "&shrink=false");
                Console.WriteLine(result);
                dynamic ePay = JValue.Parse(result);
                if (ePay.status != 1)
                    return Json(new { message = "Mobile payment api failed." });

                return Redirect("https://paymentbox.e-payouts.com/" + ePay.data.url);
            }
            return Json(new { success = "false", message = "Your are not authorized." });
        }
        [HttpGet] 
        [Route("account/{id?}")]
        public IActionResult account(int id)
        {
            ViewBag.csgoid = id;
            adminController.tokenAccess.createToken( Request, adminController.tokenType.buyaccount, null, 3 );
            return View("account");
        }
       
        private static void OnConnected(SteamClient.ConnectedCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                Console.WriteLine("Unable to connect to Steam: {0}", callback.Result);
                resultAccount = "We are unable to connect to your account.";
                _isRunning = false;
                return;
            }

            Console.WriteLine("Connected to Steam! Logging in '{0}'...", curAccount.username);

            byte[] sentryHash = null;
            if (System.IO.File.Exists("sentry.bin"))
            {
                // if we have a saved sentry file, read and sha-1 hash it
                byte[] sentryFile = System.IO.File.ReadAllBytes("sentry.bin");
                sentryHash = CryptoHelper.SHAHash(sentryFile);
            }

            _steamUser.LogOn(new SteamUser.LogOnDetails
            {
                Username = curAccount.username,
                Password = curAccount.password,

                // in this sample, we pass in an additional authcode
                // this value will be null (which is the default) for our first logon attempt
                AuthCode = _authCode,

                // if the account is using 2-factor auth, we'll provide the two factor code instead
                // this will also be null on our first logon attempt
                TwoFactorCode = _twoFactorAuth,

                // our subsequent logons use the hash of the sentry file as proof of ownership of the file
                // this will also be null for our first (no authcode) and second (authcode only) logon attempts
                SentryFileHash = sentryHash,
            });
        }

        private static void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            // after recieving an AccountLogonDenied, we'll be disconnected from steam
            // so after we read an authcode from the user, we need to reconnect to begin the logon flow again

            Console.WriteLine("Disconnected from Steam, reconnecting in 5...");
            curAccount = null;
            _isRunning = false;
            curUserId = -1;
        }

        private static void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            bool isSteamGuard = callback.Result == EResult.AccountLogonDenied;
            bool is2Fa = callback.Result == EResult.AccountLoginDeniedNeedTwoFactor;
           
            if (isSteamGuard || is2Fa)
            {
                Console.WriteLine("This account is SteamGuard protected!");
                _steamGuarded = true;
                if (is2Fa)
                {


                    resultAccount = new { success = false, message = "Please disable your 2FA authentificator and Steam Guard." };
                    curAccount = null;
                    _steamClient.Disconnect();
                    _isRunning = false;
                  
                    curUserId = -1;
                    return;
                }
                else
                {
                    resultAccount = new { success = false, message = "Please disable your  email Steam Guard." };
                    
                    curUserId = -1;
                    curAccount = null;
                    _isRunning = false;
                    return;
                }

            }

            if (callback.Result != EResult.OK)
            {
                resultAccount = new { success = false, message = "You entered a wrong password." };
                curAccount = null;
                _steamClient.Disconnect();
                _isRunning = false;
                curUserId = -1;
                return;
            }
            Console.WriteLine("EMAIL STEAM LOGIN " + callback.EmailDomain);
            /* if (callback.EmailDomain != curAccount.email)
             {
                 resultAccount = new { success = false, message = $"Steam account emails isn`s changed to {curAccount.email}" };
                 curAccount = null;
                 _steamClient.Disconnect();
                 _isRunning = false;
                 curUserId = -1;
                 return;
             }*/
            if (!_checkTemp)
            {

                var userDb = csgo.usersManager.getUserDatabyId(curUserId).Result;
                if (userDb.username == null)
                {
                    resultAccount = new { success = false, message = "Something happened wrong." };
                    curAccount = null;
                    _steamClient.Disconnect();
                    _isRunning = false;
                    curUserId = -1;
                    return;
                }
                var account = new csgo.csgoAccount();
                account.date = DateTime.Now;
                account.lastUpdate = DateTime.Now;
                account.password = curAccount.password;
                account.username = curAccount.username;
                account.seller = userDb.username;
                account.price = curAccount.price;
                account.email = curAccount.email;
                account.image = curAccount.imageLink;
                account.wins = curAccount.wins;
                account.hours = curAccount.hours;
                account.prime = curAccount.prime;
                account.rank = csgo.accountsManager.ranks.ToList().FindIndex(a => a == curAccount.rank);
                account.status = csgo.status.pending;
                account.description = curAccount.description;
                account.image = curAccount.imageLink;
                account.emailPassword = curAccount.emailPassword;
                account.sellerid = userDb.id;
                account.negativeRates = userDb.negativeRates;
                account.positiveRates = userDb.positiveRates;
                account.type = (type)(int)curAccount.type;
                account.title = (string)curAccount.title;
                account.gameList = (string)curAccount.gameList;
                account.steamId64 = _steamUser.SteamID.ConvertToUInt64().ToString();
                csgo.accountsManager.addAccount(account);

                resultAccount = new { success = true, message = "Account is in pending. Thank you" };
            }
            _steamGuarded = false;
            _checkTemp = false;
            curAccount = null;
            _steamClient.Disconnect();
            _isRunning = false;
            curUserId = -1;
        }

        private static void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            Console.WriteLine("Logged off of Steam: {0}", callback.Result);
        }

        private static void OnMachineAuth(SteamUser.UpdateMachineAuthCallback callback)
        {
            Console.WriteLine("Updating sentryfile...");

            // write out our sentry file
            // ideally we'd want to write to the filename specified in the callback
            // but then this sample would require more code to find the correct sentry file to read during logon
            // for the sake of simplicity, we'll just use "sentry.bin"

            int fileSize;
            byte[] sentryHash;
            using (var fs = System.IO.File.Open("sentry.bin", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                fs.Seek(callback.Offset, SeekOrigin.Begin);
                fs.Write(callback.Data, 0, callback.BytesToWrite);
                fileSize = (int)fs.Length;

                fs.Seek(0, SeekOrigin.Begin);
                using (var sha = new SHA1CryptoServiceProvider())
                {
                    sentryHash = sha.ComputeHash(fs);
                }
            }

            // inform the steam servers that we're accepting this sentry file
            _steamUser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails
            {
                JobID = callback.JobID,

                FileName = callback.FileName,

                BytesWritten = callback.BytesToWrite,
                FileSize = fileSize,
                Offset = callback.Offset,

                Result = EResult.OK,
                LastError = 0,
              
                OneTimePassword = callback.OneTimePassword,

                SentryFileHash = sentryHash,
            });

        }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
