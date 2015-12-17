using Peer2PeerChat.ChatService;
using Peer2PeerChat.ViewModels;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.ServiceModel.Discovery;

namespace Peer2PeerChat.Controler
{
    public partial class MeshLogic
    {
        private readonly BackgroundWorker startupWorker = new BackgroundWorker();

        private readonly BackgroundWorker discoveryWorker = new BackgroundWorker();

        private readonly BackgroundWorker registrationRespondWorker = new BackgroundWorker();

        private readonly BackgroundWorker publicMessageSender = new BackgroundWorker();

        protected void InitWorkers()
        {
            startupWorker.DoWork += startupWorkerDoWork;
            startupWorker.RunWorkerCompleted += startupWorkerCompleted;

            discoveryWorker.DoWork += discoveryWorkerDoWork;
            discoveryWorker.RunWorkerCompleted += discoveryWorkerCompleted;

            registrationRespondWorker.DoWork += registrationRespondWorkerDoWork;

            publicMessageSender.DoWork += sendPublicMessage;

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
    }
}
