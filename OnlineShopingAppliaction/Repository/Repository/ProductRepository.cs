using Microsoft.EntityFrameworkCore;
using OnlineShopingAppliaction.Data;
using OnlineShopingAppliaction.Models;
using OnlineShopingAppliaction.Repository.Interface;

namespace OnlineShopingAppliaction.Repository.Repository
{
    public class ProductRepository: IProductRepository
    {
        private readonly ApplicationDbContext _context;
        public ProductRepository(ApplicationDbContext context) { _context = context; }

        public async Task<IEnumerable<Product>> GetAllAsync(int? categoryId, string? searchQuery)
        {
            var products = _context.Products.Include(p => p.Category).AsQueryable();

            if (categoryId.HasValue)
                products = products.Where(p => p.CategoryId == categoryId);

            if (!string.IsNullOrEmpty(searchQuery))
                products = products.Where(p => p.Name.Contains(searchQuery) || p.Category.Name.Contains(searchQuery));

            return await products.ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(int id) =>
            await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);

        public async Task<IEnumerable<Product>> GetRelatedAsync(int categoryId, int excludeId) =>
            await _context.Products.Where(p => p.CategoryId == categoryId && p.Id != excludeId).ToListAsync();

        public async Task<IEnumerable<Category>> GetCategoriesAsync() =>
            await _context.Categories.ToListAsync();

        public async Task AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Product product)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasOrdersAsync(int productId) =>
            await _context.OrderItems.AnyAsync(oi => oi.ProductId == productId);
    }
}

