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