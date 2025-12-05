using Common;
using Server.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Mime;

namespace Server
{
    class Program
    {
        public static List<User> Users = new List<User>();
        public static IPAddress IpAddress = IPAddress.Any;
        public static int Port = 5001;

        static async Task Main(string[] args)
        {
            try
            {
                using (var context = new context())
                {
                    context.Database.EnsureCreated();
                    Users = context.Users.ToList();
                    Console.WriteLine($"Loaded {Users.Count} users from database");
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Server starting on Port: {Port}");
                await StartAsync();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Server startup error: {ex.Message}");
                Console.ReadLine();
            }
        }

        public static bool AuthorizationUser(string login, string password)
        {
            User user = Users.Find(x => x.Login == login && x.Password == password);
            return user is not null;
        }

        public static List<string> GetDirectory(string src)
        {
            List<string> dir = new List<string>();
            if (Directory.Exists(src))
            {
                try
                {
                    string[] dirs = Directory.GetDirectories(src);
                    foreach (string dirName in dirs)
                    {
                        string Name = dirName.Replace(src, "").TrimStart('\\', '/');
                        dir.Add(Name + "/");
                    }
                    string[] files = Directory.GetFiles(src);
                    foreach (string file in files)
                    {
                        string Name = file.Replace(src, "").TrimStart('\\', '/');
                        dir.Add(Name);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Directory error: {ex.Message}");
                }
            }
            return dir;
        }

        public static async Task StartAsync()
        {
            try
            {
                IPEndPoint endPoint = new IPEndPoint(IpAddress, Port);
                Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                listener.Bind(endPoint);
                listener.Listen(10);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Server started and listening for connections...");

                while (true)
                {
                    Socket handler = await listener.AcceptAsync();
                    _ = Task.Run(() => HandleClientAsync(handler));
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Server error: {ex.Message}");
            }
        }

        private static async Task HandleClientAsync(Socket handler)
        {
            string clientInfo = handler.RemoteEndPoint?.ToString() ?? "unknown";
            Console.WriteLine($"Client connected: {clientInfo}");

            try
            {
                byte[] buffer = new byte[400000000];
                int bytesRec = await handler.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);

                if (bytesRec == 0)
                {
                    Console.WriteLine($"Client {clientInfo} disconnected");
                    return;
                }

                string data = Encoding.UTF8.GetString(buffer, 0, bytesRec);
                Console.WriteLine($"Received from {clientInfo}: {data}");

                ViewModelSend viewModelSend = JsonConvert.DeserializeObject<ViewModelSend>(data);
                string reply = await ProcessCommandAsync(viewModelSend);

                byte[] message = Encoding.UTF8.GetBytes(reply);
                await handler.SendAsync(new ArraySegment<byte>(message), SocketFlags.None);

                // Логируем команду в БД
                if (viewModelSend != null && viewModelSend.Id != -1)
                {
                    await LogCommandAsync(viewModelSend);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error with client {clientInfo}: {ex.Message}");
            }
            finally
            {
                try
                {
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                    Console.WriteLine($"Client {clientInfo} disconnected");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error closing client {clientInfo}: {ex.Message}");
                }
            }
        }

        private static async Task<string> ProcessCommandAsync(ViewModelSend viewModelSend)
        {
            if (viewModelSend == null)
            {
                return JsonConvert.SerializeObject(new ViewModelMessage("message", "Invalid request"));
            }

            ViewModelMessage viewModelMessage;
            string[] dataCommand = viewModelSend.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            try
            {
                if (viewModelSend.Id == -1 && dataCommand[0] != "connect")
                {
                    return JsonConvert.SerializeObject(new ViewModelMessage("message", "Need authorization"));
                }

                switch (dataCommand[0])
                {
                    case "connect":
                        if (dataCommand.Length >= 3 && AuthorizationUser(dataCommand[1], dataCommand[2]))
                        {
                            int userId = Users.FindIndex(x => x.Login == dataCommand[1] && x.Password == dataCommand[2]);
                            viewModelMessage = new ViewModelMessage("autorization", userId.ToString());
                        }
                        else
                        {
                            viewModelMessage = new ViewModelMessage("message", "Invalid login or password");
                        }
                        break;

                    case "cd":
                        Console.WriteLine("Processing CD command...");
                        var user = Users.ElementAtOrDefault(viewModelSend.Id);
                        if (user == null)
                        {
                            Console.WriteLine($"User not found for ID: {viewModelSend.Id}");
                            viewModelMessage = new ViewModelMessage("message", "User not found");
                            break;
                        }

                        Console.WriteLine($"User: {user.Login}, Current Src: {user.Src}, Current TempSrc: {user.TempSrc}");

                        if (dataCommand.Length == 1)
                        {
                            user.TempSrc = user.Src;
                            Console.WriteLine($"Reset to root directory: {user.TempSrc}");
                        }
                        else
                        {
                            string path = string.Join(" ", dataCommand.Skip(1));
                            user.TempSrc = Path.Combine(user.TempSrc, path);
                            Console.WriteLine($"New directory path: {user.TempSrc}");
                        }

                        // Проверяем существование директории
                        if (!Directory.Exists(user.TempSrc))
                        {
                            Console.WriteLine($"Directory does not exist: {user.TempSrc}");
                            viewModelMessage = new ViewModelMessage("message", "Directory does not exist");
                            break;
                        }

                        var folders = GetDirectory(user.TempSrc);
                        Console.WriteLine($"Found {folders.Count} items in directory");

                        if (folders.Count == 0)
                        {
                            viewModelMessage = new ViewModelMessage("message", "Directory is empty");
                        }
                        else
                        {
                            string serializedFolders = JsonConvert.SerializeObject(folders);
                            Console.WriteLine($"Serialized folders: {serializedFolders}");
                            viewModelMessage = new ViewModelMessage("cd", serializedFolders);
                        }
                        break;

                    case "get":
                        var currentUser = Users.ElementAtOrDefault(viewModelSend.Id);
                        if (currentUser == null)
                        {
                            viewModelMessage = new ViewModelMessage("message", "User not found");
                            break;
                        }

                        string fileName = string.Join(" ", dataCommand.Skip(1));
                        string filePath = Path.Combine(currentUser.TempSrc, fileName);

                        if (File.Exists(filePath))
                        {
                            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                            viewModelMessage = new ViewModelMessage("file", JsonConvert.SerializeObject(fileBytes));
                        }
                        else
                        {
                            viewModelMessage = new ViewModelMessage("message", "File not found");
                        }
                        break;

                    default:
                        // Upload file
                        var uploadUser = Users.ElementAtOrDefault(viewModelSend.Id);
                        if (uploadUser != null)
                        {
                            FileInfoFTP fileInfo = JsonConvert.DeserializeObject<FileInfoFTP>(viewModelSend.Message);
                            string uploadPath = Path.Combine(uploadUser.TempSrc, fileInfo.Name);
                            await File.WriteAllBytesAsync(uploadPath, fileInfo.Data);
                            viewModelMessage = new ViewModelMessage("message", "File uploaded successfully");
                        }
                        else
                        {
                            viewModelMessage = new ViewModelMessage("message", "User not found");
                        }
                        break;
                }

                return JsonConvert.SerializeObject(viewModelMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command processing error: {ex.Message}");
                return JsonConvert.SerializeObject(new ViewModelMessage("message", $"Error: {ex.Message}"));
            }
        }

        private static async Task LogCommandAsync(ViewModelSend viewModelSend)
        {
            try
            {
                using var context = new context();
                var command = new Command
                {
                    User = Users[viewModelSend.Id].Id,
                    command = viewModelSend.Message
                };
                context.Commands.Add(command);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
            }
        }
    }
}