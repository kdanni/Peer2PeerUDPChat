using Peer2PeerChat.ChatService;
using Peer2PeerChat.Models;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Peer2PeerChat.Controler
{
    public partial class MeshLogic
    {
        private void registrationClient(Uri uri)
        {
            Debug.WriteLine("Registration: " + uri);
            try
            {
                if (uri != null)
                {
                    Binding b = new UdpBinding();

                    var factory = new ChannelFactory<IChatService>(b, uri.ToString());
                    var channel = factory.CreateChannel();

                    channel.Hello(Self.Address, Self.Chatter.Nick, Self.MAC_Hash);
                }

            }
            catch (Exception ex)
            {

                Debug.WriteLine(ex);
            }
        }

        private void sendPublicMessage(object sender, DoWorkEventArgs e)
        {
            try
            {
                string message = (string)e.Argument;
                foreach (Peer p in Mesh.Values)
                {
                    if (p.UdpAddress != null)
                    {
                        if (p.MAC_Hash.Equals(Self.MAC_Hash))
                        {
                            continue;
                        }

                        Binding b = new UdpBinding();

                        var factory = new ChannelFactory<IChatService>(b, p.UdpAddress.ToString());
                        var channel = factory.CreateChannel();

                        channel.Chat(message, Self.MAC_Hash);
                    }
                }
            }
            catch (Exception ex)
            {

                Debug.WriteLine(ex);
            }
        }
    }
}
