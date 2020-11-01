using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using csgo.core.logsManager;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApplication1.Models;
using RestSharp;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
namespace csgo.Controllers
{
    public class profileController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public profileController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }


        public IActionResult Index()
        {

            return View();
        }

        [HttpGet]
        public async Task<JsonResult> requestWithdraw()
        {
            if (adminController.tokenAccess.validateToken(Request, adminController.tokenType.requestwithdraw))
            {

                var account = csgo.usersManager.users.Find(a => a.cookie == Request.Cookies["sessionid"]);
                if (account == null)
                    return Json(new { success = false, message = "You are not logged in." });

                if (account.holdBalance > 0)
                    return Json(new { success = false, message = "You already  have money on hold." });

                if (account.paypalemail == null || account.paypalemail.Length < 1)
                {
                    adminController.tokenAccess.createToken(Request, adminController.tokenType.setpaypal);
                    return Json(new { success = false, message = "You dont have setup paypal email. Go to your profile setttings to set your email.", nopaypal = true });
                }
                bool exist = false;
                await databaseManager.selectQuery($"SELECT * FROM transactions WHERE userId = @userId AND type = {(int)transactions.transactionType.withdraw} AND status = {(int)transactions.transactionStatus.inpending}", delegate (DbDataReader reader)
         {
             exist = reader.HasRows;
         }).addValue("@userId", account.id).Execute();

                if (exist)
                    return Json(new { success = true, message = "You already have a pending transaction." });
                var withdrawable = account.balance;
                if (withdrawable < 4.40m)
                    return Json(new { success = true, message = "Minimum withdraw is 4 euro." });
                if (withdrawable > account.balance)
                    return Json(new { success = true, message = "You don't have enough money to withdraw. Please contact an admin." });

                await csgo.core.logsManager.transactions.addTransactionLog(account.id, account.username, core.logsManager.transactions.transactionType.withdraw, core.logsManager.transactions.transactionStatus.inpending, core.logsManager.transactions.methodType.balance, account.paypalemail, withdrawable);
                await csgo.core.balanceManager.takeUserBalance(account.id, withdrawable);
                await databaseManager.updateQuery($"UPDATE users SET holdBalance = '{withdrawable}' WHERE id = '{account.id}' LIMIT 1").Execute();
                account.holdBalance = withdrawable;
                await csgo.core.balanceManager.createWithdrawRequest(account.id, account.username, withdrawable, account.paypalemail);
                return Json(new { success = true, message = "Your withdraw request was succesfully sent." });
            }
            return Json(new { success = false, message = "Unauthorized access." });
        }
        [HttpGet]
        public async Task<JsonResult> setPayPal(string paypal)
        {
            if (adminController.tokenAccess.validateToken(Request, adminController.tokenType.setpaypal))
            {

                var account = csgo.usersManager.users.Find(a => a.cookie == Request.Cookies["sessionid"]);
                if (account == null)
                    return Json(new { success = false, message = "You are not logged in." });

                if (account.holdBalance > 0)
                    return Json(new { success = false, message = "You already  have money on hold." });

                if (account.paypalemail != null && account.paypalemail.Length > 1)
                    return Json(new { success = false, message = "You already setuped a paypal" });

                account.paypalemail = paypal;
                await databaseManager.updateQuery($"UPDATE users SET paypalemail = @paypalemail WHERE id = '{account.id}' LIMIT 1").addValue("@paypalemail", account.email).Execute();


                return Json(new { success = true, message = "Your paypal was succesfully set." });
            }
            return Json(new { success = false, message = "Unauthorized access." });
        }

        [HttpGet]
        public async Task<IActionResult> readEmailRecovery(int id)
        {
            if (csgo.core.requestsHelper.processRequest(Request))
                return Json(new { success = "false", message = "Please try again later." });



            var user = csgo.usersManager.users.Find(a => a.cookie == Request.Cookies["sessionid"]);
            if (user == null)
                return Json(new { success = false, message = "You are not logged in." });
            var account = csgo.accountsManager.revokedAccounts.Find(a => a.id == id && a.sellerId == user.id);
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(csgo.accountsManager.revokedAccounts));
            if (account != null && account.email.Length > 0)
            {
                //  Console.WriteLine("READING EMAILS" + (String)email);
                dynamic lastEmailCheck = TempData["lastEmailCheck"];
                if (lastEmailCheck == null)
                    lastEmailCheck = DateTime.Now.AddHours(-24);
                Console.WriteLine("LAST CHECK " + (DateTime.Now - (DateTime)lastEmailCheck).TotalSeconds);


                if ((DateTime.Now - (DateTime)lastEmailCheck).TotalSeconds < 40 && TempData["admin"] == null)
                    return Json(new { success = false, message = "You can check email once at 40 seconds." });
                using (MD5 md5Hash = MD5.Create())
                {
                    string hash = Controllers.marketPlaceController.GetMd5Hash(md5Hash, account.email);

                    var client = new RestClient("https://privatix-temp-mail-v1.p.rapidapi.com/request/mail/id/" + hash + "/");
                    var request = new RestRequest(Method.GET);
                    request.AddHeader("x-rapidapi-host", "privatix-temp-mail-v1.p.rapidapi.com");
                    request.AddHeader("x-rapidapi-key", "16e3b38e2amsh824c1935741225ep18aa41jsn41d3f2260d26");
                    IRestResponse a = client.Execute(request);
                    Console.WriteLine(a.Content);
                    Console.WriteLine(account.email);
                    Console.WriteLine("hash");
                    dynamic obj = JValue.Parse(a.Content);
                    try
                    {
                        if (obj["error"] != null)
                        {

                            TempData["lastEmailCheck"] = DateTime.Now;

                            Console.WriteLine("ERROR " + obj["error"]);
                            return Json(new { success = false, message = "There are no emails." });
                        }
                    }
                    catch (System.ArgumentException)
                    {

                        string steam = obj[obj.Count - 1].mail_text;

                        await csgo.core.logsManager.utilities.addEmailLog(account.email, steam);


                        TempData["lastEmailCheck"] = DateTime.Now;

                        return Json(new { success = true, content = steam });

                    }
                }


            }

            return Json(new { success = false, message = "You are not authorized." });
        }
        public IActionResult Privacy()
        {
            return View();
        }
        [HttpGet]
        public async Task<JsonResult> readNotifications()
        {
            if (csgo.core.requestsHelper.processRequest(Request))
                return Json(new { success = "false", message = "Please try again later." });
            var account = csgo.usersManager.users.Find(a => a.cookie == Request.Cookies["sessionid"]);
            if (account == null)
                return Json(new { success = false, message = "You are not logged in." });


            return Json(new { success = true, data = await csgo.core.notifyManager.readNotifications(account.id, true) });

        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
