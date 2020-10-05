using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WhistServer;

namespace WhistCilent
{
    public partial class WhistClient : Form
    {
        private TcpClient client;
        private NetworkStream stream;
        private List<Card> cards;
        public WhistClient()
        {
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;

            client = new TcpClient("localhost", 7986);
            stream = client.GetStream();

            byte[] data = Encoding.UTF8.GetBytes(Environment.UserName);
            stream.Write(data, 0, data.Length);

            cards = new List<Card>();

            byte[] data1 = new byte[256];
            stream.Read(data1, 0, data1.Length);

            Card[] temp = Card.DesserializeArr(data1);

           // cards = temp.ToList();
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F11)
            {
                if (FormBorderStyle == FormBorderStyle.Sizable)
                {
                    FormBorderStyle = FormBorderStyle.None;
                    WindowState = FormWindowState.Maximized;
                }
                else
                {
                    FormBorderStyle = FormBorderStyle.Sizable;
                    WindowState = FormWindowState.Normal;
                }
            }
            return false;
        }

        private void WhistClient_Load(object sender, EventArgs e)
        {

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

        public static Object ByteArrayToObject(byte[] arrBytes)
        {
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(arrBytes, 0, arrBytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                object obj = binForm.Deserialize(memStream);
                return obj;
            }
        }
    }
}
