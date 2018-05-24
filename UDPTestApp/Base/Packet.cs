using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace UDPTestApp
{
    class Packet
    {
        private IPEndPoint Lipep;
        private IPEndPoint Ripep;
        private ProtocolType protocol;
        private int length;
        private int _modeID; //1 for ping, 2 for message, 3 for file
        public int modeID
        {
            get { return _modeID; }
            set { _modeID = value; }
        }
        private int _packetID; //in reverse order
        public int packetID
        {
            get { return _packetID; }
            set { _packetID = value; }
        }
        private int _fileSize; //total payload sent for ping, packet for message, filesize for file
        public int fileSize
        {
            get { return _fileSize; }
            set { _fileSize = value; }
        }
        private ushort headerChecksum; //generated checksum
        private ushort packetChecksum; //checksum already in packet, if any
        private byte[] _payload; //excludes UDP/IP & custom header
        public byte[] payload
        {
            get { return _payload; }
            set { _payload = value; }
        }
        private bool flip; //is true when initialised from server

        //for server
        public Packet(Socket socket, EndPoint remoteEP, byte[] data, int recv)
        {
            try
            {
                Lipep = (IPEndPoint)remoteEP; //because client dest is server source
                Ripep = (IPEndPoint)socket.LocalEndPoint;
                protocol = socket.ProtocolType;
                length = data.Length;
                modeID = data[0] >> 4;
                packetID = (int)(((data[0] << 8) & 0x0F00) + (data[1] & 0xFF));
                fileSize = ((data[2] << 24) + (data[3] << 16) + (data[4] << 8) + data[5]);
                packetChecksum = BitConverter.ToUInt16(data, 6);
                payload = data.Skip(8).Take(recv - 8).ToArray();
                flip = false;

            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("Attempted interrupt on current action");
            }
        }

        //for client
        public Packet(Socket socket, int modeNo, int packetNo, int overallSize, byte[] message)
        {
            try
            {
                Lipep = (IPEndPoint)socket.LocalEndPoint;
                Ripep = (IPEndPoint)socket.RemoteEndPoint;
                protocol = socket.ProtocolType;
                length = 8 + message.Length;
                modeID = modeNo;
                packetID = packetNo;
                fileSize = overallSize;
                payload = message;
                flip = true;

            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("Unable to generate packet");
            }
        }

        internal byte[] GeneratePacket()
        {
            //with all the values above, plus created HEADER CHECKSUM...
            byte[] sourceAddressBytes = Lipep.Address.GetAddressBytes();
            byte[] destAddressBytes = Ripep.Address.GetAddressBytes();
            byte[] ipProtocol = new byte[2];
            ipProtocol[1] = (byte)protocol;
            short length = Convert.ToInt16(8 + payload.Length);
            byte[] packetLength = BitConverter.GetBytes(length);
            byte[] ipHeader = sourceAddressBytes.Concat(destAddressBytes).Concat(ipProtocol).Concat(packetLength).ToArray();

            byte[] modeIDByte = new byte[2]; //houses mode (1/2 byte)
            ushort mode = Convert.ToUInt16((modeID << 12) + packetID);
            modeIDByte = BitConverter.GetBytes(mode).Reverse().ToArray();
            //DEFINE OVERALL SIZE (Ping is n x each message size excl header; message is just message, file is file size excl custom header)
            byte[] overallSizeBytes = new byte[4];
            overallSizeBytes = BitConverter.GetBytes(fileSize).Reverse().ToArray();
            //set checksum (excl custom header checksum field)
            byte[] fullPacket = ipHeader.Concat(modeIDByte).Concat(overallSizeBytes).Concat(payload).ToArray();
            if((18+payload.Length) % 2 == 1) //for odd numbered payload sizes need zero padding one byte
            {
                fullPacket = fullPacket.Concat(new byte[1]).ToArray();
                headerChecksum = GetChecksum(fullPacket, 0, 18 + payload.Length +1);
            }
            else
                headerChecksum = GetChecksum(fullPacket, 0, 18+payload.Length);
            byte[] checksum = BitConverter.GetBytes(headerChecksum);
            byte[] customHeader = modeIDByte.Concat(overallSizeBytes).Concat(checksum).ToArray();

            /*printing for verification purposes
            Console.WriteLine("===========");
            Console.WriteLine(BitConverter.ToString(sourceAddressBytes));
            Console.WriteLine(BitConverter.ToString(destAddressBytes));
            Console.WriteLine(BitConverter.ToString(ipProtocol) + "-" + BitConverter.ToString(packetLength));
            Console.WriteLine("==-==|==-==");
            Console.WriteLine(BitConverter.ToString(modeIDByte) + "|" + BitConverter.ToString(overallSizeBytes) + "|" + BitConverter.ToString(checksum));
            Console.WriteLine("==-==|==-==");
            Console.WriteLine(BitConverter.ToString(payload));
            Console.WriteLine("===========");*/

            return customHeader.Concat(payload).ToArray();
        }

        internal ushort GetChecksum(byte[] payload, int start, int length)
        {
            ushort word16;
            long sum = 0;
            for (int i = start; i < (length + start); i += 2)
            {
                word16 = (ushort)(((payload[i] << 8) & 0xFF00) + (payload[i + 1] & 0xFF));
                sum += (long)word16;
            }

            while ((sum >> 16) != 0)
            {
                sum = (sum & 0xFFFF) + (sum >> 16);
            }

            if (flip)
                sum = ~sum;

            return (ushort)sum;
        }

        internal bool packetCorrect() //must do GeneratePacket()
        {
            if (headerChecksum + packetChecksum == 65535)
                return true;
            else
                return false;
        }
    }
}
