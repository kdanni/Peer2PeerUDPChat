using Peer2PeerChat.Logic;
using Peer2PeerChat.Models;
using MVVMExample.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace Peer2PeerChat.ViewModels
{
    public partial class ChatViewModel
    {
        private Dispatcher _dispatcher;
        public bool IsReady { get; set; }

        public MeshLogic MeshLogic { get; set; }

        public readonly Chatter Self = new Chatter
        {
            Nick = Chatter.Anonymous,
            ThisIsMe = true
        };

        public readonly Chatter Application = new Chatter
        {
            Nick = "",
            ThisIsMe = false
        };

        private string _text;
        public string MessageText
        {
            get { return _text; }
            set { _text = value; OnPropertyChanged(); }
        }

        private GridLength _sendbuttonWidth;
        public GridLength SendbuttonWidth
        {
            get { return _sendbuttonWidth; }
            set { _sendbuttonWidth = value; OnPropertyChanged(); }
        }

        #region messages
        protected void ApplicationMessage(string _message)
        {
            MessageFlow.Add(
                new Message
                {
                    Chatter = Application,
                    UtcTimestamp = DateTime.UtcNow,
                    TextMessage = _message,
                    Type = MessageType.Meta
                });
        }

        public List<Message> GetWelcomeMessage()
        {
            var theList = new List<Message>();

            theList.Add(
                new Message
                {
                    Chatter = Application,
                    UtcTimestamp = DateTime.UtcNow,
                    TextMessage = "Welcome " + Self.Nick + "!",
                    Type = MessageType.Meta
                });

            theList.Add(
                new Message
                {
                    Chatter = Application,
                    UtcTimestamp = DateTime.UtcNow,
                    TextMessage = "Type /help for help.",
                    Type = MessageType.Meta
                });


            return theList;
        }

        public List<Message> GetHelpMessage()
        {
            var theList = new List<Message>();

            theList.Add(
                new Message
                {
                    Chatter = Application,
                    UtcTimestamp = DateTime.UtcNow,
                    TextMessage = "/help\tDisplay this help.",
                    Type = MessageType.Meta
                });
            theList.Add(
                new Message
                {
                    Chatter = Application,
                    UtcTimestamp = DateTime.UtcNow,
                    TextMessage = "/exit\tClose the application.",
                    Type = MessageType.Meta
                });
            theList.Add(
                new Message
                {
                    Chatter = Application,
                    UtcTimestamp = DateTime.UtcNow,
                    TextMessage = "/ea\tEasy access.",
                    Type = MessageType.Meta
                });

            return theList;
        }
        #endregion
        
        #region sendCommand
        private RelayCommand _sendCommand;

        public ICommand SendCommand
        {
            get
            {
                if (_sendCommand == null)
                {
                    _sendCommand = new RelayCommand(
                        l => SendMessage(), l => CanSend());
                }
                return _sendCommand;
            }
        }

        private bool CanSend()
        {
            return IsReady;
        }
        #endregion

        #region historyCommand
        private RelayCommand _historyCommand;

        private string _history;

        public ICommand HistoryCommand
        {
            get
            {
                if (_historyCommand == null)
                {
                    _historyCommand = new RelayCommand(
                        l => History(), l => HasHistory());
                }
                return _historyCommand;
            }
        }

        private bool HasHistory()
        {
            return (_history != null && !"".Equals(_history.Trim()));
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void InvokeDispatcher(Action action)
        {
            _dispatcher.Invoke(action);
        }

        public void ApplicationMessageInvokeDispatcher(string message)
        {
            Action action = () => ApplicationMessage(message);
            _dispatcher.Invoke(action);
        }
    }
}
