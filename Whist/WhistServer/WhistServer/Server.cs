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
        private string[] names = new string[4];
        public Server()
        {
            Packet packet = new Packet();
            packet.Shuffle();
            pcards = packet.GetPcards();

            int a = (int)pcards[0][3].GetShape();
            listener = new TcpListener(IPAddress.Any, 7986);
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
                client.GetStream().Read(data, 0, data.Length);

                string name = Encoding.ASCII.GetString(data);
                names[i] = name.Substring(0, BackSlash0(name));

                clients[i] = new Client(name, client, client.GetStream());

                clients[i].stream.Write(Card.SerializeArr(pcards[i]));
            }
            
            for (int i = 0; i < 4; i++)//send the names of the other players to a player
            {
                string sendnames = i.ToString();
                for (int j = 0; j < 4; j++)
                {
                    if (j != i)
                    { 
                        sendnames += names[j].Length.ToString() + names[j];
                    }
                }
                try
                {
                    byte[] data = Encoding.UTF8.GetBytes(sendnames);
                    clients[i].stream.Write(data, 0, data.Length);
                }
                catch
                {

                }
            }
        }
        int BackSlash0(string mes)
        {
            for (int i = 0; i < mes.Length; i++)
            {
                if (mes[i] == '\0')
                {
                    return i;
                }
            }
            return mes.Length;
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
