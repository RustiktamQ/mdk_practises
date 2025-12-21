using pr10.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pr10.Response
{
    /// <summary>
    /// Класс ResponseMessage
    /// </summary>
    public class ResponseMessage
    {
        /// <summary> 
        /// Список вариантов ответов, сгенерированных моделью
        /// </summary>
        public List<Choice> choices { get; set; }

        /// <summary> 
        /// Временная метка создания ответа (Unix timestamp)
        /// </summary>
        public int created { get; set; }

        /// <summary>
        /// Имя модели, использовавшейся для генерации ответа
        /// </summary>
        public string model { get; set; }

        /// <summary> 
        /// Тип объекта (обычно "chat.completion" для чат-запросов)
        /// </summary>
        public string @object { get; set; }

        /// <summary>
        /// Информация об использовании токенов
        /// </summary>
        public Usage usage { get; set; }
    }

    /// <summary>
    /// Класс Usage - информация об использовании токенов
    /// </summary>
    public class Usage
    {
        /// <summary> 
        /// Количество токенов, потраченных на генерацию ответа (completion)
        /// </summary>
        public int completion_tokens { get; set; }

        /// <summary> 
        /// Количество токенов во входном промпте (запросе пользователя)
        /// </summary>
        public int prompt_tokens { get; set; }

        /// <summary> 
        /// Количество токенов, использованных системными сообщениями
        /// </summary>
        public int system_tokens { get; set; }

        /// <summary> 
        /// Общее количество токенов (prompt + completion + system)
        /// </summary>
        public int total_tokens { get; set; }
    }

    /// <summary>
    /// Класс Choice - вариант ответа от модели
    /// </summary>
    /// <summary>
    /// Класс Choice - вариант ответа от модели
    /// </summary>
    public class Choice
    {
        /// <summary>
        /// Причина завершения генерации:
        /// "stop" - естественное завершение,
        /// "length" - достигнут лимит токенов,
        /// "content_filter" - фильтр контента и т.д.
        /// </summary>
        public string finish_reason { get; set; }

        /// <summary> 
        /// Индекс варианта ответа (начинается с 0)
        /// </summary>
        public int index { get; set; }

        /// <summary> 
        /// Сообщение, сгенерированное моделью
        /// </summary>
        public Request.Message Message { get; set; } = null!;
    }
}
