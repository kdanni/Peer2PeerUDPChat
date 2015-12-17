using Peer2PeerChat.ChatService;
using Peer2PeerChat.Models;
using Peer2PeerChat.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Discovery;
using System.Text;
using System.Threading;

namespace Peer2PeerChat.Logic
{
    public class MeshLogic
    {
        protected ServiceHost ServiceHost { get; set; }

        public readonly Dictionary<string, Peer> Mesh =
            new Dictionary<string, Peer>();

        public readonly Peer Self = new Peer { };

        public ChatViewModel ChatViewModel { get; set; }

        private readonly BackgroundWorker startupWorker = new BackgroundWorker();

        private readonly BackgroundWorker discoveryWorker = new BackgroundWorker();

        private readonly BackgroundWorker registrationRespondWorker = new BackgroundWorker();

        private readonly BackgroundWorker publicMessageSender = new BackgroundWorker();

        public MeshLogic(ChatViewModel chatViewModel)
        {
            ChatViewModel = chatViewModel;

            Self.Chatter = ChatViewModel.Self;
            
            startupWorker.DoWork += startupWorkerDoWork;
            startupWorker.RunWorkerCompleted += startupWorkerCompleted;

            discoveryWorker.DoWork += discoveryWorkerDoWork;
            discoveryWorker.RunWorkerCompleted += discoveryWorkerCompleted;

            registrationRespondWorker.DoWork += registrationRespondWorkerDoWork;

            publicMessageSender.DoWork += sendPublicMessage;
        }

        public void registerPeer(Uri address, string nick, string mac_hash)
        {
            bool sendRespond = false;
            Peer peer = null; 
            try
            {
                peer = Mesh[mac_hash];

                if (peer != null)
                {
                    if(peer.Address.Equals(address) && peer.Chatter.Nick.Equals(nick))
                    {
                        registrationRespondWorker.RunWorkerAsync(peer.UdpAddress);
                        return;
                    }

                    Action removeAction = () => ChatViewModel.ChatterList.Remove(peer.Chatter);
                    ChatViewModel.InvokeDispatcher(removeAction);
                    
                    peer.Address = address;
                    peer.Chatter = new Chatter(nick);
                } else
                {
                    Mesh.Remove(mac_hash);
                    throw new KeyNotFoundException();
                }
            }
            catch (KeyNotFoundException) {

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

        private void registrationRespondWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Uri uri = (Uri)e.Argument;
                registrationClient(uri);
            }
            catch (Exception ex)
            {

                Debug.WriteLine(ex);
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

        public void asyncSendPublicMessage(string message)
        {
            publicMessageSender.RunWorkerAsync(message);
        }

        private void sendPublicMessage(object sender, DoWorkEventArgs e)
        {
            try
            {
                string message = (string)e.Argument;
                foreach(Peer p in Mesh.Values) {
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

        public void openServiceHost()
        {
            if (Self.Address == null)
                return;

            ServiceHost = new ServiceHost(typeof(ChatService.ChatService), Self.Address);
            
            ServiceHost.AddServiceEndpoint(typeof(IChatService), new BasicHttpBinding(), Self.Address);
            ServiceHost.AddServiceEndpoint(typeof(IChatService), new UdpBinding(), Self.UdpAddress);           

            ServiceHost.Description.Behaviors.Add(new ServiceDiscoveryBehavior());
            ServiceHost.AddServiceEndpoint(new UdpDiscoveryEndpoint());

            ServiceHost.Open();
        }

        public void closeServiceHost()
        {
            ServiceHost.Close();
        }

        public void startupAsync()
        {
            startupWorker.RunWorkerAsync();
        }

        private void startupWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                ChatService.ChatService.MeshLogic = this;

                string address = ConfigurationManager.AppSettings["local.address"];

                var uri = new Uri(address);

                Debug.WriteLine(uri);

                Self.Address = uri;
                Self.MAC_Hash = generateMAC_Hash(uri.ToString());

                Mesh.Add(Self.MAC_Hash, Self);

                openServiceHost();
            }
            catch (Exception ex)
            {

                Debug.WriteLine(ex);
            }
        }

        private void startupWorkerCompleted(object sender,
                                               RunWorkerCompletedEventArgs e)
        {
            ChatViewModel.ApplicationMessageInvokeDispatcher("Service is ready.");
            discoveryAsync();
        }

        public void discoveryAsync()
        {
            discoveryWorker.RunWorkerAsync();
        }

        private void discoveryWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            ChatViewModel.ApplicationMessageInvokeDispatcher("Discovery is running.");
            try
            {
                DiscoveryClient discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());
                FindResponse findResponse = discoveryClient.Find(new FindCriteria(typeof(IChatService)));

                Debug.WriteLine("Discovery finished at " + DateTime.Now);

                if (findResponse.Endpoints.Count > 0)
                {
                    e.Result = findResponse;
                }
                else
                {
                    e.Result = null;
                    Debug.WriteLine("No service found.");
                    return;
                }

            }
            catch (Exception ex)
            {

                Debug.WriteLine(ex);
            }
        }

        private void discoveryWorkerCompleted(object sender,
                                               RunWorkerCompletedEventArgs e)
        {
            ChatViewModel.ApplicationMessageInvokeDispatcher("Discovery finished.");
            if (e.Result != null)
            {
                var findResponse = (FindResponse)e.Result;

                foreach (var ep in findResponse.Endpoints)
                {
                    Debug.WriteLine(ep.Address + ", ContractTypeNames:" + ep.ContractTypeNames
                        + ", Scopes:" + ep.Scopes + ", ListenUris:" + ep.ListenUris);

                    if(ep.Address.Uri.Equals(Self.Address) || ep.Address.Uri.Equals(Self.UdpAddress))
                    {
                        Debug.WriteLine("Continue.");
                        continue;
                    }
                    if (ep.Address.ToString().StartsWith("soap.udp"))
                    {
                        registrationClient(ep.Address.Uri);
                    }
                }
            }
            
        }

        public static string generateMAC_Hash(string salt)
        {

            IPGlobalProperties computerProperties = IPGlobalProperties.GetIPGlobalProperties();
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();


            string concatenatedMACs = (salt==null)?"":salt;

            foreach (NetworkInterface adapter in nics)
            {
                PhysicalAddress address = adapter.GetPhysicalAddress();
                concatenatedMACs += address.ToString();
            }

            Debug.WriteLine(concatenatedMACs);

            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.Unicode.GetBytes(concatenatedMACs);
            byte[] hash = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            Debug.WriteLine(sb.ToString());

            return sb.ToString();

        }
    }
}
