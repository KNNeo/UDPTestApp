using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Input;

namespace UDPTestApp.Base.Client
{
    class FileClient : ClientSocket
    {
        //properties
        private FileInfo _filename;
        public string filename
        {
            get { return _filename.ToString(); }
            set
            {
                if (value.Length > 0)
                {
                    _filename = new FileInfo(value);
                    RaisePropertyChanged("filename");
                }
            }
        }

        private int _sendTimeout;
        public int sendTimeout
        {
            get { return _sendTimeout; }
            set
            {
                _sendTimeout = value;
                RaisePropertyChanged("sendTimeout");
                Console.WriteLine(value);
            }
        }

        private string _output;
        public string output
        {
            get { return _output; }
            set
            {
                _output = value;
                RaisePropertyChanged("output");
            }
        }

        private double _progress;
        public double progress
        {
            get { return _progress; }
            set
            {
                _progress = value;
                RaisePropertyChanged("progress");
            }
        }

        //command
        public ICommand runCommand
        {
            get
            {
                return new RelayCommand(clientRun);
            }
        }

        //methods
        internal void updateStatus(string message)
        {
            output = message;
        }

        internal void clientRun()
        {
            bool isSetup = setupSocket();
            clientThread = new Thread(new ThreadStart((Action)delegate()
            {
                //open file
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.Filter = "All Files|*.*";
                Nullable<bool> result = dlg.ShowDialog();
                filename = dlg.FileName;

                if (result == true)
                {
                    int totalPackets = ((int)_filename.Length / 1464) + 1;
                    int i = totalPackets, counter = 0, complete = 0;  //packet size available for packet after excl. header
                    bool isBroken = false;
                    updateStatus("Sending...");
                    Thread.Sleep(400);
                    int timer = Environment.TickCount;
                    while (i > 0)
                    {
                        FileStream sourceFile = new FileStream(_filename.FullName, FileMode.Open, FileAccess.Read);
                        if(sourceFile.Length >= Int32.MaxValue)
                            break;
                        byte[] message = new byte[1464];
                        sourceFile.Seek(counter * 1464, SeekOrigin.Current);
                        sourceFile.Read(message, 0, 1464);
                        if (sourceFile.Length - (counter * 1464) < 1464)
                            message = message.Take((int)sourceFile.Length - (counter * 1464)).ToArray();
                        Packet sendPacket = new Packet(client, 3, i, (int)sourceFile.Length, message);
                        byte[] packet = sendPacket.GeneratePacket();    //generate packet
                        sourceFile.Close();

                        if (((counter % 20 == 19) && counter > 0) || counter == totalPackets - 1)
                        {
                            byte[] newdata = sendReceive(packet);
                            if (recv > 0)
                            {
                                int sends = 0, max;
                                if (counter % 20 == 19)
                                    max = 20;
                                else
                                    max = totalPackets % 20;
                                for (int n = 0; n < max; n++)
                                {
                                    if (newdata[8 + n] == 0x11)
                                        sends++;
                                }
                                //Console.WriteLine("C: " + BitConverter.ToString(newdata.Skip(8).Take(max).ToArray()));
                                updateStatus("Sending....");
                                if (sends < max)
                                {
                                    isBroken = true;
                                    updateStatus((max - sends) + " packets not received by target address");
                                    Thread.Sleep(200); //show warning status
                                }
                            }
                            else
                            {
                                isBroken = true;
                                updateStatus("Unable to receive send results");
                            }
                            timer = Environment.TickCount;
                            }
                        else
                            sendOnly(packet);
                        counter++;
                        complete++;
                        progress = (double)complete / totalPackets * 100;
                        i--;               //decremented i to determine which is last packet
                        Thread.Sleep(sendTimeout + 40); //packet delay plus minimum code processing time
                    }
                    updateStatus("File send complete.");
                    progress = (double)100;
                    client.Close();
                    //send file name through msgInput IF corruption not detected
                    if (isBroken)
                        updateStatus("Warning: File sent may be corrupted.");
                    else
                    {
                        MsgClient filenameSend = new MsgClient();
                        filenameSend.remoteAddress = remoteAddress;
                        filenameSend.recvTimeout = recvTimeout;
                        filenameSend.message = _filename.Name.ToString();
                        filenameSend.clientRun();
                    }
                }
                else
                    updateStatus("No file selected");
            }));
            if (isSetup)
                clientThread.Start();
            else
                updateStatus("Client could not open: Invalid target address");


        }

        internal override void clientDisconnect()
        {
            if (client != null && clientThread != null)
            {
                if (clientThread.ThreadState != ThreadState.Stopped)
                {
                    clientThread.Abort();
                }
                client.Close();
                updateStatus("File transfer aborted by user");
                progress = 0;
            }
        }
    }
}
