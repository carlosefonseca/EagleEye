﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Linq;

namespace DeepZoomView {
	public class OrganizableByDate : Organizable {
		public new Dictionary<DateTime, List<int>> data = new Dictionary<DateTime, List<int>>();
		public new Dictionary<int, DateTime> invertedData = new Dictionary<int, DateTime>();

		public override int ItemCount {
			get {
				return invertedData.Count;
			}
		}

		public override int GroupCount {
			get {
				return data.Count;
			}
		}

		public OrganizableByDate() : base("Date") { }

		public override void Add(int k, string p) {
			DateTime v = DateTime.Parse(p);
			DateTime key = v.Date;
			if (!data.ContainsKey(key)) {
				data.Add(key, new List<int>());
			}
			data[key].Add(k);

			invertedData.Add(k, v);
		}

		public override List<KeyValuePair<String, List<int>>> GetGroups() {
			IOrderedEnumerable<KeyValuePair<DateTime, List<int>>> ordered = data.OrderBy(x => x.Key);
			List<KeyValuePair<String, List<int>>> reformated = new List<KeyValuePair<string,List<int>>>();
			foreach (KeyValuePair<DateTime, List<int>> group in ordered) {
				reformated.Add(new KeyValuePair<string, List<int>>(group.Key.ToShortDateString(), group.Value));
			}
			return reformated;
		}
	
		public override List<KeyValuePair<String, List<int>>> GetGroups(List<int> subset) {
			throw new NotImplementedException();
		}


		/// <summary>
		/// Given an image id, returns its value for this organizable
		/// </summary>
		/// <param name="k">The MSI-Id for the image</param>
		/// <returns></returns>
		public override string Id(int k) {
			if (invertedData.ContainsKey(k)) {
				return invertedData[k].ToString();
			} else {
				return null;
			}
		}

		public override Boolean ContainsId(int k) {
			return invertedData.ContainsKey(k);
		}
	}
}
