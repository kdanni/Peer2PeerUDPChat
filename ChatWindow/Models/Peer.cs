using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peer2PeerChat.Models
{

    public class Peer
    {
        public string MAC_Hash { get; set; }

        public Uri Address { get; set; }

        public Uri UdpAddress
        {
            get
            {
                return new Uri("soap.udp://" + Address.Authority + Address.PathAndQuery);
            }
        }

        public Chatter Chatter { get; set; }

    }
}
