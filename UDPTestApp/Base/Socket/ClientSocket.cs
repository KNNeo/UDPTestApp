using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Input;

namespace UDPTestApp.Base
{
    class ClientSocket : SocketBase
    {
        //attributes
        protected Socket client;
        protected Thread clientThread;
        protected int recv;

        //properties
        private int _recvTimeout;
        public int recvTimeout
        {
            get { return _recvTimeout; }
            set
            {
                _recvTimeout = value;
                RaisePropertyChanged("recvTimeout");
                Console.WriteLine(value);
            }
        }

        private static IPAddress _remoteAddress;
        public string remoteAddress
        {
            get { return _remoteAddress.ToString(); }
            set
            {
                _remoteAddress = IPAddress.Parse(value);
                RaisePropertyChanged("remoteAddress");
            }
        }

        //commands
        public ICommand cancelCommand
        {
            get
            {
                return new RelayCommand(clientDisconnect);
            }
        }

        public ClientSocket()
        {
        }

        //methods
        internal bool setupSocket()
        {
            try
            {
                client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(remoteAddress), 9050);
                client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, recvTimeout);
                client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DontFragment, 1);
                client.Connect(remoteEP);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        internal void sendOnly(byte[] packet)
        {
            client.Send(packet);
        }

        internal byte[] sendReceive(byte[] packet)
        {
            client.Send(packet);
            byte[] data = new byte[2048];
            try
            {
                recv = client.Receive(data);
                return data;
            }
            catch (SocketException c)
            {
                if (c.ErrorCode == 10051)
                {
                    Console.WriteLine("Unknown Host. Please check host address.");
                }
                else if (c.ErrorCode == 10054)
                {
                    Console.WriteLine("Destination host not open");
                }
                else if (c.ErrorCode == 10065)
                {
                    Console.WriteLine("Destination host cannot be found");
                }
                else if (c.ErrorCode == 10060)
                {
                    client.Close();
                    Console.WriteLine("Request Timeout");
                    client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(remoteAddress), 9050);
                    client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, recvTimeout);
                    client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DontFragment, 0);
                    client.Connect(remoteEP);
                }
                else
                {
                    Console.WriteLine("Client: " + c.ErrorCode);
                }
                recv = 0;
                return null;
            }
        }

        internal virtual void clientDisconnect()
        {
            if (client != null)
            {
                if (clientThread.ThreadState != ThreadState.Stopped)
                {
                    clientThread.Abort();
                }
                client.Close();
            }
        }
    }
}
