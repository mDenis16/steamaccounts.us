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
    public class marketController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public marketController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
      
        public IActionResult Index()
        {

            return View();
        }
        
        [Route( "api/fetchOfflineProducts" )]
        public async Task<IActionResult> fetchOfflineProducts( )
        {
            if ( csgo.core.requestsHelper.processRequest( Request ) )
                return Json( new { success = "false", message = "You are sending to many requests. Blacklist will expire in 30 seconds." } );

            
            return Json( new { success = "false", products = await accountsManager.fetchOfflineProducts() } );
        }
        
      

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
