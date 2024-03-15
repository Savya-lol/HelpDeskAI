namespace HelpDeskAI.Models.Chat
{
    public class Chat
    {
        public string message { get; set; }
        public string sentDate { get; set; }
        public int senderID { get; set; }
        public string senderName { get; set; }
        public bool isAI;
        public int RoomID { get; set; }
    }
}
