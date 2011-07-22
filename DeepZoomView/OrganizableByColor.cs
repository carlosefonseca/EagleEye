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

        const int BLACK = -1;
        const int GREY = -2;
        const int WHITE = -3;
        
        public override int ItemCount
        {
			get {
				return invertedData.Count;
			}
		}

		public override int GroupCount {
			get {
				return data.Count;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public OrganizableByColor()
			: base("Color") {
		}


		public override void Add(int k, string p) {
			int hue;
			double sat = -1, lig = -1;
			Color c = Colors.White;
			try {
				c = ColorUtil.FromStringToColor(p);
			} catch {
				return;
			}
			HslColor hsl = HslColor.FromColor(c);
			hue = hsl.SimpleHue();
			sat = hsl.S;
			lig = hsl.L;
			int key = hue;
			if (lig != -1 && sat != -1) {
				if (lig < 0.2) {
					key = BLACK;
				} else if (lig > 0.9) {
					key = WHITE;
				} else if (sat < 0.1) {
					key = GREY;
				}
			}


			if (!data.ContainsKey(key)) {
				data.Add(key, new List<int>());
			}
			data[key].Add(k);
			invertedData.Add(k, c);
		}


		public override List<KeyValuePair<String, List<int>>> GetGroups() {
			return GetGroups(null);
		}


		/// <summary>
		/// Takes a list of 
		/// </summary>
		/// <param name="subset"></param>
		/// <returns></returns>
		public override List<KeyValuePair<String, List<int>>> GetGroups(List<int> subset) {
			if (data.Count == 0) {
				return null;
			}

			List<KeyValuePair<String, List<int>>> groupsOut = new List<KeyValuePair<string, List<int>>>();
			Dictionary<int, List<int>> groups = new Dictionary<int, List<int>>();

			Dictionary<int, List<int>> set = null;
			if (subset == null) {
				set = data;
			} else {
				set = OrganizedSubset(subset);
			}

			Color[] theColors = new Color[] { Colors.Red, Colors.Orange, Colors.Yellow, Colors.Green, Colors.Cyan, Colors.Blue, Colors.Purple };
            
			int buckets = 12;
			double spread = 360.0 / buckets;

			foreach (KeyValuePair<int, List<int>> kv in set) {
				int group;
				if (kv.Key < 0) {	// Black/Gray/White
					group = kv.Key;
				} else {			// Hue
					group = Convert.ToInt16(Math.Round((kv.Key % Math.Ceiling(360 - (spread / 2))) / spread));
                    group = (int)(group * spread + spread * 0.5);
				}
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
            HslColor cccc = new HslColor();

			foreach (int c in sortedKeys) {
                if (c < 0)
                {
                    switch (c)
                    {
                        case WHITE: cccc.L = 1; break;
                        case GREY: cccc.L = 0.5; break;
                        case BLACK: cccc.L = 0; break;
                    }
                    cccc.S = 0;
                }
                else
                {
                    cccc.H = c;
                    cccc.S = 1;
                    cccc.L = 0.5;
                }
                Color color = cccc.ToColor();
                String s = String.Format("#{0} {1} {2}", color.R, color.G, color.B);
                groupsOut.Add(new KeyValuePair<String, List<int>>(s, groups[c]));
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
			return invertedData[k].ToString();
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
