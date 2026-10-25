using FakeRestApiServer.DataAccess;
using FakeRestApiServer.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeRestApiServer.Repository
{
    public class ProductRepository
    {
        private readonly FakeApiDbContext _context;

        public ProductRepository(FakeApiDbContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetAllProduct()
        {
            return await _context.Products.ToListAsync();
        }

        public async Task<Product?> GetProduct(int id)
        {
            return await _context.Products.SingleOrDefaultAsync(p=>p.Id== id);
        }

        public bool Add(Product product)
        {
            _context.Products.Add(product);
            var result=_context.SaveChanges();
            return result > 0;
        }
        public bool Update(Product product)
        {
            _context.Products.Update(product);
            var result = _context.SaveChanges();
            return result > 0;
        }
        public bool Delete(int id)
        {
            var product = _context.Products.SingleOrDefault(p => p.Id == id);
            if (product != null)
            {
                _context.Remove(product);
            }
            var result = _context.SaveChanges();
            return result > 0;
        }
    }
}
