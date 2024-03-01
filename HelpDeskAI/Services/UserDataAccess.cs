using System.Data.SqlClient;
using System.Diagnostics;
using System.Web.Helpers;
using HelpDeskAI.Models;

namespace HelpDeskAI.Services
{
    public class UserDataAccess
    {
        public readonly string _connectionString;

        public UserDataAccess(string connectionString)
        {
            _connectionString = connectionString;
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
                    Console.WriteLine("Table 'users' created successfully.");
                }
            }
        }

        public object GetValueFromTable(string tableName, string columnName, string whereClause)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = $"SELECT {columnName} FROM {tableName} WHERE {whereClause}";
                using (var cmd = new SqlCommand(query, connection))
                {
                    return cmd.ExecuteScalar();
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


        public object ExecuteScalarCommand(string query, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddRange(parameters);
                    return cmd.ExecuteScalar();
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
                                ConfirmationToken = reader.GetString(8)
                            };
                        }
                    }
                }
            }
            return null;
        }

        public void ConfirmUserEmail(User user)
        {
            UpdateSpecific("email", user.Email, "confirm", "True");
            UpdateSpecific("email", user.Email, "token", null);
        }

        public void UpdateUserPassword(string email, string newPassword)
        {
            string hashedPassword = Crypto.HashPassword(newPassword);
            UpdateSpecific("email", email, "password", hashedPassword);
            UpdateSpecific("email", email, "token", null);
        }

        public void UpdateUserToken(User user)
        {
            UpdateSpecific("email", user.Email, "token", user.ConfirmationToken);
    }

        public void UpdateSpecific(string condition, string identifier, string query, object value)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string updateQuery = $"UPDATE users SET {query} = @value WHERE {condition} = @identifier";
                using (SqlCommand cmd = new SqlCommand(updateQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@identifier", identifier);

                    if (value != null)
                    {
                        cmd.Parameters.AddWithValue("@value", value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@value", DBNull.Value);
                    }

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public T GetSpecific<T>(string column_name, string identifier,string condition)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = $"SELECT {column_name} FROM users WHERE {identifier} = @condition";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@condition", condition);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            return (T)reader.GetValue(0);
                        }
                    }
                }
                return default(T);
            }
        }
    }
}