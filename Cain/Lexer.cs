using System.Text;

namespace Cain
{
	public enum LexicalToken
	{
		Space,

		Control_Comma,
		Control_OL_Begin,
		Control_OL_End,
		Control_Message_Begin,
		Control_Message_End,
		Control_Attr_Access,
		Control_ParentAttr,
		Control_Ctx_Begin,
		Control_Ctx_End,

		Keyword_Asterisk,
		Keyword_Obj,
		Keyword_Ctx,

		Identifier,
		
		Literal_Str,
		Literal_Num
	}
	public static class Lexer
	{
		static LexicalToken? ResolveControl(string buf)
		{
			return buf switch
			{
				"," => LexicalToken.Control_Comma,
				"[" => LexicalToken.Control_OL_Begin,
				"]" => LexicalToken.Control_OL_End,
				":" => LexicalToken.Control_Message_Begin,
				"." => LexicalToken.Control_Message_End,
				"->" => LexicalToken.Control_Attr_Access,
				"^" => LexicalToken.Control_ParentAttr,
				"{" => LexicalToken.Control_Ctx_Begin,
				"}" => LexicalToken.Control_Ctx_End,

				_ => null
			};
		}
		public struct LexingParameters
		{
			public bool EliminateWhitespace;
			public LexingParameters(bool ew)
			{
				EliminateWhitespace = ew;
			}
		}
		static bool IsSpaceSymbol(char c) => c == ' ' || c == '\t' || c == '\n' || c == '\r';
		static LexicalToken ResolveIdentifier(string buf)
		{
			return buf switch
			{
				"obj" => LexicalToken.Keyword_Obj,
				"ctx" => LexicalToken.Keyword_Ctx,
				"*" => LexicalToken.Keyword_Asterisk,

				_ => LexicalToken.Identifier
			};
		}

		public static List<(LexicalToken, string)> Lex(string source, LexingParameters? lp = null)
		{
			List<(LexicalToken, string)> tokens = new();
			lp ??= new LexingParameters(false);
			string buffer = "";

			void flush_buffer()
			{
				if(buffer != "")
				{
					tokens.Add((ResolveIdentifier(buffer), buffer));
					buffer = "";
				}
			}

			
			for(int i = 0; i < source.Length; i++)
			{
				var curr_symbol = source[i];

				//check if current symbol (or else, current + next) is a control symbol
				var controltest_symbol = curr_symbol.ToString();
				var controltest_result = ResolveControl(controltest_symbol);
				if(controltest_result != null)
				{
					flush_buffer();
					//must be a control symbol
					tokens.Add((controltest_result.Value, controltest_symbol));

					continue;
				}
				else if(i != source.Length - 1)
				{
					//check 2 character long control symbols (->)
					controltest_symbol += source[i + 1].ToString();
					controltest_result = ResolveControl(controltest_symbol);
					if(controltest_result != null)
					{
						flush_buffer();
						tokens.Add((controltest_result.Value, controltest_symbol));
						i++; //makes the lexer indexer move 2 positions instead of one
						continue;
					}
				}

				//if it is not a control symbol, check if it is a comment
				if (curr_symbol == '#')
				{
					//search until '\n' is hit
					for(int a = i+1; a < source.Length; a++)
					{
						if (source[a] == '\n')
						{
							//newline is hit, set the lexer index after it and move forward
							i = a;
							break;
						}
					}
					continue;
				}

				//check if its a number literal
				//it is considered a number literal if there's no characters immediately
				//preceding it
				if (char.IsDigit(curr_symbol) && buffer == "")
				{
					StringBuilder numLiteral = new();
					numLiteral.Append(curr_symbol);
					
					/*
					 * no need to flush the buffer,
					 * since number literals can only be
					 * preceded by spaces and control characters,
					 * which do not append to buffer
					 */

					//iterate over all digits immediately after this one
					for(int a = i+1; a < source.Length; a++)
					{
						if (char.IsDigit(source[a]))
						{
							numLiteral.Append(source[a]);
						} else
						{
							//number literal ended
							i = a-1;
							tokens.Add((LexicalToken.Literal_Num, numLiteral.ToString()));
							break;
						}
					}
					continue;
				}

				//check if its a string literal
				if (curr_symbol == '\"')
				{
					StringBuilder strLiteral = new();

					flush_buffer();

					//iterate over all symbols until " is hit
					for (int a = i + 1; a < source.Length; a++)
					{
						if (source[a] == '\"')
						{
							
							//string literal ended
							i = a;
							tokens.Add((LexicalToken.Literal_Str, strLiteral.ToString()));
							break;
						}
						else
						{
							strLiteral.Append(source[a]);
						}
					}
					continue;
				}
				
				// finally, check if it is a space symbol
				if (IsSpaceSymbol(curr_symbol))
				{
					flush_buffer();
					if (!lp.Value.EliminateWhitespace)
						tokens.Add((LexicalToken.Space, "whitespace"));
					continue;
				}

				//if none of previous conditions were hit, we expect it to be
				//an identifier or a keyword or a literal
				buffer += curr_symbol;
			}

			return tokens;
		}
	}
}
