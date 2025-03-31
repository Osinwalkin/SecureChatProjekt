using Microsoft.AspNetCore.Mvc;
using SecureChatProjekt.Shared.Models;
using System.Collections.Concurrent;

namespace SecureChatProjekt.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {

        // Key: Recipient User ID (string)
        // Value: List of messages waiting for that user
        private static readonly ConcurrentDictionary<string, List<MessageDto>> _waitingMessages =
            new ConcurrentDictionary<string, List<MessageDto>>();

        // Key: Recipient User ID (string)
        // Value: The single key exchange message waiting for them
        private static readonly ConcurrentDictionary<string, KeyExchangeDto> _waitingKeys =
           new ConcurrentDictionary<string, KeyExchangeDto>();


        // --- API Endpoints ---
        [HttpPost]
        public IActionResult PostMessage([FromBody] MessageDto message)
        {
            if (message == null || string.IsNullOrWhiteSpace(message.RecipientId) || string.IsNullOrWhiteSpace(message.SenderId))
            {
                return BadRequest("Invalid message data.");
            }

            var messageList = _waitingMessages.GetOrAdd(message.RecipientId, _ => new List<MessageDto>());


            lock (messageList)
            {
                messageList.Add(message);
            }

            Console.WriteLine($"Message received for {message.RecipientId} from {message.SenderId}");
            return Ok(); 
        }

        // GET /api/messages/{userId}
        [HttpGet("{userId}")]
        public ActionResult<IEnumerable<MessageDto>> GetMessages(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("User ID cannot be empty.");
            }


            if (_waitingMessages.TryGetValue(userId, out var messageList))
            {
                List<MessageDto> messagesToSend;
                // Lock briefly to get and clear the list atomically
                lock (messageList)
                {
                    if (messageList.Count == 0)
                    {
                        // Return empty list if no messages, not NotFound
                        return Ok(new List<MessageDto>());
                    }
                    messagesToSend = new List<MessageDto>(messageList);
                    messageList.Clear(); 
                }
                Console.WriteLine($"Delivering {messagesToSend.Count} messages to {userId}");
                return Ok(messagesToSend); 
            }
            else
            {

                return Ok(new List<MessageDto>());
            }
        }


        // POST /api/messages/keyexchange

        [HttpPost("keyexchange")]
        public IActionResult PostKeyExchange([FromBody] KeyExchangeDto keyDto)
        {
            if (keyDto == null || string.IsNullOrWhiteSpace(keyDto.RecipientId) || string.IsNullOrWhiteSpace(keyDto.SenderId) || string.IsNullOrWhiteSpace(keyDto.EncryptedAesKeyBase64))
            {
                return BadRequest("Invalid key exchange data.");
            }

            _waitingKeys.AddOrUpdate(keyDto.RecipientId, keyDto, (key, existingValue) => keyDto);

            Console.WriteLine($"Encrypted AES key stored for {keyDto.RecipientId} from {keyDto.SenderId}");
            return Ok();
        }


        // GET /api/messages/keyexchange/{userId}

        [HttpGet("keyexchange/{userId}")]
        public ActionResult<KeyExchangeDto> GetKeyExchange(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("User ID cannot be empty.");
            }

            if (_waitingKeys.TryRemove(userId, out var keyDto))
            {
                Console.WriteLine($"Delivering encrypted AES key to {userId}");
                return Ok(keyDto);
            }
            else
            {
                return NotFound();
            }
        }
    }
}