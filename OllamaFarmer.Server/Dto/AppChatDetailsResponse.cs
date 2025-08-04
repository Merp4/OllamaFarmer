namespace OllamaFarmer.Server.Dto
{
    public class AppChatDetailsResponse
    {
        public IEnumerable<AppChatDetails> ChatDetails { get; set; }
        public int FilteredCount { get; set; } = 0;
        public int TotalCount { get; set; } = 0;
        public int Cursor { get; set; } = 0;
        public AppChatDetails? Filter { get; set; }
    }
}
