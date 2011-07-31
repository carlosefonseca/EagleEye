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
using System.Linq;
using DeepZoomView.EECanvas;

namespace DeepZoomView {
	public class OrganizableByDate : Organizable {
		public new Dictionary<DateTime, List<int>> data = new Dictionary<DateTime, List<int>>();
        public new Dictionary<int, DateTime> invertedData = new Dictionary<int, DateTime>();
        public new Dictionary<DateTime, List<int>> dataWithStacks = new Dictionary<DateTime, List<int>>();

        private int itemCount = -1;

        public override List<int> Ids
        {
            get
            {
                return invertedData.Keys.Concat(stacks.Keys).ToList();
            }
        }

		public override int ItemCount {
			get {
                if (itemCount == -1)
                {
                    itemCount = dataWithStacks.Sum(kv => kv.Value.Count);
                }

				//return invertedData.Count;
                return itemCount;
			}
		}

		public override int GroupCount {
			get {
				return data.Count;
			}
		}

		public OrganizableByDate() : base("Date") { }

		public override void Add(int k, string p) {
			DateTime v = DateTime.Parse(p, new System.Globalization.CultureInfo("pt-PT"));
			DateTime key = v.Date;
			if (!data.ContainsKey(key)) {
				data.Add(key, new List<int>());
			}
			data[key].Add(k);

			invertedData.Add(k, v);
		}

		public override List<KeyValuePair<String, List<int>>> GetGroups() {
			List<KeyValuePair<DateTime, List<int>>> ordered = dataWithStacks.OrderBy(x => x.Key).ToList();
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
        
        internal void CreateStacks()
        {
            hasStacks = true;

            // for each group, sort by time
            for (int i = 0; i < data.Count(); i++) {
                data[data.ElementAt(i).Key] = data[data.ElementAt(i).Key].OrderBy(x => invertedData[x]).ToList();
            }

            Stacking s = new Stacking();
            stacks = s.MakeStacks(this.invertedData);
            dataWithStacks = new Dictionary<DateTime, List<int>>(data);

            foreach(KeyValuePair<int, List<int>> stack in stacks) {
                int firstID = stack.Value.First();
                DateTime groupKey = invertedData[firstID].Date;
                dataWithStacks[groupKey].Add(stack.Key);
                dataWithStacks[groupKey] = dataWithStacks[groupKey].Except(stack.Value).ToList();
            }
        }
    }
}
