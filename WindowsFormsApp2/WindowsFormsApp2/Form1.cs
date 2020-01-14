using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        static System.Windows.Forms.Timer myTimer = new System.Windows.Forms.Timer();
        private int DefaultPort=31337;
        private int drawingPortNumber = 0;
        private Socket s;
        private IPEndPoint server;
        private Pen pen;
        private Pen server_pen;
        private Color[] color;
        private int color_number = 0;
        private int server_color = 0;
        Graphics graphics;
        bool drawing = false;
        Point mPosition = new Point();
        private bool connected = false;

        public Form1()
        {

            InitializeComponent();

            color = new Color[3];
            color[0] = System.Drawing.Color.Red;
            color[1] = System.Drawing.Color.Blue;
            color[2] = System.Drawing.Color.Green;

            textBox1.Text = "127.0.0.1";
            textBox2.Text = "31337";
            textBox3.Text = "Disconnected";

            pen = new Pen(color[0], 5);
            server_pen = new Pen(color[0], 5);
            pictureBox1.Image = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            graphics = Graphics.FromImage(pictureBox1.Image);
            //graphics.DrawLine(pen, 10, 10, 100, 100);
            myTimer.Tick += new EventHandler(TimerEventProcessor);
            myTimer.Interval = 30;
            
        }

        private void TimerEventProcessor(Object myObject,
                                                EventArgs myEventArgs)
        {
            while (s.Available > 0)
            {
                byte[] bytes = new byte[256];
                s.Receive(bytes);
                string data_server = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                string[] tokens = data_server.Split(',');
                Point start = new Point(Int32.Parse(tokens[0]), Int32.Parse(tokens[1]));
                Point end = new Point(Int32.Parse(tokens[2]), Int32.Parse(tokens[3]));
                server_pen.Color = color[Int32.Parse(tokens[4])];
                graphics.DrawLine(server_pen, start, end);
                pictureBox1.Invalidate();
                
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (connected) disconnect();
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                send_color_number();
                drawing = true;
                mPosition = new Point(e.X, e.Y);
                pictureBox1.Invalidate();
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                drawing = false;
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (drawing)
            {
                Point newValue = new Point(e.X, e.Y);
                graphics.DrawLine(pen, mPosition, newValue);
                pictureBox1.Invalidate();
                if (connected) send_to_server(mPosition,newValue, color_number);
                mPosition = newValue;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (color_number < 2) color_number++;
            else color_number = 0;
            pen.Color = color[color_number];
        }
         public void connect()
        {    
            s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress server_ip = IPAddress.Parse(textBox1.Text);
            server = new IPEndPoint(server_ip, Int32.Parse(textBox2.Text));

            byte[] mes = Encoding.ASCII.GetBytes("connect");
            s.SendTo(mes, server);

            byte[] portNumber = new byte[256];
            s.Receive(portNumber);
            drawingPortNumber = Convert.ToInt32(Encoding.ASCII.GetString(portNumber));
            server.Port = drawingPortNumber;
            textBox3.Text = "Connected";
            connected = true;
            myTimer.Start();

        }

        public void disconnect()
        {
            connected = false;
            byte[] mes = Encoding.ASCII.GetBytes("disconnect");
            IPEndPoint old_serv = new IPEndPoint(server.Address, DefaultPort);
            s.SendTo(mes, old_serv);
            textBox3.Text = "Disconnected";

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!connected) connect();
        }


        public void send_to_server(Point p1,Point p2, int color)
        {
            string server_message = p1.X.ToString() + "," + p1.Y.ToString() + "," + p2.X.ToString() + "," + p2.Y.ToString();
            byte[] mes = Encoding.ASCII.GetBytes(server_message.ToCharArray());
            s.SendTo(mes,server);
        }

        public void send_color_number()
        {
            if (connected)
            {
                byte[] mes = Encoding.ASCII.GetBytes(color_number.ToString().ToCharArray());
                s.SendTo(mes, server);
            }
        }

    }
}
