using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Input;

namespace UDPTestApp.Base.Client
{
    class PingClient : ClientSocket
    {
        //attributes
        private StringBuilder logBuffer;

        //properties
        private int _sendCount;
        public int sendCount
        {
            get { return _sendCount; }
            set
            {
                _sendCount = value;
                RaisePropertyChanged("sendCount");
                Console.WriteLine(value);
            }
        }

        private int _pingSize;
        public int pingSize
        {
            get { return _pingSize; }
            set
            {
                _pingSize = value;
                RaisePropertyChanged("pingSize");
                Console.WriteLine(value);
            }
        }

        private bool _isRandom;
        public bool isRandom
        {
            get { return _isRandom; }
            set
            {
                _isRandom = value;
                RaisePropertyChanged("isRandom");
                Console.WriteLine(value);
            }
        }

        private bool _isForever;
        public bool isForever
        {
            get { return _isForever; }
            set
            {
                _isForever = value;
                RaisePropertyChanged("isForever");
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

        //command
        public ICommand runCommand
        {
            get
            {
                return new RelayCommand(clientRun);
            }
        }

        public PingClient()
        {
        }

        //methods
        internal void updateStatus(string message)
        {
            output = message;
        }

        internal void clientRun()
        {
            bool isSetup = setupSocket();
            clientThread = new Thread(new ThreadStart((Action) delegate()
            {
                int i = sendCount, reply = 0, loss = 0, max = 0, min = 0, sum = 1, timer;
                logBuffer = new StringBuilder();
                logBuffer.AppendLine("Ping status for " + remoteAddress.ToString() + " at " + DateTime.Now);
                logBuffer.AppendLine("===========================================");
                updateStatus("Pinging to " + remoteAddress.ToString());
                Thread.Sleep(400);
                while (i > 0 || isForever == true)
                {
                    byte[] message = new byte[1];
                    Random rnd = new Random();
                    if (!isRandom)
                        message = new byte[pingSize - 50];
                    else
                        message = new byte[rnd.Next(62, 1500) - 50];
                    rnd.NextBytes(message);
                    Packet sendPacket = new Packet(client, 1, i, Convert.ToInt32(sendCount), message);
                    byte[] data = sendPacket.GeneratePacket();
                    if(isForever == true)
                        updateStatus("Sending packet");
                    else
                        updateStatus("Sending packet #" + (sendCount - i + 1));
                    timer = Environment.TickCount;
                    data = sendReceive(data);
                    if (recv > 0)
                    {
                        timer = Environment.TickCount - timer;
                        sum += timer;
                        if (timer >= max)
                            max = timer;
                        if (timer <= min)
                            min = timer;
                        if (timer == 0)
                            timer = 1;
                        string pingResult;
                        if (isForever == true)
                            pingResult = "";
                        else
                            pingResult = "(Packet " + (sendCount - i + 1) + "/" + sendCount + ") ";
                        if (Encoding.ASCII.GetString(data, 0, 8) != "\0\0\0\0\0\0\0\0")
                        {
                            pingResult += "Reply from " + remoteAddress.ToString() +
                                ": size=" + (50 + message.Length) + "bytes time=" + timer + "ms checksum=pass";
                        }
                        else
                        {
                            pingResult += "Reply from " + remoteAddress.ToString() +
                                ": size=" + (50 + message.Length) + "bytes time=" + timer + "ms checksum=fail";
                        }
                        updateStatus(pingResult);
                        reply++;
                    }
                    else
                    {
                        if(isForever == true)
                            updateStatus("No reply from " + remoteAddress.ToString() + ": size=" + (50 + message.Length) + "bytes time=n.a. checksum=n.a.");
                        else
                            updateStatus("(Packet " + (sendCount - i + 1) + "/" + sendCount + ") No reply from " + remoteAddress.ToString() +
                                ": size=" + (50 + message.Length) + "bytes time=n.a. checksum=n.a.");
                        loss++; //if reach here means no response from server
                    }
                    logBuffer.AppendLine(output);
                    Thread.Sleep(recvTimeout); //delay similar to timeout
                    if (isForever == false)
                    {
                        i--;
                    }
                    
                }
                updateStatus("Printing summary...");
                Thread.Sleep(400);
                logBuffer.AppendLine("\nPing statistics for " + remoteAddress.ToString() + ":");
                logBuffer.AppendLine("\tPackets: Sent=" + sendCount + " Received=" + reply + " Lost=" + loss);
                logBuffer.AppendLine("\tApprox. Round Trip Times: Min=" + min + " Max=" + max + " Average=" + (sum / sendCount));

                updateStatus("Writing to log...");
                FileStream log = new FileStream(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                        + "\\log_" + DateTime.Now.Hour.ToString("00") + DateTime.Now.Minute.ToString("00") + DateTime.Now.Second.ToString("00") + ".txt",
                        FileMode.Create, FileAccess.Write);
                log.Write(Encoding.ASCII.GetBytes(logBuffer.ToString()), 0, logBuffer.Length);
                log.Close();
                Thread.Sleep(400);
                updateStatus("Ping complete. Opening log at " + log.Name);
                System.Diagnostics.Process.Start(@log.Name);
                client.Close();
            }));
            if (isSetup)
                clientThread.Start();
            else
                updateStatus("Ping could not start: Please check remote address");
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
                updateStatus("Ping aborted by user");
            }
            
        }
    }
}
