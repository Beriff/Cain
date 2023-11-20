using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cain
{
	public class InterpreterException : Exception
	{
		public InterpreterException(string message) : base(message) { }
	}

	public class Interpreter
	{
		public CavyObject GlobalObjects;

		public CavyObject? CurrentParentObject;

		public Interpreter(CavyObject? parent = null, CavyObject? globals = null)
		{
			GlobalObjects = globals ?? new();
			CurrentParentObject = parent;
		}

		public CavyObject Interpret(ASTNode root)
		{
			if(root.Parent == null)
			{
				if (root.Token != ASTToken.Obj
					&& root.Token != ASTToken.Ctx)
					throw new InterpreterException(
						"Only objects and contexts are allowed as top level statements");
			}

			switch(root.Token)
			{
				case ASTToken.CodeLtr:
					var codeLtr = new CavyObject();

					//all things interpreter will be applied to the code literal owner
					//(object or a context)
					var subinterpreter = new Interpreter(CurrentParentObject, GlobalObjects);

					codeLtr.Message = (msg, parent) =>
					{
						for (int i = 0; i < root.Children.Count; i++)
						{
							if (i == root.Children.Count - 1)
								return subinterpreter.Interpret(root.Children[i]);
							subinterpreter.Interpret(root.Children[i]);
						}
						return null;
					};
					codeLtr[CavyObject.RawString("type")] = CavyObject.RawString("codeltr");
					return codeLtr;

				case ASTToken.NumLtr:
					return CavyObject.RawNumeral(int.Parse(root.IdData));

				case ASTToken.StrLtr:
					return CavyObject.String(root.IdData);

				case ASTToken.Identifier:
					return CavyObject.RawString(root.IdData);

				case ASTToken.Obj:
					var newobj = CavyObject.CopyFrom(Interpret(root.Children[0]));

					//apply the provided code literal immediately
					var prevparent = CurrentParentObject;
					CurrentParentObject = newobj;
					var objcodeltr = Interpret(root.Children[1]);
					objcodeltr.Message(null, newobj);
					CurrentParentObject = prevparent;

					return newobj;

				case ASTToken.Asterisk:
					if (CurrentParentObject == null)
						throw new InterpreterException("* cannot be used at a top level");
					return CurrentParentObject;

				case ASTToken.Ctx:
					var ctx_parentobj = Interpret(root.Children[0]);

					prevparent = CurrentParentObject;
					CurrentParentObject = ctx_parentobj;

					var ctx_codeltr = Interpret(root.Children[1]);
					//if the statement is toplevel, run it
					if(root.Parent == null)
						ctx_codeltr.Message(null, ctx_parentobj);

					CurrentParentObject = prevparent;

					var ctx_obj = new CavyObject();
					ctx_obj[CavyObject.RawString("type")] = CavyObject.RawString("context");
					ctx_obj[CavyObject.RawString("code")] = ctx_codeltr;
					return ctx_obj;

				case ASTToken.MsgCall:
					var msgcall_parent = Interpret(root.Children[0]);
					var msgcall_obj = Interpret(root.Children[1]);

					return msgcall_parent.Message(msgcall_obj, msgcall_parent);

				case ASTToken.ParentAccess:
					if (CurrentParentObject == null)
						throw new InterpreterException("^ cannot be used at top level");
					return CurrentParentObject[Interpret(root.Children[0])];

				case ASTToken.GlobalObj:
					return GlobalObjects[CavyObject.RawString(root.IdData)];

				case ASTToken.AttrAccess:
					//handle chained accessing
					//a->b->c is encoded as [attr: a, [attr: b, c]]
					//we need to convert it to [attr: [attr: a, b], c]
					var attr_parent = Interpret(root.Children[0]);
					if (root.Children[1].Token == ASTToken.AttrAccess)
					{
						//its chained attraccess
						ASTNode reformed = new(ASTToken.AttrAccess, null!);
						reformed.Children.Add(
							new ASTNode(ASTToken.AttrAccess, reformed,
							root.Children[0],
							root.Children[1].Children[0]
							)
							);
						reformed.Children.Add(root.Children[1].Children[1]);
						return Interpret(reformed);
					}
					else
					{
						//its not chained, handle as usual
						var attr_prop = Interpret(root.Children[1]);
						return attr_parent[attr_prop];
					}

				case ASTToken.ObjLtr:
					CavyObject objltr = new();
					for(int i = 0; i < root.Children.Count; i++)
					{
						objltr[CavyObject.RawString($"attr{i}")] = Interpret(root.Children[i]);
					}
					return objltr;

				default:
					throw new InterpreterException("Unknown token");
			}
		}

	}
}
