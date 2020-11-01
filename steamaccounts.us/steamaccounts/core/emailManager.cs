using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebApplication1;

namespace csgo.core
{
    public class emailManager
    {
      
        public static string confirmEmailPage = "";
        public static string recoveryEmailPage = "";
        private static Random random = new Random();
        public static string randomToken( int length )
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string( Enumerable.Repeat( chars, length )
              .Select( s => s[ random.Next( s.Length ) ] ).ToArray( ) );
        }
        public static Random rdn = new Random();
        static string GetMd5Hash(MD5 md5Hash, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
       
        public static string getEmail(string username)
        {
            Random rand = new Random(DateTime.Now.Second); // we need a random variable to select names randomly

            csgo.RandomNameGen.RandomName nameGen = new csgo.RandomNameGen.RandomName(rand); // create a new instance of the RandomName class

            string name = nameGen.Generate(csgo.RandomNameGen.Sex.Male, 1).Replace(" ", "").ToLower();

            var client = new RestClient("https://privatix-temp-mail-v1.p.rapidapi.com/request/domains/");
            var request = new RestRequest(Method.GET);
            request.AddHeader("x-rapidapi-host", "privatix-temp-mail-v1.p.rapidapi.com");
            request.AddHeader("x-rapidapi-key", "16e3b38e2amsh824c1935741225ep18aa41jsn41d3f2260d26");
            IRestResponse response = client.Execute(request);
            string[] domains = Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(response.Content);
           /* Guid g = Guid.NewGuid();
            string GuidString = Convert.ToBase64String(g.ToByteArray());
            GuidString = GuidString.Replace("=", "");
            GuidString = GuidString.Replace("+", "");
            if (GuidString.Length > 10)
                GuidString = GuidString.Substring(0, 6);*/
            return name + domains[new Random().Next(domains.Length - 1)];
            
        }
        public static string readEmail(string email)
        {
            string response = "";
            using (MD5 md5Hash = MD5.Create())
            {
                string hash = GetMd5Hash(md5Hash, email);
                var client = new RestClient("https://privatix-temp-mail-v1.p.rapidapi.com/request/mail/id/" + hash + "/");
                var request = new RestRequest(Method.GET);
                request.AddHeader("x-rapidapi-host", "privatix-temp-mail-v1.p.rapidapi.com");
                request.AddHeader("x-rapidapi-key", "16e3b38e2amsh824c1935741225ep18aa41jsn41d3f2260d26");
                IRestResponse a = client.Execute(request);
                response = a.Content;
            }
            return response;
        }

        public static async Task<bool> validateEmailToken(string token)
        {
            var success = false;
            await databaseManager.selectQuery( "SELECT username FROM users WHERE validateToken = @validateToken AND confirmed = FALSE LIMIT 1", async delegate ( DbDataReader reader )
            {
                if ( reader.HasRows )
                {
                    success = true;
                    await databaseManager.updateQuery( $"UPDATE users SET confirmed = True WHERE validateToken = @validateToken LIMIT 1" ).addValue( "@validateToken", token ).Execute( );
                
                }
            } ).addValue( "@validateToken", token).Execute( );
            return success;
        }
        public static IRestResponse sendConfirmationEmail( string email, string code )
        {
            Console.WriteLine( "generated code " + code );

            RestClient client = new RestClient();
            client.BaseUrl = new Uri( "https://api.mailgun.net/v3" );

            client.Authenticator = new HttpBasicAuthenticator( "api",
                  "540cbf4e285d3cc9ec3fe0c560d813a9-e5e67e3e-d8994bc7" );
            RestRequest request = new RestRequest();
            request.AddParameter( "domain", "noreply.steamaccounts.us", ParameterType.UrlSegment );
            request.Resource = "{domain}/messages";
            request.AddParameter( "from", "noreply <noreply@steamaccounts.us>" );

            request.AddParameter( "to", email );
            request.AddParameter( "subject", "Confirmation code" );
            request.AddParameter( "html", $@"Your recovery link is  <a href='https://steamaccounts.us/confirm?token={code}'>https://steamaccounts.us/confirm?token={code}</a>" );



            request.Method = Method.POST;
            return client.Execute( request );
        }
        public static IRestResponse sendRecoveryEmail(string email, string code)
        {
            Console.WriteLine("generated code " + code);

            RestClient client = new RestClient();
            client.BaseUrl = new Uri("https://api.mailgun.net/v3/");

            client.Authenticator = new HttpBasicAuthenticator("api",
                     "540cbf4e285d3cc9ec3fe0c560d813a9-e5e67e3e-d8994bc7");
            RestRequest request = new RestRequest();
            request.AddParameter("domain", "noreply.steamaccounts.us", ParameterType.UrlSegment);
            request.Resource = "{domain}/messages";
            request.AddParameter("from", "noreply <noreply@steamaccounts.us>");

            request.AddParameter("to", email);
            request.AddParameter("subject", "Recovery account");
            request.AddParameter("html", $@"Your recovery link is  <a href='https://steamaccounts.us/recoveryPassword?token={code}'>https:\\steamaccounts.us\recoveryPassword?token={code}</a>");


            request.Method = Method.POST;
            return client.Execute(request);
        }
        public static IRestResponse sendNoSeller( string email, string reason )
        {
           

            RestClient client = new RestClient();
            client.BaseUrl = new Uri( "https://api.mailgun.net/v3/" );

            client.Authenticator = new HttpBasicAuthenticator( "api",
                     "540cbf4e285d3cc9ec3fe0c560d813a9-e5e67e3e-d8994bc7" );
            RestRequest request = new RestRequest();
            request.AddParameter( "domain", "noreply.steamaccounts.us", ParameterType.UrlSegment );
            request.Resource = "{domain}/messages";
            request.AddParameter( "from", "noreply <noreply@steamaccounts.us>" );

            request.AddParameter( "to", email );
            request.AddParameter( "subject", "Seller status" );
            request.AddParameter( "html", $@"An admin removed your seller abilities because of the following reasons: <br><br>- {reason}" );


            request.Method = Method.POST;
            return client.Execute( request );
        }
    }
}
