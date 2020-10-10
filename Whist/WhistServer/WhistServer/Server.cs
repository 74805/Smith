using System;
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
        private Client[] clients = new Client[4];
        private List<Card>[] pcards = new List<Card>[4];
        private string[] names = new string[4];
        private int trump;
        private Card[] thisround;
        private int firstplayer;
        public Server()
        {
            listener = new TcpListener(IPAddress.Any, 7986);
            listener.Start();

            StartServer(true);
        }
        void StartServer(bool isfirstgame)
        {
            Packet packet = new Packet();
            packet.Shuffle();

            Card[][] pcards = packet.GetPcards();

            int a = (int)pcards[0][3].GetShape();
           
            for (int i = 0; i < 4; i++)
            {
                this.pcards[i] = pcards[i].ToList();
            }
            if (isfirstgame)
            {
                WaitForClient();
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    clients[i].stream.Write(Card.SerializeArr(pcards[i].ToArray()));
                }
                GetTrump();
            }
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

            StartServer(false);
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
            firstplayer = trump % 10;
            trump = trump / 10; 

            for (int j = 0; j < 4; j++)
            {
                SendInt((firstplayer + 4 - j - 1) % 4, j);//send the firstplayer when a new round starts
            }

            for (int i = 0; i < 13; i++)//13 rounds of game
            {
                thisround = new Card[4];

                for (int j = firstplayer; j < firstplayer + 4; j++)//one turn to each player
                {
                    while (true)
                    {
                        thisround[j % 4] = RecieveCard(j % 4);

                        if (IsValid(thisround[j % 4], j % 4))
                        {
                            RemoveCard(thisround[j % 4], j % 4);
                            clients[j % 4].stream.Write(Encoding.UTF8.GetBytes("a"));
                            break;
                        }
                        else
                        {
                            clients[j % 4].stream.Write(Encoding.UTF8.GetBytes("b"));
                        }
                    }

                    for (int k = j % 4+1 ; k < j % 4 +4; k++)
                    {
                        SendCard(thisround[j % 4], k % 4);
                    }
                }
                firstplayer = GetWinner(thisround);

                for (int j = 0; j < 4; j++)
                {
                    SendInt((firstplayer + 4 - j - 1) % 4, j);//send the winner when a round ends
                }

                if (i < 12)
                {
                    byte[] data = new byte[1];
                    clients[firstplayer].stream.Read(data, 0, 1);

                    for (int j = firstplayer + 1; j < firstplayer + 4; j++)
                    {
                        clients[j % 4].stream.Write(new byte[] { 0 }, 0, 1);
                    }
                }
            }
            byte[] data1 = new byte[1];
            clients[firstplayer].stream.Read(data1, 0, 1);

            for (int i = firstplayer + 1; i < firstplayer + 4; i++)
            {
                clients[i % 4].stream.Write(Encoding.UTF8.GetBytes("a"),0,1);
            }

            NewGame();

        }
        int GetWinner(Card[] cards)
        {
            int max = 0;

            for (int i = 1; i < 4; i++)
            {
                if (cards[i].GetShape() == cards[max].GetShape())
                {
                    if (cards[i].GetNum() > cards[max].GetNum())
                    {
                        max = i;
                        continue;
                    }
                    continue;
                }
                if ((int)cards[i].GetShape() == trump && (int)cards[max].GetShape() != trump)
                {
                    max = i;
                    continue;
                }
                if ((int)cards[i].GetShape() != trump && (int)cards[max].GetShape() == trump)
                {
                    continue;
                }
            }
            return max;
        }
        void RemoveCard(Card card,int id)
        {
            foreach (Card card1 in pcards[id])
            {
                if (card1.GetShape()==card.GetShape()&& card1.GetNum() == card.GetNum())
                {
                    pcards[id].Remove(card1);
                    return;
                }
            }
        }
        bool IsValid(Card card, int clientid)
        {

            Card first = thisround[firstplayer];

            if (card.GetShape() == first.GetShape())
            {
                return true;
            }
            foreach (Card card1 in pcards[clientid])
            {
                if (card1.GetShape() == first.GetShape())
                {
                    return false;
                }
            }
            return true;
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
