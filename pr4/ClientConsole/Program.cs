using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FTPConsoleClient
{
    // Классы должны точно соответствовать серверным
    public class ViewModelSend
    {
        public string Message { get; set; }
        public int Id { get; set; }

        public ViewModelSend(string message, int id)
        {
            Message = message;
            Id = id;
        }
    }

    public class ViewModelMessage
    {
        public string Command { get; set; }
        public string Data { get; set; }
    }

    public class FileInfoFTP
    {
        public byte[] Data { get; set; }
        public string Name { get; set; }
    }

    class Program
    {
        private static TcpClient _client;
        private static NetworkStream _stream;
        private static bool _isConnected = false;
        private static int _userId = -1;
        private static string _currentDirectory = "/";

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== FTP Console Client ===");
            Console.WriteLine("Commands: connect, cd [path], get [file], set [file], ls, exit, help");

            while (true)
            {
                try
                {
                    Console.Write($"{_currentDirectory}> ");
                    var input = Console.ReadLine()?.Trim();

                    if (string.IsNullOrEmpty(input))
                        continue;

                    var parts = input.Split(' ', 2);
                    var command = parts[0].ToLower();

                    switch (command)
                    {
                        case "connect":
                            await ConnectCommand(parts.Length > 1 ? parts[1] : null);
                            break;
                        case "cd":
                            await ChangeDirectoryCommand(parts.Length > 1 ? parts[1] : "");
                            break;
                        case "get":
                            if (parts.Length > 1)
                                await DownloadFileCommand(parts[1]);
                            else
                                Console.WriteLine("Usage: get <filename>");
                            break;
                        case "set":
                            if (parts.Length > 1)
                                await UploadFileCommand(parts[1]);
                            else
                                Console.WriteLine("Usage: set <filename>");
                            break;
                        case "ls":
                            await ListFilesCommand();
                            break;
                        case "exit":
                            await Disconnect();
                            Console.WriteLine("Goodbye!");
                            return;
                        case "help":
                            ShowHelp();
                            break;
                        default:
                            Console.WriteLine($"Unknown command: {command}. Type 'help' for commands list.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine("  connect [server:port] - Connect to FTP server (default: 127.0.0.1:5000)");
            Console.WriteLine("  cd [path] - Change directory (use 'cd' for root, 'cd ..' for parent)");
            Console.WriteLine("  get <filename> - Download file");
            Console.WriteLine("  set <filename> - Upload file");
            Console.WriteLine("  ls - List files in current directory");
            Console.WriteLine("  exit - Exit program");
        }

        static async Task ConnectCommand(string serverInfo = null)
        {
            if (_isConnected)
            {
                Console.WriteLine("Already connected. Use 'exit' to disconnect first.");
                return;
            }

            string server = "127.0.0.1";
            int port = 5000;

            if (!string.IsNullOrEmpty(serverInfo))
            {
                var serverParts = serverInfo.Split(':');
                server = serverParts[0];
                if (serverParts.Length > 1)
                    port = int.Parse(serverParts[1]);
            }

            Console.Write("Login: ");
            var login = Console.ReadLine() ?? "1";

            Console.Write("Password: ");
            var password = ReadPassword();

            try
            {
                Console.WriteLine($"Connecting to {server}:{port}...");
                _client = new TcpClient();
                await _client.ConnectAsync(server, port);
                _stream = _client.GetStream();

                // Создаем ViewModelSend для подключения
                var viewModelSend = new ViewModelSend($"connect {login} {password}", -1);

                // Сериализуем в JSON и отправляем
                string jsonData = JsonConvert.SerializeObject(viewModelSend);
                byte[] dataBytes = Encoding.UTF8.GetBytes(jsonData);

                // Отправляем длину данных
                byte[] lengthBytes = BitConverter.GetBytes(dataBytes.Length);
                await _stream.WriteAsync(lengthBytes, 0, lengthBytes.Length);

                // Отправляем сами данные
                await _stream.WriteAsync(dataBytes, 0, dataBytes.Length);
                await _stream.FlushAsync();

                // Читаем ответ
                var response = await ReadResponse();

                if (response?.Command == "autorization")
                {
                    _userId = int.Parse(response.Data);
                    _isConnected = true;
                    Console.WriteLine("Connected successfully! User ID: " + _userId);
                    await ListFilesCommand();
                }
                else
                {
                    Console.WriteLine($"Connection failed: {response?.Data}");
                    await Disconnect();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection error: {ex.Message}");
                await Disconnect();
            }
        }

        static async Task<ViewModelMessage> ReadResponse()
        {
            try
            {
                // Читаем длину ответа
                byte[] lengthBytes = new byte[4];
                int bytesRead = await _stream.ReadAsync(lengthBytes, 0, 4);
                if (bytesRead < 4) return null;

                int length = BitConverter.ToInt32(lengthBytes, 0);

                // Читаем сам ответ
                byte[] buffer = new byte[length];
                int totalRead = 0;
                while (totalRead < length)
                {
                    bytesRead = await _stream.ReadAsync(buffer, totalRead, length - totalRead);
                    if (bytesRead == 0) break;
                    totalRead += bytesRead;
                }

                string jsonResponse = Encoding.UTF8.GetString(buffer, 0, totalRead);
                return JsonConvert.DeserializeObject<ViewModelMessage>(jsonResponse);
            }
            catch
            {
                return null;
            }
        }

        static async Task SendCommand(string message)
        {
            if (!_isConnected) return;

            var viewModelSend = new ViewModelSend(message, _userId);
            string jsonData = JsonConvert.SerializeObject(viewModelSend);
            byte[] dataBytes = Encoding.UTF8.GetBytes(jsonData);

            // Отправляем длину данных
            byte[] lengthBytes = BitConverter.GetBytes(dataBytes.Length);
            await _stream.WriteAsync(lengthBytes, 0, lengthBytes.Length);

            // Отправляем сами данные
            await _stream.WriteAsync(dataBytes, 0, dataBytes.Length);
            await _stream.FlushAsync();
        }

        static async Task ChangeDirectoryCommand(string path)
        {
            if (!_isConnected)
            {
                Console.WriteLine("Not connected. Use 'connect' first.");
                return;
            }

            await SendCommand($"cd {path}");
            var response = await ReadResponse();

            if (response != null)
            {
                if (response.Command == "cd")
                {
                    // Обновляем текущую директорию
                    if (path == "")
                        _currentDirectory = "/";
                    else if (path == "..")
                        _currentDirectory = Path.GetDirectoryName(_currentDirectory.TrimEnd('/'))?.Replace('\\', '/') ?? "/";
                    else if (path.StartsWith("/"))
                        _currentDirectory = path;
                    else
                        _currentDirectory = (_currentDirectory == "/" ? "" : _currentDirectory) + "/" + path;

                    _currentDirectory = _currentDirectory.Replace("//", "/");
                    if (_currentDirectory == "") _currentDirectory = "/";

                    Console.WriteLine($"Current directory: {_currentDirectory}");

                    // Показываем файлы из полученного списка
                    var files = JsonConvert.DeserializeObject<List<string>>(response.Data);
                    DisplayFiles(files);
                }
                else
                {
                    Console.WriteLine($"Failed to change directory: {response.Data}");
                }
            }
        }

        static async Task ListFilesCommand()
        {
            if (!_isConnected)
            {
                Console.WriteLine("Not connected. Use 'connect' first.");
                return;
            }

            await SendCommand("cd");
            var response = await ReadResponse();

            if (response != null && response.Command == "cd")
            {
                var files = JsonConvert.DeserializeObject<List<string>>(response.Data);
                DisplayFiles(files);
            }
            else if (response != null)
            {
                Console.WriteLine($"Error: {response.Data}");
            }
        }

        static void DisplayFiles(List<string> files)
        {
            Console.WriteLine($"\nFiles in {_currentDirectory}:");
            Console.WriteLine(new string('-', 50));
            Console.WriteLine("{0,-30} {1,-10}", "Name", "Type");
            Console.WriteLine(new string('-', 50));

            if (files == null || files.Count == 0)
            {
                Console.WriteLine("Directory is empty");
            }
            else
            {
                foreach (var file in files)
                {
                    bool isDirectory = file.EndsWith("/");
                    string name = isDirectory ? file.Substring(0, file.Length - 1) : file;
                    string type = isDirectory ? "Folder" : "File";

                    Console.WriteLine("{0,-30} {1,-10}", name, type);
                }
            }
            Console.WriteLine();
        }

        static async Task DownloadFileCommand(string fileName)
        {
            if (!_isConnected)
            {
                Console.WriteLine("Not connected. Use 'connect' first.");
                return;
            }

            Console.Write($"Save as [{fileName}]: ");
            var savePath = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(savePath))
                savePath = fileName;

            try
            {
                await SendCommand($"get {fileName}");
                var response = await ReadResponse();

                if (response != null && response.Command == "file")
                {
                    // Десериализуем байты файла
                    var fileBytes = JsonConvert.DeserializeObject<byte[]>(response.Data);

                    Console.WriteLine($"Downloading {fileName} ({FormatSize(fileBytes.Length)})...");

                    await File.WriteAllBytesAsync(savePath, fileBytes);
                    Console.WriteLine($"File downloaded successfully to: {savePath}");
                }
                else if (response != null)
                {
                    Console.WriteLine($"Download failed: {response.Data}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Download error: {ex.Message}");
            }
        }

        static async Task UploadFileCommand(string fileName)
        {
            if (!_isConnected)
            {
                Console.WriteLine("Not connected. Use 'connect' first.");
                return;
            }

            if (!File.Exists(fileName))
            {
                Console.WriteLine($"File not found: {fileName}");
                return;
            }

            try
            {
                var fileBytes = await File.ReadAllBytesAsync(fileName);
                var fileInfo = new FileInfoFTP
                {
                    Name = Path.GetFileName(fileName),
                    Data = fileBytes
                };

                string fileInfoJson = JsonConvert.SerializeObject(fileInfo);

                // Создаем ViewModelSend с сериализованным FileInfoFTP
                var viewModelSend = new ViewModelSend(fileInfoJson, _userId);
                string jsonData = JsonConvert.SerializeObject(viewModelSend);
                byte[] dataBytes = Encoding.UTF8.GetBytes(jsonData);

                // Отправляем длину данных
                byte[] lengthBytes = BitConverter.GetBytes(dataBytes.Length);
                await _stream.WriteAsync(lengthBytes, 0, lengthBytes.Length);

                // Отправляем сами данные
                await _stream.WriteAsync(dataBytes, 0, dataBytes.Length);
                await _stream.FlushAsync();

                Console.WriteLine($"Uploading {fileName} ({FormatSize(fileBytes.Length)})...");

                var response = await ReadResponse();

                if (response != null && response.Command == "message")
                {
                    Console.WriteLine($"Upload result: {response.Data}");
                }
                else
                {
                    Console.WriteLine($"Upload failed. Response command: {response?.Command}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload error: {ex.Message}");
            }
        }

        static async Task Disconnect()
        {
            if (_isConnected && _client != null)
            {
                try
                {
                    // Отправляем команду exit
                    if (_stream != null)
                    {
                        var viewModelSend = new ViewModelSend("exit", _userId);
                        string jsonData = JsonConvert.SerializeObject(viewModelSend);
                        byte[] dataBytes = Encoding.UTF8.GetBytes(jsonData);

                        byte[] lengthBytes = BitConverter.GetBytes(dataBytes.Length);
                        await _stream.WriteAsync(lengthBytes, 0, lengthBytes.Length);
                        await _stream.WriteAsync(dataBytes, 0, dataBytes.Length);
                        await _stream.FlushAsync();
                    }
                }
                catch { }

                _stream?.Dispose();
                _client?.Close();

                _isConnected = false;
                _userId = -1;
                _currentDirectory = "/";
                Console.WriteLine("Disconnected.");
            }
        }

        static string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return string.Format("{0:0.##} {1}", len, sizes[order]);
        }

        static string ReadPassword()
        {
            var password = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                    {
                        password.Remove(password.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    password.Append(key.KeyChar);
                    Console.Write("*");
                }
            }
            return password.ToString();
        }
    }
}