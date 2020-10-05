using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
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
        private List<Card> hand;
        private List<Label> visHand;
        private List<Label>[] othercards;
        public WhistClient()
        {
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;

            this.Height = Screen.PrimaryScreen.Bounds.Height;
            this.Width = Screen.PrimaryScreen.Bounds.Width;

            client = new TcpClient("localhost", 7986);
            stream = client.GetStream();

            byte[] data = Encoding.UTF8.GetBytes(Environment.UserName); //save the user name string in bytes array
            stream.Write(data, 0, data.Length); //send the data array
            byte[] data1 = new byte[104]; 
            stream.Read(data1, 0, data1.Length); //recive the cards from the server

            Card[] temp = Card.DesserializeArr(data1); //parse the cards that has been recived
            hand = temp.ToList(); //create List of Cards from the Card array

            hand = temp.ToList();

            //creating a visual form of the cards
            CreateCards();
        }
        void CreateCards()
        {
            othercards = new List<Label>[3];
            for (int i = 0; i < 3; i++)
            {
                othercards[i] = new List<Label>();

                Label label = new Label();
                Image image = (Image)Properties.Resources.ResourceManager.GetObject(i.ToString());
                label.Image = i == 1 ? Resize(image, (int)(this.Width / 24.3125), (int)(this.Height / 8.95)) : Resize(image, (int)(this.Height / 8.95), (int)(this.Width / 24.3125)) ;
                label.Size = label.Image.Size;
                
                if (i == 0)
                {
                    label.Location = new Point(Width / 20, Height / 10);
                }
                else
                {
                    if (i == 1)
                    {
                        label.Location = new Point(Width / 5, Height / 30);
                    }
                    else
                    {
                        label.Location = new Point(Width-Width/20-label.Size.Width, Height / 10);
                    }
                }
                

                Controls.Add(label);
                othercards[i].Add(label);
            }
            
            visHand = new List<Label>();

            for (int i = 0; i < 13; i++)
            {
                Label label = new Label();
                Image image = (Image)Properties.Resources.ResourceManager.GetObject(hand[i].GetNum().ToString() + ((int)hand[i].GetShape()).ToString());
                label.Image = Resize(image, (int)(this.Width / 19.45), (int)(this.Height / 7.1591));
                label.Size = label.Image.Size;
                label.Location = new Point((int)(this.Width / 6.3) + (int)(i * label.Size.Width * 1), 4 * this.Height / 5);

                Controls.Add(label);
                visHand.Add(label);

                if (i != 0)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        Label label1 =new Label();
                        label1.Image = othercards[j][0].Image;
                        label1.Size = othercards[j][0].Size;
                        label1.Location = j == 1 ? new Point(othercards[j][0].Location.X + i * label1.Size.Width, othercards[j][0].Location.Y) : new Point(othercards[j][0].Location.X, othercards[j][0].Location.Y + (int)(0.8 * label1.Size.Height) * i);

                        Controls.Add(label1);
                        othercards[j].Add(label1);
                    }
                }

            }
        }
        public Image Resize(Image image, int w, int h)
        {
            Bitmap bmp = new Bitmap(w, h);
            Graphics grp = Graphics.FromImage(bmp);
            grp.DrawImage(image, 0, 0, w, h);
            grp.Dispose();

            return bmp;
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
      
    }
}
