using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace csgo {
    class normalQuery : query {
        public normalQuery( string query, Action<DbDataReader> code = null ) : base( query, code ) { }

        public override async Task<int> Execute( ) {
            DateTime old = DateTime.Now;
            int id = 0;
            try {
                await mysqlCommand.ExecuteNonQueryAsync( ).ConfigureAwait( false );
                connection.Close( );
                id = ( int ) mysqlCommand.LastInsertedId;
            } catch ( Exception e ) {
                
                Console.WriteLine( $"[debug] mysql error at {DateTime.Now.ToString( )} : {e.ToString( )}" );
                 Console.WriteLine( $"[debug] WRONG QUERY -> {mysqlCommand.CommandText}" );


            }
            Console.WriteLine($"MYSQL QUERY TOOK {(DateTime.Now - old).TotalSeconds} ms [{mysqlCommand.CommandText}]");
            return id;
        }
    }
}
