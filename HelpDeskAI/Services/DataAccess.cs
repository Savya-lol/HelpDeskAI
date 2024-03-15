using System.Data.SqlClient;

namespace HelpDeskAI.Services
{
    public class DataAccess
    {
        public readonly string _connectionString;

        public DataAccess(string connectionString)
        {
            _connectionString = connectionString;
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

        public void UpdateSpecific(string table, string condition, string identifier, string query, object value)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string updateQuery = $"UPDATE @table SET {query} = @value WHERE {condition} = @identifier";
                using (SqlCommand cmd = new SqlCommand(updateQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@identifier", identifier);
                    cmd.Parameters.AddWithValue("@table", table);

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
