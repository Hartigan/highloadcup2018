using System;
using System.Threading;
using AspNetCoreWebApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreWebApi.Storage
{
    class AccountContext : DbContext 
    {
        public DbSet<Account> Accounts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=accounts.db");
        }
    }
}