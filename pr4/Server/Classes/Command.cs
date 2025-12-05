using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Classes
{
    public class Command
    {
        [Key]
        public int Id {  get; set; }
        public int User {  get; set; }
        public string command { get; set; }
    }
}
