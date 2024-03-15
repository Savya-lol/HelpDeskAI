using HelpDeskAI.Models.Auth;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Web.Helpers;

namespace HelpDeskAI.Services
{
    public class ChatDataAccess
    {
        public readonly string _connectionString;

        public ChatDataAccess(string connectionString)
        {
            _connectionString = connectionString;
            CheckAndCreateChatTable();
        }

        private void CheckAndCreateChatTable()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var cmd = new SqlCommand(
                   @"SELECT COUNT(*) 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_SCHEMA = 'dbo' 
            AND TABLE_NAME = 'users'", connection);

                int tableExists = (int)cmd.ExecuteScalar();

                if (tableExists == 0)
                {
                    string createTableQuery = @"
              CREATE TABLE users (
                  id INT PRIMARY KEY IDENTITY(1,1),
                  first_name VARCHAR(50) NOT NULL,
                  last_name VARCHAR(50) NOT NULL,
                  username VARCHAR(25) NOT NULL UNIQUE,
                  email VARCHAR(50) NOT NULL UNIQUE,
                  password VARCHAR(255) NOT NULL,
                  confirm VARCHAR(5),
                  role VARCHAR(50) DEFAULT 'User' NOT NULL,
                  token VARCHAR(255)
              ); 
            ";
                    var createCmd = new SqlCommand(createTableQuery, connection);
                    createCmd.ExecuteNonQuery();
                    Console.WriteLine("Table 'chat' created successfully.");
                }
            }
        }
    }
}
