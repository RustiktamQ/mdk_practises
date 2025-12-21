using Newtonsoft.Json;
using System.IO;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using pr10.Response;
using pr10.Classes;
using pr10.Classes;
using pr10.Models;

namespace APIGigaChatImage
{
    class Program
    {
        static string ClientId = "019b4089-fe1b-7a5c-a167-2d3b664dc135";
        static string AuthorizationKey = "MDE5YjQwODktZmUxYi03YTVjLWExNjctMmQzYjY2NGRjMTM1OjdlNjBmNDk4LWZiODYtNGU2YS05MGJjLWVjZDdjMmYyYTVmNw==";
        
        static async Task Main(string[] args)
        {
            Console.Write("\nВведите описание: ");
            string userPrompt = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userPrompt))
                return;

            string prompt = userPrompt;
            string token = await GetToken(ClientId, AuthorizationKey);

            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Ошибка токена");
                return;
            }

            Console.WriteLine("Генерация...");
            string fileId = await GenerateImage(token, prompt);
            Console.WriteLine("Сгенерилось!!!");

            if (!string.IsNullOrEmpty(fileId))
            {
                Console.WriteLine($"Скачивание {fileId}...");
                string imagePath = $"gigaImage_{DateTime.Now:HHmmss}.jpg";
                bool downloaded = await DownloadImage(token, fileId, imagePath);

                if (downloaded && File.Exists(imagePath))
                {
                    WallpaperSetter.SetWallpaper(Path.GetFullPath(imagePath));
                    Console.WriteLine("Готово");
                }
            }

            Console.ReadKey();
        }

        public static async Task<string> GetToken(string rqUID, string bearer)
        {
            string token = null;
            string url = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";

            using (HttpClientHandler handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;

                using (HttpClient client = new HttpClient(handler))
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, url);

                    request.Headers.Add("Accept", "application/json");
                    request.Headers.Add("RqUID", rqUID);
                    request.Headers.Add("Authorization", $"Bearer {bearer}");

                    var data = new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS")
                    };

                    request.Content = new FormUrlEncodedContent(data);

                    var response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        ResponseToken tokenObj = JsonConvert.DeserializeObject<ResponseToken>(content);
                        token = tokenObj.access_token;
                    }
                }
            }

            return token;
        }

        public static async Task<string> GenerateImage(string token, string prompt)
        {
            try
            {
                string url = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";

                using (HttpClientHandler handler = new HttpClientHandler())
                {
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;

                    using (HttpClient client = new HttpClient(handler))
                    {
                        var body = new
                        {
                            model = "GigaChat",
                            messages = new[]
                            {
                                new { role = "system", content = "Ты — Василий Кандинский" },
                                new { role = "user", content = prompt }
                            },
                            function_call = "auto"
                        };

                        string json = JsonConvert.SerializeObject(body);

                        var request = new HttpRequestMessage(HttpMethod.Post, url);
                        request.Headers.Add("Accept", "application/json");
                        request.Headers.Add("Authorization", $"Bearer {token}");
                        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                        var response = await client.SendAsync(request);

                        if (response.IsSuccessStatusCode)
                        {
                            string content = await response.Content.ReadAsStringAsync();
                            dynamic jsonResponse = JsonConvert.DeserializeObject(content);
                            string imgContent = jsonResponse.choices[0].message.content;

                            int start = imgContent.IndexOf("src=\"") + 5;
                            int end = imgContent.IndexOf("\"", start);

                            if (end > start)
                            {
                                string fileId = imgContent.Substring(start, end - start);
                                return fileId;
                            }
                        }
                    }
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        public static async Task<bool> DownloadImage(string token, string fileId, string fileName)
        {
            try
            {
                string url = $"https://gigachat.devices.sberbank.ru/api/v1/files/{fileId}/content";

                using (HttpClientHandler handler = new HttpClientHandler())
                {
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;

                    using (HttpClient client = new HttpClient(handler))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, url);
                        request.Headers.Add("Accept", "application/jpg");
                        request.Headers.Add("Authorization", $"Bearer {token}");

                        var response = await client.SendAsync(request);

                        if (response.IsSuccessStatusCode)
                        {
                            byte[] data = await response.Content.ReadAsByteArrayAsync();
                            File.WriteAllBytes(fileName, data);
                            return true;
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }
}