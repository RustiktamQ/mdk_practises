using System;
using System.IO;
using System.Net;

class Program
{
    const string urlMain = "http://news.permaviat-local.ru/main";

    static void Main()
    {
        var request = (HttpWebRequest)WebRequest.Create(urlMain);
        request.UserAgent = "Mozilla/5.0";

        using var response = (HttpWebResponse)request.GetResponse();
        Console.WriteLine($"Status: {response.StatusCode}");

        using var stream = response.GetResponseStream();
        using var reader = new StreamReader(stream);
        string html = reader.ReadToEnd();
        Console.WriteLine(html);

        Console.Read();
    }
}
