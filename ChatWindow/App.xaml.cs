using Peer2PeerChat.ViewModels;
using Peer2PeerChat.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Peer2PeerChat
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ChatViewModel _chatViewModel;

        protected override void OnStartup(StartupEventArgs e)
        {
            var view = new MainView();
            _chatViewModel = new ChatViewModel();
            view.DataContext = _chatViewModel;

            view.Show();
            _chatViewModel.IsReady = true;
            
        }
        
    }
}
