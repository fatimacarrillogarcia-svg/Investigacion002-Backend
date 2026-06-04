using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibraryService.WebAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryService.WebAPI.Services
{
    public class BooksService : IBooksService
    {
        private readonly LibraryContext _libraryContext;

        public BooksService(LibraryContext libraryContext)
        {
            _libraryContext = libraryContext;
        }

        public async Task<IEnumerable<Book>> Get(int libraryId, int[] ids)
        {
            var query = _libraryContext.Books.AsQueryable().Where(b => b.LibraryId == libraryId);
            if (ids != null && ids.Any())
                query = query.Where(b => ids.Contains(b.Id));

            return await query.ToListAsync();
        }

        public async Task<Book> Add(Book book)
        {
            await _libraryContext.Books.AddAsync(book);
            await _libraryContext.SaveChangesAsync();
            return book;
        }

        public async Task<Book> Update(Book book)
        {
            var existing = await _libraryContext.Books.SingleOrDefaultAsync(b => b.Id == book.Id && b.LibraryId == book.LibraryId);
            if (existing == null) return null;
            existing.Name = book.Name;
            existing.Category = book.Category;

            _libraryContext.Books.Update(existing);
            await _libraryContext.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> Delete(Book book)
        {
            var existing = await _libraryContext.Books.SingleOrDefaultAsync(b => b.Id == book.Id && b.LibraryId == book.LibraryId);
            if (existing == null) return false;
            _libraryContext.Books.Remove(existing);
            await _libraryContext.SaveChangesAsync();
            return true;
        }
    }

    public interface IBooksService
    {
        Task<IEnumerable<Book>> Get(int libraryId, int[] ids);

        Task<Book> Add(Book book);

        Task<Book> Update(Book book);

        Task<bool> Delete(Book book);
    }
}
