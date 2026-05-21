public class Drill8
{
	public static void Main()
	{
		string[] words = { "apple", "banana", "apple", "cherry", "banana", "apple" };

		Dictionary<string, int> dict = new Dictionary<string, int>();

		foreach (var word in words)
		{
			if (dict.ContainsKey(word))
			{
				dict[word]++;
			}
			else
			{
				dict.Add(word, 1);
			}
		}

		foreach (var (key, value) in dict)
		{
			Console.WriteLine($"{key}:{value}");
		}
	}
}
