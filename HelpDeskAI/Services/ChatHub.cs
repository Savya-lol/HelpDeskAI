using HelpDeskAI.Models.Chat;
using HelpDeskAI.Models;
using OpenAI_API;
using OpenAI_API.Completions;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Diagnostics;

namespace HelpDeskAI.Services
{
	public class ChatHub:Hub
	{
		private readonly OpenAIAPI _openAIAPI;
		private readonly ChatDataAccess _chatDataAccess;
        private readonly UserDataAccess _userDataAccess;
        public ChatHub(string apiKey,ChatDataAccess chatDataAccess, UserDataAccess userDataAccess)
		{
			_openAIAPI = new OpenAIAPI(apiKey);
			_chatDataAccess = chatDataAccess;
			_userDataAccess = userDataAccess;
		}

        public override async Task OnConnectedAsync()
        {
			Debug.WriteLine("SignalR Connected! ID:"+ Context.ConnectionId);
            //await JoinRoom(_userDataAccess.GetUserByEmail(Context.User.FindFirstValue(ClaimTypes.Email)));
            await JoinRoom(new ChatUser(0,"savya","mail@savya.com.np"));
            await base.OnConnectedAsync();
        }


        public async Task<string> GetAIResponseAsync(string prompt)
		{
			if (string.IsNullOrEmpty(prompt))
			{
				throw new ArgumentException("Prompt cannot be empty.");
			}

			var completionRequest = new CompletionRequest
			{
				Prompt = prompt,
				Model = "text-davinci-003"
			};

			var completion = await _openAIAPI.Completions.CreateCompletionAsync(completionRequest);

			return completion.Completions[0].Text;
		}

         public async Task SendMessage(string user, string message, string room)
        {
            await Clients.Group(room).SendAsync("ReceiveMessage", user, message);
			Debug.WriteLine($"{room} message: {message}");
        }

        public async Task<Room> JoinRoom(ChatUser owner)
		{
			if(_chatDataAccess.GetRoomByUser(owner.name) == null)
			{
				await CreateRoom(owner);
			}

            await Groups.AddToGroupAsync(Context.ConnectionId, owner.name);
			Debug.WriteLine($"Joined Room: {owner.name}");
            await Clients.Caller.SendAsync("SetCurrentRoom", owner.name);
            return await _chatDataAccess.GetRoomByUser(owner.name);
        }

        public async Task CloseRoom(ChatUser owner)
		{
			Room room = await _chatDataAccess.GetRoomByUser(owner.name);
			room.ClosedDate = DateTime.Now;
			await _chatDataAccess.SaveRoom(room);
			await Groups.RemoveFromGroupAsync(Context.ConnectionId,owner.name);
		}

        public async Task<Room> CreateRoom(ChatUser owner)
        {
            Room room = new Room(owner.name, DateTime.Now);
            await _chatDataAccess.SaveRoom(room);
            await Clients.Caller.SendAsync("SetCurrentRoom", owner.name);
            await Groups.AddToGroupAsync(Context.ConnectionId, owner.name);
			return room;
        }
    }
}

