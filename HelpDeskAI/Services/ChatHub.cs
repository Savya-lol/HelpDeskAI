using HelpDeskAI.Models;
using HelpDeskAI.Models.Chat;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;

namespace HelpDeskAI.Services
{
    public class ChatHub : Hub
    {
        private readonly GeminiService geminiService;
        private readonly ChatDataAccess _chatDataAccess;
        public ChatHub(string apiKey, ChatDataAccess chatDataAccess)
        {
            geminiService = new GeminiService(apiKey);
            _chatDataAccess = chatDataAccess;
        }

        public override async Task OnConnectedAsync()
        {
            Debug.WriteLine("SignalR Connected! ID:" + Context.ConnectionId);
            //await JoinRoom(_userDataAccess.GetUserByEmail(Context.User.FindFirstValue(ClaimTypes.Email)));
            await JoinRoom(new ChatUser(0, "savya", "tropedotuber@gmail.com"));
            await base.OnConnectedAsync();
        }

        public async Task SendMessage(string user, string message, string room)
        {
            Room roomObj = await _chatDataAccess.GetRoomByUser(room);
            if (roomObj != null)
            {
                if (roomObj.isAIassisted != 1)
                {
                    int roomId = roomObj.Id;
                    Debug.WriteLine($"{roomId}");
                    await Clients.Group(room).SendAsync("ReceiveMessage", user, message, room);
                    await _chatDataAccess.SaveMessage(new Chat(message, DateTime.Now, user, roomId));
                    Debug.WriteLine($"{room} message: {message}");
                }
                else
                {
                    int roomId = roomObj.Id;
                    await Clients.Group(room).SendAsync("ReceiveMessage", user, message, room);
                    await _chatDataAccess.SaveMessage(new Chat(message, DateTime.Now, user, roomId));
                    string AIresponse = await geminiService.GetAIResponseAsync(message);
                    await Clients.Group(room).SendAsync("ReceiveMessage", "Support Bot", AIresponse, room);
                    await _chatDataAccess.SaveMessage(new Chat(AIresponse, DateTime.Now, "Support Bot", roomId));
                }
            }
            else
            {
                // Handle the case where the room doesn't exist for the user
                Debug.WriteLine($"Room not found for user: {user}");
            }
        }

        public async Task<Room> JoinRoom(ChatUser owner)
        {
            Room room = await _chatDataAccess.GetRoomByUser(owner.name);


            if (room == null)
            {
                room = await CreateRoom(owner);
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, owner.name);
            Debug.WriteLine($"Joined Room: {owner.name}");
            await Clients.Caller.SendAsync("SetCurrentRoom", owner.name);
            room.messages = await _chatDataAccess.GetMessagesByRoom(room.Id);
            if (room.messages != null)
                await Clients.Caller.SendAsync("RenderOldMessages", room.messages);
            return await _chatDataAccess.GetRoomByUser(owner.name);
        }

        public async Task CloseRoom(ChatUser owner)
        {
            Room room = await _chatDataAccess.GetRoomByUser(owner.name);
            room.ClosedDate = DateTime.Now;
            await _chatDataAccess.SaveRoom(room);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, owner.name);
        }

        public async Task<Room> CreateRoom(ChatUser owner)
        {
            Room room = new Room(owner.name, DateTime.Now);
            await Clients.Caller.SendAsync("SetCurrentRoom", owner.name);
            await Groups.AddToGroupAsync(Context.ConnectionId, owner.name);
            await _chatDataAccess.SaveRoom(room);
            return room;
        }
    }
}

