using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
//using Microsoft.Extensions.Configuration;

namespace MultiClientTcpClient
{
    internal class Program
    {
        private static string serverIpAddress;
        private static int serverPort;

        static void Main(string[] args)
        {

            Console.WriteLine("Enter your name:");
            string clientName = Console.ReadLine();

            using (TcpClient clientSocket = new TcpClient())
            {

                clientSocket.Connect("127.0.0.1", 8888);
                Console.WriteLine($"Connected to the server.");

                try
                {
                    using (NetworkStream serverStream = clientSocket.GetStream())
                    {
                        while (true)
                        {
                            Console.Write($"{clientName}: ");
                            string input = Console.ReadLine();

                            if (string.IsNullOrEmpty(input))
                                continue;

                            if (input.ToLower() == "exit")
                                break;

                            if (input.ToLower() == "file")
                            {
                                Console.Write("Enter the path of the file to send: ");
                                string filePath = Console.ReadLine();

                                if (File.Exists(filePath))
                                {
                                    SendFile(serverStream, filePath);
                                }
                                else
                                {
                                    Console.WriteLine("File not found.");
                                }

                                continue;
                            }

                            string message = $"{clientName}: {input}\0";
                            byte[] outStream = Encoding.ASCII.GetBytes(message);
                            serverStream.Write(outStream, 0, outStream.Length);
                            serverStream.Flush();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }



        private static void SendFile(NetworkStream networkStream, string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            byte[] fileNameBytes = Encoding.ASCII.GetBytes($"FILE:{fileName}\0");
            networkStream.Write(fileNameBytes, 0, fileNameBytes.Length);
            networkStream.Flush();

            using (FileStream fileStream = File.OpenRead(filePath))
            {
                byte[] buffer = new byte[1024];
                int bytesRead;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    networkStream.Write(buffer, 0, bytesRead);
                }
            }

            Console.WriteLine($"File '{fileName}' sent successfully.");
        }
    }
}
