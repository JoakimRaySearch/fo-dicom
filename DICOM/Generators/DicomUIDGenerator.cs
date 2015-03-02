﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Dicom.Generators {
	using System.Linq;

	public class DicomUIDGenerator {
		public static string Process(string file)
		{
			return Emit(Parse(file));
		}

		public static IEnumerable<DicomUID> Parse(string file)
		{
			XDocument doc = XDocument.Load(file);
			XElement xdict = doc.Element("dictionary");
			if (xdict == null) throw new DicomDataException("Expected <dictionary> root node in DICOM dictionary.");

			foreach (XElement xuid in xdict.Elements("uid"))
			{
				string name = xuid.Value;

				if (xuid.Attribute("uid") == null) continue;
				string uid = xuid.Attribute("uid").Value;

				if (xuid.Attribute("keyword") == null) continue;
				string keyword = xuid.Attribute("keyword").Value;

				bool retired = false;
				XAttribute xretired = xuid.Attribute("retired");
				if (xretired != null && !String.IsNullOrEmpty(xretired.Value) && Boolean.Parse(xretired.Value)) retired = true;

				if (retired) keyword += "RETIRED";

				if (xuid.Attribute("type") == null) continue;
				string type = xuid.Attribute("type").Value;

				DicomUidType uidType = DicomUidType.Unknown;
				switch (type)
				{
					case "Transfer":
					case "Transfer Syntax":
						uidType = DicomUidType.TransferSyntax;
						break;
					case "SOP Class":
					case "Query/Retrieve":
						uidType = DicomUidType.SOPClass;
						break;
					case "Meta SOP Class":
						uidType = DicomUidType.MetaSOPClass;
						break;
					case "Service Class":
						uidType = DicomUidType.ServiceClass;
						break;
					case "Well-known frame of reference":
					case "Synchronization Frame of Reference":
						uidType = DicomUidType.FrameOfReference;
						break;
					case "Well-known SOP Instance":
					case "Well-known Printer SOP Instance":
					case "Well-known Print Queue SOP Instance":
						uidType = DicomUidType.SOPInstance;
						break;
					case "Coding Scheme":
					case "DICOM UIDs as a Coding Scheme":
						uidType = DicomUidType.CodingScheme;
						break;
					case "Application Context Name":
						uidType = DicomUidType.ApplicationContextName;
						break;
					case "LDAP":
					case "LDAP OID":
						uidType = DicomUidType.LDAP;
						break;
					case "Context Group Name":
						uidType = DicomUidType.ContextGroupName;
						break;
					case "Application Hosting Model":
						uidType = DicomUidType.ApplicationHostingModel;
						break;
					case "":
						uidType = DicomUidType.Unknown;
						break;
					default:
						throw new DicomDataException("Unkown UID type: {0}", type);
				}
				yield return new DicomUID(uid, name, uidType, retired);
			}
		}

		public static string Emit(IEnumerable<DicomUID> uidList) {
			StringBuilder list = new StringBuilder();
			StringBuilder uids = new StringBuilder();

			foreach (DicomUID uid in uidList)
			{
				var keyword =
					string.Join(string.Empty,
					uid.Name
						.Replace("@", " ")
						.Replace("&", " ")
						.Replace("(Process", " ")
						.Replace("(", " ")
						.Replace(")", " ")
						.Replace("/", " ")
						.Replace("-", " ")
						.Replace(",", " ")
						.Replace("Retired", "RETIRED")
						.Replace(".", " ")
						.Split(' ').Select(x => x.Length > 0 ? char.ToUpper(x[0]) + x.Substring(1) : ""));

				list.AppendFormat("\t\t\t_uids.Add(DicomUID.{0}.UID, DicomUID.{0});", keyword).AppendLine();

				uids.AppendLine();
				uids.AppendFormat("\t\t/// <summary>{0}: {1}</summary>", uid.Type, uid.Name).AppendLine();
				uids.AppendFormat("\t\tpublic readonly static DicomUID {0} = new DicomUID(\"{1}\", \"{2}\", DicomUidType.{3}, {4});", keyword, uid.UID, uid.Name, uid.Type, uid.IsRetired ? "true" : "false").AppendLine();
			}

			string code = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dicom {
	public partial class DicomUID {
		private static void LoadInternalUIDs() {
";

			code += list.ToString();
			code += "		}";
			code += uids.ToString();

			code += @"	}
}";
			return code;
		}
	}
}
