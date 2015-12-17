using Peer2PeerChat.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Peer2PeerChat.Controler
{
    public partial class MeshLogic
    {
        public void registerPeer(Uri address, string nick, string mac_hash)
        {
            bool sendRespond = false;
            Peer peer = null;
            try
            {
                peer = Mesh[mac_hash];

                if (peer != null)
                {
                    if (peer.Address.Equals(address) && peer.Chatter.Nick.Equals(nick))
                    {
                        return;
                    }

                    Action removeAction = () => ChatViewModel.ChatterList.Remove(peer.Chatter);
                    ChatViewModel.InvokeDispatcher(removeAction);

                    peer.Address = address;
                    peer.Chatter = new Chatter(nick);
                }
                else
                {
                    Mesh.Remove(mac_hash);
                    throw new KeyNotFoundException();
                }
            }
            catch (KeyNotFoundException)
            {

                peer = new Peer()
                {
                    Address = address,
                    MAC_Hash = mac_hash,
                    Chatter = new Chatter(nick)
                };

                Mesh.Add(peer.MAC_Hash, peer);

                sendRespond = true;

            }

            Action addAction = () => ChatViewModel.ChatterList.Add(peer.Chatter);
            ChatViewModel.InvokeDispatcher(addAction);
            ChatViewModel.ApplicationMessageInvokeDispatcher("Chatter joined.");

            if (sendRespond)
            {
                registrationRespondWorker.RunWorkerAsync(peer.UdpAddress);
            }
        }

        public void handleChatMessage(string message, string mac_hash, bool isPublic)
        {
            if (message == null || mac_hash == null)
                return;

            var peer = Mesh[mac_hash];

            if (peer == null)
                return;

            var type = isPublic ? MessageType.Public : MessageType.Private;

            var messageObject = new Models.Message()
            {
                Chatter = peer.Chatter,
                TextMessage = message,
                Type = type,
                UtcTimestamp = DateTime.UtcNow
            };

            Action addAction = () => ChatViewModel.MessageFlow.Add(messageObject);
            ChatViewModel.InvokeDispatcher(addAction);


        }

        public void removePeer(string mac_hash)
        {
            Debug.WriteLine("Remove peer " + mac_hash);

            if (mac_hash == null)
                return;

            var peer = Mesh[mac_hash];

            if (peer == null)
                return;

            Action a = () => ChatViewModel.ChatterList.Remove(peer.Chatter);
            ChatViewModel.InvokeDispatcher(a);
            Mesh.Remove(mac_hash);
            ChatViewModel.ApplicationMessageInvokeDispatcher("Chatter left.");
        }
    }
}
