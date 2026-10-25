using FakeRestApiServer.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeRestApiServer.DataAccess
{
    public class FakeApiDbContext : DbContext
    {
        public DbSet<Product> Products { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
         "Data Source=STHQ012D-01;" +
         "Initial Catalog=FakeApiDB;" +
         "Integrated Security=False;" +
         "User Id=admin;" +
         "Password=admin;" +
         "TrustServerCertificate=True;" +
         "Encrypt=True;");

        }
    }
}
