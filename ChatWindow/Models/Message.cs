using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peer2PeerChat.Models
{
    public enum MessageType
    {
        Public,
        Private,
        Meta
    }

    public class Message
    {
        public Chatter Chatter { get; set; }

        public string TextMessage { get; set; }

        public MessageType Type { get; set; }

        public DateTime UtcTimestamp { get; set; } = DateTime.UtcNow;

        public DateTime LocalTimestamp { get { return UtcTimestamp.ToLocalTime(); } }
    }
}
