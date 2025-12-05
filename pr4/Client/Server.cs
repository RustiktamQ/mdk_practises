using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Client
{
    public class Server
    {
        private IPAddress IP;
        private IPEndPoint EndPoint;
        private Socket Socket;
        public string Ip { get; private set; }
        public int Port { get; private set; }
        public int ClientId { get; set; }
        public bool Connected { get; private set; }

        public MainWindow MainWindow { get; set; }

        public Server(string ip = "127.0.0.1", int port = 5001)
        {
            ClientId = -1;
            Ip = ip;
            Port = port;
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                IP = IPAddress.Parse(Ip);
                EndPoint = new IPEndPoint(IP, Port);

                Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                Socket.ReceiveTimeout = 10000;
                Socket.SendTimeout = 10000;

                await Socket.ConnectAsync(EndPoint);
                Connected = Socket.Connected;

                if (Connected)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Successfully connected to {Ip}:{Port}",
                            "Connection", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                    return true;
                }
            }
            catch (Exception ex)
            {
                Connected = false;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Connection failed: {ex.Message}",
                        "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            return false;
        }

        private bool IsSocketConnected()
        {
            try
            {
                if (Socket == null) return false;

                bool part1 = Socket.Poll(1000, SelectMode.SelectRead);
                bool part2 = (Socket.Available == 0);
                if (part1 && part2)
                    return false;
                else
                    return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SendAsync(string message, string? file = null)
        {
            if (!Connected || !IsSocketConnected())
            {
                Connected = await ConnectAsync();
                if (!Connected|| !IsSocketConnected())
                {
                    MessageBox.Show("Not connected to server");
                    return false;
                }
            }

            if (!CheckCommand(message))
            {
                MessageBox.Show("Invalid command");
                return false;
            }

            try
            {
                
                ViewModelSend send = new ViewModelSend(message, ClientId);

                if (message.StartsWith("set "))
                {
                    string[] dataMessage = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    string fileName = string.Join(" ", dataMessage, 1, dataMessage.Length - 1);

                    if (File.Exists(file))
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        byte[] fileData = await File.ReadAllBytesAsync(file);
                        FileInfoFTP fileInfoFTP = new FileInfoFTP(fileData, fileInfo.Name);
                        send = new ViewModelSend(JsonConvert.SerializeObject(fileInfoFTP), ClientId);
                    }
                    else
                    {
                        MessageBox.Show($"File not found: {fileName}");
                        return false;
                    }
                }

                string jsonData = JsonConvert.SerializeObject(send);
                byte[] bytes = Encoding.UTF8.GetBytes(jsonData);

                // Отправка данных
                int bytesSent = await Socket.SendAsync(new ArraySegment<byte>(bytes), SocketFlags.None);
                if (bytesSent == 0)
                {
                    MessageBox.Show("Failed to send data");
                    return false;
                }

                byte[] buffer = new byte[400000000];
                int bytesReceived = await Socket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);

                if (bytesReceived == 0)
                {
                    MessageBox.Show("Server disconnected");
                    Connected = false;
                    return false;
                }

                string response = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                ViewModelMessage responseMessage = JsonConvert.DeserializeObject<ViewModelMessage>(response);

                await ProcessServerResponseAsync(responseMessage, message);
                return true;
            }
            catch (Exception ex)
            {
                Connected = false;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Communication error: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                return false;
            }
        }

        private async Task ProcessServerResponseAsync(ViewModelMessage response, string originalMessage)
        {
            if (response == null) return;

            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    switch (response.Command)
                    {
                        case "autorization":
                            ClientId = int.Parse(response.Data);
                            MessageBox.Show("Authorization successful!", "Success",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            break;

                        case "message":
                            if (MainWindow != null)
                                MainWindow.ShowMessage(response.Data);
                            break;

                        case "cd":
                            List<string> folders = JsonConvert.DeserializeObject<List<string>>(response.Data);
                            if (MainWindow != null)
                                MainWindow.UpdateFileList(folders);
                            break;

                        case "file":
                            string[] messageParts = originalMessage.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            string fileName = string.Join(" ", messageParts, 1, messageParts.Length - 1);

                            byte[] fileData = JsonConvert.DeserializeObject<byte[]>(response.Data);

                            var saveDialog = new Microsoft.Win32.SaveFileDialog
                            {
                                FileName = fileName,
                                Filter = "All files (*.*)|*.*"
                            };

                            if (saveDialog.ShowDialog() == true)
                            {
                                await File.WriteAllBytesAsync(saveDialog.FileName, fileData);
                                MessageBox.Show($"File downloaded: {fileName}", "Success",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error processing response: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        private bool CheckCommand(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;

            string[] dataMessage = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (dataMessage.Length == 0) return false;

            switch (dataMessage[0])
            {
                case "connect":
                    return dataMessage.Length == 3;
                case "cd":
                    return true;
                case "get":
                    return dataMessage.Length >= 2;
                case "set":
                    return dataMessage.Length >= 2;
                default:
                    return true;
            }
        }

        public void Disconnect()
        {
            try
            {
                Connected = false;
                Socket?.Shutdown(SocketShutdown.Both);
                Socket?.Close();
                Socket?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Disconnect error: {ex.Message}");
            }
        }
    }
}