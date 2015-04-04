using System;
using System.Reflection;

namespace poco2swift.probe
{
	public class MemberProxy : MarshalByRefObject
	{
		internal MemberProxy(IProxyUtils utils, MemberInfo member)
		{
			if (utils == null)
				throw new ArgumentNullException("utils");

			if (member == null)
				throw new ArgumentNullException("member");

			_utils = utils;
			_member = member;
		}

		#region PropertyInfo forwarding

		public TypeProxy DeclaringType { get { return MakeTypeProxy(_member.DeclaringType); } }
		public string Name { get { return _member.Name; } }

		#endregion

		private TypeProxy MakeTypeProxy(Type type)
		{
			return _utils.MakeTypeProxy(type);
		}

		private readonly IProxyUtils _utils;
		private readonly MemberInfo _member;
	}
}
