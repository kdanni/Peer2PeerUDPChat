using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Peer2PeerChat.ChatService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IChatService" in both code and config file together.
    [ServiceContract]
    public interface IChatService
    {
        [OperationContract(IsOneWay = true)]
        void Hello(Uri address, string nick, string mac_hash);

        [OperationContract(IsOneWay = true)]
        void Chat(string message, string mac_hash);

        [OperationContract(IsOneWay = true)]
        void Wishper(string message, string mac_hash);

        [OperationContract(IsOneWay = true)]
        void Bye(string mac_hash);

    }
}
