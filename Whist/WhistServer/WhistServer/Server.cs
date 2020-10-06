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
        private int trump;
        public Server()
        {
            Packet packet = new Packet();
            packet.Shuffle();
            pcards = packet.GetPcards();

            int a = (int)pcards[0][3].GetShape();
            listener = new TcpListener(IPAddress.Any, 7986);
            listener.Start();

            WaitForClient();
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
            GetTrump();
        }
        void GetTrump()
        {
            trump = ReciveInt(0);

            byte[] data;
            if (trump == 5)//frish
            {
                data = Encoding.UTF8.GetBytes("a");//tell all clients that its frish
            }
            else
            {
                data = Encoding.UTF8.GetBytes("b");//tell all clients to bet
            }
            for (int i = 0; i < 4; i++) 
            {
                clients[i].stream.Write(data, 0, data.Length);
            
            }
           // DontCloseServer();
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
        public int ReciveInt(int clientid)
        {
            byte[] buffer = new byte[1];
            clients[clientid].stream.Read(buffer, 0, 1);
            return (int)buffer[0];
        }
        void DontCloseServer()
        {
            Thread thread = new Thread(DontCloseServer1);
            thread.Start();
        }
        void DontCloseServer1()
        {
            while (true)
            {
                Thread.Sleep(1000000);
            }
        }
    }
}
