using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using poco2swift.probe;

namespace poco2swift
{
	public class DocumentationCache
	{
		public string GetTypeSummary(TypeProxy type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			return GetSummary(type, GetTypeXPath(type));
		}

		public string GetPropertySummary(PropertyProxy property)
		{
			if (property == null)
				throw new ArgumentNullException("property");

			return GetSummary(property.DeclaringType, GetPropertyXPath(property));
		}

		public string GetFieldSummary(MemberProxy field)
		{
			if (field == null)
				throw new ArgumentNullException("field");

			return GetSummary(field.DeclaringType, GetFieldXPath(field));
		}

		private string GetSummary(TypeProxy type, string xpathRoot)
		{
			return GetDocumentation(type, xpathRoot, "/summary");
		}

		private string GetDocumentation(TypeProxy type, string xpathRoot, string subpath)
		{
			var document = GetDocument(type.Assembly);

			if (document != null)
			{
				var node = document.SelectSingleNode(xpathRoot + subpath);

				if (node != null)
					return node.InnerText;
			}

			return null;
		}

		private static string GetTypeXPath(TypeProxy type)
		{
			return String.Format(CultureInfo.InvariantCulture, "/doc/members/member[@name = 'T:{0}']", type.FullName);
		}

		private static string GetPropertyXPath(PropertyProxy property)
		{
			return String.Format(CultureInfo.InvariantCulture, "/doc/members/member[@name = 'P:{0}.{1}']", property.DeclaringType.FullName, property.Name);
		}

		private static string GetFieldXPath(MemberProxy field)
		{
			return String.Format(CultureInfo.InvariantCulture, "/doc/members/member[@name = 'F:{0}.{1}']", field.DeclaringType.FullName, field.Name);
		}

		private XmlDocument GetDocument(AssemblyProxy assembly)
		{
			XmlDocument document = null;

			if (!_cache.TryGetValue(assembly, out document))
			{
				var path = assembly.Location;

				path = Path.ChangeExtension(path, ".xml");

				if (File.Exists(path))
				{
					document = new XmlDocument();

					document.Load(path);
				}

				_cache.Add(assembly, document);
			}

			return document;
		}

		private IDictionary<AssemblyProxy, XmlDocument> _cache = new Dictionary<AssemblyProxy, XmlDocument>();
	}
}
