using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Classes
{
    public class context: DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Command> Commands { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql("server=localhost;user=root;password=;database=pr4;port=3307;",
                new MySqlServerVersion(new Version(8, 0, 30)));
        }
    }
}
