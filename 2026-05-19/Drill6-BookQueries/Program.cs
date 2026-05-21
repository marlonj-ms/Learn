public class Book
{
	private string _title;
	private string _author;
	private int _pages;

	public Book(string title, string author, int pages)
	{
		_title  = title;
		_author = author;
		_pages  = pages;
	}

	public string Title  => _title;
	public string Author => _author;
	public int    Pages  => _pages;

	public void Describe()
	{
		Console.WriteLine($"{_title} by {_author}, {_pages} pages");
	}
}

public class Drill6
{
	public static void Main()
	{
		List<Book> booklist = new()
		{
			new Book("C# in Depth",     "Jon Skeet",     900),
			new Book("Clean Code",      "Robert Martin", 464),
			new Book("Clean Architecture","Robert Martin", 432),
			new Book("Refactoring",     "Martin Fowler", 448),
			new Book("Tiny Book",       "Jon Skeet",     100),
		};

		Console.WriteLine("--- Q2 foreach: pages > 500 ---");
		foreach (var book in booklist)
		{
			if (book.Pages > 500)
				Console.WriteLine($"{book.Title} larger than 500 pages");
		}

		Console.WriteLine("--- Q2 LINQ: pages > 500 ---");
		var bigTitles = booklist.Where(b => b.Pages > 500).Select(b => b.Title);
		foreach (var t in bigTitles)
			Console.WriteLine($"{t} larger than 500 pages");

		Console.WriteLine("--- Q3 thickest book ---");
		var thickest = booklist.OrderByDescending(b => b.Pages).First();
		Console.WriteLine($"Biggest book is {thickest.Title} by {thickest.Author}, {thickest.Pages} pages");

		Console.WriteLine("--- Q4 distinct authors ---");
		var authors = booklist.Select(b => b.Author).Distinct();
		Console.WriteLine(string.Join(", ", authors));

		Console.WriteLine("--- Q5a OrderBy (non-destructive) ---");
		var orderedTitles = booklist.OrderBy(b => b.Pages).Select(b => b.Title);
		foreach (var t in orderedTitles)
			Console.WriteLine(t);

		Console.WriteLine("--- Q5b List.Sort (in-place) ---");
		booklist.Sort((a, b) => a.Pages - b.Pages);
		foreach (var b in booklist)
			Console.WriteLine(b.Title);
	}
}
