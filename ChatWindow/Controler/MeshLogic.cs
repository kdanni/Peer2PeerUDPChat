using Peer2PeerChat.ChatService;
using Peer2PeerChat.Models;
using Peer2PeerChat.ViewModels;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Discovery;

namespace Peer2PeerChat.Controler
{
    public partial class MeshLogic
    {
        protected ServiceHost ServiceHost { get; set; }

        public readonly Dictionary<string, Peer> Mesh =
            new Dictionary<string, Peer>();

        public readonly Peer Self = new Peer { };

        public ChatViewModel ChatViewModel { get; set; }

        protected bool NoNickServer { get; set; } = false;
        
        public MeshLogic(ChatViewModel chatViewModel)
        {
            ChatViewModel = chatViewModel;

            Self.Chatter = ChatViewModel.Self;

            InitWorkers();
        }
        
        public void startupAsync()
        {
            startupWorker.RunWorkerAsync();
        }

        public void sendPublicMessageAsync(string message)
        {
            publicMessageSender.RunWorkerAsync(message);
        }

        public void sendPrivateMessageAsync(string message)
        {
            privateMessageSender.RunWorkerAsync(message);
        }

        public void changeNickAsync(string nick)
        {
            if (NoNickServer)
                return;
            nickReservationWorker.RunWorkerAsync(nick);
        }

        public void discoveryAsync()
        {
            discoveryWorker.RunWorkerAsync();
        }

        public void nickServerRegistrationAsync()
        {
            nickserverRegistrationWorker.RunWorkerAsync();
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
    }
}
