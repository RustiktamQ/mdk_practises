using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net.Http;
using Nancy.Json;

class Program
{
    public static string domain = "news.permaviat-local.ru";

    class ConsoleAndFileWriter : TextWriter
    {
        private TextWriter consoleWriter;
        private StreamWriter fileWriter;

        public ConsoleAndFileWriter(string filePath)
        {
            consoleWriter = Console.Out;
            fileWriter = new StreamWriter(filePath, true, Encoding.UTF8);
            fileWriter.AutoFlush = true;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void WriteLine(string value)
        {
            consoleWriter.WriteLine(value);
            fileWriter.WriteLine(value);
        }

        public override void Write(char value)
        {
            consoleWriter.Write(value);
            fileWriter.Write(value);
        }
    }

    static Program()
    {
        Console.SetOut(new ConsoleAndFileWriter("output_log.txt"));
    }

    static void Main()
    {
        Console.WriteLine("Выберите режим:\n1) Парсинг сайта news.permaviat-local.ru\n2) Парсинг DummyJSON fake API");
        string choice = Console.ReadLine()?.Trim();

        if (choice == "1")
        {
            CookieContainer cookies = new CookieContainer();
            string login = "user";
            string password = "user";
            SingInWithCookies(login, password, cookies);

            string pageContent = GetContent("main", cookies);
            ParsingHtml(pageContent);
        }
        else if (choice == "2")
        {
            string json = GetJsonContentAsync("https://dummyjson.com/posts").GetAwaiter().GetResult();
            ParseDummyJson(json);
        }
        else
        {
            Console.WriteLine("Неверный выбор");
        }

        Console.WriteLine("Готово. Нажмите Enter для выхода.");
        Console.ReadLine();
    }

    public static void SingInWithCookies(string Login, string Password, CookieContainer cookies)
    {
        string url = $"http://{domain}/ajax/login.php";
        Console.WriteLine($"Выполняем запрос: {url}");

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "POST";
        request.ContentType = "application/x-www-form-urlencoded";
        request.CookieContainer = cookies;

        string postData = $"login={Login}&password={Password}";
        byte[] Data = Encoding.ASCII.GetBytes(postData);
        request.ContentLength = Data.Length;

        using (var stream = request.GetRequestStream())
        {
            stream.Write(Data, 0, Data.Length);
        }

        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        Console.WriteLine($"Статус выполнения: {response.StatusCode}");

        using (var reader = new StreamReader(response.GetResponseStream()))
        {
            reader.ReadToEnd();
        }
    }

    public static string GetContent(string relativeUrl, CookieContainer cookies)
    {
        string url = $"http://{domain}/{relativeUrl}";
        Console.WriteLine($"Получаем страницу: {url}");

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "GET";
        request.CookieContainer = cookies;

        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        string responseText;
        using (var reader = new StreamReader(response.GetResponseStream()))
        {
            responseText = reader.ReadToEnd();
        }

        return responseText;
    }

    public static void ParsingHtml(string htmlCode)
    {
        var html = new HtmlDocument();
        html.LoadHtml(htmlCode);
        var document = html.DocumentNode;
        var divsNews = document.Descendants().Where(n => n.HasClass("news"));
        foreach (var div in divsNews)
        {
            var src = div.ChildNodes[1].GetAttributeValue("src", "none");
            var name = div.ChildNodes[3].InnerText;
            var description = div.ChildNodes[5].InnerText;
            Console.WriteLine(name + "\nИзображение: " + src + "\nОписание: " + description + "\n");
        }
    }

    public static async Task<string> GetJsonContentAsync(string url)
    {
        Console.WriteLine($"Выполняем GET: {url}");
        using (HttpClient client = new HttpClient())
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }

    public class DummyPost
    {
        public int id { get; set; }
        public string title { get; set; }
        public string body { get; set; }
        public int userId { get; set; }
    }

    public class DummyResponse
    {
        public List<DummyPost> posts { get; set; }
    }

    public static void ParseDummyJson(string json)
    {
        var serializer = new JavaScriptSerializer();
        var resp = serializer.Deserialize<DummyResponse>(json);

        foreach (var post in resp.posts)
        {
            Console.WriteLine($"Post ID: {post.id}\nUser ID: {post.userId}\nTitle: {post.title}\nBody: {post.body}\n");
        }
    }
}
