using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pr10.Models
{
    class Request
    {
        public string Model { get; set; }
        public List<Message> messages { get; set; }
        public bool stream { get; set; }
        public int repetion_penalty { get; set; }
        public class Message
        {
            public string role { get; set; }
            public string content { get; set; }
        }
    }
}
