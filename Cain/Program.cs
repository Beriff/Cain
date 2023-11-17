namespace Cain
{
	internal class Program
	{
		static void Main(string[] args)
		{
			string file = args[0];
			var data = Lexer.Lex(File.ReadAllText(file), new(true));
			foreach( var (token, tokendata) in data )
			{
				Console.WriteLine($"[ {token}: ({tokendata}) ],");
			}
		}
	}
}