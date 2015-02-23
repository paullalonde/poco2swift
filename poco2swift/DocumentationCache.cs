using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;

namespace poco2swift
{
	public class DocumentationCache
	{
		public string GetTypeSummary(Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			return GetSummary(type, GetTypeXPath(type));
		}

		public string GetPropertySummary(PropertyInfo property)
		{
			if (property == null)
				throw new ArgumentNullException("property");

			return GetSummary(property.DeclaringType, GetPropertyXPath(property));
		}

		public string GetFieldSummary(MemberInfo field)
		{
			if (field == null)
				throw new ArgumentNullException("field");

			return GetSummary(field.DeclaringType, GetFieldXPath(field));
		}

		private string GetSummary(Type type, string xpathRoot)
		{
			return GetDocumentation(type, xpathRoot, "/summary");
		}

		private string GetDocumentation(Type type, string xpathRoot, string subpath)
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

		private static string GetTypeXPath(Type type)
		{
			return String.Format(CultureInfo.InvariantCulture, "/doc/members/member[@name = 'T:{0}']", type.FullName);
		}

		private static string GetPropertyXPath(PropertyInfo property)
		{
			return String.Format(CultureInfo.InvariantCulture, "/doc/members/member[@name = 'P:{0}.{1}']", property.DeclaringType.FullName, property.Name);
		}

		private static string GetFieldXPath(MemberInfo field)
		{
			return String.Format(CultureInfo.InvariantCulture, "/doc/members/member[@name = 'F:{0}.{1}']", field.DeclaringType.FullName, field.Name);
		}

		private XmlDocument GetDocument(Assembly assembly)
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

		private IDictionary<Assembly, XmlDocument> _cache = new Dictionary<Assembly, XmlDocument>();
	}
}
