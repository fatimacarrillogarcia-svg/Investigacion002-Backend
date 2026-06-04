
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using LibraryService.WebAPI;
using LibraryService.WebAPI.Data;
using LibraryService.WebAPI.DTO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Xunit;

namespace LibraryService.Tests
{    
    public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly System.Data.Common.DbConnection _connection;
        private System.IServiceProvider _testServiceProvider;

        public HttpClient Client { get; private set; }

        public IntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;

            // Use a shared in-memory Sqlite connection for the test host
            _connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var customizedFactory = _factory.WithWebHostBuilder(builder =>
                builder.UseStartup<Startup>()
                .ConfigureServices(services =>
                {
                    services.RemoveAll(typeof(DbContextOptions<LibraryContext>));
                    services.AddDbContext<LibraryContext>(options =>
                        options.UseSqlite(_connection).EnableSensitiveDataLogging());
                    // Remove Swagger/Swashbuckle registrations added by the app to avoid
                    // duplicate SwaggerDoc key errors during tests.
                    services.RemoveAll(typeof(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions));
                    services.RemoveAll(typeof(Swashbuckle.AspNetCore.SwaggerGen.ISwaggerProvider));
                })
            );

            Client = customizedFactory.CreateClient();
            _testServiceProvider = customizedFactory.Services;

            // Ensure DB schema is created using the test service provider
            using (var scope = _testServiceProvider.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetRequiredService<LibraryContext>();
                ctx.Database.EnsureCreated();
            }
        }

        private async Task SeedLibrary()
        {
            var libraries = new List<Library>
            {
                new Library { Name = "Library Name 1", Location = "Location 1" },
                new Library { Name = "Library Name 2", Location = "Location 2" },
                new Library { Name = "Library Name 3", Location = "Location 3" },
                new Library { Name = "Library Name 4", Location = "Location 4" }
            };

            using (var scope = _testServiceProvider.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetRequiredService<LibraryContext>();
                await ctx.Libraries.AddRangeAsync(libraries);
                await ctx.SaveChangesAsync();  // Save to the database
            }
        }

        private async Task SeedBook(string bookName, int libraryId)
        {
            var bookForm = new BookForm
            {
                Name = bookName
            };
            var response1 = await Client.PostAsync($"/api/libraries/{libraryId}/books",
                new StringContent(JsonConvert.SerializeObject(bookForm), Encoding.UTF8, "application/json"));
        }

        // TEST NAME - addBookToLibrary
        // TEST DESCRIPTION - It adds book to a library
        [Fact]
        public async Task TestAddBook_Ok_GetBook_NotFound()
        {
            await SeedLibrary();

            var bookForm = new BookForm
            {
                Name = "Test book 1",
            };

            var response1 = await Client.PostAsync($"/api/libraries/1/books",
                new StringContent(JsonConvert.SerializeObject(bookForm), Encoding.UTF8, "application/json"));

            response1.StatusCode.Should().BeEquivalentTo(StatusCodes.Status201Created);

            bookForm = new BookForm
            {
                Name = "Test book 2",
            };

            var response2 = await Client.PostAsync($"/api/libraries/100/books",
                new StringContent(JsonConvert.SerializeObject(bookForm), Encoding.UTF8, "application/json"));

            response2.StatusCode.Should().BeEquivalentTo(StatusCodes.Status404NotFound);
        }

        // TEST NAME - getBooksInALibrary
        // TEST DESCRIPTION - It finds all books in a library by ID
        [Fact]
        public async Task TestGetBooks_Ok_NotFound()
        {
            await SeedLibrary();

            await SeedBook("test book 1", 1);
            await SeedBook("test book 2", 1);

            var response1 = await Client.GetAsync($"/api/libraries/2/books");
            response1.StatusCode.Should().BeEquivalentTo(StatusCodes.Status200OK);
            var content1 = await response1.Content.ReadAsStringAsync();
            var books = JsonConvert.DeserializeObject<IEnumerable<Book>>(content1).ToList();
            books.Count.Should().Be(0);

            var response2 = await Client.GetAsync($"/api/libraries/1/books");
            response2.StatusCode.Should().BeEquivalentTo(StatusCodes.Status200OK);
            var content2 = await response2.Content.ReadAsStringAsync();
            var books2 = JsonConvert.DeserializeObject<IEnumerable<Book>>(content2).ToList();
            books2.Count.Should().Be(2);

            var response3 = await Client.GetAsync($"/api/libraries/31232/books");
            response3.StatusCode.Should().BeEquivalentTo(StatusCodes.Status404NotFound);
        }

        // TEST NAME - deleteLibraryById
        // TEST DESCRIPTION - Check delete library web api end point
        [Fact]
        public async Task TestDeleteLibrary()
        {
            await SeedLibrary();

            var bookForm = new BookForm
            {
                Name = "test book 1",
            };

            // add book to library
            var response0 = await Client.PostAsync("/api/libraries/1/books",
                new StringContent(JsonConvert.SerializeObject(bookForm), Encoding.UTF8, "application/json"));
            response0.StatusCode.Should().BeEquivalentTo(StatusCodes.Status201Created);

            // delete library
            var response1 = await Client.DeleteAsync("/api/libraries/1");
            response1.StatusCode.Should().BeEquivalentTo(StatusCodes.Status204NoContent);

            // Verify that delete is successful
            var response2 = await Client.GetAsync("/api/libraries/1/books");
            response2.StatusCode.Should().BeEquivalentTo(StatusCodes.Status404NotFound);

            var response3 = await Client.DeleteAsync("/api/libraries/1");
            response3.StatusCode.Should().BeEquivalentTo(StatusCodes.Status404NotFound);
        }
    }
}
