using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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
        private Label[] score;
        private Button[] choosetrump;
        private Label[,] frishcards;
        private int currentturn;
        private Label[] thisround = new Label[4];
        private int firstturn;
        private Thread otherscards;
        private Label thisturn;
        private Button win;
        private Thread nextound;
        private bool cardenable;
        private ChatClient chatclient = null;
        private Label centertext;
        private Button[] betnumbers;
        private Thread betting;
        private int bet = 0;
        public WhistClient()
        {
            this.FormClosed += (sender, e) => { Environment.Exit(Environment.ExitCode); };

            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;

            this.Height = Screen.PrimaryScreen.Bounds.Height;
            this.Width = Screen.PrimaryScreen.Bounds.Width;

            Button chat = new Button();
            chat.Text = "Open chat";
            chat.Font = new Font("Arial", Height / 108);
            chat.Location = new Point(0, 0);
            chat.Size = new Size(Width / 20, Height / 30);
            chat.Click += (object sender, EventArgs e) =>
            {
                try
                {
                    chatclient.BringToFront();//if the chat was open and its closed now
                    chatclient.Show();
                }
                catch
                {
                    try
                    {//if the chat was never open
                        chatclient = new ChatClient();
                        chatclient.Show();
                        chatclient.BringToFront();
                    }
                    catch
                    {
                        MessageBox.Show("Chat server is offline");
                        if (chatclient != null)
                        {
                            chatclient.Close();
                        }
                    }
                }
            };
            Controls.Add(chat);

            client = new TcpClient("77.139.114.151", 7986);
            stream = client.GetStream();

            cardenable = true;

            othercards = new List<Label>[3];

            byte[] data = Encoding.UTF8.GetBytes(Environment.UserName); //save the user name string in bytes array
            stream.Write(data, 0, data.Length); //send the name to the server

            Shown += CreateCards;//creating a visual form of the cards
        }

        void CreateCards(object sender, EventArgs args)
        {
            byte[] data1 = new byte[104];
            try
            {
                stream.Read(data1, 0, data1.Length); //receive the cards from the server
            }
            catch
            {
                MessageBox.Show("Server is offline");
            }

            Card[] temp = Card.DesserializeArr(data1); //parse the cards that has been Received
            hand = temp.ToList(); //create list of cards from the card array


            try
            {
                string a = (string)sender;//if sender is null then it is the first game
                betting = new Thread(BeforeGetBet);
            }
            catch
            {
                betting = new Thread(PlaceNames);
                //put names on screes
            }

            betting.Start();

            
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
                label.Location = new Point(Width / 2 - 13 * label.Size.Width / 2 - 3 * Width / 400 + i * (Width / 800 + label.Size.Width), 4 * this.Height / 5);
                label.Tag = hand[i];

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
        void GetTrump()//this client's turn to make a bet on a trump
        {
            betnumbers = new Button[9];//buttons of the bets
            choosetrump = new Button[6];//buttons of the possible trumps

            for (int i = 0; i < betnumbers.Length; i++)
            {
                if (i < 6)
                {
                    choosetrump[i] = new Button();
                    if (i < 4)
                    {
                        Image image = (Image)Properties.Resources.ResourceManager.GetObject((10 + i).ToString());
                        choosetrump[i].Image = Resize(image, (int)(0.055339 * Width), (int)(0.09838 * Height));
                    }
                    else
                    {
                        choosetrump[i].Font = new Font("Arial", 7 * Width / 960);
                    }
                    if (i == 2)
                    {
                        choosetrump[2].Tag = 3;
                    }else
                    {
                        if (i == 3)
                        {
                            choosetrump[3].Tag = 2;
                        }
                        else
                        {
                            choosetrump[i].Tag = i;
                        }
                    }
                    
                    choosetrump[i].Size = choosetrump[0].Image.Size;
                    choosetrump[i].Location = new Point(Width / 2 - 3 * choosetrump[0].Size.Width - Width / 160 + i * (Width / 400 + choosetrump[0].Size.Width), 4 * Height / 7 + choosetrump[0].Image.Size.Height + 5);
                    choosetrump[i].Click += TrumpClick;

                    Controls.Add(choosetrump[i]);

                }

                betnumbers[i] = new Button();
                betnumbers[i].Font = new Font("Arial", 7 * Width / 960);
                betnumbers[i].Size = new Size(Width / 20, Width / 20);
                betnumbers[i].Location = new Point(Width / 2 - 9 * betnumbers[0].Size.Width / 2 - Width / 100 + i * (Width / 400 + betnumbers[0].Size.Width), 4 * Height / 7);
                betnumbers[i].Enabled = false;
                betnumbers[i].Click += BetClick;
                betnumbers[i].Tag = (i + 5);
                if (i < 9)
                {
                    betnumbers[i].Text = (i + 5).ToString();
                }

                Controls.Add(betnumbers[i]);
            }

            win = new Button();
            win.Font = new Font("Arial", 7 * Width / 960);
            win.Text = "Submit";
            win.Tag = 0;
            win.Size = new Size(5 * Width / 96, 5 * Height / 54);
            win.Location = new Point(Width / 2 - win.Size.Width / 2, 4 * Height / 9);
            win.Enabled = false;
            win.Click += SubmitClick;//WinnerClick;
            
            choosetrump[4].Text = "ללא שליט";
            choosetrump[5].Text = "Pass";
            Controls.Add(win);
            
        }
        void SubmitClick(object sender, EventArgs args)
        {
            int tosend = (int)win.Tag;//betnumber*10+trumpnumber
            bet = tosend / 10;
            if (tosend / 10 == 0)
            {
                tosend = 10 + tosend;
            }
            SendInt(tosend);

            for (int i = 0; i < betnumbers.Length; i++)
            {
                if (i < choosetrump.Length)
                {
                    choosetrump[i].Hide();
                }
                betnumbers[i].Enabled = false;
                betnumbers[i].Hide();
            }
            if (tosend % 10 == 5)
            {
                ChangeCenterText("You passed");
            }
            else
            {
                Card card = new Card(1, (CardEnum)(tosend % 10));//to get the name of the shape 
                if (tosend % 10 == 4)
                {
                    ChangeCenterText("You bet " + tosend / 10 + " if theere is no trump");
                }
                else
                {
                    ChangeCenterText("You bet " + tosend / 10 + " if the trump is " + card.GetShape());
                }
            }

            win.Hide();

            betting = new Thread(WaitToBet);
            betting.Start();
        }
        void ChangeCenterText(string text)
        {
            centertext.Text = text;
            centertext.Size = TextRenderer.MeasureText(centertext.Text, centertext.Font);
            centertext.Location = new Point(Width / 2 - centertext.Size.Width / 2, 2 * Height / 5);
        }
        void TrumpClick(object sender, EventArgs args)
        {
            Button trump = (Button)sender;
            int tosend = (int)trump.Tag;
            trump.BackColor = Color.Yellow;

            win.Tag = ((int)win.Tag / 10) * 10 + tosend;

            for (int i = 0; i < choosetrump.Length; i++) 
            {
                if (choosetrump[i] != trump && choosetrump[i].BackColor == Color.Yellow) 
                {
                    choosetrump[i].BackColor = SystemColors.ButtonFace;
                    choosetrump[i].ForeColor = default(Color);
                    choosetrump[i].UseVisualStyleBackColor = true;
                }
            }
            if (tosend == 5)//pass
            {
                win.Enabled = true;
                for (int i = 0; i < betnumbers.Length; i++)
                {
                    betnumbers[i].Enabled = false;
                    if (betnumbers[i].BackColor == Color.Yellow)
                    {
                        betnumbers[i].BackColor = SystemColors.ButtonFace;
                        betnumbers[i].ForeColor = default(Color);
                        betnumbers[i].UseVisualStyleBackColor = true;
                    }
                }
            }
            else
            {
                win.Enabled = false;
                SendInt(tosend);
                int num = ReceiveInt();//receive the minimum number that is possible to bet with this trump
                
                for (int i = 0; i < num - 5; i++)
                {
                    betnumbers[i].Enabled = false;
                    if (betnumbers[i].BackColor == Color.Yellow)
                    {
                        betnumbers[i].BackColor = SystemColors.ButtonFace;
                        betnumbers[i].ForeColor = default(Color);
                        betnumbers[i].UseVisualStyleBackColor = true;
                    }
                }
                for (int i = num - 5; i < betnumbers.Length; i++)
                {
                    betnumbers[i].Enabled = true;
                    if (betnumbers[i].BackColor == Color.Yellow)
                    {
                        betnumbers[i].BackColor = SystemColors.ButtonFace;
                        betnumbers[i].ForeColor = default(Color);
                        betnumbers[i].UseVisualStyleBackColor = true;
                    }
                }
                
            }
           
        }

        void BetClick(object sender, EventArgs args)
        {
            Button betbutton = (Button)sender;
            int bet = (int)betbutton.Tag;
            win.Tag = (int)win.Tag % 10 + bet * 10;
            betbutton.BackColor = Color.Yellow;

            for (int i = 0; i < betnumbers.Length; i++)
            {
                if (betnumbers[i] != betbutton && betnumbers[i].BackColor == Color.Yellow)
                {
                    betnumbers[i].BackColor = SystemColors.ButtonFace;
                    betnumbers[i].ForeColor = default(Color);
                    betnumbers[i].UseVisualStyleBackColor = true;
                }
            }

            win.Enabled = true;
        }
        string GetName(string score)
        {
            for (int i = 0; i < score.Length; i++)
            {
                if (score[i] == '\n')
                {
                    return score.Substring(0, i);
                }
            }
            return score;
        }
        public void SendInt(int num)
        {
            this.stream.Write(new byte[1] { (byte)num }, 0, 1);
        }
        void Frish()
        {
            frishcards = new Label[4, 3];
            foreach (Label label in visHand)
            {
                label.Click += FrishClick;
            }
        }
        void FrishClick(object sender, EventArgs args)
        {

            for (int i = 0; i < 3; i++)
            {
                if (frishcards[0, i] == null)
                {
                    Label card = (Label)sender;
                    frishcards[0, i] = card;
                    NewPosition(card);
                    frishcards[0, i].Image = othercards[0][0].Image;
                    frishcards[0, i].Size = othercards[0][0].Size;
                    frishcards[0, i].Location = new Point(Width / 5, Height / 2 + (2 * i - 3) * card.Size.Height / 2);
                    visHand.Remove(card);

                    if (i == 2)
                    {
                        Thread frish = new Thread(GetFrishCards);
                        frish.Start();

                    }
                    break;
                }

            }
        }
        void GetFrishCards()
        {
            foreach (Label label in visHand)
            {
                label.Click -= FrishClick;
            }

            Card[] cards = new Card[3];
            for (int j = 0; j < 3; j++)
            {
                cards[j] = (Card)frishcards[0, j].Tag;
            }
            SendCardArr(cards);

            Card[] getfrishcards = ReceiveCardArr(3);

            for (int j = 0; j < 3; j++)
            {
                frishcards[3, j] = new Label();
                Image image = (Image)Properties.Resources.ResourceManager.GetObject(getfrishcards[j].GetNum().ToString() + ((int)getfrishcards[j].GetShape()).ToString());
                frishcards[3, j].Image = Resize(image, visHand[0].Size.Width, visHand[0].Size.Height);
                frishcards[3, j].Size = visHand[0].Size;
                frishcards[3, j].Tag = getfrishcards[j];
                frishcards[3, j].Location = new Point(Width / 2 + (j * 2 - 3) * (frishcards[3, j].Size.Width / 2), 3 * Height / 5);
                frishcards[3, j].Click += PlaceFrishCards;
                this.Invoke(new del(() =>
                {
                    Controls.Add(frishcards[3, j]);
                }));

                for (int i = 1; i < 3; i++)
                {
                    frishcards[i, j] = new Label();
                    frishcards[i, j].Image = othercards[i][0].Image;
                    frishcards[i, j].Size = othercards[i][0].Size;

                }
                frishcards[1, j].Location = new Point(Width / 2 + (2 * j - 3) * (frishcards[1, j].Size.Width / 2), Height / 6);
                frishcards[2, j].Location = new Point(4 * Width / 5, Height / 2 + (2 * j - 3) * frishcards[2, j].Size.Height / 2);
                this.Invoke(new del(() =>
                {
                    Controls.Add(frishcards[3, j]);
                    Controls.Add(frishcards[1, j]);
                    Controls.Add(frishcards[2, j]);
                }));
            }
        }
        void IsDoneTakingCards()
        {
            byte[] data = new byte[1];
            try
            {
                stream.Read(data, 0, data.Length);
            }
            catch
            {
                MessageBox.Show("Server is offline");
            }

            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < 3; i++)
                {
                    this.Invoke(new del(() =>
                    {
                        Controls.Remove(frishcards[j, i]);
                    }));
                    frishcards[j, i] = null;
                }
            }
            frishcards = null;

            centertext.Text = "";
            WaitToBet();
        }
        void PlaceFrishCards(object sender, EventArgs args)
        {
            Label label = (Label)sender;
            label.Click -= PlaceFrishCards;

            Card card = (Card)label.Tag;

            SendCard(card);

            int index = ReceiveInt();

            visHand.Add(new Label());

            for (int i = visHand.Count - 1; i > index; i--)
            {
                visHand[i] = visHand[i - 1];
                visHand[i].Location = new Point(visHand[i].Location.X + visHand[i].Size.Width / 2 + Width / 1600, visHand[i].Location.Y);
            }
            visHand[index] = label;
            for (int i = 0; i < index; i++)
            {
                visHand[i].Location = new Point(visHand[i].Location.X - visHand[i].Size.Width / 2 - Width / 1600, visHand[i].Location.Y);
            }
            visHand[index].Location = index != 0 ? (new Point((visHand[0].Location.X + index * (visHand[0].Size.Width + Width / 800)), visHand[0].Location.Y)) : new Point(visHand[1].Location.X - visHand[1].Size.Width, visHand[1].Location.Y);

            if (visHand.Count == 13)
            {
                Thread thread = new Thread(IsDoneTakingCards);
                thread.Start();
            }
        }
        void PlaceNames()
        {
            string names = ReceiveString(256);//getting other players' names

            clientid = (int)names[0] - 48;
            names = names.Substring(1);
            score = new Label[4]; //Creating labels for other players' name and score

            for (int i = 0; i < 3; i++)
            {
                string temp = "";
                int namelenlen = (int)names[0] - 48;
                int namelen = int.Parse(names.Substring(1, namelenlen));
                names = names.Substring(1 + namelenlen);
                //try
                //{
                //    namelen = int.Parse(names.Substring(0, 2));
                //    names = names.Substring(2);
                //}
                //catch
                //{
                //    namelen = (int)names[1] - 48;
                //    names = names.Substring(1);
                //}

                for (int j = 0; j < namelen; j++)
                {
                    temp += names[0];
                    names = names.Substring(1);
                }

                score[i] = new Label();
                score[i].Size = new Size(Width / 15, Height / 20);
                score[i].Font = new Font("Arial", 7 * Width / 960);
                score[i].Text = temp + '\n' + "Score: 0";
                score[i].Tag = GetName(score[i].Text);
            }
            score[0].Location = new Point(0, Height / 2 - score[0].Size.Height / 2);
            score[1].Location = new Point(Width / 2 - score[1].Size.Width / 2, 0);
            score[2].Location = new Point(Width - score[2].Size.Width, Height / 2 - score[2].Size.Height / 2);

            score[3] = new Label();
            score[3].Size = new Size(Width / 15, Height / 20);
            score[3].Location = new Point(Width / 2 - score[3].Size.Width / 2, Height - (int)(0.6 * score[3].Size.Height));
            score[3].Font = new Font("Arial", 7 * Width / 960);
            score[3].Text = "Score: 0";
            score[3].Tag = Environment.UserName;
            this.Invoke(new del(() =>
            {
                for (int i = 0; i < 4; i++)
                {
                    Controls.Add(score[i]);
                }
            }));
            BeforeGetBet();
        }

        void WaitToBet()//deciding on the trump
        {
            byte[] data = new byte[256];

            try
            {
                stream.Read(data, 0, 256);
            }
            catch
            {
                MessageBox.Show("Server is offline");
            }
            
            string clientname = Encoding.UTF8.GetString(data);

            while (clientname[0] != 'a' && clientname[0] != 'b' && clientname[0] != 'c') //until betting is over
            {
                if (clientname[0] != '\0') //if someone had bet for a trump
                {
                    Card currenttopbet = ReceiveCard();//when data is received the client needs to make a bet (or pass) or if its a card it means that someone had bet (then the first char of clientsname would be '\0') 

                    CardEnum trump = currenttopbet.GetShape();
                    this.Invoke(new del(() =>
                    {
                        if (trump == (CardEnum)5)
                        {
                            ChangeCenterText(clientname.Substring(0, BackSlash0(clientname)) + " passed");
                        }
                        else
                        {
                            if (trump == (CardEnum)4)
                            {
                                ChangeCenterText(clientname.Substring(0, BackSlash0(clientname)) + " bet " + currenttopbet.GetNum() + " if theere is no trump");
                            }
                            else
                            {
                                ChangeCenterText(clientname.Substring(0, BackSlash0(clientname)) + " bet " + currenttopbet.GetNum() + " if the trump is " + trump);
                                if (bet != 0)
                                {
                                    bet = 0;
                                }
                            }
                        }
                    }));
                }
                else
                {
                    this.Invoke(new del(() =>
                    {
                        GetTrump();
                    })); 
                    return;
                }

                try
                {
                    stream.Read(data, 0, 256);
                }
                catch
                {
                    MessageBox.Show("Server is offline");
                }
                
                clientname = Encoding.UTF8.GetString(data);

            }

            if (clientname[0] == 'b')
            {
                GetBet();
            }
            else
            {
                if (clientname[0] == 'a')//not too many frishes
                {
                    ChangeCenterText("Its frisch!");
                    Frish();
                }
                else//start the game over
                {
                    NewGame(new object(), new EventArgs());
                }
            }
        }
       
        void BeforeGetBet()
        {
            centertext = new Label();
            centertext.Font = new Font("Arial", 7 * Width / 960);
            centertext.Text = "";
            this.Invoke(new del(() =>
            {
                Controls.Add(centertext);
            }));

            betting = new Thread(WaitToBet);
            betting.Start();
        }

        void GetBet()//final betting
        {
            currentturn = ReceiveInt();
            int betsum = 0;
            string receive;
            for (int j = 0; j < 4; j++)
            {
                if ((j + currentturn) % 4 == 3)//when its this client's turn to bet
                {
                    choosetrump = new Button[14];
                    for (int i = 0; i < choosetrump.Length; i++)
                    {
                        choosetrump[i] = new Button();
                        choosetrump[i].Size = new Size(Width / 20, Width / 20);
                        choosetrump[i].Text = i.ToString();
                        choosetrump[i].Location = new Point(Width / 2 + (-7 + i) * Width / 20, 3 * Height / 5);
                        if (i < bet || (i + betsum == 13 && currentturn == 0))
                        {
                            choosetrump[i].Enabled = false;
                        }
                        else
                        {
                            choosetrump[i].Click += GotBet;
                        }

                        this.Invoke(new del(() =>
                        {
                            Controls.Add(choosetrump[i]);
                        }));
                    }
                }
                else
                { 
                    receive = ReceiveString(2);
                    betsum += int.Parse(receive.Split('\\')[0]);
                    this.Invoke(new del(() =>
                    {
                        ChangeCenterText(GetName(score[(j + currentturn) % 4].Text) + " bet " + receive);
                        score[(j + currentturn) % 4].Text += "/" + receive;
                    }));
                    
                }
            }

            this.Invoke(new del(() =>
            {
                StartGame();
            }));
        }
        void StartGame()
        {
            firstturn = currentturn;

            Controls.Remove(centertext);
            nextound = new Thread(NextRoundThread);

            win.Text = "Next Round";
            win.Click -= SubmitClick;
            win.Click += WinnerClick;

            thisturn = new Label();
            thisturn.Font = new Font("Arial", 7 * Width / 960);
            Controls.Add(thisturn);

            foreach (Label label in visHand)
            {
                label.Click += ChoseCard;
            }

            otherscards = new Thread(GetOtherCard);
            otherscards.Start();
        }
        void GetOtherCard()
        {
            bool flag = true;
            while (currentturn != 4 + firstturn)
            {
                while (currentturn == 3)
                {
                    if (flag)
                    {
                        this.Invoke(new del(() =>
                        {
                            thisturn.Text = "It's your turn.";
                            thisturn.Size = TextRenderer.MeasureText(thisturn.Text, thisturn.Font);
                            thisturn.Location = new Point(Width / 2 - thisturn.Size.Width / 2, 2 * Height / 5);
                        }));

                    }
                    flag = false;
                }
                this.Invoke(new del(() =>
                {
                    thisturn.Text = "It's " + (string)score[currentturn % 4].Tag + "'s turn.";
                    thisturn.Size = TextRenderer.MeasureText(thisturn.Text, thisturn.Font);
                    thisturn.Location = new Point(Width / 2 - thisturn.Size.Width / 2, 2 * Height / 5);
                }));

                Card card = ReceiveCard();

                thisround[currentturn % 4] = new Label();
                Image image = (Image)Properties.Resources.ResourceManager.GetObject(card.GetNum().ToString() + ((int)card.GetShape()).ToString());

                if (currentturn % 4 != 1)
                {
                    image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    thisround[currentturn % 4].Image = Resize(image, (int)(this.Height / 7.1591), (int)(this.Width / 19.45));
                    thisround[currentturn % 4].Location = new Point((currentturn % 4 == 0 ? (5 * (Width / 16)) : (11 * Width / 16 - thisround[2].Size.Width)), Height / 2 - thisround[currentturn % 4].Image.Size.Height / 2);
                }
                else
                {
                    thisround[currentturn % 4].Image = Resize(image, (int)(this.Width / 19.45), (int)(this.Height / 7.1591));
                    thisround[currentturn % 4].Location = new Point(Width / 2 - thisround[currentturn % 4].Image.Size.Width / 2, Height / 4);
                }
                thisround[currentturn % 4].Size = thisround[currentturn % 4].Image.Size;
                thisround[currentturn % 4].Tag = card;
                this.Invoke(new del(() =>
                {
                    Controls.Add(thisround[currentturn % 4]);
                }));

                currentturn++;

                Thread.Sleep(100);
            }
            NextRound();
        }
        void NextRound()
        {
            int winner = ReceiveInt();

            win.Tag = winner;

            string score1 = score[winner].Text;
            int score2;
            if (score1[score1.Length - 2] == '/')
            {
                score2 = 3;
            }
            else
            {
                score2 = 4;
            }

            this.Invoke(new del(() =>
            {
                if (winner == 3)
                {
                    thisturn.Text = "You are the winner!";
                }
                else
                {
                    thisturn.Text = (string)score[winner].Tag + " is the winner!";
                }
                thisturn.Size = TextRenderer.MeasureText(thisturn.Text, thisturn.Font);
                thisturn.Location = new Point(Width / 2 - thisturn.Size.Width / 2, 2 * Height / 5);

                score[winner].Text = score1.Substring(0, score1.Length - score2) + ((int)score1[score1.Length - score2] - 47).ToString() + score1.Substring(score1.Length - score2 + 1, score2 - 1);
            }));

            if (winner == 3)//You win yes?
            {
                
                if (visHand.Count == 0)
                {
                    this.Invoke(new del(() =>
                    {
                        win.Text = "New Game";
                    }));

                    win.Click -= WinnerClick;
                    win.Click += NewGame;
                }
                this.Invoke(new del(() =>
                {
                    win.Show();
                }));

            }
            else
            {
                nextound = new Thread(NextRoundThread);
                nextound.Start(winner);
            }
        }

        void ChoseCard(object sender, EventArgs args)
        {
            if (currentturn == 3 && cardenable)
            {
                Label card = (Label)sender;

                SendCard((Card)card.Tag);

                if (ReceiveString(1) == "a")
                {
                    cardenable = false;
                    NewPosition(card);
                    card.Location = new Point(Width / 2 - card.Size.Width / 2, 3 * Height / 5);

                    visHand.Remove(card);
                    thisround[3] = card;


                    if (firstturn + 3 == currentturn)
                    {
                        NextRound();
                    }
                    else
                    {
                        currentturn++;
                    }
                }
            }
        }
        void NewPosition(Label card)
        {
            foreach (Label label in visHand)
            {
                if (label.Location.X < card.Location.X)
                {
                    label.Location = new Point(label.Location.X + label.Size.Width / 2 + Width / 1600, label.Location.Y);
                }
                else if (label.Location.X > card.Location.X)
                {
                    label.Location = new Point(label.Location.X - label.Size.Width / 2 - Width / 1600, label.Location.Y);
                }
            }
        }
        void NextRoundThread(object winner)
        {

            if (ReceiveString(1) != "a")
            {
                this.Invoke(new del(() =>
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Controls.Remove(thisround[i]);
                    }
                }));

                thisround = new Label[4];

                firstturn = (int)winner;
                currentturn = firstturn;
                cardenable = true;

                otherscards.Abort();
                otherscards = new Thread(GetOtherCard);
                otherscards.Start();
            }
            else
            {
                NewGame(new object(), new EventArgs());
            }

        }
        void WinnerClick(object sender, EventArgs args)
        {
            cardenable = true;

            stream.Write(new byte[] { 0 }, 0, 1);

            win.Hide();

            for (int i = 0; i < 4; i++)
            {
                Controls.Remove(thisround[i]);
            }
            thisround = new Label[4];

            firstturn = (int)win.Tag;
            currentturn = firstturn;

            otherscards.Abort();
            otherscards = new Thread(GetOtherCard);
            otherscards.Start();
        }

        void SendCardArr(Card[] cards)
        {
            byte[] data = Card.SerializeArr(cards);
            stream.Write(data, 0, data.Length);
        }
        Card[] ReceiveCardArr(int length)
        {
            byte[] data = new byte[8 * length];

            try
            {
                stream.Read(data, 0, data.Length);
            }
            catch
            {
                MessageBox.Show("Server is offline");
            }

            Card[] cards = Card.DesserializeArr(data);
            return cards;
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

            this.Invoke(new del(() =>
            {
                ChangeCenterText("You bet " + send);
            }));

            SendInt(send);
        }
        string ReceiveString(int len)
        {
            byte[] data = new byte[len];
            try
            {
                stream.Read(data, 0, data.Length);
            }
            catch
            {
                MessageBox.Show("Server is offline");
            }
            return Encoding.UTF8.GetString(data);
        }
        Card ReceiveCard()
        {
            byte[] data = new byte[8];
            try
            {
                stream.Read(data, 0, data.Length);
            }
            catch
            {
                MessageBox.Show("Server is offline");
            }

            return Card.Desserialize(data);
        }
        void SendCard(Card card)
        {
            byte[] data = Card.Serialize(card);
            stream.Write(data, 0, data.Length);
        }
        public Image Resize(Image image, int w, int h)
        {
            Bitmap bmp = new Bitmap(w, h);
            Graphics grp = Graphics.FromImage(bmp);
            grp.DrawImage(image, 0, 0, w, h);
            grp.Dispose();

            return bmp;
        }
        void NewGame(object sender, EventArgs args)
        {
            try
            {
                Button win = (Button)sender;
                byte[] data = Encoding.UTF8.GetBytes("a");
                stream.Write(data, 0, data.Length);
            }
            catch
            {

            }

            this.Invoke(new del(() =>
            {
                for (int i = 0; i < 13; i++)
                {
                    try
                    {
                        Controls.Remove(visHand[i]);
                    }
                    catch
                    {

                    }

                    if (i < 4)
                    {

                        score[i].Text = (i != 3 ? (string)score[i].Tag + "\n" : "") + "score: 0";

                        Controls.Remove(thisround[i]);
                    }
                }
                Controls.Remove(win);
                Controls.Remove(thisturn);

                if (otherscards != null)
                {
                    otherscards.Abort();

                }

                cardenable = true;
                centertext.Text = "";
                CreateCards("a", new EventArgs());
            }));


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
        public int ReceiveInt()
        {
            byte[] buffer = new byte[1];
            try
            {
                stream.Read(buffer, 0, 1);
            }
            catch
            {
                MessageBox.Show("Server is offline");
            }
            
            return (int)buffer[0];
        }
        private void WhistClient_Load(object sender, EventArgs e)
        {

        }

    }
}
