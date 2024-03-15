using System.Data.SqlClient;
using System.Diagnostics;
using System.Web.Helpers;
using HelpDeskAI.Models.Auth;

namespace HelpDeskAI.Services
{
    public class UserDataAccess : DataAccess
    {
        public readonly string _userTableName;
        public UserDataAccess(string connectionString, string userTableName) : base(connectionString)
        {
            _userTableName = userTableName;
            CheckAndCreateUsersTable();
        }

        private void CheckAndCreateUsersTable()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var cmd = new SqlCommand(
                    @"SELECT COUNT(*) 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_SCHEMA = 'dbo' 
            AND TABLE_NAME = @usertable", connection);
                cmd.Parameters.AddWithValue("@usertable", _userTableName);

                int tableExists = (int)cmd.ExecuteScalar();

                if (tableExists == 0)
                {
                    string createTableQuery = @"
              CREATE TABLE @usertable (
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
                    createCmd.Parameters.AddWithValue("@usertable", _userTableName);
                    createCmd.ExecuteNonQuery();
                    Console.WriteLine("Table 'users' created successfully.");
                }
            }
        }

        public bool UserExists(string column, string identifier, string column2 = null, string value2 = null)
        {
            string whereClause = column + " = @identifier"; 
            if (column2 != null && value2 != null)
            {
                whereClause += " AND " + column2 + " = @value2";
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = $"SELECT COUNT(*) FROM users WHERE {whereClause}";
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@identifier", identifier);
                    if (column2 != null && value2 != null)
                    {
                        cmd.Parameters.AddWithValue("@value2", value2);
                    }
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }


        public void CreateUser(User model)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                    INSERT INTO users (first_name, last_name, username, email, password, confirm, token) 
                    VALUES (@fname, @lname, @uname, @mail, @pass, @conf_status, @conf_token)";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@fname", model.FirstName);
                    cmd.Parameters.AddWithValue("@lname", model.LastName);
                    cmd.Parameters.AddWithValue("@uname", model.Username);
                    cmd.Parameters.AddWithValue("@mail", model.Email);
                    cmd.Parameters.AddWithValue("@pass", Crypto.HashPassword(model.Password));
                    cmd.Parameters.AddWithValue("@conf_status", model.IsConfirmed);
                    cmd.Parameters.AddWithValue("@conf_token", model.ConfirmationToken);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public User GetUserByConfirmationToken(string token)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM users WHERE token = @token";
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@token", token);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {

                        Debug.WriteLine("Token before query:" + token);
                        if (reader.HasRows)
                        {
                            reader.Read();
                            Debug.WriteLine("Token after query:" + token);
                            Debug.WriteLine("User Email:"+ reader.GetString(4));
                            return new User
                            {
                                Id = reader.GetInt32(0),
                                FirstName = reader.GetString(1),
                                LastName = reader.GetString(2),
                                Username = reader.GetString(3),
                                Email = reader.GetString(4),
                                Password = reader.GetString(5),
                                IsConfirmed = reader.GetString(6),
                                Role = reader.GetString(7),
                                ConfirmationToken = reader.GetString(8)
                            };
                        }
                    }
                }
            }
            return null;
        }

        public User GetUserByEmail(string email)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM users WHERE email = @email";
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            return new User
                            {
                                Id = reader.GetInt32(0),
                                FirstName = reader.GetString(1),
                                LastName = reader.GetString(2),
                                Username = reader.GetString(3),
                                Email = reader.GetString(4),
                                Password = reader.GetString(5),
                                IsConfirmed = reader.GetString(6),
                                Role = reader.GetString(7),
                                ConfirmationToken = reader.IsDBNull(8) ? "" : reader.GetString(8),
                            };
                        }
                    }
                }
            }
            return null;
        }

        public void ConfirmUserEmail(User user)
        {
            UpdateSpecific(_userTableName, "email", user.Email, "confirm", "True");
            UpdateSpecific(_userTableName, "email", user.Email, "token", null);
        }

        public void UpdateUserPassword(string email, string newPassword)
        {
            string hashedPassword = Crypto.HashPassword(newPassword);
            UpdateSpecific(_userTableName, "email", email, "password", hashedPassword);
            UpdateSpecific(_userTableName, "email", email, "token", null);
        }

        public void UpdateUserToken(User user)
        {
            UpdateSpecific(_userTableName,"email", user.Email, "token", user.ConfirmationToken);
         }
    }
}