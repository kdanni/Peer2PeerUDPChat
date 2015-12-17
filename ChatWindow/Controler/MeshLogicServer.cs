using Peer2PeerChat.Models;
using System;
using System.Collections.Generic;

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
                        registrationRespondWorker.RunWorkerAsync(peer.UdpAddress);
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

        public void handleChatMessage(string message, string mac_hash)
        {
            if (message == null || mac_hash == null)
                return;

            var peer = Mesh[mac_hash];

            if (peer == null)
                return;

            var messageObject = new Models.Message()
            {
                Chatter = peer.Chatter,
                TextMessage = message,
                Type = MessageType.Public,
                UtcTimestamp = DateTime.UtcNow
            };

            Action addAction = () => ChatViewModel.MessageFlow.Add(messageObject);
            ChatViewModel.InvokeDispatcher(addAction);


        }

        public void removePeer(string mac_hash)
        {
            if (mac_hash == null)
                return;

            var peer = Mesh[mac_hash];

            if (peer == null)
                return;

            ChatViewModel.ChatterList.Remove(peer.Chatter);
            Mesh.Remove(mac_hash);
        }
    }
}
