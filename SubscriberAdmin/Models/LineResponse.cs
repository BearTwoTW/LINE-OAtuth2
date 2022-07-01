namespace WebApplication1.Models
{
    public class LineResponse
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string id_token { get; set; }
        public string refresh_token { get; set; }
        public string scope { get; set; }
        public string token_type { get; set; }
    }

    public class LineMessage
    {
        public int id { get; set; }

        public string message { get; set; }
    }

    public class LineMessageResponse
    {
        public int status { get; set; }

        public string message { get; set; }
    }
}
