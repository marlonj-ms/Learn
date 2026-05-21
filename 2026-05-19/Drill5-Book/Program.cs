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

	 public void  Describe()
	{
		Console.WriteLine($"{_title} by {_author}, {_pages} pages");
	 }

}

public class Drill5
{
	public static void Main()
	{
		List<Book> booklist= new List<Book>();
		Book book1= new Book("C# in Depth", "Jon Skeet", 900);
		booklist.Add(book1);
		booklist.Add(new Book("Clean Code", "Robert Martin", 464));

		foreach (Book book in booklist)
		{
		book.Describe();
		}

	}
}
