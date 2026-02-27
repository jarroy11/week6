using Bookstore.Models;
using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;

namespace Bookstore.Controllers
{
    public class BookController : Controller
    {
        private Repository<Book> data { get; set; }
        public BookController(BookstoreContext ctx) => data = new Repository<Book>(ctx);

        public RedirectToActionResult Index() => RedirectToAction("List");

        public ViewResult List(BookGridData values)
        {
            // create options for querying books
            var options = new QueryOptions<Book>
            {
                Includes = "Authors, Genre",
                OrderByDirection = values.SortDirection,
                PageNumber = values.PageNumber,
                PageSize = values.PageSize
            };
            if (values.IsSortByGenre)
                options.OrderBy = b => b.GenreId;
            else if (values.IsSortByPrice)
                options.OrderBy = b => b.Price;
            else
                options.OrderBy = b => b.Title;

            // create view model
            var vm = new BookListViewModel
            {
                Books = data.List(options),
                CurrentRoute = values,
                TotalPages = values.GetTotalPages(data.Count)
            };

            return View(vm);
        }

        public ViewResult Details(int id)
        {
            var book = data.Get(new QueryOptions<Book>
            {
                Where = b => b.BookId == id,
                Includes = "Authors, Genre"
            }) ?? new Book();
            return View(book);
        }

        [HttpPost]
        public RedirectToActionResult PageSize(BookGridData CurrentRoute)
        {
            return RedirectToAction("List", CurrentRoute.ToDictionary());
        }
        [HttpPost]
        public RedirectToActionResult ExportToXml()
        {
            var books = data.List(new QueryOptions<Book>
            {
                Includes = "Authors, Genre"
            });
            XDocument doc =
                new XDocument(
                    new XElement("Books",
                    from b in books
                    select new XElement("Book",
                       new XAttribute("id", b.BookId),
                          new XElement("Title", b.Title),
                          new XElement("Price", b.Price),
                          new XElement("Genre",
                              b.Genre != null ? b.Genre.Name : "N/A"),
                          new XElement("Authors",
                                from a in b.Authors
                                select new XElement("Author", a.FullName)
                                )
                            )
                    )
                );
            string path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot", "books.xml");

            Directory.CreateDirectory(
                Path.GetDirectoryName(path)!);

            doc.Save(path);

            TempData["message"] = "Books exported to XML file.";
            return RedirectToAction("List");

        }
    }
}