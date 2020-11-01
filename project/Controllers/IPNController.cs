using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using csgo.core;
using Microsoft.AspNetCore.SignalR;

namespace csgo.Controllers
{
    public class IPNController : Controller
    {
        private readonly ILogger<IPNController> _logger;
      
        public IPNController(ILogger<IPNController> logger)
        {
            _logger = logger;
        }
        private class IPNContext
        {
            public HttpRequest IPNRequest { get; set; }

            public string RequestBody { get; set; }

            public string Verification { get; set; } = String.Empty;
            public NameValueCollection Args { get; set; }
        }
        [HttpPost]
        [Route("ipn")]
        public async Task<IActionResult> Receive()
        {
            IPNContext ipnContext = new IPNContext()
            {
                IPNRequest = Request
            };
           
          

            using (StreamReader reader = new StreamReader(ipnContext.IPNRequest.Body, Encoding.ASCII))
            {
                ipnContext.RequestBody = await reader.ReadToEndAsync();
            }
            ipnContext.Args = HttpUtility.ParseQueryString(ipnContext.RequestBody);
            //Store the IPN received from PayPal
            LogRequest(ipnContext);

            //Fire and forget verification task
            await Task.Run(async () => await VerifyTask(ipnContext));

            //Reply back a 200 code
            return Ok();
        }
        [HttpGet]
        [Route( "notifications" )]
        public async Task<IActionResult> ePayouts(int uid, int mid, string cc, float price, string ucode, string email, string country, string type, string result)
        {
            if (Request.getIPAddress() != "137.74.17.102" )
            {
                Console.WriteLine($"{Request.getIPAddress()} tried to access epayouts api.");
                return Json( new { success = false, message = "You are not authorized." } );
            }
            if ( result == "ok" )
            {
                int userId = int.Parse(ucode);
                if (type == "fortumo")
                {
                    var calculatedPrice = ((decimal)price * 100m) / (47m + 100m);
                    Console.WriteLine($"NEW PAYMENT COMPLETED MOBILE USERID {ucode} | AMOUNT : {price} {result} WITH FEES: {calculatedPrice}");
                   
                    await balanceManager.addUserBalance(userId, (decimal) calculatedPrice );
                    await csgo.core.logsManager.transactions.addTransactionLog(userId, await csgo.usersManager.getUsernameById(userId), csgo.core.logsManager.transactions.transactionType.deposit, csgo.core.logsManager.transactions.transactionStatus.complete, csgo.core.logsManager.transactions.methodType.mobile, email, (decimal) calculatedPrice );
                }
                else
                {
                  
                    var calculatedPrice = ((decimal)price * 100m) / (17m + 100m);
                    Console.WriteLine($"NEW PAYMENT COMPLETED PAYASAFECARD USERID {ucode} | AMOUNT : {price} {result} WITHOUT FEE: {calculatedPrice}");
                    await balanceManager.addUserBalance(userId, calculatedPrice);
                    await csgo.core.logsManager.transactions.addTransactionLog(userId, await csgo.usersManager.getUsernameById(userId), csgo.core.logsManager.transactions.transactionType.deposit, csgo.core.logsManager.transactions.transactionStatus.complete, csgo.core.logsManager.transactions.methodType.paysafe, email, calculatedPrice);
                }
        
                   
                var usr =  csgo.usersManager.users.Find(a=> a.id == userId);
                if ( usr != null )
                    await csgo.core.notifyManager.sendNotify( usr, core.notifyManager.notifyType.success, $"Your money have just arrived." );
            }
            return Ok( );
        }
        [HttpGet]
        [Route( "access" )]
        public async Task<IActionResult> ePayoutsAccess( )
        {

            return Ok( );
        }
        [HttpGet]
        [Route( "error" )]
        public async Task<IActionResult> ePayoutsError( )
        {

            return Ok( );
        }
        private async Task VerifyTask(IPNContext ipnContext)
        {
            try
            {
                var verificationRequest = WebRequest.Create("https://ipnpb.paypal.com/cgi-bin/webscr");

                //Set values for the verification request
                verificationRequest.Method = "POST";
                verificationRequest.ContentType = "application/x-www-form-urlencoded";

                //Add cmd=_notify-validate to the payload
                string strRequest = "cmd=_notify-validate&" + ipnContext.RequestBody;
                verificationRequest.ContentLength = strRequest.Length;

                //Attach payload to the verification request
                using (StreamWriter writer = new StreamWriter(verificationRequest.GetRequestStream(), Encoding.ASCII))
                {
                    writer.Write(strRequest);
                }

                //Send the request to PayPal and get the response
                using (StreamReader reader = new StreamReader(verificationRequest.GetResponse().GetResponseStream()))
                {
                    ipnContext.Verification = reader.ReadToEnd();
                }
            }
            catch (Exception exception)
            {
                //Capture exception for manual investigation
            }

           await ProcessVerificationResponse(ipnContext);
        }


        private void LogRequest(IPNContext ipnContext)
        {
            // Persist the request values into a database or temporary data store
        }

        private async Task ProcessVerificationResponse(IPNContext ipnContext)
        {
            Console.WriteLine( ipnContext.Verification );
            Console.WriteLine(ipnContext.RequestBody);
            try
            {
                if (ipnContext.Verification.Equals("VERIFIED"))
                {

                    // check that Payment_status=Completed
                    // check that Txn_id has not been previously processed
                    // check that Receiver_email is your Primary PayPal email
                    // check that Payment_amount/Payment_currency are correct
                    // process payment
                    if (ipnContext.Args["payment_status"] == "Completed")
                    {
                        Console.WriteLine(ipnContext.RequestBody);
                        int userID = int.Parse(ipnContext.Args["item_number"]);
                        decimal amount = decimal.Parse((string)ipnContext.Args["mc_gross"]) - decimal.Parse((string)ipnContext.Args["mc_fee"]);
                        Console.WriteLine($"NEW PAYMENT COMPLETED USERID {userID} | AMOUNT : {amount}");// Newtonsoft.Json.JsonConvert.SerializeObject(ipnContext.RequestBody));
                        await balanceManager.addUserBalance(userID, amount);
                        await csgo.core.logsManager.transactions.addTransactionLog(userID, await csgo.usersManager.getUsernameById(userID), csgo.core.logsManager.transactions.transactionType.deposit, csgo.core.logsManager.transactions.transactionStatus.complete, csgo.core.logsManager.transactions.methodType.paypal, (string)ipnContext.Args["payer_email"], amount);

                        var usr = csgo.usersManager.users.Find(a => a.id == userID);
                        if (usr != null)
                            await csgo.core.notifyManager.sendNotify(usr, core.notifyManager.notifyType.success, $"Your money have just arrived.");
                    }
                }
                else if (ipnContext.Verification.Equals("INVALID"))
                {

                    Console.WriteLine(ipnContext.RequestBody);
                    int userID = int.Parse(ipnContext.Args["item_number"]);
                    decimal amount = decimal.Parse((string)ipnContext.Args["mc_gross"]);
                    Console.WriteLine($"PAMENT FAILED");// Newtonsoft.Json.JsonConvert.SerializeObject(ipnContext.RequestBody));
                    await databaseManager.updateQuery($"INSERT INTO failedPayments (text) VALUES ('USERID {userID} FAILED PAYMENT EMAIL: {(string)ipnContext.Args["payer_email"]} AMOUNT: {amount}') ").Execute();
                    var usr = csgo.usersManager.users.Find(a => a.id == userID);
                    if (usr != null)
                        await csgo.core.notifyManager.sendNotify(usr, core.notifyManager.notifyType.success, $"Your transaction failed.");
                }
                else
                {
                    //Log error
                }
            }
            catch
            {
                Console.WriteLine(ipnContext.RequestBody);

                decimal amount = decimal.Parse((string)ipnContext.Args["mc_gross"]);
                Console.WriteLine($"PAMENT FAILED");// Newtonsoft.Json.JsonConvert.SerializeObject(ipnContext.RequestBody));
                await databaseManager.updateQuery($"INSERT INTO failedPayments (text) VALUES ('USERID 0 FAILED PAYMENT EMAIL: {(string)ipnContext.Args["payer_email"]} AMOUNT: {amount}') ").Execute();
            }
        }
    }
}
