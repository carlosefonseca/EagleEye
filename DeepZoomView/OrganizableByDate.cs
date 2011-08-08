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
using System.Globalization;
using System.Text.RegularExpressions;
using DeepZoomView.Controls;

namespace DeepZoomView
{
	public class OrganizableByDate : Organizable
	{
		public new Dictionary<DateTime, List<int>> data = new Dictionary<DateTime, List<int>>();
		public new Dictionary<int, DateTime> invertedData = new Dictionary<int, DateTime>();
		public new Dictionary<DateTime, List<int>> dataWithStacks = new Dictionary<DateTime, List<int>>();
		public new Dictionary<int, DateTime> invertedDataWithStacks = new Dictionary<int, DateTime>();

		private int itemCount = -1;

		public override List<int> Ids
		{
			get
			{
				if (HasFilter)
				{
					if (filteredIds == null)
					{
						if (filter.Any(i => i < 0))
						{
							filteredIds = invertedDataWithStacks.Keys.Intersect(filter);
						}
						else
						{
							filteredIds = invertedData.Keys.Intersect(filter);
						}
					}
					return filteredIds.ToList();
				}
				else
				{
					return invertedDataWithStacks.Keys.ToList();
				}
			}
		}


		public override int ItemCount
		{
			get
			{
				return this.Ids.Count;
			}
		}

		public override int GroupCount
		{
			get
			{
				return data.Count;
			}
		}

		public OrganizableByDate()
			: base("Date")
		{
			hasStacks = true;
			Dispositions.Add("Linear");
		}

		public override void Add(int k, string p)
		{
			DateTime v = DateTime.Parse(p, new System.Globalization.CultureInfo("pt-PT"));
			DateTime key = v.Date;
			if (!data.ContainsKey(key))
			{
				data.Add(key, new List<int>());
			}
			data[key].Add(k);

			invertedData.Add(k, v);
		}

		public override List<KeyValuePair<String, List<int>>> GetGroups()
		{
			Dictionary<DateTime, List<int>> filteredDataWithStacks;
			if (HasFilter)
			{
				Dictionary<DateTime, List<int>> dataOrDataWithStacks;
				if (filter.Any(i => i < 0))
				{ // filter is stack aware
					dataOrDataWithStacks = dataWithStacks;
				}
				else
				{
					dataOrDataWithStacks = data;
				}


				filteredDataWithStacks = new Dictionary<DateTime, List<int>>();
				foreach (KeyValuePair<DateTime, List<int>> kv in dataOrDataWithStacks)
				{
					IEnumerable<int> newV = kv.Value.Intersect(filter);
					if (newV.Count() > 0)
					{
						filteredDataWithStacks.Add(kv.Key, newV.ToList());
					}
				}
			}
			else
			{
				filteredDataWithStacks = dataWithStacks;
			}


			IEnumerable<KeyValuePair<DateTime, List<int>>> ordered = filteredDataWithStacks;
			List<KeyValuePair<String, List<int>>> reformated = new List<KeyValuePair<string, List<int>>>();
			foreach (KeyValuePair<DateTime, List<int>> group in ordered)
			{
				reformated.Add(new KeyValuePair<string, List<int>>(group.Key.ToShortDateString(), group.Value));
			}
			return reformated;
		}

		public override List<KeyValuePair<String, List<int>>> GetGroups(List<int> subset)
		{
			throw new NotImplementedException();
		}


		/// <summary>
		/// Given an image id, returns its value for this organizable
		/// </summary>
		/// <param name="k">The MSI-Id for the image</param>
		/// <returns></returns>
		public override string Id(int k)
		{
			if (invertedData.ContainsKey(k))
			{
				return invertedData[k].ToString();
			}
			else
			{
				return null;
			}
		}

		public override Boolean ContainsId(int k)
		{
			return invertedData.ContainsKey(k);
		}

		internal void CreateStacks()
		{
			hasStacks = true;

			// for each group, sort by time
			for (int i = 0; i < data.Count(); i++)
			{
				data[data.ElementAt(i).Key] = data[data.ElementAt(i).Key].OrderBy(x => invertedData[x]).ToList();
			}

			Stacking s = new Stacking();
			stacks = s.MakeStacks(this.invertedData);
			dataWithStacks = new Dictionary<DateTime, List<int>>(data);

			IEnumerable<int> stackItems = stacks.SelectMany(kv => kv.Value);
			invertedDataWithStacks = invertedData.Where(kv => !stackItems.Contains(kv.Key)).ToDictionary(k => k.Key, k => k.Value);

			foreach (KeyValuePair<int, List<int>> stack in stacks)
			{
				int firstID = stack.Value.First();
				DateTime groupKey = invertedData[firstID].Date;
				dataWithStacks[groupKey].Add(stack.Key);
				dataWithStacks[groupKey] = dataWithStacks[groupKey].Except(stack.Value).ToList();
				invertedDataWithStacks.Add(stack.Key, groupKey);
			}
		}


		internal virtual IEnumerable<AutocompleteOption> RelatedKeys(String k)
		{
			List<AutocompleteOption> list = new List<AutocompleteOption>();

			DateTimeFormatInfo date = new CultureInfo("en-US").DateTimeFormat;
			list.Concat(date.AbbreviatedMonthGenitiveNames.Where(s => s.Contains(k)).Select(s => new AutocompleteOption(s, "Taken in "+s, null, this)));

			list.Concat(new List<String>() { "Winter", "Spring", "Summer", "Autum" }.Where(s => s.Contains(k)).Select(s => new AutocompleteOption(s, "Taken during "+s, null, this)));

			try
			{
				DateTime d = DateTime.Parse(k);
				list.Concat(data.Keys.Where(dt => dt.Date.CompareTo(d.Date) == 0).Select(dd => new AutocompleteOption(dd.ToShortDateString(), "Taken in "+dd.ToShortDateString(), null, this)));
			}
			catch { }


			try
			{
				Match m;
				if (new Regex(@"^\d{4}$").IsMatch(k))
				{
					list.Add(new AutocompleteOption(k, "Year " + k, this));
				}
				else
				{
					m = new Regex(@"^(\d{4})[-/.](\d{2})$").Match(k);
					if (m.Success)
					{
						String ym = new DateTime(Convert.ToInt32(m.Groups[1]), Convert.ToInt32(m.Groups[2]), 1).ToString(date.YearMonthPattern);
						list.Add(new AutocompleteOption(k, ym, this));
					}
				}
			}
			catch { }

			return list;
		}
	}
}
