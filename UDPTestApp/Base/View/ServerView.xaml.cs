using System;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace UDPTestApp.Base
{
    /// <summary>
    /// Interaction logic for ServerView.xaml
    /// </summary>
    public partial class ServerView : UserControl
    {
        DispatcherTimer dt;
        public ServerView()
        {
            InitializeComponent();
            dt = new DispatcherTimer();
            dt.Interval = TimeSpan.FromSeconds(1);
            dt.Tick += dt_Tick;
        }

        void dt_Tick(object sender, EventArgs e)
        {
            ServerSocket.Message item = (ServerSocket.Message)output.Items.GetItemAt(output.Items.Count - 1);
            if (item.message == "Server unable to open: Check localhost address")
            {
                localAddr.IsEnabled = true;
                disconnect.Visibility = Visibility.Hidden;
            }
            dt.Stop();
        }

        private void listenToggle(object sender, RoutedEventArgs e)
        {
            string name = ((Button)sender).Name;
            if(name == "connect")
            {
                localAddr.IsEnabled = false;
                disconnect.Visibility = Visibility.Visible;
                dt.Start();
            }
            else if (name == "disconnect")
            {
                localAddr.IsEnabled = true;
                disconnect.Visibility = Visibility.Hidden;
            }

        }

        private void output_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (output.Items.Count > 0)
                output.ScrollIntoView(output.Items.GetItemAt(output.Items.Count - 1));
        }
    }
}
