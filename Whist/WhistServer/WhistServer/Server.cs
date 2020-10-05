using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace WhistServer
{
    class Server
    {
        private TcpListener listener;
        private Thread waitforclients;
        private Client[] clients = new Client[4];
        private Card[][] pcards = new Card[4][];
        public Server()
        {
            Packet packet = new Packet();
            packet.Shuffle();
            pcards = packet.GetPcards();

            listener =new TcpListener(IPAddress.Any, 7986);
            listener.Start();

            waitforclients = new Thread(WaitForClient);
            waitforclients.Start();
        }
        void WaitForClient()
        {
            for (int i = 0; i < 4; i++)
            {
                TcpClient client = listener.AcceptTcpClient();

                byte[] data = new byte[256];
                client.GetStream().Read(data,0,data.Length);

                string name = Encoding.ASCII.GetString(data);

                clients[i] = new Client(name, client, client.GetStream());
                
                clients[i].stream.Write(Card.SerializeArr(pcards[i]));

            }
        }
        public static byte[] ObjectToByteArray(Object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

    }
}
