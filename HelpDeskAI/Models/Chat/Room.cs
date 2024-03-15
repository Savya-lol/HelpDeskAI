namespace HelpDeskAI.Models.Chat
{
    public class Room
    {
        public int RoomID { get; set; }
        public List<Chat> chats { get; set; }
        public List<ChatUser> users { get; set; }
        public bool isAIassisted { get; set; }
        public string OpenDate { get; set; }
        public string ClosedDate { get; set; }
    }
}
