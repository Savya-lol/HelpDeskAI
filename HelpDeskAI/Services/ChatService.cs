using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OpenAI_API;
using OpenAI_API.Completions;

namespace HelpDeskAI.Services
{
	public class ChatService
	{
		private readonly OpenAIAPI _openAIAPI;

		public ChatService(string apiKey)
		{
			_openAIAPI = new OpenAIAPI(apiKey);
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
	}
}

