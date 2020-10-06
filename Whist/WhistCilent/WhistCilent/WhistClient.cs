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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WhistServer;

namespace WhistCilent
{
    delegate void del();
    public partial class WhistClient : Form
    {
        private TcpClient client;
        private NetworkStream stream;
        private List<Card> hand;
        private List<Label> visHand;
        private List<Label>[] othercards;
        Label score;
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
            Shown += CreateCards;

            Thread thread = new Thread(PlaceNames);
            //put names on screes yes?
            thread.Start();
        }
        void CreateCards(object sender,EventArgs args)
        {
            othercards = new List<Label>[3];
            for (int i = 0; i < 3; i++)
            {
                othercards[i] = new List<Label>();

                Label label = new Label();
                Image image = (Image)Properties.Resources.ResourceManager.GetObject(i.ToString());
                label.Image = i == 1 ? Resize(image, (int)(this.Width / 24.3125), (int)(this.Height / 8.95)) : Resize(image, (int)(this.Height / 8.95), (int)(this.Width / 24.3125));
                label.Size = label.Image.Size;

                if (i == 0)
                {
                    label.Location = new Point(Width / 15, (int)(Height /5));
                }
                else
                {
                    if (i == 1)
                    {
                        label.Location = new Point((int)(Width / 3.6), Height / 20);
                    }
                    else
                    {
                        label.Location = new Point(Width - Width / 15 - label.Size.Width, (int)(Height /5));
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
                        Label label1 = new Label();
                        label1.Image = othercards[j][0].Image;
                        label1.Size = othercards[j][0].Size;
                        label1.Location = j == 1 ? new Point(othercards[j][0].Location.X + (int)(i * 0.8 * label1.Size.Width), othercards[j][0].Location.Y) : new Point(othercards[j][0].Location.X, othercards[j][0].Location.Y + (int)(0.65 * label1.Size.Height) * i);

                        Controls.Add(label1);
                        othercards[j].Add(label1);
                    }
                }

            }
            
        }
        void PlaceNames()
        {
            byte[] data = new byte[256];

            stream.Read(data, 0, data.Length);//getting other players' names

            string names = Encoding.UTF8.GetString(data);

            Label[] otherplayers = new Label[3]; //Creating labels for other players' name and score

            for (int i = 0; i < 3; i++)
            {
                string temp = "";
                int namelen;
                try
                {
                    namelen = int.Parse(names.Substring(0,2));
                    names = names.Substring(2);
                }
                catch
                {
                    namelen = (int)names[1] - 48;
                    names = names.Substring(1);
                }

                for (int j = 0; j < namelen; j++)
                {
                    temp += names[0];
                    names = names.Substring(1);
                }

                otherplayers[i] = new Label();
                otherplayers[i].Size = new Size(Width / 15, Height / 20);
                otherplayers[i].Font = new Font("Ariel", 14);
                otherplayers[i].Text = temp + '\n' + "Score: 0";
            }
            otherplayers[0].Location = new Point(0, Height / 2 - otherplayers[0].Size.Height / 2);
            otherplayers[1].Location = new Point(Width / 2 - otherplayers[1].Size.Width / 2, 0);
            otherplayers[2].Location = new Point(Width - otherplayers[2].Size.Width, Height / 2 - otherplayers[2].Size.Height / 2);

            score = new Label();
            score.Size= new Size(Width / 15, Height / 20);
            score.Location = new Point(Width / 2 - score.Size.Width / 2, Height - (int)(0.6*score.Size.Height));
            score.Font = new Font("Ariel", 14);
            score.Text = "Score: 0";
            this.Invoke(new del(() =>
            {
                Controls.Add(score);
                for (int i = 0; i < 3; i++)
                {
                    Controls.Add(otherplayers[i]);
                }
            }));
            
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
