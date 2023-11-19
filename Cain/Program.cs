namespace Cain
{
	internal class Program
	{
		static void Main(string[] args)
		{
			string file = args[0];
			var data = Lexer.Lex(File.ReadAllText(file), new(true));
			try
			{
				var root = Parser.Parse(data);
				Parser.PrettyPrint(root);
			} catch (ParsingException e)
			{
				Console.WriteLine(e.Message);
			}
			
		}
	}
}