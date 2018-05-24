using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace UDPTestApp.Base
{
    class ServerSocket : SocketBase
    {
        //attributes
        private Socket server;
        private Thread serverThread;

        private byte[] checklist;
        private int timer;

        //properties
        private IPAddress _localAddress;
        public string localAddress
        {
            get { return _localAddress.ToString(); }
            set
            {
                if(value != null)
                    _localAddress = IPAddress.Parse(Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString());
                RaisePropertyChanged("localAddress");
            }
        }

        private string _status;
        public string status
        {
            get { return _status; }
            set
            {
                _status = value;
                RaisePropertyChanged("status");
            }
        }

        public class Message
        {
            public string timestamp
            {
                get;
                set;
            }
            public string message
            {
                get;
                set;
            }
        }

        private ObservableCollection<Message> _feed;
        public ObservableCollection<Message> feed
        {
            get
            {
                if (_feed == null)
                    _feed = new ObservableCollection<Message>();
                return _feed;
            }
        }

        //methods for feed
        internal void updateFeed(string message)
        {
            string input = message;
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                feed.Add(new Message { timestamp = DateTime.Now.ToString(), message = input });
            }));
        }

        internal void clearFeed()
        {
            _feed.Clear();
        }

        internal void updateStatus(string message)
        {
            status = message;
        }

        //commands
        public ICommand runCommand
        {
            get
            {
                return new RelayCommand(serverRun);
            }
        }

        public ICommand cancelCommand
        {
            get
            {
                return new RelayCommand(serverDisconnect);
            }
        }

        public ICommand clearCommand
        {
            get
            {
                return new RelayCommand(clearFeed);
            }
        }

        public ServerSocket()
        {
        }

        //methods
        internal void fileReceive(Packet packet, int recv, byte[] data, EndPoint remote)
        {
            SystemSounds.Beep.Play();
            ushort packetID = (ushort)packet.packetID;
            string filename = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\new.temp";
            FileStream writable = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write);
            int totalPackets = (packet.fileSize / 1464) + 1;
            writable.Seek((((totalPackets) - packetID) * 1464), SeekOrigin.Current);
            writable.Write(packet.payload, 0, packet.payload.Length);
            writable.Close();

            int update;
            if (totalPackets <= 5)
                update = 0;
            else
                update = packetID % (totalPackets*1/5);
            if (update == 0 && packetID != 1 || packetID == totalPackets) //reduce updating messages to ~5 times only
                updateFeed(packetID + " file packets incoming...");
            int counter = totalPackets - packetID;
            int count = counter % 20;
            checklist[count] = 0x11; //if successful show 1, else will stay 0

            if (counter == 0) //for packet log creation
            {
                timer = Environment.TickCount;
                string logFilename = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\filelog.txt";
                if (File.Exists(logFilename))
                    File.Delete(logFilename);
                File.AppendAllText(logFilename,
                    "[Each byte 0x11 represents packet checksum pass, else fail]" + Environment.NewLine
                    + "Packet log result for file transfer: (Each line 20 packets)" + Environment.NewLine);
            }
            if (((counter % 20 == 19) && counter > 0))
            {
                int timing = (1000/((Environment.TickCount - timer)/(counter%20)))*1464; //time in ms to receive (counter % 20) packets
                updateFeed("Estimated receive rate: " + timing + "bytes/s");
                timer = Environment.TickCount;
                if (counter % 20 == 19)
                    checklist = checklist.Take(20).ToArray();
                else
                    checklist = checklist.Take(totalPackets % 20).ToArray();
                checklist.CopyTo(data, 8);
                server.SendTo(data, remote);
                File.AppendAllText(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\log.txt",
                    (counter - 18) + ": " + BitConverter.ToString(checklist) + Environment.NewLine);
                checklist = new byte[20];
            }
            if (counter == totalPackets - 1) //last packet
            {
                checklist = checklist.Take(totalPackets % 20).ToArray();
                checklist.CopyTo(data, 8);
                File.AppendAllText(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\log.txt",
                    (totalPackets - count) + ": " + BitConverter.ToString(checklist) + Environment.NewLine);
                server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 4000);
                server.SendTo(data, remote);
                try
                {
                    recv = server.ReceiveFrom(data, ref remote);
                }
                catch (Exception)
                {
                    Console.WriteLine("S: File received corrupted");
                    server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 0);
                }
                if (data[0] >> 4 == 2)
                {
                    string newfilename = Encoding.ASCII.GetString(data, 8, recv - 8);
                    string oldfilename = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\new.temp";
                    Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                    dlg.FileName = newfilename;
                    dlg.OverwritePrompt = false;
                    dlg.AddExtension = false;
                    dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    Nullable<bool> result = dlg.ShowDialog();
                    if (result == true)
                    {
                        newfilename = dlg.FileName;
                        if (File.Exists(newfilename))
                            File.Delete(newfilename);
                        File.Copy(oldfilename, newfilename);
                        File.Delete(oldfilename);
                        updateFeed("File saved. Saved as " + newfilename);
                    }
                    else
                    {
                        newfilename = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\new.temp";
                        if (File.Exists(newfilename))
                            File.Delete(newfilename);
                        File.Copy(oldfilename, newfilename);
                        File.Delete(oldfilename);
                        updateFeed("File not saved. Temp file at " + newfilename);
                    }
                }
                else
                    updateFeed("Unable to save file: File may be corrupted");
                updateFeed("Packet log available at My Documents\\log.txt");
                checklist = new byte[1024];
            }

        }

        internal bool setupSocket()
        {
            try
            {
                server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPEndPoint localEP = new IPEndPoint(IPAddress.Parse(localAddress), 9050);
                server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 100);
                server.Bind(localEP);
                return true;

            }
            catch (SocketException)
            {
                return false;
            }
        }

        internal void serverRun()
        {
            bool isSetup = setupSocket();
            serverThread = new Thread(new ThreadStart((Action) delegate()
            {
                checklist = new byte[20];
                while (true)
                {
                    IPEndPoint sender1 = new IPEndPoint(IPAddress.Any, 0);
                    EndPoint remote = (EndPoint)(sender1);

                    byte[] data = new byte[2048];
                    try
                    {
                        int recv = server.ReceiveFrom(data, ref remote);
                        updateStatus("Receiving...");
                        Packet recvPacket = new Packet(server, remote, data, recv); //to extract packet checksum
                        recvPacket.GeneratePacket(); //to generate checksum, got packet already
                        if (recvPacket.packetCorrect())
                        {
                            if (recvPacket.modeID == 1)
                            {
                                updateFeed("Ping from " + remote + ": Checksum correct. Sending echo reply to client...");
                                server.SendTo(data, 8, SocketFlags.None, remote);  //ping send back header as echo
                            }
                            else if (recvPacket.modeID == 2)
                            {
                                SystemSounds.Beep.Play();
                                updateFeed("Message from " + remote);
                                updateFeed(Encoding.ASCII.GetString(data, 8, (recv - 8)));
                            }
                            else if (recvPacket.modeID == 3)
                            {
                                fileReceive(recvPacket, recv, data, remote);
                            }
                        }
                        else
                        {
                            updateFeed("From " + remote + ": Packet send fail");
                            for (int i = 0; i < 8; i++) //custom header cleared to be detected by client
                                data[i] = 0x0;
                            server.SendTo(data, 8, SocketFlags.None, remote);
                        }

                    }
                    catch (SocketException)
                    {
                        updateStatus("Waiting...");
                    }
                }
            }));
            if (isSetup)
            {
                serverThread.Start();
                updateFeed("Server open. Receiving from " + localAddress);
            }
            else
                updateFeed("Server unable to open: Check localhost address");
        }

        internal void serverDisconnect()
        {
            serverThread.Abort();
            server.Close();
            updateFeed("Server closed.");
            updateStatus("");
        }
    }
}
