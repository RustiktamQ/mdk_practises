using System;
using System.IO;
using System.Net;
using System.Text;
using System.Diagnostics;

class Program
{
    public static string domain = "news.permaviat-local.ru";

    public static void SingIn(string Login, string Password)
    {
        string url = $"http://{domain}/ajax/login.php";
        Debug.WriteLine($"Выполняем запрос: {url}");

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "POST";
        request.ContentType = "application/x-www-form-urlencoded";
        request.CookieContainer = new CookieContainer();

        string postData = $"login={Login}&password={Password}";
        byte[] Data = Encoding.ASCII.GetBytes(postData);
        request.ContentLength = Data.Length;

        using (var stream = request.GetRequestStream())
        {
            stream.Write(Data, 0, Data.Length);
        }

        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        Debug.WriteLine($"Статус выполнения: {response.StatusCode}");

        string responseFromServer;
        using (var reader = new StreamReader(response.GetResponseStream()))
        {
            responseFromServer = reader.ReadToEnd();
        }

        Console.WriteLine(responseFromServer);
    }

    public static string GetContent(string relativeUrl, CookieContainer cookies)
    {
        string url = $"http://{domain}/{relativeUrl}";
        Debug.WriteLine($"Получаем страницу: {url}");

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

    static void Main()
    {
        CookieContainer cookies = new CookieContainer();

        string login = "user";
        string password = "user";
        SingInWithCookies(login, password, cookies);

        string pageContent = GetContent("main", cookies);
        Console.WriteLine(pageContent);

        Console.ReadLine();
    }

    public static void SingInWithCookies(string Login, string Password, CookieContainer cookies)
    {
        string url = $"http://{domain}/ajax/login.php";
        Debug.WriteLine($"Выполняем запрос: {url}");

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
        Debug.WriteLine($"Статус выполнения: {response.StatusCode}");

        using (var reader = new StreamReader(response.GetResponseStream()))
        {
            reader.ReadToEnd();
        }
    }
}
