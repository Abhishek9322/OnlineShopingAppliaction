
using Microsoft.Identity.Client;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Realtime;
using System.Data;

namespace OnlineShopingAppliaction.Service
{
    public class ApiService
    {
        private readonly OpenAIClient _client;
        public ApiService(IConfiguration configuration)
        {
            var apiKey = configuration["OpenAI:ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentNullException(nameof(apiKey), "OpenAI API key is missing. Please set it in appsettings.json or secrets.");

            _client = new OpenAIClient(apiKey);
        }

        public async Task<string> GetChatResponceAsync(string userMessage, string conversationHistory = "")
        {

            try
            {
                var chatClient = _client.GetChatClient("gpt-4o-mini"); //  use updated model

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage("You are a helpful AI phone recommendation expert. Give precise and clear answers about mobile phones, their price ranges, and features.")
                };

                if (!string.IsNullOrEmpty(conversationHistory))
                    messages.Add(new UserChatMessage(conversationHistory));

                messages.Add(new UserChatMessage(userMessage));

                var result = await chatClient.CompleteChatAsync(messages);

                //  Ensure we safely access the text
                var text = result?.Value?.Content?.FirstOrDefault()?.Text;

                if (string.IsNullOrEmpty(text))
                    throw new Exception("No content returned from AI.");

                return text;
            }
            catch (Exception ex)
            {
                // Log full details for debugging
                Console.WriteLine($"[OpenAI ERROR] {ex.Message}\n{ex.StackTrace}");
                throw new Exception("AI service failed to respond properly. " + ex.Message);
            }
        }

    }
}
