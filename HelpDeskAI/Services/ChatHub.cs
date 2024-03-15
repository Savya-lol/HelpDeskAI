using System;
using System.Threading.Tasks;
using HelpDeskAI.Models.Chat;
using HelpDeskAI.Models;
using Microsoft.Extensions.Options;
using OpenAI_API;
using OpenAI_API.Completions;

namespace HelpDeskAI.Services
{
	public class ChatHub
	{
		private readonly OpenAIAPI _openAIAPI;
		private readonly ChatDataAccess _chatDataAccess;


        public ChatHub(string apiKey,ChatDataAccess chatDataAccess)
		{
			_openAIAPI = new OpenAIAPI(apiKey);
			_chatDataAccess = chatDataAccess;
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

        public async Task CreateRoom(ChatUser owner)
        {
            Room room = new Room(owner.name, DateTime.Now);
            owner.openChat = room;
            await _chatDataAccess.SaveRoom(room);
        }
    }
}

