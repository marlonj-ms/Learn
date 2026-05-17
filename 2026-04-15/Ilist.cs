void Main()
{
	 string[] strs = ["eat", "tea", "tan", "ate", "nat", "bat"];
	 Solution s1=new Solution();
	IList<IList<string>> stxc= s1.GroupAnagrams(strs);

}


public class Solution
{
	public IList<IList<string>> GroupAnagrams(string[] strs)
	{
		//	foreach (var id in GroupAnagrams)
		//	{
		//		List<string>=id;
		//	}
		
		IList<IList<string>> main = new List<IList<string>>();
		string[] st1= [strs[0],strs[1]];
		string[] st2= [strs[2],strs[3]];
		
		main.Add(st1);
		main.Add(st2);

		return main;
	}
}

// You can define other methods, fields, classes and namespaces here
