using Peer2PeerChat.ChatService;
using Peer2PeerChat.Models;
using Peer2PeerChat.PeerServiceReference;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text.RegularExpressions;

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
        
        private void sendPrivateMessage(object sender, DoWorkEventArgs e)
        {
            string line = (string)e.Argument;
            if (line != null && line.StartsWith("/msg "))
            {
                try
                {

                    Debug.WriteLine("private message");
                    var match = Regex.Match(line, @"^(/msg )(\w+)( )(.*)$");
                    if (match.Groups.Count < 4)
                        return;

                    string nick = match.Groups[2].Value;
                    string message = match.Groups[4].Value;

                    if (nick == null || message == null || Chatter.Anonymous.Equals(nick))
                        return;

                    Debug.WriteLine("nick: " + nick);
                    Debug.WriteLine("message: " + message);

                    foreach (Peer p in Mesh.Values)
                    {
                        if (p.UdpAddress != null)
                        {
                            Debug.WriteLine(p.Chatter.Nick);

                            if (p.MAC_Hash.Equals(Self.MAC_Hash))
                                continue;
                            if (!nick.Equals(p.Chatter.Nick))
                                continue;

                            Binding b = new UdpBinding();

                            var factory = new ChannelFactory<IChatService>(b, p.UdpAddress.ToString());
                            var channel = factory.CreateChannel();

                            channel.Wishper(message, Self.MAC_Hash);
                        }
                    }
                }
                catch (Exception ex)
                {

                    Debug.WriteLine(ex);
                }
            }
        }

        public void sendBye()
        {
            try
            {
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

                        channel.Bye(Self.MAC_Hash);
                    }
                }

                if (!NoNickServer)
                {
                    string serverAddress = ConfigurationManager.AppSettings["server.address"];
                    Uri serverUri = new Uri(serverAddress + "peer/http");

                    Binding b = new BasicHttpBinding();

                    var factory = new ChannelFactory<IPeerService>(b, serverUri.ToString());
                    var channel = factory.CreateChannel();

                    channel.kickoutPeer(Self.MAC_Hash,Self.MAC_Hash);
                }
            }
            catch (Exception ex)
            {

                Debug.WriteLine(ex);
            }
        }
    }
}
