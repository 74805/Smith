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
        private int trump;
        private Card[] thisround;
        private int firstplayer;
        private int betstarterid = 0;
        private Card currenttopbet;
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
                try
                {
                    for (int i = 0; i < 4; i++)
                    {
                        clients[i].stream.Write(Card.SerializeArr(pcards[i].ToArray()));
                    }
                }
                catch
                {

                }
               
                GetTrump(0);
            }
        }
        void WaitForClient()
        {
            for (int i = 0; i < 4; i++)//get clients' names
            {
                TcpClient client = listener.AcceptTcpClient();

                byte[] data = new byte[256];

                try
                {
                    client.GetStream().Read(data, 0, data.Length);
                }
                catch
                {

                }

                string name = Encoding.ASCII.GetString(data);

                clients[i] = new Client(name.Substring(0, BackSlash0(name)), client, client.GetStream());

                try
                {
                    clients[i].stream.Write(Card.SerializeArr(pcards[i].ToArray()));
                }
                catch
                {

                }
            }

            for (int i = 0; i < 4; i++)//send the names of the other clients to a client
            {
                string sendnames = i.ToString();
                int namelengthlen = 0;//the amount of digits of the name's length

                for (int j = i + 1; j < i + 4; j++)
                {
                    int namelen = clients[j % 4].name.Length;
                    while (namelen != 0)
                    {
                        namelengthlen++;
                        namelen = namelen / 10;
                    }

                    sendnames += namelengthlen.ToString() + clients[j % 4].name.Length.ToString() + clients[j % 4].name;
                    namelengthlen = 0;
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
            GetTrump(0);
        }
        void ClientBetTrump(int clientid, int bet, int frischtimes)
        {
            Card card = new Card(1, (CardEnum)(bet % 10));//to get the name of the shape (line 96)
            for (int j = clientid % 4 + 1; j < clientid % 4 + 4; j++)//tell all clients what the client bet
            {
                currenttopbet = new Card(bet / 10, (CardEnum)(bet % 10));
                try
                {
                    clients[j % 4].stream.Write(Encoding.UTF8.GetBytes(clients[clientid].name));
                }
                catch
                {

                }
                SendCard(currenttopbet, j % 4);
            }

            for (int i = clientid + 1; i < clientid + 4; i++) 
            {
                try
                {
                    clients[i % 4].stream.Write(new byte[] { 0 });//send the client to make a bet on a trump
                }
                catch
                {

                }
                
                int newtrump = ReceiveInt(i % 4);//5 is pass, 4 is without trump and otherwise its the trump Enum
                while (newtrump / 10 == 0)
                {
                    
                    if (newtrump == 4 && currenttopbet.GetShape() != (CardEnum)4) //if its without trump
                    {
                        SendInt(currenttopbet.GetNum(), i % 4);
                    }
                    else
                    {
                        SendInt(MinCardNumPossible((CardEnum)newtrump, frischtimes), i % 4);
                    }
                    
                    newtrump = ReceiveInt(i % 4);//Final bet - number*10+trump if he bet and trump=5 if he passed, otherwise last digit is trump and other digits are bet,
                                              //4 is without trump and otherwise its the trump Enum
                }
                int newbet = newtrump;
                if (newbet % 10 != 5)
                {
                    ClientBetTrump(i % 4, newbet, frischtimes);
                    return;
                }
                else
                {
                    Card card1 = new Card(1, (CardEnum)(newbet % 10));

                    for (int j = i % 4 + 1; j < i % 4 + 4; j++)//tell all clients that the client passed
                    {
                        try
                        {
                            clients[j % 4].stream.Write(Encoding.UTF8.GetBytes(clients[i % 4].name));
                        }
                        catch
                        {

                        }
                        
                        SendCard(card1, j % 4);
                    }
                }
            }
            trump = bet % 10;
            firstplayer = clientid;
        }

        void GetTrump(int frishtimes)
        {
            currenttopbet = new Card();
            trump = -1;
            for (int i = betstarterid; i < betstarterid + 4; i++) 
            {
                try
                {
                    clients[i % 4].stream.Write(new byte[] { 0 });//send the client to make a bet on a trump
                }
                catch
                {

                }

                int trump = ReceiveInt(i % 4);//5 is pass, 4 is without trump and otherwise its the trump Enum
                while (trump / 10 == 0)
                {
                    if (currenttopbet.GetNum() == 0)
                    {
                        SendInt(5 + frishtimes, i % 4);//first one to bet
                    }
                    else
                    {
                        if (trump == 4 && currenttopbet.GetShape() != (CardEnum)4) //if its without trump
                        {
                            SendInt(currenttopbet.GetNum(), i % 4);
                        }
                        else
                        {
                            SendInt(MinCardNumPossible((CardEnum)trump, frishtimes), i % 4);
                        }
                    }

                    trump = ReceiveInt(i % 4);//Final bet - number*10+trump if he bet and trump=5 if he passed, otherwise last digit is trump and other digits are bet,
                                              //4 is without trump and otherwise its the trump Enum
                }

                int bet = trump;

                if (trump % 10 != 5) 
                {
                    ClientBetTrump(i % 4, bet, frishtimes);
                    break;
                }
                else
                {
                    Card card = new Card(1, (CardEnum)(bet % 10));

                    for (int j = i % 4 + 1; j < i % 4 + 4; j++)//tell all clients that the client passed
                    {
                        try
                        {
                            clients[j % 4].stream.Write(Encoding.UTF8.GetBytes(clients[i % 4].name));
                        }
                        catch
                        {

                        }
                        SendCard(card, j % 4);
                    }
                }
            }

            byte[] data;
            bool isfrish = false;
            bool newgame = false;
            if (trump == -1)//frish
            {
                if (frishtimes == 3)
                {
                    newgame = true;
                    data = Encoding.UTF8.GetBytes("c");//tell all clients to start over the game (frish is limited to 3 times)
                }
                else
                {
                    isfrish = true;
                    data = Encoding.UTF8.GetBytes("a");//tell all clients that its frish
                }
                
            }
            else
            {
                data = Encoding.UTF8.GetBytes("b");//tell all clients to bet
            }
            try
            {
                for (int i = 0; i < 4; i++)
                {
                    clients[i].stream.Write(data, 0, data.Length);
                }
            }
            catch
            {

            }
           
            if (isfrish)
            {
                GetFrishCards();
                GetTrump(frishtimes + 1);
            }
            else
            {
                Thread.Sleep(150);
                if (newgame) 
                {
                    StartServer(false);//New Game
                }
                else
                {
                    GetAndSendBets();
                }
            }
        }
       
        int MinCardNumPossible(CardEnum trump, int frischtimes)
        {
            if (currenttopbet.GetShape() == (CardEnum)4)//if its no trump
            {
                return currenttopbet.GetNum() + 1;
            }

            Card card = new Card(5, trump);
            for (int i = 5 + frischtimes; i < 14; i++) 
            {
                card.SetNum(i);
                if (card.CompareToBet(currenttopbet)==1)
                {
                    return i;
                }
            }
            return 14;
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
            try
            {
                for (int i = 0; i < 4; i++)
                {
                    clients[i].stream.Write(new byte[] { 0 });
                }
            }
            catch
            {

            }
            
            threads = null;
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

        public void SendInt(int num, int clientid)
        {
            try
            {
                clients[clientid].stream.Write(new byte[1] { (byte)num }, 0, 1);
            }
            catch
            {

            }
        }
       
        void GetAndSendBets()
        {
            for (int j = 0; j < 4; j++)
            {
                SendInt((firstplayer + 4 - j - 1) % 4, j);//send the firstplayer to bet
            }

            for (int i = firstplayer; i < firstplayer + 4; i++)
            {
                clients[i % 4].bet = ReceiveInt(i % 4);
                for (int j = i % 4 + 1; j < i % 4 + 4; j++)
                {
                    SendString(clients[i % 4].bet.ToString(), j % 4);
                }
            }

            //byte[] data;
            //for (int i = 0; i < 4; i++)
            //{
            //    string bets = "";
            //    for (int j = i + 1; j < i + 4; j++)
            //    {
            //        string thisbet = clients[j % 4].bet.ToString();
            //        bets += thisbet.Length.ToString() + thisbet;
            //    }
            //    data = Encoding.UTF8.GetBytes(bets);
            //    clients[i].stream.Write(data, 0, data.Length);
            //}
            StartGame();
        }
        void StartGame()
        {

            //for (int j = 0; j < 4; j++)
            //{
            //    SendInt((firstplayer + 4 - j - 1) % 4, j);//send the firstplayer when a new round starts
            //}
            
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
                            try
                            {
                                clients[j % 4].stream.Write(Encoding.UTF8.GetBytes("a"));
                            }
                            catch
                            {

                            }
                            break;
                        }
                        else
                        {
                            try
                            {
                                clients[j % 4].stream.Write(Encoding.UTF8.GetBytes("b"));
                            }
                            catch
                            {

                            }
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
                    try
                    {
                        clients[firstplayer].stream.Read(data, 0, 1);
                        for (int j = firstplayer + 1; j < firstplayer + 4; j++)
                        {
                            clients[j % 4].stream.Write(new byte[] { 0 }, 0, 1);
                        }
                    }
                    catch
                    {

                    }
                }
            }
            byte[] data1 = new byte[1];

            try
            {
                clients[firstplayer].stream.Read(data1, 0, 1);
                for (int i = firstplayer + 1; i < firstplayer + 4; i++)
                {
                    clients[i % 4].stream.Write(Encoding.UTF8.GetBytes("a"), 0, 1);
                }
            }
            catch
            {

            }

            StartServer(false);//New Game

        }
        int GetWinner(Card[] cards)
        {
            int max = firstplayer;

            for (int i = firstplayer + 1; i < firstplayer + 4; i++) 
            {
                if (cards[i%4].GetShape() == cards[max].GetShape())
                {
                    if (cards[i % 4].GetNum() > cards[max].GetNum())
                    {
                        max = i % 4;
                    }
                    continue;
                }
                if ((int)cards[i % 4].GetShape() == trump && (int)cards[max].GetShape() != trump)
                {
                    max = i % 4;
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
            try
            {
                clients[clientid].stream.Read(data, 0, data.Length);
            }
            catch
            {

            }
            

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
        public int ReceiveInt(int clientid)
        {
            byte[] buffer = new byte[1];
            try
            {
                clients[clientid].stream.Read(buffer, 0, 1);
            }
            catch
            {

            }
            return (int)buffer[0];
        }
        void SendCard(Card card,int clientid)
        {
            byte[] data = Card.Serialize(card);
            try
            {
                clients[clientid].stream.Write(data);
            }
            catch
            {

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
        void SendString(string tosend, int clientid)
        {
            byte[] data = Encoding.UTF8.GetBytes(tosend);
            try
            {
                clients[clientid].stream.Write(data, 0, data.Length);
            }
            catch
            {

            }
        }
        void SendCardArr(Card[] cards, int clientid)
        {
            byte[] data = Card.SerializeArr(cards);
            try
            {
                clients[clientid].stream.Write(data);
            }
            catch
            {

            }
            
        }
    }
}
