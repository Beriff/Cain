namespace Cain
{
	internal class Program
	{
		static void Main(string[] args)
		{
			string file = args[0];
			var data = Lexer.Lex(File.ReadAllText(file), new(true));
			var root = Parser.Parse(data);



			Parser.PrettyPrint(root);
		}
	}
}