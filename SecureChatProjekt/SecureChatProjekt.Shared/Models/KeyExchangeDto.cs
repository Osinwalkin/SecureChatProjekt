namespace SecureChatProjekt.Shared.Models
{
    public class KeyExchangeDto
    {
        public string SenderId { get; set; }
        public string RecipientId { get; set; }
        public string EncryptedAesKeyBase64 { get; set; }
    }
}