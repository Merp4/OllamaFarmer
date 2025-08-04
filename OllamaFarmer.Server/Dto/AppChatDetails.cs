
using OllamaFarmer.Server.Models;
using OllamaFarmer.Server.Services;


namespace OllamaFarmer.Server.Dto
{
    public class AppChatDetails
    {

        public Guid Id { get; set; } = Guid.NewGuid();

        public string? Name { get; set; }
        public bool Persisted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? Model { get; set; }

        public string? ServerName { get; set; }


        public AppChatDetails()
        {
        }
        public AppChatDetails(AppChat c, string chatServerName)
        {
            Id = c.Id;
            Name = c.Name;
            Persisted = true;
            CreatedAt = c.CreatedAt;
            Model = c.Model;
            ServerName = chatServerName ?? c.ChatServerId.ToString();
        }


        //internal static AppChatDetails FromAppChat(AppChat chat)
        //{
        //    return new AppChatDetails
        //    {
        //        Id = chat.Id,
        //        Name = chat.Name,
        //        Persisted = chat.Persist,
        //        CreatedAt = chat.CreatedAt,
        //        Model = chat.Model
        //    };
        //}
    }
}
