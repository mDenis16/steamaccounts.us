using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace csgo {
    class selectQuery : query {
        public  selectQuery( string query,  Action<DbDataReader> code ) : base( query, code ) { }

        public override async Task<int> Execute( ) {

            DateTime date = DateTime.Now;
            DbDataReader reader = await mysqlCommand.ExecuteReaderAsync( ).ConfigureAwait( false );
            if ( reader.HasRows ) {
                while ( await reader.ReadAsync( ).ConfigureAwait( false ) ) {
                    code( reader );
                }
            }

            reader.Close( );
            connection.Close( );
            Console.WriteLine($"MYSQL QUERY TOOK {(DateTime.Now - date).TotalSeconds} ms [{mysqlCommand.CommandText}]");
            return 1;
        }
    }
}
