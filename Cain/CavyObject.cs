
namespace Cain
{
	public class CavyObject
	{
		public Dictionary<string, CavyObject> Attributes { get; set; }
		public Func<CavyObject, CavyObject> Message { get; set; }

		public CavyObject()
		{
			Attributes = new();
			Message = x => x;
		}
		public CavyObject(Func<CavyObject,CavyObject> msg, params (string, CavyObject)[] attrs)
		{
			Message = msg;
			Attributes = new();
			foreach(var (name, obj) in attrs)
				Attributes[name] = obj;
		}
	}
}
