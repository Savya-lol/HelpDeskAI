namespace HelpDeskAI.Models.Chat
{
    public class Chat
    {
        public string message { get; set; }
        public DateTime sentDate { get; set; }
        public string senderUsername { get; set; }

        public bool isAI;
        public int RoomID { get; set; }
    }
}
