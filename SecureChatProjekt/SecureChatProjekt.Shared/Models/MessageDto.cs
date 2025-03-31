namespace SecureChatProjekt.Shared.Models
{
    public class MessageDto
    {

        public string SenderId { get; set; }

        public string RecipientId { get; set; }

        public string IvBase64 { get; set; }

        public string CiphertextBase64 { get; set; }
    }
}
