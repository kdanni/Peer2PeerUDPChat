using Peer2PeerChat.ViewModels;
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Windows;

namespace Peer2PeerChat.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
        }

        private void ChatControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                var viewModel = (ChatViewModel)DataContext;

                viewModel.MessageFlow.CollectionChanged +=
                    new NotifyCollectionChangedEventHandler(MessageFlowChanged);

            }
            catch (Exception ex)
            {
                //No auto scroll if something wrong, but it's not fatal.
                Debug.WriteLine(ex);
            }

            string address = ConfigurationManager.AppSettings["local.address"];
            Title = address;

        }

        private void MessageFlowChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ChatControl.MessageFlowChanged();
        }
    }
}
