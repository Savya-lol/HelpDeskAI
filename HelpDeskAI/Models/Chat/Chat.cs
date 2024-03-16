namespace HelpDeskAI.Models.Chat
{
    public class Chat
    {
        public string message { get; set; }
        public DateTime sentDate { get; set; }
        public string senderUsername { get; set; }
        public int RoomID { get; set; }

        public Chat(string message, DateTime sentDate, string senderUsername, int roomID)
        {
            this.message = message;
            this.sentDate = sentDate;
            this.senderUsername = senderUsername;
            RoomID = roomID;
        }

        public Chat() { }
    }
}
