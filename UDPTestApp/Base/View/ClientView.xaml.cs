using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace UDPTestApp.Base
{
    public partial class ClientView : UserControl
    {
        DispatcherTimer dt;
        public ClientView()
        {
            InitializeComponent();
            dt = new DispatcherTimer();
            dt.Interval = TimeSpan.FromSeconds(0.5);
            dt.Tick += dt_Tick;
        }

        void dt_Tick(object sender, EventArgs e)
        {
            if (pingStatus.Text == "Ping could not start: Please check remote address")
            {
                cancelPing.Visibility = Visibility.Hidden;
                ping.Visibility = Visibility.Visible;
            }
            else if (pingStatus.Text == "Writing to log...")
            {
                cancelPing.Visibility = Visibility.Hidden;
                ping.Visibility = Visibility.Visible;
                dt.Stop();
            }
            else if (fileStatus.Value == 100)
            {
                cancelFile.Visibility = Visibility.Hidden;
                dt.Stop();
                fileStatus.Value = 0;
            }
            else if (fileOutput.Text == "No file selected" || fileOutput.Text == "Client could not open: Invalid target address")
            {
                cancelFile.Visibility = Visibility.Hidden;
                dt.Stop();
            }
        }

        void pingClick(object sender, RoutedEventArgs e)
        {
            string name = ((Button)sender).Name;
            if (name == "ping")
            {
                cancelPing.Visibility = Visibility.Visible;
                ping.Visibility = Visibility.Hidden;
                dt.Start();
            }
            else if (name == "cancelPing")
            {
                cancelPing.Visibility = Visibility.Hidden;
                ping.Visibility = Visibility.Visible;
            }
        }

        void fileClick(object sender, RoutedEventArgs e)
        {
            string name = ((Button)sender).Name;
            if (name == "browse")
            {
                cancelFile.Visibility = Visibility.Visible;
                dt.Start();
            }
            else if (name == "cancelFile")
            {
                cancelFile.Visibility = Visibility.Hidden;
            }
        }

        void msgInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                msgInput.Text += Environment.NewLine;
                msgInput.CaretIndex = msgInput.Text.Length;
            }
            else if (e.Key == Key.Enter)
            {
                send.Command.Execute(msgInput.Text);
            }
        }
    }
}
