using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using csgo.core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using WebApplication1.Models;

namespace csgo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
       [Route( "{*url}", Order = 999 )]
        public IActionResult notFound( )
        {
            Response.StatusCode = 404;
           
            return View( );
        }
        public IActionResult Warning( )
        {
         
            return View( );
        }
        public IActionResult Index()
        {
            var s =  TempData[ "loginRequest" ];
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
        private string ePayOutRequest( string body, string referer )
        {
            HttpWebResponse response;
            string responseText = "";

            if ( Request_paymentbox_e_payouts_com( body, referer, out response ) )
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
                Console.WriteLine( "REFERERERR" + referer );
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
        [Route( "donatepaySafeProcessor" )]
        public async Task<IActionResult> donatepaySafeProcessor( decimal amount, string username,  string message )
        {
            if ( csgo.core.requestsHelper.processRequest( Request ) )
                return Json( new { success = "false", message = "You are sending to many requests. Blacklist will expire in 30 seconds." } );

            if (message.Length > 50)
                return Json( new { success = "false", message = "Your message is too long." } );

            //    if ( adminController.tokenAccess.validateToken( Request, adminController.tokenType.paysafe ) )
            {



                string ucode = $"{username}@#{message}";
                string result =  ePayOutRequest( $"payment-method-type=paysafecard&email=sa@s.ro&uid=5377&mid=2382&lang=ro&price={amount}&currency=EUR&ucode={ucode}&country=RO&rec=", $"https://paymentbox.e-payouts.com/?uid=5377&mid=2382&price=" + amount + "&pm=paysafecard&title=raul2k donation&name=" + username + "&ucode=" + ucode +"&shrink=false" );
                Console.WriteLine( result );
                dynamic ePay = JValue.Parse(result );
                if ( ePay.status != 1 )
                    return Json( new { message = "Paysafecard api failed." } );


                return Redirect( "https://paymentbox.e-payouts.com/" + ePay.data.url );
            }
            return Json( new { success = "false", message = "Your are not authorized." } );
        }
        [Route( "donatefortumoProcessor" )]
        public async Task<IActionResult> donatefortumoProcessor( decimal amount, string username, string message )
        {
            if ( csgo.core.requestsHelper.processRequest( Request ) )
                return Json( new { success = "false", message = "You are sending to many requests. Blacklist will expire in 30 seconds." } );

            if ( message.Length > 50 )
                return Json( new { success = "false", message = "Your message is too long." } );

            //    if ( adminController.tokenAccess.validateToken( Request, adminController.tokenType.paysafe ) )
            {



                string ucode = $"{username}@#{message}";
                string result =  ePayOutRequest( $"payment-method-type=fortumo&email=sa@s.ro&uid=5377&mid=2382&lang=ro&price={amount}&currency=EUR&ucode={ucode}&country=RO&rec=", $"https://paymentbox.e-payouts.com/?uid=5377&mid=2382&price=" + amount + "&pm=fortumo&title=raul2k donation&name=" + username + "&ucode=" + ucode +"&shrink=false" );
                Console.WriteLine( result );
                dynamic ePay = JValue.Parse(result );
                if ( ePay.status != 1 )
                    return Json( new { message = "Paysafecard api failed." } );


                return Redirect( "https://paymentbox.e-payouts.com/" + ePay.data.url );
            }
        }
        [HttpGet]
        [Route( "epaynotify" )]
        public async Task<IActionResult> ePayouts( int uid, int mid, string cc, float price, string ucode, string email, string country, string type, string result )
        {
    /*        if ( Request.getIPAddress( ) != "137.74.17.102" )
            {
                Console.WriteLine( $"{Request.getIPAddress( )} tried to access epayouts api." );
                return Json( new { success = false, message = "You are not authorized." } );
            }*/
            if ( result == "ok" )
            {
                string[] args = ucode.Split("#@"); string donator = args[0]; string message = args[1];
                Console.WriteLine( @$"{donator} donated {price} with {message}." );
                csgo.core.ChatHub.donationConnections.ForEach( async a =>
                {
                    await csgo.core.ChatHub.Current.Clients.Client( a ).SendAsync( "receivedDonation", Json( new { name = donator, message = message, amount = price } ) );
                } );
            }
            return Ok( );
        }
        public async Task<JsonResult> simulateDonation(string name, string message, int amount)
        {
            csgo.core.ChatHub.donationConnections.ForEach(async a =>
            {
                await csgo.core.ChatHub.Current.Clients.Client(a).SendAsync("receivedDonation", Json(new { name = name, message  = message, amount = amount }));
            });

            return  Json(new { status = true, message = "Request succesfully sent." });
        }
        public IActionResult raul2knotificari()
        {
        
            return View();
        }
        public IActionResult donate( )
        {

            return View( );
        }
        public IActionResult logout( )
        {
            TempData[ "toast" ] = "{type:'success',message:'You succesfully logged out.'}";
            Response.Cookies.Delete( "sessionid" );
  
            return this.Redirect( Url.Action( "index", "home" ) );
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
