using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static string _filepath = @"C:\Users\sedyh\source\repos\TestMacroscop\Client\Textfiles";
        private static byte[] buffer = new byte[1024];
        private static Socket _clientSocket = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static void Main(string[] args)
        {
            Console.Title = "Client";
            LoopConnect();
            SendFilesLoop();
            Console.ReadLine();
        }

        private static void SendFilesLoop()
        {
            while (true)
            {
                Console.WriteLine("Press any button to start sending files...");
                Console.ReadLine();
                foreach (var file in Directory.GetFiles(_filepath, "*.txt"))
                {
                    string text = File.ReadAllText(file).Replace(Environment.NewLine," ");
                    if (text is null)
                    {
                        continue;
                    }
                    byte[] buffer = Encoding.Default.GetBytes(text);
                    _clientSocket.SendAsync(buffer);


                    byte[] receivedBuff = new byte[1024];
                    int rec = _clientSocket.Receive(receivedBuff);
                    byte[] data = new byte[rec];
                    Array.Copy(receivedBuff, data, rec);
                    Console.WriteLine("Received: " + Encoding.Default.GetString(data));

                    if (!_clientSocket.Connected)
                    {
                        _clientSocket.Shutdown(SocketShutdown.Both);
                        _clientSocket.Close();
                        Environment.Exit(0);
                    }

                }
                //Console.Write("Enter request: ");
                //string req = Console.ReadLine();

                //byte[] buffer = Encoding.Default.GetBytes(req);
                //_clientSocket.Send(buffer);
            }
            
        }

        private static void LoopConnect()
        {
            int attempts = 0;

            while (!_clientSocket.Connected)
                try
                {
                    attempts++;
                    _clientSocket.Connect(IPAddress.Loopback, 100);
                }
                catch (SocketException)
                {
                    Console.Clear();
                    Console.WriteLine("Попытка соединения: " + attempts.ToString());
                }

            Console.Clear();
            Console.WriteLine("Подключено");
        }

    }
}
