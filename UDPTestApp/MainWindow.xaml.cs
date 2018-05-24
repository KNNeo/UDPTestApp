using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

//ON HOLD: Consider doing UI class for get/set, when initialise setup mainWindow and process formatexceptions from there
//ISSUE: Cannot listen to single port as client decides on random port
//NOTE: Max file size to send across is int32 max value (~2GB), may want to check filesize before starting

//AGENDA: Do view side data binding ie. bind all ui elements to a suitable (type) variable in a viewmodel
//AGENDA: Tie up viewmodel to various socket classes?
//[[[[[NEW AGENDA: Try ObjectDataProvider as an alternative to Button Click event??]]]]]


namespace UDPTestApp
{
    // Interaction logic for MainWindow.xaml
    public partial class MainWindow : Window
    {
        //"Main" function: start here
        public MainWindow()
        {
            InitializeComponent();
        }

        private void CloseApp(object sender, CancelEventArgs e)
        {
            Environment.Exit(0);
        }

        private void HelpOption(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1)
                DisplayHelp();
        }

        void DisplayHelp()
        {
            MessageBoxResult result = MessageBox.Show(
                "For client (Send):\n" +
                "- Input all values in Ping/Settings and test out connection first\n" + 
                "- Messages sent may not reach remote host\n" + 
                "- Send file by hitting Browse, select file\n" + 
                "Progress bar will show completion and success status\n\n" + 
                "For server(Receive):\n" + 
                "- Status beside Listen button (when active) shows if idle or not\n" + 
                "- For ping acknowledgement message will appear;\n" + 
                "For file transfer messages will show incoming packets left and receiving data rate", "Help");
        }
    }
}
