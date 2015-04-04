using System;

namespace poco2swift
{
	public partial class Poco2SwiftType
	{
		public Poco2SwiftType()
		{
			InitializeCollections();
		}

		public void PostDeserialize()
		{
			InitializeCollections();
		}

		private void InitializeCollections()
		{
			if (importsField == null)
				importsField = new ModuleType[0];

			if (skiptypesField == null)
				skiptypesField = new SkipType[0];

			if (externaltypesField == null)
				externaltypesField = new ExternalType[0];

			if (enumerationsField == null)
				enumerationsField = new EnumType[0];

			if (classesField == null)
				classesField = new ClassType[0];
		}
	}
}
