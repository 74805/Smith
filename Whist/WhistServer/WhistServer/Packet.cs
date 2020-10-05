using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhistServer
{
    [Serializable]
    public class Packet
    {
        private Card[] cards;
        public Packet()
        {
            cards = new Card[52];

            for (int i = 0; i < 13; i++)
            {
                this.cards[i] = (new Card(i + 2, CardEnum.heart));
                this.cards[i + 13] = (new Card(i + 2, CardEnum.spade));
                this.cards[i + 26] = (new Card(i + 2, CardEnum.diamond));
                this.cards[i + 39] = (new Card(i + 2, CardEnum.club));
            }
        }
        public Card[] GetCards()
        {
            return cards;
        }
        public void Shuffle()
        {
            Random random = new Random();
            for (int i = 0; i < 52; i++)
            {
                int ind = random.Next(i, 52);
                Card temp = cards[i];
                cards[i] = cards[ind];
                cards[ind] = temp;
            }
        }

        public Card[][] GetPcards()
        {
            Card[][] pcards = new Card[4][];
            for (int i = 0; i < 4; i++)
            {
                pcards[i] = new Card[13];
            }
            for (int i = 0; i < 13; i++)
            {
                pcards[0][i]=(cards[i]);
                pcards[1][i]=(cards[i + 13]);
                pcards[2][i] = (cards[i + 26]);
                pcards[3][i] = (cards[i + 39]);
            }

            //sorting the cards the players

            List<Card>[] list = new List<Card>[4];
            for(int i = 0; i < 4; i++)
            {
                list[i] = pcards[i].ToList();
                list[i].Sort();
                pcards[i] = list[i].ToArray();
            }
            return pcards;
        }
    }
}
