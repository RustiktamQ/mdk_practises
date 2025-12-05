using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class User
    {
        [Key]
        public int Id {  get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Src { get; set; }
        public string TempSrc { get; set; }

        public User(string login, string password, string src)
        {
            this.Login = login;
            this.Password = password;
            this.Src = src;

            TempSrc = src;
        }
    }
}
