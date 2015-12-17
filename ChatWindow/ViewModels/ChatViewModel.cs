using Peer2PeerChat.Controler;
using Peer2PeerChat.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

namespace Peer2PeerChat.ViewModels
{
    public partial class ChatViewModel : INotifyPropertyChanged
    {

        public ObservableCollection<Chatter> ChatterList { get; set; }

        public ObservableCollection<Message> MessageFlow { get; set; }

        public ChatViewModel()
        {
            ChatterList = new ObservableCollection<Chatter>();
            MessageFlow = new ObservableCollection<Message>();

            ChatterList.Add(Self);
            MeshLogic = new MeshLogic(this);
            MeshLogic.startupAsync();

            MessageText = "";
            SendbuttonWidth = new GridLength(0, GridUnitType.Star);
            foreach (Message m in GetWelcomeMessage()) {
                MessageFlow.Add(m);
            }
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        private void History()
        {
            MessageText = (_history==null)?"":_history;
            _history = null;
        }

        private void EasyAccess()
        {
            if (SendbuttonWidth.Value == 0)
            {
                SendbuttonWidth = new GridLength(1, GridUnitType.Star);
            }
            else
            {
                SendbuttonWidth = new GridLength(0, GridUnitType.Star);
            }
        }

        private void HandleChatCommand()
        {
            if (MessageText != null && MessageText.StartsWith("/nick "))
            {
                MeshLogic.changeNickAsync(MessageText);
                return;
            }
            if (MessageText != null && MessageText.StartsWith("/msg "))
            {
                MeshLogic.sendPrivateMessageAsync(MessageText);

                var m = new Message()
                {
                    Chatter = Self,
                    TextMessage = MessageText,
                    UtcTimestamp = DateTime.UtcNow,
                    Type = MessageType.Private
                };

                MessageFlow.Add(m);

                return;
            }
            if ("/discovery".Equals(MessageText))
            {
                MeshLogic.discoveryAsync();
                return;
            }
            if ("/help".Equals(MessageText))
            {
                foreach (Message m in GetHelpMessage())
                {
                    MessageFlow.Add(m);
                }
                return;                    
            }
            if ("/exit".Equals(MessageText))
            {
                System.Windows.Application.Current.Shutdown();
                return;
            }
            if ("/ea".Equals(MessageText))
            {
                EasyAccess();
                return;
            }
            ApplicationMessage("No " + MessageText + " command found!");
            ApplicationMessage("Type /help for help.");
        }

        private void SendMessage()
        {
            if (MessageText != null && MessageText.StartsWith("/"))
            {
                HandleChatCommand();
                _history = MessageText;
                MessageText = "";
                return;
            }
            else
            {
                var m = new Message()
                {
                    Chatter = Self,
                    TextMessage = MessageText,
                    UtcTimestamp = DateTime.UtcNow,
                    Type = MessageType.Public
                };

                this.MessageFlow.Add(m);
            }
            _history = MessageText;
            MessageText = "";
            MeshLogic.sendPublicMessageAsync(_history);
        }
    }
}
