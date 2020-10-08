﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

namespace WhistServer
{
    class Server
    {
        private TcpListener listener;
        private Thread waitforclients;
        private Client[] clients = new Client[4];
        private List<Card>[] pcards = new List<Card>[4];
        private string[] names = new string[4];
        private int trump;
        public Server()
        {
            Packet packet = new Packet();
            packet.Shuffle();

            Card[][] pcards = packet.GetPcards();

            int a = (int)pcards[0][3].GetShape();
            listener = new TcpListener(IPAddress.Any, 7986);
            listener.Start();

            for (int i = 0; i < 4; i++)
            {
                this.pcards[i] = pcards[i].ToList();
            }

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

                clients[i].stream.Write(Card.SerializeArr(pcards[i].ToArray()));
            }

            for (int i = 0; i < 4; i++)//send the names of the other players to a player
            {
                string sendnames = i.ToString();
                for (int j = i + 1; j < i + 4; j++)
                {
                    sendnames += names[j % 4].Length.ToString() + names[j % 4];
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
            bool isfrish = false;
            if (trump == 5)//frish
            {
                isfrish = true;
                data = Encoding.UTF8.GetBytes("a");//tell all clients that its frish
            }
            else
            {
                if (trump == 6)
                {
                    data = Encoding.UTF8.GetBytes("c");//tell all clients to start over the game
                }
                else
                {
                    data = Encoding.UTF8.GetBytes("b");//tell all clients to bet

                }
            }
            for (int i = 0; i < 4; i++)
            {
                clients[i].stream.Write(data, 0, data.Length);
            }
            if (isfrish)
            {
                GetFrishCards();
            }
            else
            {
                if (trump == 6)
                {
                    NewGame();
                }
                else
                {
                    
                    GetAndSendBets();
                }
            }
        }
       
        void GetFrishCards()
        {
            Card[][] frishcards = new Card[4][];
            
            for (int i = 0; i < 4; i++)
            {
                frishcards[i] = RecieveCardArr(3, i);
                
                foreach (Card card in frishcards[i])
                {
                    foreach (Card card1 in pcards[i].ToArray())
                    {
                        if (card.GetNum()==card1.GetNum()&& card.GetShape() == card1.GetShape())
                        {
                            pcards[i].Remove(card1);
                        }
                    }
                }
            }

            for (int i = 0; i < 4; i++)//send each player the cards that he got
            {
                SendCardArr(frishcards[i == 0 ? 3 : i - 1], i);
            }
            Thread.Sleep(100);

            Thread[] threads = new Thread[4];
            for (int i = 0; i < 4; i++)
            {
                threads[i] = new Thread(SendIndex);
                threads[i].Start(i);
            }
            bool areallplayersdone = true;
            while (areallplayersdone)
            {
                areallplayersdone = !(pcards[0].Count == 13 && pcards[1].Count == 13 && pcards[2].Count == 13 && pcards[3].Count == 13); 
            }
            for (int i = 0; i < 4; i++)
            {
                clients[i].stream.Write(new byte[] { 0 });
            }
            threads = null;
            GetTrump();
        }
        void SendIndex(object clientid)
        {
            int id = (int)clientid;

            for (int i = 0; i < 3; i++)
            {
                Card findindex = RecieveCard(id);
                pcards[id].Add(findindex);
                pcards[id].Sort();

                int index;

                for (int j = 0; j < pcards[id].Count; j++)
                {
                    if (findindex.GetNum()==pcards[id][j].GetNum()&& findindex.GetShape() == pcards[id][j].GetShape())
                    {
                        index = j;

                        SendInt(index,id);
                    }
                }
            }
        }
        void NewGame()//start a new game
        {

        }
        public void SendInt(int num,int clientid)
        {
            clients[clientid].stream.Write(new byte[1] { (byte)num }, 0, 1);
        }
       
        void GetAndSendBets()
        {
            for (int i = 0; i < 4; i++)
            {
                clients[i].bet = ReciveInt(i);
            }

            byte[] data;
            for (int i = 0; i < 4; i++)
            {
                string bets = "";
                for (int j = i + 1; j < i + 4; j++)
                {
                    string thisbet = clients[j % 4].bet.ToString();
                    bets += thisbet.Length.ToString() + thisbet;
                }
                data = Encoding.UTF8.GetBytes(bets);
                clients[i].stream.Write(data, 0, data.Length);
            }
            StartGame();
        }
        void StartGame()
        {
            int firstplayer = trump % 10;
            trump = trump / 10;
            for (int i = 0; i < 13; i++)//13 rounds of game
            {
                Card[] thisround = new Card[4];
                for (int j = 0; j < 4; j++)
                {
                    SendInt((firstplayer+4-j-1)%4, j);//send the firstplayer when a new round starts
                }

                for (int j = firstplayer; j < firstplayer + 4; j++)//one turn to each player
                {
                    thisround[j % 4] = RecieveCard(j % 4);

                    for (int k = j % 4+1 ; k < j % 4 +4; k++)
                    {
                        SendCard(thisround[j % 4], k % 4);
                    }
                }
            }
        }
        Card RecieveCard(int clientid)
        {
            byte[] data = new byte[8];
            clients[clientid].stream.Read(data, 0, data.Length);

            Card card = Card.Desserialize(data);
            return card;
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
        void SendCard(Card card,int clientid)
        {
            byte[] data = Card.Serialize(card);
            clients[clientid].stream.Write(data);
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
        Card[] RecieveCardArr(int length, int clientid)
        {
            byte[] data = new byte[8 * length];
            try
            {
                clients[clientid].stream.Read(data, 0, data.Length);
            }
            catch
            {

            }

            Card[] cards = Card.DesserializeArr(data);
            return cards;
        }
        void SendCardArr(Card[] cards, int clientid)
        {
            byte[] data = Card.SerializeArr(cards);
            clients[clientid].stream.Write(data);
        }
    }
}
