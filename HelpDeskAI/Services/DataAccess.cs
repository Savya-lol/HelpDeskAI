﻿using System.Data.SqlClient;

namespace HelpDeskAI.Services
{
    public class DataAccess
    {
        public readonly string _connectionString;

        public DataAccess(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void UpdateSpecific(string table, string condition, string identifier, string query, object value)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string updateQuery = $"UPDATE {table} SET {query} = @value WHERE {condition} = @identifier";
                using (SqlCommand cmd = new SqlCommand(updateQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@identifier", identifier);

                    // No need for @table parameter here

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

        public T GetSpecific<T>(string table, string column_name, string identifier, string condition)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = $"SELECT {column_name} FROM {table} WHERE {identifier} = @condition";
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
