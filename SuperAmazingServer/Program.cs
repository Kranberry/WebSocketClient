using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace SuperAmazingServer
{
    class Program
    {
        static List<Socket> Clients = new(); 

        // Incoming data from the client
        public static string ClientData = null;
        private readonly static int port = 3300;

        public static void StartListening()
        {

            // Establish the local endpoint of the socket
            // Dns.GetHostName returns the name of the host running the application
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAdress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new(ipAdress, port);

            // Create the Tcp/IP socket
            Socket server = new(ipAdress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local end point
            // And listen for incoming connections
            try
            {
                server.Bind(localEndPoint);
                server.Listen(10);

                // Start listening for connections
                while (true)
                {
                    bool alreadyConnected = false;
                    Console.WriteLine("Waiting on connection...");

                    // Block the program until a connection is made
                    Socket client = server.Accept();
                    foreach(Socket oldClient in Clients)
                    {
                        IPEndPoint oldEndPoint = oldClient.RemoteEndPoint as IPEndPoint;
                        IPEndPoint newEndPoint = client.RemoteEndPoint as IPEndPoint;

                        if ( oldEndPoint.Address.ToString() == newEndPoint.Address.ToString())
                        {
                            alreadyConnected = true;
                            break;
                        }
                    }

                    if (!alreadyConnected)
                    {
                        Console.WriteLine("Connection successfull");
                        Clients.Add(client);
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("Press ENTER to continue...");
            Console.Read();
        }

        static void ListenToRequests()
        {
            // Data buffer for incoming data
            byte[] dataBuffer = new byte[1024];

            while (true)
            {
                foreach (Socket client in Clients.ToList())
                {
                    ClientData = null;

                    // All incoming connections needs to be processed
                    while (true)
                    {
                        int recievedBuffer = client.Receive(dataBuffer);
                        ClientData += Encoding.ASCII.GetString(dataBuffer, 0, recievedBuffer);

                        if (ClientData.Length > 0)   // Marks the end of the message data
                            break;
                    }

                    if(ClientData == "Disconnecting")
                    {
                        Console.WriteLine("Client disconnected peacefully.");
                        DisconnectClient(client);
                        break;
                    }

                    // Show the data on the console
                    Console.WriteLine("Recieved : " + ClientData);

                    // Send a message back to the client
                    byte[] msg = Encoding.ASCII.GetBytes("Heard you loud and clear baby!");
                    client.Send(msg);
                }
            }
        }

        static void DisconnectClient(Socket client)
        {
            client.Shutdown(SocketShutdown.Both);   // Disable the socket from both sending and recieving data
            client.Close(); // Disconnect the socket and dispose of associated resources
            Clients.Remove(client);
        }

        static void Main(string[] args)
        {
            ThreadPool.QueueUserWorkItem((x) => ListenToRequests());
            StartListening();
        }
    }
}
