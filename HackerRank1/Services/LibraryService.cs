using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibraryService.WebAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryService.WebAPI.Services
{
    public class LibrariesService : ILibrariesService
    {
        private readonly LibraryContext _libraryContext;

        public LibrariesService(LibraryContext libraryContext)
        {
            _libraryContext = libraryContext;
        }

        public async Task<IEnumerable<Library>> Get(int[] ids)
        {
            var projects = _libraryContext.Libraries.AsQueryable();

            if (ids != null && ids.Any())
                projects = projects.Where(x => ids.Contains(x.Id));

            return await projects.ToListAsync();
        }

        public async Task<Library> Add(Library library)
        {
            await _libraryContext.Libraries.AddAsync(library);

            await _libraryContext.SaveChangesAsync();
            return library;
        }

        public async Task<IEnumerable<Library>> AddRange(IEnumerable<Library> projects)
        {
            await _libraryContext.Libraries.AddRangeAsync(projects);
            await _libraryContext.SaveChangesAsync();
            return projects;
        }

        public async Task<Library> Update(Library library)
        {
            var projectForChanges = await _libraryContext.Libraries.SingleAsync(x => x.Id == library.Id);
            projectForChanges.Name = library.Name;
            projectForChanges.Location = library.Location;

            _libraryContext.Libraries.Update(projectForChanges);
            await _libraryContext.SaveChangesAsync();
            return library;
        }

        public async Task<bool> Delete(Library library)
        {
            var existing = await _libraryContext.Libraries.SingleOrDefaultAsync(l => l.Id == library.Id);
            if (existing == null) return false;

            // Remove related books first
            var books = _libraryContext.Books.Where(b => b.LibraryId == existing.Id);
            _libraryContext.Books.RemoveRange(books);

            _libraryContext.Libraries.Remove(existing);
            await _libraryContext.SaveChangesAsync();
            return true;
        }
    }

    public interface ILibrariesService
    {
        Task<IEnumerable<Library>> Get(int[] ids);

        Task<Library> Add(Library library);

        Task<Library> Update(Library library);

        Task<bool> Delete(Library library);
    }
}
