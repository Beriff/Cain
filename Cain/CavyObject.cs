
namespace Cain
{
	public class CavyObject
	{
		public Dictionary<CavyObject, CavyObject> Attributes { get; set; }
		public Func<CavyObject, CavyObject, CavyObject> Message { get; set; }

		public CavyObject()
		{
			Attributes = new();
			Message = (_,self) => self;
		}

		public CavyObject(Func<CavyObject, CavyObject, CavyObject> msg, params (CavyObject, CavyObject)[] attrs)
		{
			Message = msg;
			Attributes = new();
			foreach(var (name, obj) in attrs)
				Attributes[name] = obj;
		}

		public override bool Equals(object obj)
		{

			if (obj == null || GetType() != obj.GetType())
			{
				return false;
			}
			CavyObject o = obj as CavyObject;

			if (Attributes.Count == o.Attributes.Count)
			{
				for(int i = 0; i < Attributes.Count; i++)
				{
					var e = Attributes.ElementAt(i);
					var oe = o.Attributes.ElementAt(i);
					if (!e.Value.Equals(oe.Value))
						return false;
					if(!e.Key.Equals(oe.Key))
						return false;

				}
				return true;
			}
			return false;
		}

		public static CavyObject CopyFrom(CavyObject obj)
		{
			var newobj = new CavyObject { Message = obj.Message };
			foreach (var kv in obj.Attributes)
			{
				newobj[kv.Key] = CopyFrom(kv.Value);
			}
			return newobj;
		}

		public CavyObject this[CavyObject index]
		{
			get
			{
				foreach(var kv in Attributes)
				{
					if (kv.Key.Equals(index))
						return kv.Value;
				}
				throw new KeyNotFoundException("Wrong cavy object attribute key");
			}
			set
			{
				foreach(var kv in Attributes)
				{
					if(kv.Key.Equals(index))
					{
						Attributes[kv.Key] = value;
						return;
					}
						
				}
				Attributes.Add(index, value);
			}
		}


		public static CavyObject RawNumeral(int numeral)
		{
			if (numeral == 0)
				return new CavyObject() { Attributes = new() { { new(), new() } } };
			return new CavyObject() { Attributes = new() { { new(), RawNumeral(numeral - 1) } } };
		}

		public static CavyObject RawString(string str)
		{
			CavyObject cavystr = new();
			for(int i = 0; i < str.Length; i++)
				cavystr[RawNumeral(i)] = RawNumeral(str[i]);
			return cavystr;
		}

		public static CavyObject String(string str)
		{
			CavyObject cavystr = new();
			var str_string = RawString("str");

			cavystr[RawString("type")] = str_string;
			cavystr[str_string] = RawString(str);

			return cavystr;
		}

		public static int RawNumber2Number(CavyObject rawnum)
		{
			int count = 0;
			CavyObject current = rawnum;
			CavyObject empty = new();
			while(true)
			{
				if (current[empty].Equals(empty))
				{
					return count;
				}
				else
				{
					current = current[empty];
					count++;
				}
			}
			
		}

		public static string RawString2String(CavyObject rawstr)
		{
			var str = "";
			foreach(var kv in rawstr.Attributes)
			{
				str += (char)RawNumber2Number(kv.Value);
			}
			return str;
		}

		public static string CavyString2String(CavyObject str)
		{
			return RawString2String(str[RawString("str")]);
		}

		public static CavyObject System()
		{
			CavyObject system = new();

			CavyObject console = new();
			CavyObject _out = new();
			_out.Message = (msgobj, parent) =>
			{
				Console.WriteLine(CavyString2String(msgobj));
				return new();
			};
			console[RawString("out")] = _out;
			system[RawString("console")] = console;

			CavyObject var = new();
			var.Message = (msgobj, parent) =>
			{
				msgobj[RawString("attr0")][msgobj[RawString("attr1")]] = msgobj[RawString("attr2")];
				return new();
			
			};
			system[RawString("var")] = var;

			return system;

		}

		public static CavyObject GetGlobals()
		{
			CavyObject globals = new();
			globals[RawString("System")] = System();
			return globals;
		}
		
	}
}
