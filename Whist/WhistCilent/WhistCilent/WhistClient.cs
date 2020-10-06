﻿using System;
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
        private int clientid;
        private TcpClient client;
        private NetworkStream stream;
        private List<Card> hand;
        private List<Label> visHand;
        private List<Label>[] othercards;
        Label[] score;
        private Button[] choosetrump;
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
        void CreateCards(object sender, EventArgs args)
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
                    label.Location = new Point(Width / 15, (int)(Height / 5));
                }
                else
                {
                    if (i == 1)
                    {
                        label.Location = new Point((int)(Width / 3.6), Height / 20);
                    }
                    else
                    {
                        label.Location = new Point(Width - Width / 15 - label.Size.Width, (int)(Height / 5));
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
                label.Location = new Point((int)(this.Width / 2 - 6.5 * label.Size.Width) + (int)(i * label.Size.Width * 1), 4 * this.Height / 5);

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
        void GetTrump()
        {

            choosetrump = new Button[6];

            for (int i = 0; i < 6; i++)
            {
                choosetrump[i] = new Button();
                if (i < 4)
                {
                    Image image = (Image)Properties.Resources.ResourceManager.GetObject((10 + i).ToString());
                    choosetrump[i].Image = Resize(image, (int)(0.055339 * Width), (int)(0.09838 * Height));
                }
                else
                {
                    choosetrump[i].Font = new Font("Ariel", 14);
                    choosetrump[i].Text = i == 4 ? "ללא שליט" : "פריש";
                }
                choosetrump[i].Size = choosetrump[0].Image.Size;
                choosetrump[i].Location = new Point(Width / 2 + (i - choosetrump.Length / 2) * choosetrump[i].Size.Width, 4 * Height / 7);
                choosetrump[i].Tag = i;
                choosetrump[i].Click += TrumpClick;

                this.Invoke(new del(() =>
                {
                    Controls.Add(choosetrump[i]);
                }));
            }

        }
        void TrumpClick(object sender, EventArgs args)
        {
            Button trump = (Button)sender;

            int send = (int)trump.Tag;

            for (int i = 0; i < 6; i++)
            {
                Controls.Remove(choosetrump[i]);
            }
            if (send != 5)
            {
                choosetrump = null;
            }

            SendInt(send);
            
        }
        public void SendInt(int num)
        {
            this.stream.Write(new byte[1] { (byte)num }, 0, 1);
        }
        void Frish()
        {

        }
        void PlaceNames()
        {
            byte[] data = new byte[256];

            stream.Read(data, 0, data.Length);//getting other players' names

            string names = Encoding.UTF8.GetString(data);

            clientid = (int)names[0] - 48;
            names = names.Substring(1);
            score = new Label[4]; //Creating labels for other players' name and score

            for (int i = 0; i < 3; i++)
            {
                string temp = "";
                int namelen;
                try
                {
                    namelen = int.Parse(names.Substring(0, 2));
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

                score[i] = new Label();
                score[i].Size = new Size(Width / 15, Height / 20);
                score[i].Font = new Font("Ariel", 14);
                score[i].Text = temp + '\n' + "Score: 0";
            }
            score[0].Location = new Point(0, Height / 2 - score[0].Size.Height / 2);
            score[1].Location = new Point(Width / 2 - score[1].Size.Width / 2, 0);
            score[2].Location = new Point(Width - score[2].Size.Width, Height / 2 - score[2].Size.Height / 2);

            score[3] = new Label();
            score[3].Size = new Size(Width / 15, Height / 20);
            score[3].Location = new Point(Width / 2 - score[3].Size.Width / 2, Height - (int)(0.6 * score[3].Size.Height));
            score[3].Font = new Font("Ariel", 14);
            score[3].Text = "Score: 0";
            this.Invoke(new del(() =>
            {
                for (int i = 0; i < 4; i++)
                {
                    Controls.Add(score[i]);
                }
            }));
            if (clientid == 0)
            {
                GetTrump();
            }
            byte[] data1 = new byte[256];
            stream.Read(data1, 0, data1.Length);

            char isfrish = Encoding.UTF8.GetString(data1)[0];

            if (isfrish == 'a')
            {
                Frish();
            }
            else
            {
                GetBet();
            }
        }
        void GetBet()
        {
            choosetrump = new Button[14];
            for (int i = 0; i < 14; i++)
            {
                choosetrump[i] = new Button();
                choosetrump[i].Size = new Size(Width / 20, Width / 20);
                choosetrump[i].Text = i.ToString();
                choosetrump[i].Location = new Point(Width / 2 + (-7 + i) * Width / 20, 3 * Height / 5);
                choosetrump[i].Click += GotBet;

                this.Invoke(new del(() =>
                {
                    Controls.Add(choosetrump[i]);
                }));
            }
        }
        void GotBet(object sender, EventArgs args)
        {
            Button bet = (Button)sender;

            int send = int.Parse(bet.Text);

            score[3].Text += "/" + send;

            for (int i = 0; i < 14; i++)
            {
                Controls.Remove(choosetrump[i]);
                choosetrump[i] = null;
            }
            choosetrump = null;

            SendInt(send);

            //getting other's bets
            byte[] data = new byte[36];
            stream.Read(data, 0, data.Length);

            string othersbets = Encoding.UTF8.GetString(data);
            string[] bets = new string[3];
            for (int i = 0; i < 3; i++)
            {
                int len = (int)othersbets[0] - 48;

                bets[i] = othersbets.Substring(1, len);
                score[i].Text += "/" + bets[i];
                othersbets = othersbets.Substring(len + 1);
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
