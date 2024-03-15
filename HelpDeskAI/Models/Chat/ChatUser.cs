using HelpDeskAI.Models.Auth;

namespace HelpDeskAI.Models
{
    public class ChatUser
    {
        public int id { get; set; }
        public string name {  get; set; }
        public string email { get; set; }


        //constructor
        public ChatUser(int id,string name, string email)
        {
            this.id = id;
            this.name = name;
            this.email = email;
        }

        //Operator Overloading
        public static explicit operator ChatUser(User user) { //to only get the required information
            ChatUser chatUser = new ChatUser(user.Id,user.Username,user.Email);
            return chatUser;
        }
    }
}
