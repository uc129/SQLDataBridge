
namespace Domain.Shared
{
    public class Message
    {
        public Message()
        {
        }

        public Message(string title, string text, bool success)
        {
            Title = title;
            Text = text;
            Success = success;
        }
        public Message(string title, string text, bool success, DateTime date)
        {
            Title = title;
            Text = text;
            Success = success;
            DateTime = date;
        }

        public string Title { get; set; } = null!;
        public string Text { get; set; } = null!;
        public bool Success { get; set; }
        public DateTime DateTime { get; set; } = DateTime.Now;
    }
}
