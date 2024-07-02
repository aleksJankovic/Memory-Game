using MG_46_2020.Properties;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace MG_46_2020
{
    public partial class MemoryGame : Form
    {
        public int pomerajLevo, pomerajDesno, pomerajGore, pomerajDole;
        
        private int Rows;
        private int brojPoteza;
        private int Columns;

        private List<string> slike = new List<string>();
        private List<PictureBox> otvoreneSlike;
        private List<PictureBox> zatvoreneSlike;

        private PictureBox prvaSlika = null;

        private Timer timerVreme;
        bool zavrsena;
        int nivo = 1;
        DateTime pocetakIgre;

        public MemoryGame()
        {
            InitializeComponent();
            button1.Image = Properties.Resources.newgame;
            this.Size = new Size(1200, 700);
            pomerajGore = pomerajDole = (int)(0.03 * 700);
            pomerajLevo = (int)(0.03 * 1200);
            pomerajDesno = (int)(0.08 * 1200);
            Rows = 3;
            Columns = 4;
            init();

        }

        private void init()
        {
            
            zavrsena = false;
            otvoreneSlike = new List<PictureBox>();
            zatvoreneSlike = new List<PictureBox>();

            ListaSlika();
            ListaPictureBoxes();

            brojPoteza = 0;
            labelPotezi.Text = "Broj poteza: " + brojPoteza;

            timerVreme = new Timer();
            timerVreme.Tick += timer1_Tick;

            pocetakIgre = DateTime.Now;
            timerVreme.Start();
            nivo = dajNivo();

            SqlConnection conn = DbConnection.GetConnection;
            if (conn == null)
            {
                labelRezultat.Text = "Nema konekcije sa bazom!";
            }
            else
            {
                string query = "SELECT MIN(Rezultat) FROM Memorija WHERE Nivo = @nivo";
                conn.Open();

                SqlCommand command = new SqlCommand(query, conn);
                command.Parameters.AddWithValue("@nivo", nivo);

                object rezultat = command.ExecuteScalar();
                int najboljiRezultat = Convert.ToInt32(rezultat);
                labelRezultat.Text = "Najbolji rezultat za nivo " + nivo + " jeste: " + najboljiRezultat;
                conn.Close();
            }
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!zavrsena)
            {
                TimeSpan protekloVreme = DateTime.Now - pocetakIgre;
                labelTajmer.Text = "Proteklo vreme: " + protekloVreme.ToString(@"mm\:ss");
                
            }
        }
        private void ListaPictureBoxes()
        {
            int pictureBoxSirina = 105;
            int pictureBoxVisina = 130;
            int razmak = 5; // Razmak između PictureBox-eva

            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    PictureBox pictureBox = new PictureBox(); 
                    pictureBox.Width = pictureBoxSirina;
                    pictureBox.Height = pictureBoxVisina;
                    pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                    pictureBox.BorderStyle = BorderStyle.FixedSingle;
                    pictureBox.Image = Properties.Resources.Neotvoreno;
                    pictureBox.Tag = slike[row * Columns + col]; 
                    pictureBox.Click += PictureBox_Click; 

                    int x = col * (pictureBoxSirina + razmak);
                    int y = row * (pictureBoxVisina + razmak);
                    pictureBox.Location = new Point(x, y);

                    this.Controls.Add(pictureBox);
                    zatvoreneSlike.Add(pictureBox);
                }
            }
        }
        private void PictureBox_Click(object sender, EventArgs e)
        {
            SoundPlayer click = new SoundPlayer(Properties.Resources.klik);
            click.Play();
            PictureBox clickedPictureBox = sender as PictureBox;

            if (clickedPictureBox == null || otvoreneSlike.Contains(clickedPictureBox))
            {
                return;
            }            
            string imeSlike = clickedPictureBox.Tag.ToString();
            pronadjiSliku(imeSlike, clickedPictureBox);
            if (prvaSlika == null)
            {
                // Prvi klik
                prvaSlika = clickedPictureBox;
            }
            else
            {
                //ako je ista slika kliknuta dva puta
                if (prvaSlika.Location == clickedPictureBox.Location) return;
                // Drugi klik - provera da li se slike poklapaju
                if (prvaSlika.Tag.ToString() == clickedPictureBox.Tag.ToString())
                {
                    SoundPlayer tacno = new SoundPlayer(Properties.Resources.tacno);
                    tacno.Play();
                    // Poklapanje slika
                    otvoreneSlike.Add(prvaSlika);
                    otvoreneSlike.Add(clickedPictureBox);

                    prvaSlika = null;
                }
                else
                {
                    // Ne poklapaju se slike
                    Timer timer = new Timer();
                    timer.Interval = 500; // Prikazati sliku 0.7 sekundi pre nego što se sakrije
                    timer.Tick += (s, ea) =>
                    { 
                        prvaSlika.Image = Properties.Resources.Neotvoreno;
                        clickedPictureBox.Image = Properties.Resources.Neotvoreno;

                        prvaSlika = null;
                        timer.Stop();
                    };
                    timer.Start();
                }
                brojPoteza++;
                labelPotezi.Text = "Broj poteza: " + brojPoteza;

                if (IgraZavrsena())
                {
                    zavrsena = true;
                    krajIgre();
                    ResetujIgru();
                }
            }
        }

        private void krajIgre()
        {
            SoundPlayer kraj = new SoundPlayer(Properties.Resources.gotovo);
            kraj.Play();
            string message = "Igra je završena";
            string title = "Kraj igre";
            MessageBox.Show(message, title, MessageBoxButtons.OK);

            string playerName = Interaction.InputBox("Unesite vaše ime:", "Unos imena", "");
            if (!string.IsNullOrWhiteSpace(playerName))
            {
                MessageBox.Show("Hvala što ste igrali, " + playerName + "!");
            }

            //logika za bodove
            
            int brojBodova = rezultat(nivo);
            SqlConnection conn = DbConnection.GetConnection;
            string query = "INSERT INTO Memorija (ImeIgraca,BrojPoteza,Nivo,Rezultat) VALUES (@ime,@brojPoteza,@nivo,@brojBodova)";
            conn.Open();

            SqlCommand command = new SqlCommand(query, conn);
            command.Parameters.AddWithValue("@ime", playerName);
            command.Parameters.AddWithValue("@brojPoteza", brojPoteza);
            command.Parameters.AddWithValue("@nivo", nivo);
            command.Parameters.AddWithValue("brojBodova", brojBodova);
            

            command.ExecuteNonQuery();
            conn.Close();
        }

        private int dajNivo()
        {
            if (Rows == 3) return 1;
            else if (Rows == 4)
            {
                if (Columns == 4) return 2;
                else return 4;
            }
            return 3;
        }

        private int rezultat(int nivo)
        {
            return brojPoteza * nivo;
        }

        private void ResetujIgru()
        {
            Obrisi();
            slike.Clear();
            init();
        }

        private bool IgraZavrsena() 
        {
            if (otvoreneSlike.Count == slike.Count) return true;
            return false;

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e){
           
            Rows = 3;
            Columns = 4;
            ResetujIgru();
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            
            Rows = 4;
            Columns = 4;
            ResetujIgru();
        }


        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            Rows = 5;
            Columns = 4;
            ResetujIgru();
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ResetujIgru();
            SoundPlayer click = new SoundPlayer(Properties.Resources.klik);
            click.Play();
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            Rows = 4;
            Columns = 6;
            ResetujIgru();
        }
        private void Obrisi()
        {
            if (zatvoreneSlike != null && zatvoreneSlike.Count > 0)
            {
                foreach (var picture in zatvoreneSlike)
                {
                    this.Controls.Remove(picture);
                    picture.Dispose();
                }
                zatvoreneSlike.Clear();
                otvoreneSlike.Clear();
            }
            
        }

        private void pronadjiSliku(string imageName, PictureBox clickedPictureBox)
        {
            if (imageName.Equals("_1"))
            {
                clickedPictureBox.Image = Properties.Resources._1;
            }else if (imageName.Equals("_2"))
            {
                clickedPictureBox.Image = Properties.Resources._2;
            }
            else if (imageName.Equals("_3"))
            {
                clickedPictureBox.Image = Properties.Resources._3;
            }
            else if (imageName.Equals("_4"))
            {
                clickedPictureBox.Image = Properties.Resources._4;
            }
            else if (imageName.Equals("_5"))
            {
                clickedPictureBox.Image = Properties.Resources._5;
            }
            else if (imageName.Equals("_6"))
            {
                clickedPictureBox.Image = Properties.Resources._6;
            }
            else if (imageName.Equals("_7"))
            {
                clickedPictureBox.Image = Properties.Resources._7;
            }
            else if (imageName.Equals("_8"))
            {
                clickedPictureBox.Image = Properties.Resources._8;
            }
            else if (imageName.Equals("_9"))
            {
                clickedPictureBox.Image = Properties.Resources._9;
            }
            else if (imageName.Equals("_10"))
            {
                clickedPictureBox.Image = Properties.Resources._10;
            }
            else if (imageName.Equals("_11"))
            {
                clickedPictureBox.Image = Properties.Resources._11;
            }
            else if (imageName.Equals("_12"))
            {
                clickedPictureBox.Image = Properties.Resources._12;
            }
        }

        private void ListaSlika()
        {
            for (int i = 1; i <= Rows * Columns / 2; i++)
            {
                slike.Add("_"+i);
                slike.Add("_"+i); 
            }

            Random random = new Random();
            int n = slike.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                string value = slike[k];
                slike[k] = slike[n];
                slike[n] = value;
            }

        }


    }
}

