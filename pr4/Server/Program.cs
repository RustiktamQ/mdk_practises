using Common;
using Server.Classes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        public static List<User> Users = new();
        public static IPAddress IpAddress = IPAddress.Any;
        public static int Port = 5000;

        static async Task Main()
        {
            using (var context = new context())
            {
                context.Database.EnsureCreated();
                Users = context.Users.ToList();
                Console.WriteLine($"Loaded {Users.Count} users from database");
            }

            Console.WriteLine($"Server started on port {Port}");
            await StartAsync();
        }

        public static async Task StartAsync()
        {
            var listener = new TcpListener(IpAddress, Port);
            listener.Start();

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClientAsync(client));
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");
            using var stream = client.GetStream();

            try
            {
                while (true)
                {
                    // ---- READ LENGTH ----
                    byte[] lengthBuf = new byte[4];
                    int read = await ReadExactAsync(stream, lengthBuf, 4);
                    if (read == 0) break;

                    int length = BitConverter.ToInt32(lengthBuf, 0);

                    // ---- READ JSON ----
                    byte[] dataBuf = new byte[length];
                    await ReadExactAsync(stream, dataBuf, length);

                    string json = Encoding.UTF8.GetString(dataBuf);
                    var request = JsonConvert.DeserializeObject<ViewModelSend>(json);

                    var response = await ProcessCommandAsync(request);

                    // ---- SEND RESPONSE ----
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    byte[] responseLen = BitConverter.GetBytes(responseBytes.Length);

                    await stream.WriteAsync(responseLen);
                    await stream.WriteAsync(responseBytes);
                    await stream.FlushAsync();

                    if (request.Message == "exit")
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client error: {ex.Message}");
            }

            client.Close();
            Console.WriteLine("Client disconnected");
        }

        private static async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer, int size)
        {
            int total = 0;
            while (total < size)
            {
                int read = await stream.ReadAsync(buffer, total, size - total);
                if (read == 0) return 0;
                total += read;
            }
            return total;
        }

        private static async Task<string> ProcessCommandAsync(ViewModelSend vm)
        {
            try
            {
                var parts = vm.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (vm.Id == -1 && parts[0] != "connect")
                    return JsonConvert.SerializeObject(new ViewModelMessage("message", "Need authorization"));

                switch (parts[0])
                {
                    case "connect":
                        var user = Users.FirstOrDefault(x => x.Login == parts[1] && x.Password == parts[2]);
                        return user != null
                            ? JsonConvert.SerializeObject(new ViewModelMessage("autorization", Users.IndexOf(user).ToString()))
                            : JsonConvert.SerializeObject(new ViewModelMessage("message", "Invalid login"));

                    case "cd":
                        return HandleCd(vm);

                    case "get":
                        return await HandleGet(vm, parts);

                    default:
                        return await HandleUpload(vm);
                }
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new ViewModelMessage("message", ex.Message));
            }
        }

        private static string HandleCd(ViewModelSend vm)
        {
            var user = Users[vm.Id];

            if (vm.Message == "cd")
                user.TempSrc = user.Src;
            else
                user.TempSrc = Path.Combine(user.TempSrc, vm.Message[3..]);

            if (!Directory.Exists(user.TempSrc))
                return JsonConvert.SerializeObject(new ViewModelMessage("message", "Directory not found"));

            var files = Directory.GetDirectories(user.TempSrc).Select(x => Path.GetFileName(x) + "/")
                .Concat(Directory.GetFiles(user.TempSrc).Select(Path.GetFileName))
                .ToList();

            return JsonConvert.SerializeObject(new ViewModelMessage("cd", JsonConvert.SerializeObject(files)));
        }

        private static async Task<string> HandleGet(ViewModelSend vm, string[] parts)
        {
            var user = Users[vm.Id];
            var path = Path.Combine(user.TempSrc, string.Join(" ", parts.Skip(1)));

            if (!File.Exists(path))
                return JsonConvert.SerializeObject(new ViewModelMessage("message", "File not found"));

            var bytes = await File.ReadAllBytesAsync(path);
            return JsonConvert.SerializeObject(new ViewModelMessage("file", JsonConvert.SerializeObject(bytes)));
        }

        private static async Task<string> HandleUpload(ViewModelSend vm)
        {
            var user = Users[vm.Id];
            var file = JsonConvert.DeserializeObject<FileInfoFTP>(vm.Message);
            var path = Path.Combine(user.TempSrc, file.Name);

            await File.WriteAllBytesAsync(path, file.Data);
            return JsonConvert.SerializeObject(new ViewModelMessage("message", "File uploaded"));
        }
    }
}
