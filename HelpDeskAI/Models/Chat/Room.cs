namespace HelpDeskAI.Models.Chat
{
    public class Room
    {
        public int Id { get; set; }
        public List<Chat> messages { get; set; }
        public int isAIassisted { get; set; } = 1;
        public string ownerName { get; set; }
        public DateTime OpenDate { get; set; }
        public DateTime? ClosedDate { get; set; } = null;

        public Room(string ownerName, DateTime OpenDate) 
        {
            this.ownerName = ownerName;
            this.OpenDate = OpenDate;
        }
        public Room()
        {
        }
    }
}
