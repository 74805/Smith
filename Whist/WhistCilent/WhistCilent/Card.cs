using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WhistServer
{
    [Serializable]
    public class Card
    {
        private int num;
        private string shape;

        public Card(int num, string shape)
        {
            this.num = num;
            this.shape = shape;
        }
        public Card()
        {

        }
        public Card(Card other)
        {
            this.num = other.num;
            this.shape = other.shape;
        }
        public int GetNum()
        {
            return this.num;
        }

        public string GetShape()
        {
            return this.shape;
        }

        public void SetNum(int num)
        {
            this.num = num;
        }

        public void SetShape(string shape)
        {
            this.shape = shape;
        }

        public override string ToString()
        {
            return this.num + " - " + this.shape;
        }

        public byte[] Serialize()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(num);
                    writer.Write(shape);
                }
                return m.ToArray();
            }
        }

        public static Card Desserialize(byte[] data)
        {
            Card result=new Card();
            using (MemoryStream m = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    result.num = reader.ReadInt32();
                    result.shape = reader.ReadString();
                }
            }
            return result;
        }
        

    }
}
