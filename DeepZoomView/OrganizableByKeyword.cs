using System;
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
using System.Collections;
using System.Linq;
using ColorUtils;

namespace DeepZoomView {
	public class OrganizableByKeyword : Organizable {

		/// <summary>
		/// Constructor
		/// </summary>
		public OrganizableByKeyword()
			: base("Keyword") {
			base.AvailableForGroupping = false;
		}


		public override void Add(int k, string p) {
			String[] keywords = p.Split(",".ToCharArray());
			foreach (String keyword in keywords) {
				if (!data.ContainsKey(keyword)) {
					data.Add(keyword, new List<int>());
				}
				data[keyword].Add(k);
			}
			invertedData[k] = p;
		}


		/*

		public override List<KeyValuePair<String, List<int>>> GetGroups() {
			return GetGroups(null);
		}


		/// <summary>
		/// Takes a list of 
		/// </summary>
		/// <param name="subset"></param>
		/// <returns></returns>
		public override List<KeyValuePair<String, List<int>>> GetGroups(List<int> subset) {
			return null;
			//if (data.Count == 0) {
			//    return null;
			//}

			//List<KeyValuePair<String, List<int>>> groupsOut = new List<KeyValuePair<string, List<int>>>();
			//Dictionary<int, List<int>> groups = new Dictionary<int, List<int>>();

			//Dictionary<int, List<int>> set = null;
			//if (subset == null) {
			//    set = data;
			//} else {
			//    set = OrganizedSubset(subset);
			//}

			//Color[] theColors = new Color[] { Colors.Red, Colors.Orange, Colors.Yellow, Colors.Green, Colors.Cyan, Colors.Blue, Colors.Purple };

			//int buckets = 12;
			//double spread = 360.0 / buckets;

			//foreach (KeyValuePair<int, List<int>> kv in set) {
			//    int group;
			//    if (kv.Key < 0) {	// Black/Gray/White
			//        group = kv.Key;
			//    } else {			// Hue
			//        group = Convert.ToInt16(Math.Round((kv.Key % Math.Ceiling(360 - (spread / 2))) / spread));
			//    }
			//    if (!groups.ContainsKey(group)) {
			//        groups.Add(group, new List<int>());
			//    }
			//    List<int> tmp1 = kv.Value;
			//    IEnumerable<int> tmp = groups[group].Union<int>(tmp1);
			//    groups[group] = tmp.ToList<int>();
			//}

			//Dictionary<int, String> colorNames = new Dictionary<int, string>();
			//colorNames.Add(0, "Red");
			//colorNames.Add(1, "Yellow");
			//colorNames.Add(2, "Green");
			//colorNames.Add(3, "Cyan");
			//colorNames.Add(4, "Blue");
			//colorNames.Add(5, "Pink");

			//List<int> sortedKeys = groups.Keys.ToList<int>();
			//sortedKeys.Sort();
			//foreach (int c in sortedKeys) {
			//    groupsOut.Add(new KeyValuePair<String, List<int>>(c.ToString(), groups[c]));
			//}

			//return groupsOut;
		}

		/// <summary>
		/// Intersects the collection with a given list of ids. For internal use.
		/// </summary>
		/// <param name="subset">A list of images</param>
		/// <returns>A new dictionary with only the chosen groups and images</returns>
		private Dictionary<int, List<int>> OrganizedSubset(List<int> subset) {
			return null;
			//Dictionary<int, List<int>> newOrg = new Dictionary<int, List<int>>();
			//IEnumerable<int> intersectedList;
			//foreach (KeyValuePair<int, List<int>> kv in data) {
			//    intersectedList = kv.Value.Intersect(subset);
			//    if (intersectedList.Count<int>() > 0) {
			//        newOrg.Add(kv.Key, intersectedList.ToList<int>());
			//    }
			//}
			//return newOrg;
		}

		public override string Id(int k) {
			return invertedData[k].ToString();
		}


		/// <summary>
		/// Given an image id, returns its value for this organizable
		/// </summary>
		/// <param name="k">The MSI-Id for the image</param>
		/// <returns></returns>
		public String Color(int k) {
			return invertedData[k];
		}

		public override Boolean ContainsId(int k) {
			return invertedData.ContainsKey(k);
		}*/
	}
}
