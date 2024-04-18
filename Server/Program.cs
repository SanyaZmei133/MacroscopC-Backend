using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace MacroscopTest
{
    class Program
    {
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(3);
        private static byte[] _buffer = new byte[1024];
        private static List<Socket> _clientSockets = new List<Socket>();
        private static Socket _serverSocket = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static void Main(string[] args)
        {
            Console.Title = "Server";
            SetupServer();
            Console.ReadLine();
            CloseAllSockets();

        }
        private static void SetupServer()
        {
            Console.WriteLine("Подготовка сервера...");
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, 100));
            _serverSocket.Listen(1);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptedCallback), null);
        }

        private static void AcceptedCallback(IAsyncResult ar)
        {
            Socket socket = _serverSocket.EndAccept(ar);
            _clientSockets.Add(socket);
            Console.WriteLine("Клиент Подключился");
            socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptedCallback), null);
        }

        private static async void ReceiveCallback(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState;
            int received;

            try
            {
                received = socket.EndReceive(ar);
            }
            catch (SocketException)
            {
                Console.WriteLine("Client forcefully disconnected");
                socket.Close();
                _clientSockets.Remove(socket);
                return;
            }

            byte[] dataBuff = new byte[received];
            Array.Copy(_buffer, dataBuff, received);
            string text = Encoding.Default.GetString(dataBuff);
            Console.WriteLine("Received: " + text);

            var tasks = new List<Task>();

            string response = string.Empty;

            await semaphoreSlim.WaitAsync();
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    if (isPolindrome(text))
                    {
                        response = "Содержимое файла является полиндромом";
                    }
                    else
                    {
                        response = "Содержимое файла не является полиндромом";
                    }

                    Console.WriteLine($"Start task, CurrentCount: {semaphoreSlim.CurrentCount}");
                    Thread.Sleep(1000);
                    Console.WriteLine($"End task, CurrentCount: {semaphoreSlim.CurrentCount}");

                    byte[] data = Encoding.Default.GetBytes(response);
                    socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
                }
                finally
                {
                    semaphoreSlim.Release();
                }
            }));
            
            Task.WaitAll(tasks.ToArray());
            
            socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
        }

        private static bool isPolindrome(string? text)
        {
            text = text.ToLower();
            string[] words = text.Split(['.', ',', ' ', ';']);
            foreach (string word in words)
                for (int i = 0, j = word.Length - 1; i < j; i++, j--)
                {
                    if (word[i] != word[j])
                        return false;
                }
            return true;
        }

        private static void SendCallback(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState;
            socket.EndSend(ar);
        }

        private static void CloseAllSockets()
        {
            foreach (Socket socket in _clientSockets)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            _serverSocket.Close();
        }

    }
}
