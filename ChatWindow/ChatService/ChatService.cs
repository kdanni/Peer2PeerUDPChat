using Peer2PeerChat.Controler;
using Peer2PeerChat.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Windows;

namespace Peer2PeerChat.ChatService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "ChatService" in both code and config file together.
    public class ChatService : IChatService
    {
        public static MeshLogic MeshLogic { get; set; }

        public void Chat(string message, string mac_hash)
        {
            if (MeshLogic == null)
                return;
            try
            {
                MeshLogic.handleChatMessage(message, mac_hash);
            }
            catch (Exception e)
            { 
                Debug.WriteLine(e);
            }
        }

        public void Hello(Uri address, string nick, string mac_hash)
        {
            if (MeshLogic == null)
                return;
            try
            {
                MeshLogic.registerPeer(address, nick, mac_hash);
            }
            catch (Exception e)
            { 
                Debug.WriteLine(e);
            }
        }

        public void Bye(string mac_hash)
        {
            if (MeshLogic == null)
                return;
            try
            {
                MeshLogic.removePeer(mac_hash);
            }
            catch (Exception e)
            { 
                Debug.WriteLine(e);
            }
        }
    }
}
