using HelpDeskAI.Models.Chat;
using System.Data.SqlClient;

namespace HelpDeskAI.Services
{
    public class ChatDataAccess : DataAccess
    {
        public readonly string _chatTableName;
        public readonly string _roomTableName;
        public ChatDataAccess(string connectionString, string roomTableName, string chatTableName) : base(connectionString)
        {
            _roomTableName = roomTableName;
            _chatTableName = chatTableName;
            CheckAndCreateRoomsTable();
            CheckAndCreateChatTable();
        }

        private void CheckAndCreateRoomsTable()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var cmd = new SqlCommand(
                    @"SELECT COUNT(*) 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_SCHEMA = 'dbo' 
            AND TABLE_NAME = @roomtable", connection);
                cmd.Parameters.AddWithValue("@roomtable", _roomTableName);

                int tableExists = (int)cmd.ExecuteScalar();

                if (tableExists == 0)
                {
                    string createTableQuery = $@"
                CREATE TABLE {_roomTableName} (
                    RoomId INT IDENTITY(1,1) PRIMARY KEY,
                    OpenDate DATETIME,
                    ClosedDate DATETIME,
                    isOpen INT,
                    isAIassisted INT,
                    RoomOwnerUsername VARCHAR(50)
                ); 
            ";
                    var createCmd = new SqlCommand(createTableQuery, connection);
                    createCmd.ExecuteNonQuery();
                    Console.WriteLine($"Table '{_roomTableName}' created successfully.");
                }
            }
        }


        private void CheckAndCreateChatTable()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var cmdCheck = new SqlCommand(
                    $@"SELECT COUNT(*) 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_SCHEMA = 'dbo' 
            AND TABLE_NAME = '{_chatTableName}'", connection);

                int tableExists = (int)cmdCheck.ExecuteScalar();

                if (tableExists == 0)
                {
                    string createTableQuery = $@"
                CREATE TABLE {_chatTableName} (
                    MessageId INT IDENTITY(1,1) PRIMARY KEY,
                    SenderName VARCHAR(50),
                    MessageContent VARCHAR(MAX),
                    RoomId INT,
                    Timestamp DATETIME,
                    FOREIGN KEY (RoomId) REFERENCES {_roomTableName}(RoomId)
                )";

                    var createCmd = new SqlCommand(createTableQuery, connection);
                    createCmd.ExecuteNonQuery();
                    Console.WriteLine("Table 'chat' created successfully.");
                }
            }
        }

        public async Task SaveMessage(Chat chat)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var cmd = new SqlCommand(
           $"INSERT INTO {_chatTableName} (SenderName, MessageContent, RoomId, Timestamp) VALUES (@SenderName, @MessageContent, @RoomId, @Timestamp)",
           connection);
                cmd.Parameters.AddWithValue("@SenderName", chat.senderUsername);
                cmd.Parameters.AddWithValue("@MessageContent", chat.message);
                cmd.Parameters.AddWithValue("@RoomId", chat.RoomID);
                cmd.Parameters.AddWithValue("@Timestamp", chat.sentDate);

                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task SaveRoom(Room room)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var checkCmd = new SqlCommand($"SELECT COUNT(*) FROM {_roomTableName} WHERE RoomId = @id", connection);
                checkCmd.Parameters.AddWithValue("@id", room.Id);
                var roomExists = (int)await checkCmd.ExecuteScalarAsync() > 0;

                if (roomExists)
                {
                    var updateCmd = new SqlCommand(
                        $"UPDATE {_roomTableName} SET RoomOwnerUsername = @RoomOwnerUsername, OpenDate = @OpenDate, isAIassisted = @isai, isOpen = @isopen {(room.ClosedDate != null ? ", ClosedDate = @ClosedDate" : "")} WHERE RoomId = @id",
                        connection);
                    updateCmd.Parameters.AddWithValue("@RoomOwnerUsername", room.ownerName);
                    updateCmd.Parameters.AddWithValue("@OpenDate", room.OpenDate);
                    updateCmd.Parameters.AddWithValue("@isai", room.isAIassisted);
                    updateCmd.Parameters.AddWithValue("@id", room.Id);
                    updateCmd.Parameters.AddWithValue("@isopen", room.isOpen);
                    if (room.ClosedDate != null)
                    {
                        updateCmd.Parameters.AddWithValue("@ClosedDate", room.ClosedDate);
                    }
                    await updateCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    var insertCmd = new SqlCommand(
                        $"INSERT INTO {_roomTableName} (RoomOwnerUsername, OpenDate, ClosedDate, isAIassisted, isOpen) VALUES (@RoomOwnerUsername, @OpenDate, @ClosedDate, @isai, @isopen)",
                        connection);
                    insertCmd.Parameters.AddWithValue("@RoomOwnerUsername", room.ownerName);
                    insertCmd.Parameters.AddWithValue("@OpenDate", room.OpenDate);
                    insertCmd.Parameters.AddWithValue("@ClosedDate", (object)room.ClosedDate ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@isai", room.isAIassisted);
                    insertCmd.Parameters.AddWithValue("@isopen", room.isOpen);

                    await insertCmd.ExecuteNonQueryAsync();
                }
            }
        }


        public async Task<Room> GetRoomByUser(string username)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = $"SELECT RoomId, RoomOwnerUsername, OpenDate, ClosedDate, isAIassisted FROM {_roomTableName} WHERE RoomOwnerUsername = @ownername";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@ownername", username);
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Room room = new Room
                            {
                                Id = (int)reader["RoomId"],
                                ownerName = (string)reader["RoomOwnerUsername"],
                                OpenDate = (DateTime)reader["OpenDate"],
                                isAIassisted = (int)reader["isAIassisted"]
                            };

                            if (reader["ClosedDate"] != DBNull.Value)
                            {
                                room.ClosedDate = (DateTime)reader["ClosedDate"];
                            }
                            else
                            {
                                room.ClosedDate = null; 
                            }

                            room.messages = await GetMessagesByRoom(room.Id);

                            return room;
                        }
                    }
                }
            }
            return null;
        }


        public async Task<List<Chat>> GetMessagesByRoom(int roomID)
        {
            List<Chat> messages = new List<Chat>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = $"SELECT SenderName, MessageContent, RoomId, Timestamp FROM {_chatTableName} WHERE RoomId = @roomId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@roomId", roomID);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Chat message = new Chat
                            {
                                senderUsername = (string)reader["SenderName"],
                                message = (string)reader["MessageContent"],
                                sentDate = (DateTime)reader["Timestamp"]
                            };

                            messages.Add(message);
                        }
                    }
                }
            }

            return messages;
        }

    }
}
