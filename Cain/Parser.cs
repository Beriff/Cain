using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Cain
{
	public enum ASTToken
	{
		Root,

		Ctx,
		Obj,
		MsgCall,
		ParentAccess,
		AttrAccess,
		Identifier,
		StrLtr,
		NumLtr,
		ObjLtr,
		CodeLtr,
		GlobalObj,
		Asterisk
	}
	public class ASTNode
	{
		public ASTToken Token;
		public ASTNode Parent;
		public List<ASTNode> Children;
		public string IdData;

		public ASTNode(ASTToken token, ASTNode parent, params ASTNode[] children)
		{
			Token = token;
			Parent = parent;
			Children = children.ToList();
		}
		public ASTNode(ASTToken token, ASTNode parent, List<ASTNode> children)
		{
			Token = token;
			Parent = parent;
			Children = children;
		}
	}
	public class ParsingException : Exception
	{
		public ParsingException(string message) : base(message) { }
		public static void UnexpectedIdentifier(string identifier)
		{
			throw new ParsingException($"Unexpected identifier: {identifier}.");
		}
		public static void UnexpectedCode(string after, string expected)
		{
			throw new ParsingException($"Unexpected code after [{after}]: {expected} was expected.");
		}
		public static void ExpectedBefore(string symbol, string before)
		{
			throw new ParsingException($"Expected '{symbol}' before {before}");
		}
		public static void ExpectedAfter(string symbol, string after)
		{
			throw new ParsingException($"Expected '{symbol}' after {after}");
		}
		public static void TokenAtEOF(string token)
		{
			throw new ParsingException($"{token} is not allowed at EOF");
		}
	}

	public static class Parser
	{
		public static bool CanReturnObject(ASTToken token) =>
			token switch
			{
				ASTToken.NumLtr => true,
				ASTToken.StrLtr => true,
				ASTToken.Ctx => true,
				ASTToken.Obj => true,
				ASTToken.GlobalObj => true,
				ASTToken.AttrAccess => true,
				ASTToken.ParentAccess => true,
				ASTToken.MsgCall => true,
				ASTToken.ObjLtr => true,
				ASTToken.Asterisk => true,
				ASTToken.Identifier => true,

				ASTToken.CodeLtr => false,
				ASTToken.Root => false,
				_ => false
			};
		public static ASTNode IdentifierOrGlobalObj(string data)
		{
			return new(data switch
			{
				"System" => ASTToken.GlobalObj,
				_ => ASTToken.Identifier,
			}, null)
			{ IdData = data };
		}

		public static ASTNode Parse(
			List<(LexicalToken token, string data)> lextokens
			)
		//pivot point importance order (descending)
		//(losers call it order of operations):
		// (ctx/obj/:/{}), ->, [,,,], ^, single-return values (literals, global obj, *, identifier literals)
		{
			int tokencount = lextokens.Count;

			//search for pivots
			for (int i = 0; i < tokencount; i++)
			{
				var tokenpair = lextokens[i];

				if (tokenpair.token == LexicalToken.Keyword_Ctx
					|| tokenpair.token == LexicalToken.Keyword_Obj
					|| tokenpair.token == LexicalToken.Control_Message_Begin
					|| tokenpair.token == LexicalToken.Control_Ctx_Begin
					|| tokenpair.token == LexicalToken.Control_OL_Begin
					)
				{

					switch (tokenpair.token)
					{
						case LexicalToken.Control_OL_Begin:
							//parse only if its the only element in lextokens
							int olparity = 0;
							for (int j = 0; j < tokencount; j++)
							{
								int token_response = lextokens[j].token switch
								{
									LexicalToken.Control_OL_Begin => 1,
									LexicalToken.Control_OL_End => -1,
									_ => 0
								};
								olparity += token_response;
								if (olparity == 0 && token_response == -1)
								{
									if (j == tokencount - 1)
									{
										//its the only element in lextokens
										//evaluate it
										if (tokencount == 2)
										{
											//its empty object literal []
											return new ASTNode(ASTToken.ObjLtr, null!);
										}

										//split the execution by commas
										List<List<(LexicalToken, string)>> betweencommas = new();
										betweencommas.Add(new());
										var ol_inside = lextokens.GetRange(1, tokencount - 2);
										int sublist_pointer = 0;
										for (int k = 0; k < ol_inside.Count; k++)
										{
											if (ol_inside[k].token == LexicalToken.Control_Comma)
											{
												sublist_pointer++;
												betweencommas.Add(new());
											}
											else
											{
												betweencommas[sublist_pointer].Add(ol_inside[k]);
											}
										}
										List<ASTNode> object_attrs = new();
										foreach (var ls in betweencommas)
										{
											var objltr_attr = Parse(ls);
											if (!CanReturnObject(objltr_attr.Token))
												throw new ParsingException("Object literal can only contain objects");

											object_attrs.Add(objltr_attr);
										}
										return new ASTNode(ASTToken.ObjLtr, null!, object_attrs);

									}
									else
									{
										//skip object literal
										i = j; continue;
									}
								}
							}
							break;

						case LexicalToken.Keyword_Ctx:
							if (i == 0 || i == tokencount - 1)
								throw new ParsingException("Invalid token location");

							var tokens_before = lextokens.GetRange(0, i);
							var tokens_after = lextokens.GetRange(i + 1, tokencount - i - 1);

							var val_before = Parse(tokens_before);
							//value before 'ctx' is expected to be an object
							if (!CanReturnObject(val_before.Token))
								ParsingException.ExpectedBefore("object", "ctx");

							var val_after = Parse(tokens_after);
							//value after 'ctx' is expected to be a codeltr
							if (val_after.Token != ASTToken.CodeLtr)
								ParsingException.ExpectedAfter("{code}", "ctx");

							var ctxnode = new ASTNode(ASTToken.Ctx, null!, val_before, val_after);
							val_before.Parent = ctxnode;
							val_after.Parent = ctxnode;
							return ctxnode;

						case LexicalToken.Keyword_Obj:
							if (i == 0 || i == tokencount - 1)
								throw new ParsingException("Invalid token location");

							tokens_before = lextokens.GetRange(0, i);
							tokens_after = lextokens.GetRange(i + 1, tokencount - i - 1);

							val_before = Parse(tokens_before);
							//value before 'obj' is expected to be an object
							if (!CanReturnObject(val_before.Token))
								ParsingException.ExpectedBefore("object", "ctx");

							val_after = Parse(tokens_after);
							//value after 'obj' is expected to be a codeltr
							if (val_after.Token != ASTToken.CodeLtr)
								ParsingException.ExpectedAfter("{code}", "ctx");

							var objnode = new ASTNode(ASTToken.Obj, null!, val_before, val_after);
							val_before.Parent = objnode;
							val_after.Parent = objnode;
							return objnode;

						case LexicalToken.Control_Message_Begin:
							if (i == 0 || i == tokencount - 1)
								throw new ParsingException("Invalid token location");

							tokens_before = lextokens.GetRange(0, i);
							tokens_after = lextokens.GetRange(i + 1, tokencount - i - 2); //subtract additional 1 to account for '.' token

							val_before = Parse(tokens_before);
							//value before ':' is expected to be an object
							if (!CanReturnObject(val_before.Token))
								ParsingException.ExpectedBefore("object", "ctx");

							val_after = Parse(tokens_after);
							//value after ':' is expected to be an object
							if (!CanReturnObject(val_after.Token))
								ParsingException.ExpectedAfter("{code}", "ctx");

							var msgnode = new ASTNode(ASTToken.MsgCall, null!, val_before, val_after);
							val_before.Parent = msgnode;
							val_after.Parent = msgnode;
							return msgnode;

						case LexicalToken.Control_Ctx_Begin:
							//ladies and gentlemen, fasten your seatbelts

							//insulate lexical tokens based on bracket parity
							int parity = 1;
							for (int j = i + 1; j < tokencount; j++)
							{
								int token_response = lextokens[j].token switch
								{
									LexicalToken.Control_Ctx_Begin => 1,
									LexicalToken.Control_Ctx_End => -1,
									_ => 0
								};
								parity += token_response;

								//found the matching curly bracket
								if (parity == 0)
								{
									//the only thing allowed inside code literal are
									//message calls
									//therefore, we can deduct the number of operations
									//by counting : and .
									List<int> dot_positions = new();
									int message_parity = 0;

									//iterate from the token after '{', up until the token before '}',
									//counting amount of message calls
									for (int k = i + 1; k < j; k++)
									{
										int message_token_response = lextokens[k].token switch
										{
											LexicalToken.Control_Message_Begin => 1,
											LexicalToken.Control_Message_End => -1,
											_ => 0
										};
										message_parity += message_token_response;
										if (message_parity == 0 && message_token_response == -1)
										{
											//take note of '.' positions
											dot_positions.Add(k);
										}
									}

									if (dot_positions.Count == 0)
										throw new ParsingException("Only messaging is allowed inside a context");

									List<ASTNode> codeltr_children = new();

									//parse every single message call separately
									var codeltrnode = new ASTNode(ASTToken.CodeLtr, null!);
									for (int k = 0; k < dot_positions.Count; k++)
									{
										int dotpos = dot_positions[k];
										int startpos = k == 0 ? 1 : dot_positions[k - 1] + 1;
										var child = Parse(lextokens.GetRange(startpos, dotpos - startpos + 1));
										child.Parent = codeltrnode;
										codeltr_children.Add(child);
									}
									codeltrnode.Children = codeltr_children;
									return codeltrnode;
									

								}
							}
							throw new ParsingException("Unmatched curly bracket");

						default:
							throw new ParsingException("Something went wrong");
					}


				}
			}

			for (int i = 0; i < tokencount; i++)
			{
				var tokenpair = lextokens[i];

				if (tokenpair.token == LexicalToken.Control_Attr_Access)
				{
					if (i == 0 || i == tokencount - 1)
						throw new ParsingException("Invalid token location");

					var tokens_before = lextokens.GetRange(0, i);
					var tokens_after = lextokens.GetRange(i + 1, tokencount - i - 1);

					var val_before = Parse(tokens_before);
					//value before '->' is expected to be an object
					if (!CanReturnObject(val_before.Token))
						ParsingException.ExpectedBefore("object", "->");

					var val_after = Parse(tokens_after);
					//value after '->' is expected to be an identifier
					//or a chained attraccess
					if (val_after.Token != ASTToken.Identifier
						&& val_after.Token != ASTToken.AttrAccess)
						ParsingException.ExpectedAfter("identifier", "->");

					var attraccessnode = new ASTNode(ASTToken.AttrAccess, null!, val_before, val_after);
					val_before.Parent = attraccessnode;
					val_after.Parent = attraccessnode;
					return attraccessnode;
				}

			}
			for (int i = 0; i < tokencount; i++)
			{
				var tokenpair = lextokens[i];
				if (tokenpair.token == LexicalToken.Control_ParentAttr)
				{
					if (i == tokencount - 1)
						throw new ParsingException("Invalid token location");

					var token_after = lextokens[i + 1];

					if (token_after.token != LexicalToken.Identifier)
						ParsingException.ExpectedAfter("identifier", "^");

					ASTNode child_identifier = new(ASTToken.Identifier, null!)
					{ IdData = token_after.data };
					var parentattr = new ASTNode(ASTToken.ParentAccess, null!, child_identifier);
					child_identifier.Parent = parentattr;
					return parentattr;
				}

			}
			for (int i = 0; i < tokencount; i++)
			{
				var tokenpair = lextokens[i];
				//low priority targets
				if (lextokens.Count == 1)
				{
					return tokenpair.token switch
					{
						LexicalToken.Literal_Str => new ASTNode(ASTToken.StrLtr, null!) { IdData = tokenpair.data },
						LexicalToken.Literal_Num => new ASTNode(ASTToken.NumLtr, null!) { IdData = tokenpair.data },
						LexicalToken.Identifier => IdentifierOrGlobalObj(tokenpair.data),
						LexicalToken.Keyword_Asterisk => new ASTNode(ASTToken.Asterisk, null!),
						_ => throw new ParsingException("Holy hell"),
					};
				}

			}

			throw new ParsingException($"Unexpected symbol - {lextokens[0].token}");
		}

		public static void PrettyPrint(ASTNode root, string inden = "")
		{
			Console.WriteLine($"{inden}[ {root.Token} {root.IdData} ]");
			if(root.Children != null)
			{
				foreach (var node in root.Children)
				{
					PrettyPrint(node, inden + "  ");
				}
			}
			
		}
	}
}
