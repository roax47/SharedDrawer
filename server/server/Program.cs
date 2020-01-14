using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace server
{
    public static class Globals
    {
        public const int portNumber = 31337;
    }
    class Program
    {
        static void Main(string[] args)
        {
            Server s = new Server();
            Thread t1 = new Thread(new ThreadStart(s.Connection_listener));
            Thread t2 = new Thread(new ThreadStart(s.Drawing_listener));
            Thread t3 = new Thread(new ThreadStart(s.Client_sender));


            Parallel.Invoke(
                    () => t1.Start(),
                    () => t2.Start(),
                    () => t3.Start());

        }
    }

    class Client
    {
        public int ID;
        public IPAddress ip;
        public int port;
        public Client()
        {
            ID = -1;
            ip = IPAddress.Parse("0.0.0.0");
            port = -1;
        }
        public Client(int ID,IPAddress ip,int port)
        {
            this.ID = ID;
            this.ip = ip;
            this.port = port;
        }
    }
    class DrawingData
    {
        public char ID;
        public Point LineStart;
        public Point LineEnd;
        public int color;

        public DrawingData()
        {
            ID = (char)0;
            LineStart.X = 0;
            LineStart.Y = 0;
            LineEnd.X = 0;
            LineEnd.Y = 0;
            color = 0;
        }

        public DrawingData(int id,Point start,Point end,int col)
        {
            ID = (char)id;
            LineStart = start;
            LineEnd = end;
            color = col;
        }
    }
    class Server
    {
        UdpClient listener,listener2;
        IPEndPoint client_global;
        Client[] ClientList;
        ConcurrentQueue<DrawingData> queue;
        int CLIENT_MAX = 255;
        int new_port = 31338;
        int color = 0;
        public Server()
        {
            listener = new UdpClient(Globals.portNumber);
            listener2 = new UdpClient(new_port);
            client_global = new IPEndPoint(IPAddress.Any, Globals.portNumber);
            ClientList = new Client[CLIENT_MAX];
            queue = new ConcurrentQueue<DrawingData>();
            for (int i = 0; i < CLIENT_MAX; i++)
            {
                ClientList[i] = new Client();
            }
        }
        public void Connection_listener()
        {
            Console.WriteLine("Server available at: 31337");
            while (true)
            {

                byte[] bytes = listener.Receive(ref client_global);
                IPEndPoint client = client_global;

  
                string client_command = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                if (client_command == "connect")
                {
                    Console.WriteLine("{0} is connected \n", client.ToString());
                    int new_id = -1;
                    for(int i = 0; i < CLIENT_MAX; i++)
                    {
                        if (ClientList[i].ID == -1)
                        {
                            new_id = i;
                            break;
                        }
                    }
                    if(new_id == -1 ) Console.WriteLine("Max client number");
                    ClientList[new_id] = new Client(new_id, client.Address, client.Port);

                    char[] server_message = new_port.ToString().ToCharArray();
                    byte[] msg = Encoding.ASCII.GetBytes(server_message, 0, server_message.Length);
                    listener.Send(msg, msg.Length, client);

                }
                else if(client_command == "disconnect"){
                    for (int i = 0; i < CLIENT_MAX; i++) { 
                        if (ClientList[i].ip.Equals(client.Address) && ClientList[i].port.Equals(client.Port))
                        {
                            ClientList[i].ID = -1;
                            Console.WriteLine("{0} is disconnected \n", client.ToString());
                            break;
                        }
                    }
                 }
             }
       
        }
        public void Drawing_listener()
        {
            while (true)
            {
       
                IPEndPoint client = new IPEndPoint(IPAddress.Any, new_port);
                byte[] bytes = listener2.Receive(ref client);
                string client_command = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                if (client_command.Length == 1)
                {
                    color = Int32.Parse(client_command);

                }
                else if (client_command != "disconnect" && client_command != "connect")
                {
                    string[] tokens = client_command.Split(',');
                    Point start = new Point(Int32.Parse(tokens[0]), Int32.Parse(tokens[1]));
                    Point end = new Point(Int32.Parse(tokens[2]), Int32.Parse(tokens[3]));
                    DrawingData data = null;
                    for (int i = 0; i < CLIENT_MAX; i++)
                    {
                        if (ClientList[i].ip.Equals(client.Address) && ClientList[i].port.Equals(client.Port))
                        {
                            data = new DrawingData(ClientList[i].ID, start, end, color);
                            break;
                        }
                    }
                    queue.Enqueue(data);
                }
                    
                
            }
        }
        public void Client_sender()
        {

            while (true)
            {
                DrawingData tmp = new DrawingData();
                while (!queue.TryDequeue(out tmp));
                string server_message = tmp.LineStart.X.ToString() + "," + tmp.LineStart.Y.ToString() + "," + tmp.LineEnd.X.ToString() + "," + tmp.LineEnd.Y.ToString()+","+tmp.color.ToString();
                char[] tab = server_message.ToCharArray();
                byte[] mes = Encoding.ASCII.GetBytes(tab);
                for (int i = 0; i < CLIENT_MAX; i++)
                {
                    if (ClientList[i].ID!=-1)
                    {                       
                        IPEndPoint client = new IPEndPoint(ClientList[i].ip, ClientList[i].port);
                        listener.Send(mes, mes.Length, client);
                    }
                }
                
            }
        }
    }
}
