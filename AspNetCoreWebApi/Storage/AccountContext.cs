using System;
using System.Threading;
using AspNetCoreWebApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreWebApi.Storage
{
    class AccountContext : DbContext 
    {
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Like> Likes { get; set; }

        public AccountContext(DbContextOptions<AccountContext> options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=accounts.db");
        }
    }
}