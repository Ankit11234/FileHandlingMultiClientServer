using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace MultiClientTcpServer
{
    internal class Program
    {
        private static TcpListener serverSocket;
        private static List<TcpClient> clients = new List<TcpClient>();
        private static string filesDirectory;
        private static FileSystemWatcher fileSystemWatcher;

        static async Task Main(string[] args)
        {

            IConfiguration configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
               .Build();

            filesDirectory = configuration["Server:FilesDirectory"];
            Directory.CreateDirectory(filesDirectory);
            IPAddress ipAd = IPAddress.Parse(configuration["Server:IPAddress"]);

            int port = int.Parse(configuration["Server:Port"]);

            serverSocket = new TcpListener(ipAd, port);
            serverSocket.Start();

            Console.WriteLine("***********Server Started *********");

            while (true)
            {
                TcpClient clientSocket = await serverSocket.AcceptTcpClientAsync();
                clients.Add(clientSocket);

                Console.WriteLine($"Accepted connection from client {clients.Count}");

                 Task.Run(() => HandleClient(clientSocket));
            }
        }



        private static async Task HandleClient(TcpClient clientSocket)
        {
            try
            {
                using (NetworkStream networkStream = clientSocket.GetStream())
                {
                    byte[] bytesFrom = new byte[10025];

                    while (true)
                    {
                        int bytesRead = await networkStream.ReadAsync(bytesFrom, 0, bytesFrom.Length);
                        if (bytesRead == 0)
                        {

                            Console.WriteLine($"Client {clientSocket.Client.RemoteEndPoint} disconnected.");
                            break;
                        }

                        string dataFromClient = Encoding.ASCII.GetString(bytesFrom, 0, bytesRead);
                        dataFromClient = dataFromClient.Substring(0, dataFromClient.IndexOf('\0'));

                        Console.WriteLine($"Data from client {clientSocket.Client.RemoteEndPoint}: {dataFromClient}");

                        if (dataFromClient.StartsWith("FILE:"))
                        {
                            string fileName = dataFromClient.Substring(5);
                            await ReceiveFile(networkStream, Path.Combine(filesDirectory, fileName));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client {clientSocket.Client.RemoteEndPoint}: {ex.Message}");
            }
            finally
            {
                clients.Remove(clientSocket);
                clientSocket.Close();
            }
        }

        private static async Task ReceiveFile(NetworkStream networkStream, string filePath)
        {
            using (FileStream fileStream = File.Create(filePath))
            {
                byte[] buffer = new byte[1024];
                int bytesRead;
                // for checking starts

                    bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                    fileStream.Write(buffer, 0, bytesRead);
                    InitializeFileSystemWatcher();


                // ends


                /*  while ((bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                  {
                      fileStream.Write(buffer, 0, bytesRead); 
                      InitializeFileSystemWatcher();
                  }
                */
            }
            Console.WriteLine($"File received and saved at: {filePath}");
        }

        private static void InitializeFileSystemWatcher()
        {
            fileSystemWatcher = new FileSystemWatcher(filesDirectory);

            fileSystemWatcher.Created += OnFileCreated;
            fileSystemWatcher.Changed += OnFileChanged;
            fileSystemWatcher.Deleted += OnFileDeleted;

            fileSystemWatcher.EnableRaisingEvents = true;
        }

        

        private static void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"File created: {e.FullPath}");

            string fileContent = ReadFileContent(e.FullPath);
            if (fileContent.Length > 0)
            {
                  Console.WriteLine($"File content:\n{fileContent}");

            }
        }

        private static void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"File changed: {e.FullPath}");

            string fileContent = ReadFileContent(e.FullPath);
            if (fileContent.Length > 0)
            {
                Console.WriteLine($"File content:\n{fileContent}");

            }
        }

        private static void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"File deleted: {e.FullPath}");
        }

        private static string ReadFileContent(string filePath)
        {
            try
            {
                
                return File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file content: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
