using Newtonsoft.Json;
using System.Text;
using pr10.Models;
using pr10.Response;

class pr9
{
    static string ClientId = "";
    static string AuthorizationKey = "";

    /// <summary>
    /// Метод получения токена пользователя
    /// </summary>
    /// <param name="RqUID">Клиент ID</param>
    /// <param name="Веагег">Ключ авторизации</param>
    /// <returns>Токен для выполнения запросов</returns>
    /// 
    
    static async Task Main(string[] args)
    {
        string Token = await GetToken(ClientId, AuthorizationKey);

        if (Token == null) {
            Console.WriteLine("Не удалось получить токен");
            return;
        }

        while (true)
        {
            Console.WriteLine($"Сообщенние: ");
            string Message = Console.ReadLine();

            ResponseMessage Answer = await GetAnswer(ClientId, AuthorizationKey);
            Console.WriteLine("Ответ: " + Answer.choices[0].message.content);
        }
    }

    public static async Task<ResponseMessage> GetAnswer(string token, string message)
    {
        // Переменная для хранения объекта ответа от API
        ResponseMessage responseMessage = null;

        // URL endpoint для отправки запроса к GigaChat API
        string Url = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";

        // Создаем обработчик HTTP-клиента с настройками SSL
        using (HttpClientHandler Handler = new HttpClientHandler())
        {
            // Отключаем проверку SSL-сертификатов
            Handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;

            // Создаем HTTP-клиент с использованием кастомного обработчика
            using (HttpClient Client = new HttpClient(Handler))
            {
                // Создаем POST-запрос к API чат-комплиментов GigaChat
                HttpRequestMessage Request = new HttpRequestMessage(HttpMethod.Post, Url);

                // Добавляем заголовки запроса
                Request.Headers.Add("Accept", "application/json"); // Ожидаем JSON в ответе
                Request.Headers.Add("Authorization", $"Bearer {token}"); // Токен авторизации для доступа к API

                // Создаем объект запроса с параметрами для модели GigaChat
                Request DataRequest = new Request()
                {
                    model = "GigaChat", // Указываем используемую модель
                    stream = false, // Отключаем потоковый ответ
                    repetition_penalty = 1, // Штраф за повторения (1 = без штрафа)
                    messages = new List<Request.Message>()
                {
                    new Request.Message()
                    {
                        role = "user", // Роль отправителя сообщения
                        content = message // Текст сообщения пользователя
                    }
                }
                };

                // Сериализуем объект запроса в JSON строку
                string JsonContent = JsonConvert.SerializeObject(DataRequest);

                // Создаем содержимое запроса в формате JSON
                Request.Content = new StringContent(JsonContent, Encoding.UTF8, "application/json");

                // Отправляем асинхронный запрос и получаем ответ
                HttpResponseMessage Response = await Client.SendAsync(Request);

                // Проверяем успешность HTTP-запроса (статус 200–299)
                if (Response.IsSuccessStatusCode)
                {
                    // Читаем содержимое ответа как строку
                    string ResponseContent = await Response.Content.ReadAsStringAsync();

                    // Десериализуем JSON-ответ в объект ResponseMessage
                    responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(ResponseContent);
                }
            }
        }

        // Возвращаем объект ответа (или null, если запрос не удался)
        return responseMessage;
    }

    public static async Task<string> GetToken(string rqUID, string bearer)
    {
        string ReturnToken = null; // Переменная для хранения полученного токена
        string Url = "https://ngw.devices.sberbank.ru:9WU3/api/v2/oauth"; // URL endpoint для получения токена

        // Создаем обработчик HTTP-клиента с настройками SSL
        using (HttpClientHandler Handler = new HttpClientHandler())
        {
            // Отключаем проверку SSL-сертификатов
            Handler.ServerCertificateCustomValidationCallback = (message, cert, chain, ss1PolicyErrors) => true;

            // Создаем HTTP-клиент с использованием кастомного обработчика
            using (HttpClient Clien = new HttpClient(Handler))
            {
                // Создаем POST-запрос к указанному URL
                HttpRequestMessage Request = new HttpRequestMessage(HttpMethod.Post, Url);

                // Добавляем заголовки запроса
                Request.Headers.Add("Accept", "application/json"); // Ожидаем JSON в ответе
                Request.Headers.Add("RqUID", rqUID); // Уникальный идентификатор запроса
                Request.Headers.Add("Authorization", $"Bearer {bearer}"); // Токен авторизации

                // Подготавливаем данные для формы (application/x-www-form-urlencoded)
                var Data = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS") // Запрашиваемые разрешения
            };

                // Создаем содержимое запроса в формате form-urlencoded
                Request.Content = new FormUrlEncodedContent(Data);

                // Отправляем асинхронный запрос и получаем ответ
                HttpResponseMessage Response = await Clien.SendAsync(Request);

                // Проверяем успешность HTTP-запроса
                if (Response.IsSuccessStatusCode)
                {
                    // Читаем содержимое ответа как строку
                    string ResponseContent = await Response.Content.ReadAsStringAsync();

                    // Десориализуем JSON-ответ в объект ResponseToken
                    ResponseToken Token = JsonConvert.DeserializeObject<ResponseToken>(ResponseContent);

                    // Извлекаем access_token из объекта ответа
                    ReturnToken = Token.access_token;
                }
            }
        }

        // Возвращаем полученный токен (или null, если запрос не удался)
        return ReturnToken;
    }
}