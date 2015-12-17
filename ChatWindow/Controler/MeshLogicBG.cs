using Peer2PeerChat.ChatService;
using Peer2PeerChat.NickServiceReference;
using Peer2PeerChat.PeerServiceReference;
using Peer2PeerChat.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Discovery;
using System.Text.RegularExpressions;

namespace Peer2PeerChat.Controler
{
    public partial class MeshLogic
    {
        private readonly BackgroundWorker startupWorker = new BackgroundWorker();

        private readonly BackgroundWorker nickserverRegistrationWorker = new BackgroundWorker();

        private readonly BackgroundWorker discoveryWorker = new BackgroundWorker();

        private readonly BackgroundWorker registrationRespondWorker = new BackgroundWorker();

        private readonly BackgroundWorker publicMessageSender = new BackgroundWorker();

        private readonly BackgroundWorker privateMessageSender = new BackgroundWorker();

        private readonly BackgroundWorker nickReservationWorker = new BackgroundWorker();

        private readonly BackgroundWorker memoDeliverWorker = new BackgroundWorker();

        private readonly BackgroundWorker memoRetrieverWorker = new BackgroundWorker();

        private void InitWorkers()
        {
            startupWorker.DoWork += startupWorkerDoWork;
            startupWorker.RunWorkerCompleted += startupWorkerCompleted;

            discoveryWorker.DoWork += discoveryWorkerDoWork;
            discoveryWorker.RunWorkerCompleted += discoveryWorkerCompleted;

            registrationRespondWorker.DoWork += registrationRespondWorkerDoWork;

            publicMessageSender.DoWork += sendPublicMessage;
            privateMessageSender.DoWork += sendPrivateMessage;

            nickserverRegistrationWorker.DoWork += nickserverRegistrationWorkerDoWork;

            nickReservationWorker.DoWork += nickReservationWorkerDoWork;

            memoDeliverWorker.DoWork += memoDeliverWorkerDoWork;

            memoRetrieverWorker.DoWork += memoRetrieverWorkerDoWork;
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

            bool discovery = Boolean.Parse(ConfigurationManager.AppSettings["startup.discovery"]);
            if (discovery)
            {
                discoveryAsync();
            }

            bool nickserver = Boolean.Parse(ConfigurationManager.AppSettings["startup.nickserver"]);
            if (nickserver)
            {
                nickServerRegistrationAsync();
            } else
            {
                NoNickServer = true;
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

                    if (ep.Address.Uri.Equals(Self.Address) || ep.Address.Uri.Equals(Self.UdpAddress))
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

        private void nickserverRegistrationWorkerDoWork(object sender, DoWorkEventArgs e)
        {

            Debug.WriteLine("Peer registration...");
            try
            {
                string serverAddress = ConfigurationManager.AppSettings["server.address"];
                Uri serverUri = new Uri(serverAddress + "peer/http");

                if (serverUri != null)
                {
                    Binding b = new BasicHttpBinding();

                    var factory = new ChannelFactory<IPeerService>(b, serverUri.ToString());
                    var channel = factory.CreateChannel();

                    channel.registerPeer(Self.MAC_Hash,Self.UdpAddress);

                    Debug.WriteLine("Peer registration completed.");

                    Debug.WriteLine("Get peer list.");

                    var uris = channel.getPeerList();

                    foreach(var uri in uris)
                    {
                        Debug.WriteLine(uri);
                        if (uri.Equals(Self.Address) || uri.Equals(Self.UdpAddress))
                        {
                            Debug.WriteLine("Continue.");
                            continue;
                        }
                        if (uri.ToString().StartsWith("soap.udp"))
                        {
                            registrationClient(uri);
                        }
                    }

                }
                else
                {
                    NoNickServer = true;
                }
                
            }
            catch (Exception ex)
            {
                NoNickServer = true;
                Debug.WriteLine(ex);
            }


        }

        private void nickReservationWorkerDoWork(object sender, DoWorkEventArgs e)
        {

            Debug.WriteLine("Nick registration...");
            try
            {
                string nick = (string)e.Argument;
                Debug.WriteLine("Nick: "+ nick);
                if (nick != null && nick.StartsWith("/nick "))
                {
                    var array = nick.Split(' ');
                    if (array.Length < 2)
                    {
                        nick = null;
                    } else
                    {
                        nick = "";

                        for (int i = 1 ; i < array.Length; i++)
                        {
                            nick += array[i];
                        }
                    }
                }
                nick = Regex.Replace(nick, @"\s+", "");
                Debug.WriteLine("Nick: " + nick);

                string serverAddress = ConfigurationManager.AppSettings["server.address"];
                Uri serverUri = new Uri(serverAddress + "nick/http");

                if (nick != null && serverUri != null && !"".Equals(nick.Trim()))
                {
                    Binding b = new BasicHttpBinding();

                    var factory = new ChannelFactory<INickService>(b, serverUri.ToString());
                    var channel = factory.CreateChannel();

                    bool success = channel.registerNick(nick, Self.MAC_Hash);

                    if (!success)
                    {
                        Debug.WriteLine("Nick registration failed.");
                        return;
                    }

                    Self.Chatter.Nick = nick;
                    Action a = () =>
                    {
                        ChatViewModel.ChatterList.Remove(Self.Chatter);
                        ChatViewModel.ChatterList.Insert(0,Self.Chatter);
                    };
                    ChatViewModel.InvokeDispatcher(a);

                    Debug.WriteLine("Nick registration completed.");

                    foreach (var peer in Mesh.Values)
                    {
                        Debug.WriteLine(peer.Address);
                        if (Self.Equals(peer))
                        {
                            Debug.WriteLine("Continue.");
                            continue;
                        }
                        registrationClient(peer.UdpAddress);
                    }

                }
                else
                {
                    NoNickServer = true;
                }

            }
            catch (Exception ex)
            {
                NoNickServer = true;
                Debug.WriteLine(ex);
            }
        }

        private void memoDeliverWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void memoRetrieverWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
