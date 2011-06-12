﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Json;

/// <summary>
/// Summary description for Class1
/// </summary>

namespace DeepZoomView {

	public class MetadataCollection {
		public Dictionary<int, Dictionary<string, string>> imgMetadata = new Dictionary<int, Dictionary<string, string>>();
		public Dictionary<string, Organizable> organizedMetadata = new Dictionary<string, Organizable>();

		public MetadataCollection() {

		}

		public void AddImageMetadata(int msiId, Dictionary<string, string> metadata) {
			metadata["id"] = (String)JsonPrimitive.Parse(metadata["id"]);
			imgMetadata.Add(msiId, metadata);
			String k;
			foreach (KeyValuePair<string, string> kv in metadata) {
				k = kv.Key;

				if (!organizedMetadata.ContainsKey(k)) {
					CreateOrganizable(k, JsonPrimitive.Parse(kv.Value).JsonType);
				}
				JsonValue val = JsonPrimitive.Parse(kv.Value);
				if (val.JsonType == JsonType.String) {
					organizedMetadata[k].Add(msiId, (String)val);
				} else {
					organizedMetadata[k].Add(msiId, ((int)val).ToString());
				}
			}
		}

		/// <summary>
		/// Creates an Organizable object according to the id or JSON type passed.
		/// </summary>
		/// <param name="k">The name of the parameter</param>
		/// <param name="type">The JSON type of the parameter (relevant when the name doesn't relate to a predefined type)</param>
		private void CreateOrganizable(string k, JsonType type) {
			Organizable o;
			if (k == "color") o = new OrganizableByColor();
			else if (k == "date") o = new OrganizableByDate();
			else {
				Console.WriteLine("Unknown data type: '" + k + "'. Using base 'Organizable' type.");
				o = new Organizable(k);
				if (type == JsonType.Number) {
					o.isNumber = true;
				}
			}
			organizedMetadata.Add(k, o);
		}



		/// <summary>
		/// Parses JSON data from the Tag field from inside the collection.xml file
		/// </summary>
		/// <param name="stream">The file contents of collection.xml</param>
		internal void ParseXML(System.IO.StreamReader stream) {
			XElement xml = XElement.Load(stream);
			foreach (XElement a in xml.Elements().First().Elements()) {
				int id = int.Parse(a.Attribute("Id").Value);
				String tag = a.Element("Tag").Value;
				JsonObject obj = (JsonObject)JsonObject.Parse(tag);
				Dictionary<string, string> data = new Dictionary<string, string>();
				foreach (KeyValuePair<string, JsonValue> n in obj) {
					data.Add(n.Key, n.Value.ToString());
				}
				AddImageMetadata(id, data);
			}
			//test();
		}


		/// <summary>
		/// Listing of names of the available organized options
		/// </summary>
		/// <returns></returns>
		public IEnumerable<String> GetOrganizationOptions() {
			return (IEnumerable<String>)organizedMetadata.Select(x => x.Value.Name);
		}


		/// <summary>
		/// Returns the Organizable Object for the key
		/// </summary>
		/// <param name="p">The key for the Organizable Object</param>
		/// <returns></returns>
		internal Organizable GetOrganized(string p) {
			if (organizedMetadata.ContainsKey(p)) {
				return organizedMetadata[p];
			}
			foreach (Organizable o in organizedMetadata.Values) {
				if (o.Name == p) return o;
			}
			return null;
		}


		public Boolean ContainsOrganizable(string p) {
			if (organizedMetadata.ContainsKey(p)) {
				return true;
			}
			foreach (Organizable o in organizedMetadata.Values) {
				if (o.Name == p) return true;
			}
			return false;
		}




		internal void test() {
			List<int> subset = new List<int>();
			for (int i = 0; i < 5; i++) {
				subset.Add(i * (imgMetadata.Count / 5));
			}

			foreach (KeyValuePair<string, Organizable> kv in organizedMetadata) {
				Console.WriteLine("Testing " + kv.Key);
				Console.WriteLine("\tall count: " + kv.Value.GetGroups().Count);
				Console.WriteLine("\tall example: " + kv.Value.GetGroups().ElementAt(0).ToString());
				Console.WriteLine("\tsubset count: " + kv.Value.GetGroups(subset).Count);
				Console.WriteLine("\tsubset example: " + kv.Value.GetGroups(subset).ElementAt(0).ToString());
			}
		}
	}
}