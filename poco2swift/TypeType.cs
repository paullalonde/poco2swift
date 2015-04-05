using System;

namespace poco2swift
{
	public partial class TypeType
	{
		public string EffectiveName
		{
			get
			{
				if (!String.IsNullOrEmpty(this.name))
					return this.name;

				if (!String.IsNullOrEmpty(this.fullname))
				{
					var fullnameParts = this.fullname.Split(new char[] { ',' });

					if ((fullnameParts != null) && (fullnameParts.Length > 0))
					{
						var qnameParts = fullnameParts[0].Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

						if ((qnameParts != null) && (qnameParts.Length > 0))
						{
							return qnameParts[qnameParts.Length - 1];
						}
					}
				}

				return null;
			}
		}

		public string EffectiveFullName
		{
			get
			{
				if (!String.IsNullOrEmpty(this.fullname))
					return this.fullname;
				else
					return null;
			}
		}

		public string EffectiveSwiftName
		{
			get
			{
				if (!String.IsNullOrEmpty(this.swiftname))
					return this.swiftname;

				var name = this.EffectiveName;

				if (!String.IsNullOrEmpty(name))
					name = MakeSwiftSafeName(name);

				return name;
			}
		}

		public static string MakeSwiftSafeName(string name)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");

			int backquote = name.IndexOf('`');

			if (backquote >= 0)
				name = name.Substring(0, backquote);

			return name;
		}
	}
}
