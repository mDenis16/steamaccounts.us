using System;
using System.Data.Common;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace csgo {
    abstract class query {
        private String queryString;
        protected Action<DbDataReader> code;
        protected MySqlCommand mysqlCommand;
        protected MySqlConnection connection;

        public query( String query, Action<DbDataReader> code = null ) {
            this.queryString = query;
            this.code = code;
            this.connection = databaseManager.Instance( ).newConnection( );
            mysqlCommand = new MySqlCommand( query, this.connection );
        }

        public query addValue( string key, object value ) {
            mysqlCommand.Parameters.AddWithValue( key, value );
            return this;
        }

        abstract public Task<int> Execute( );
    }
}
