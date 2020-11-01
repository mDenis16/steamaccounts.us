using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace csgo.core
{
    public class ratesManager
    {
      
        public class rate
        {
            public int id { get; set; }
            public int ratedId { get; set; }
            public int raterId { get; set; }
            public string message { get; set; }
            public int csgoId { get; set; }
            public DateTime date { get; set; }
            public string ratedUsername { get; set; }
            public string raterUsername { get; set; }
            public bool value { get; set; }
        }
        public static async Task<List<rate>> readRates(int userId)
        {
            List<rate> rates = new List<rate>();
            await databaseManager.selectQuery($"SELECT * FROM rates WHERE ratedId = '{userId}'", delegate (DbDataReader reader)
            {
                if (reader.HasRows)
                {
                    rate rate = new rate();
                    rate.id = ( int ) reader[ "id" ];
                    rate.ratedId = ( int ) reader[ "ratedId" ];
                    rate.raterId = ( int ) reader[ "raterId" ];
                    rate.csgoId = ( int ) reader[ "csgoId" ];
                    rate.date = ( DateTime ) reader[ "date" ];
                    rate.message = ( string ) reader[ "message" ];
                    rate.value  = ( bool ) reader[ "rate" ];
                    rate.ratedUsername = ( string ) reader[ "ratedUsername" ];
                    rate.raterUsername = ( string ) reader[ "raterUsername" ];
                    rates.Add( rate );
                }
            }).Execute();
            return rates;
        }
        public static async Task<bool> existRate(int raterId,int csgoId )
        {
            var exist = false;
            await databaseManager.selectQuery( $"SELECT * FROM rates WHERE csgoId = '{csgoId}' AND raterId = '{raterId}' LIMIT 1", delegate ( DbDataReader reader )
            {
                exist = reader.HasRows;
            } ).Execute( );
            return exist;
        }

        public static async Task<bool> addRate( int ratedId, int raterId, bool rate, string message, int csgoId, string raterUsername, string ratedUsername)
        {
            if (rate)
            {
                accountsManager.csgoAccounts.FindAll(a => a.sellerid == ratedId).ForEach( b => b.positiveRates += 1 );
                await databaseManager.updateQuery($"UPDATE csgoaccounts SET positiveRates = positiveRates + 1 WHERE sellerid = @id").addValue("@id", ratedId).Execute();
                await databaseManager.updateQuery($"UPDATE users SET positiveRates = positiveRates + 1 WHERE id = @id").addValue("@id", ratedId).Execute();
            }
            else
            {
                accountsManager.csgoAccounts.FindAll(a => a.sellerid == ratedId).ForEach(b => b.negativeRates += 1);
                await databaseManager.updateQuery($"UPDATE csgoaccounts SET negativeRates = negativeRates + 1 WHERE sellerid = @id").addValue("@id", ratedId).Execute();
                await databaseManager.updateQuery($"UPDATE users SET negativeRates = negativeRates + 1 WHERE id = @id").addValue("@id", ratedId).Execute();
            }
            await databaseManager.updateQuery($"INSERT INTO rates (ratedId, raterId, rate, message, csgoId, raterUsername, ratedUsername) VALUES (@ratedId, @raterId, @rate, @message, @csgoId, @raterUsername, @ratedUsername) " ).addValue("@ratedId", ratedId).addValue("@raterId", raterId).addValue("@rate", rate).addValue("@message", message).addValue("@csgoId", csgoId).addValue("@raterUsername", raterUsername).addValue("@ratedUsername", raterUsername).Execute();
            return true;
        }

    }
}
