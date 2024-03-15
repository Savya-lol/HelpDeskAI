using HelpDeskAI.Models.Auth;
using HelpDeskAI.Models.Chat;

namespace HelpDeskAI.Models
{
    public class ChatUser
    {
        public int id { get; set; }
        public string name {  get; set; }
        public string email { get; set; }
        public ChatUser(int id,string name, string email)
        {
            this.id = id;
            this.name = name;
            this.email = email;
        }

        //Operator Overloading
        public static implicit operator ChatUser(User user) { //to only get the required information
            return  new ChatUser(user.Id,user.Username,user.Email);
        }
    }
}
