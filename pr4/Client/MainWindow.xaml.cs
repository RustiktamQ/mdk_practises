using Microsoft.Win32;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client
{
    public partial class MainWindow : Window
    {
        private Server server;
        private List<FileItem> currentFiles = new List<FileItem>();

        public class FileItem
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string Size { get; set; }
            public bool IsDirectory { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
            server = new Server();
            server.MainWindow = this;
            UpdateStatus("Disconnected");
        }

        private void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            var testFiles = new List<string>
            {
                "TestFolder1/",
                "TestFolder2/",
                "testfile1.txt",
                "testfile2.doc"
            };

            UpdateFileList(testFiles);
            UpdateStatus("Test data loaded");
        }

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string login = txtLogin.Text;
                string password = txtPassword.Password;

                if (await server.ConnectAsync())
                {
                    string command = $"connect {login} {password}";
                    if (await server.SendAsync(command))
                    {
                        UpdateStatus("Connected successfully");
                        txtConnectionStatus.Text = "Connected";
                        RefreshFileList();
                    }
                    else
                    {
                        UpdateStatus("Connection failed - check credentials");
                        txtConnectionStatus.Text = "Connection failed";
                    }
                }
                else
                {
                    UpdateStatus("Server connection failed");
                    txtConnectionStatus.Text = "Server unavailable";
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Connection error: {ex.Message}");
            }
        }

        private async void RefreshFileList()
        {
            if (await server.SendAsync("cd"))
            {
                UpdateStatus("Directory refreshed");
            }
        }

        private async void ListViewFiles_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectedItem = listViewFiles.SelectedItem as FileItem;
            if (selectedItem != null)
            {
                if (selectedItem.IsDirectory)
                {
                    if (await server.SendAsync($"cd {selectedItem.Name}"))
                    {
                        UpdateStatus($"Entered directory: {selectedItem.Name}");
                    }
                }
                else
                {
                    DownloadFile(selectedItem.Name);
                }
            }
        }

        private async void BtnUpload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                if (openFileDialog.ShowDialog() == true)
                {
                    string fileName = System.IO.Path.GetFileName(openFileDialog.FileName);
                    if (await server.SendAsync($"set {fileName}", openFileDialog.FileName))
                    {
                        UpdateStatus($"File uploaded: {fileName}");
                        RefreshFileList();
                    }
                    else
                    {
                        UpdateStatus("Upload failed");
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Upload error: {ex.Message}");
            }
        }

        private void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = listViewFiles.SelectedItem as FileItem;
            if (selectedItem != null && !selectedItem.IsDirectory)
            {
                DownloadFile(selectedItem.Name);
            }
            else
            {
                UpdateStatus("Please select a file to download");
            }
        }

        private async void DownloadFile(string fileName)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = fileName;
                if (saveFileDialog.ShowDialog() == true)
                {
                    if (await server.SendAsync($"get {fileName}"))
                    {
                        UpdateStatus($"File downloaded: {fileName}");
                    }
                    else
                    {
                        UpdateStatus("Download failed");
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Download error: {ex.Message}");
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshFileList();
        }

        private async void BtnRoot_Click(object sender, RoutedEventArgs e)
        {
            if (await server.SendAsync("cd"))
            {
                UpdateStatus("Returned to root directory");
            }
        }

        private void UpdateStatus(string message)
        {
            txtStatus.Text = message;
            Console.WriteLine($"Status: {message}");
        }

        public void UpdateFileList(List<string> files)
        {
            try
            {
                Console.WriteLine($"UpdateFileList called with {files?.Count} files");

                if (files == null)
                {
                    Console.WriteLine("Files list is null");
                    return;
                }

                currentFiles.Clear();

                foreach (string file in files)
                {
                    try
                    {
                        bool isDirectory = file.EndsWith("/");
                        string name = isDirectory ? file.Substring(0, file.Length - 1) : file;

                        string size = "";
                        if (!isDirectory)
                        {
                            size = "N/A";
                        }

                        currentFiles.Add(new FileItem
                        {
                            Name = name,
                            Type = isDirectory ? "Folder" : "File",
                            Size = size,
                            IsDirectory = isDirectory
                        });

                        Console.WriteLine($"Added item: {name} ({(isDirectory ? "Folder" : "File")})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing file item '{file}': {ex.Message}");
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        listViewFiles.ItemsSource = null;
                        listViewFiles.ItemsSource = currentFiles;
                        Console.WriteLine($"ListView updated with {currentFiles.Count} items");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating ListView: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateFileList: {ex}");
            }
        }

        public void ShowMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, "Server Message", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
    }
}