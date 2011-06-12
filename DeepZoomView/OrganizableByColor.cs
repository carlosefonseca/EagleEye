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
	public class OrganizableByColor : Organizable {
		public new Dictionary<int, List<int>> data = new Dictionary<int, List<int>>();
		public new Dictionary<int, Color> invertedData = new Dictionary<int, Color>(); 
		/*Dictionary<int, List<int>> organization;
		public Dictionary<int, int> colorOfId;*/

		/// <summary>
		/// Constructor
		/// </summary>
		public OrganizableByColor() : base("Color"){
			//organization = new Dictionary<int, List<int>>();
			//colorOfId = new Dictionary<int, int>();
		}

		
		public override void Add(int k, string p) {
			Color c = ColorUtil.FromStringToColor(p);
			int hue = HslColor.FromColor(c).SimpleHue();
			if (!data.ContainsKey(hue)) {
				data.Add(hue, new List<int>());
			}
			data[hue].Add(k);

			invertedData.Add(k, c);
		}



		/// <summary>
		/// Imports the contents of a metadata file into the this organizable
		/// </summary>
		/// <param name="s">The string with the contents of the file</param>
		/// <returns>True if the import is successful and false otherwise</returns>
		//new public Boolean Import(String s) {
		//    throw new NotSupportedException();
		//    try {
		//        String[] lines = s.Split(new string[1] { Environment.NewLine }, 
		//                                    StringSplitOptions.RemoveEmptyEntries);

		//        foreach (String line in lines) {
		//            String[] split = line.Split(':');
		//            String[] ids = split[1].Split(new Char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
		//            Add(Convert.ToInt16(split[0]), ids);
		//        }
		//    } catch {
		//        organization = new Dictionary<int, List<int>>();
		//        return false;
		//    }
		//    return true;
		//}

		/// <summary>
		/// Adds a Color, Image pair to the organizable
		/// </summary>
		/// <param name="color">The Color</param>
		/// <param name="id">The image ID</param>
		//public void Add(int color, int id) {
		//    if (!organization.ContainsKey(color)) {
		//        organization.Add(color, new List<int>());
		//    }
		//    organization[color].Add(id);
		//}

		/// <summary>
		/// Adds a set of images to a color. Intended to be used with the Import
		/// </summary>
		/// <param name="color">The Color</param>
		/// <param name="ids">Array of ids in string format</param>
		//private void Add(int color, String[] ids) {
		//    if (!organization.ContainsKey(color)) {
		//        organization.Add(color, new List<int>());
		//    }
		//    foreach (String id in ids) {
		//        organization[color].Add(Convert.ToInt16(id));
		//        colorOfId.Add(Convert.ToInt16(id), color);
		//    }
		//}


		public override List<KeyValuePair<String, List<int>>> GetGroups() {
			return GetGroups(null);
		}


		/// <summary>
		/// Takes a list of 
		/// </summary>
		/// <param name="subset"></param>
		/// <returns></returns>
		public override List<KeyValuePair<String,List<int>>> GetGroups(List<int> subset) {
			if (data.Count == 0) {
				return null;
			}

			List<KeyValuePair<String, List<int>>> groupsOut = new List<KeyValuePair<string, List<int>>>();
			Dictionary<int, List<int>> groups = new Dictionary<int,List<int>>();

			Dictionary<int, List<int>> set = null;
			if (subset == null) {
				set = data;
			} else {
				set = OrganizedSubset(subset);
			}

			Color[] theColors = new Color[] { Colors.Red, Colors.Orange, Colors.Yellow, Colors.Green, Colors.Cyan, Colors.Blue, Colors.Purple };

			int buckets = 20;
			double spread = 360.0 / buckets;

			foreach (KeyValuePair<int, List<int>> kv in set) {
				int group = Convert.ToInt16(Math.Round((kv.Key % Math.Ceiling(360-(spread/2))) / spread));
				Console.WriteLine("Hue: "+kv.Key+" -> "+group);
				if (!groups.ContainsKey(group)) {
					groups.Add(group, new List<int>());
				}
				List<int> tmp1 = kv.Value;
				IEnumerable<int> tmp = groups[group].Union<int>(tmp1);
				groups[group] = tmp.ToList<int>();
			}

			Dictionary<int, String> colorNames = new Dictionary<int, string>();
			colorNames.Add(0, "Red");
			colorNames.Add(1, "Yellow");
			colorNames.Add(2, "Green");
			colorNames.Add(3, "Cyan");
			colorNames.Add(4, "Blue");
			colorNames.Add(5, "Pink");

			List<int> sortedKeys = groups.Keys.ToList<int>();
			sortedKeys.Sort();
			foreach (int c in sortedKeys) {
				groupsOut.Add(new KeyValuePair<String, List<int>>(c.ToString(), groups[c]));
			}

			return groupsOut;
		}

		/// <summary>
		/// Intersects the collection with a given list of ids. For internal use.
		/// </summary>
		/// <param name="subset">A list of images</param>
		/// <returns>A new dictionary with only the chosen groups and images</returns>
		private Dictionary<int, List<int>> OrganizedSubset(List<int> subset) {
			Dictionary<int, List<int>> newOrg = new Dictionary<int, List<int>>();
			IEnumerable<int> intersectedList;
			foreach (KeyValuePair<int, List<int>> kv in data) {
				intersectedList = kv.Value.Intersect(subset);
				if (intersectedList.Count<int>() > 0) {
					newOrg.Add(kv.Key, intersectedList.ToList<int>());
				}
			}
			return newOrg;
		}

		public override string Id(int k) {
			throw new NotSupportedException("Use Color() instead");
		}


		/// <summary>
		/// Given an image id, returns its value for this organizable
		/// </summary>
		/// <param name="k">The MSI-Id for the image</param>
		/// <returns></returns>
		public Color Color(int k) {
			return invertedData[k];
		}

		public override Boolean ContainsId(int k) {
			return invertedData.ContainsKey(k);
		}
	}
}
