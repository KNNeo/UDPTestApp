using System;
using System.Text;
using System.Threading;
using System.Windows.Input;

namespace UDPTestApp.Base.Client
{
    class MsgClient : ClientSocket
    {
        private byte[] _message;
        public string message
        {
            get
            {
                if (_message != null)
                {
                    return Encoding.ASCII.GetString(_message);
                }
                else
                    return "";
            }
            set
            {
                if (value.Length > 0)
                {
                    _message = Encoding.ASCII.GetBytes(value);
                    RaisePropertyChanged("message");
                }
            }
        }

        public ICommand runCommand
        {
            get
            {
                return new RelayCommand(clientRun);
            }
        }

        public MsgClient()
        {

        }

        internal void clientRun()
        {
                bool isSetup = setupSocket();
                clientThread = new Thread(new ThreadStart((Action)delegate()
                {
                    if (message.Length > 0)
                    {
                        Packet sendPacket = new Packet(client, 2, 0, _message.Length, _message);
                        byte[] data = sendPacket.GeneratePacket();
                        sendOnly(data);
                        client.Close();
                        message = "";
                    }
                    else
                        System.Windows.MessageBox.Show("No message input: Please try again.");
                }));
                if (isSetup)
                    clientThread.Start();
                else
                    System.Windows.MessageBox.Show("Address invalid: Please input valid remote address");
            }
    }
}
