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
using DeepZoomView.EECanvas;
using DeepZoomView.Controls;

namespace DeepZoomView
{
	public class Organizable
	{
		public readonly String Name;
		public Boolean isNumber = false;
		public Dictionary<string, List<int>> data;
		public Dictionary<int, string> invertedData;
		public Boolean AvailableForGroupping = true;
		public List<Group> ListOfGroups = null;
		public Dictionary<Object, List<int>> dataWithStacks;
		public Dictionary<int, List<int>> stacks;
		public Dictionary<int, List<CanvasItem>> stacksForCanvas;
		public Boolean hasStacks = false;
		public List<String> Dispositions = new List<string>();

		protected IEnumerable<int> filter = null;
		protected IEnumerable<int> filteredIds = null;
		protected List<Group> filteredListOfGroups = null;
		protected Dictionary<string, List<int>> filteredData = null;

		protected bool HasFilter { get { return (filter != null && filter.Count() > 0); } }

		public IEnumerable<String> KeysThatMatch(String txt)
		{
			return data.Keys.Where(s => s.Contains(txt));
		}

		public IEnumerable<int> IdsForKey(string k)
		{
			return data[k];
		}

		public IEnumerable<int> IdsForKey(IEnumerable<String> ks)
		{
			List<int> list = new List<int>();
			foreach (String s in ks)
			{
				list.AddRange(data[s]);
			}
			return list;
		}

		public virtual List<int> Ids
		{
			get
			{
				if (HasFilter)
				{
					if (filteredIds == null)
					{
						filteredIds = invertedData.Keys.Intersect(filter);
					}
					return filteredIds.ToList();
				}
				else
				{
					return invertedData.Keys.ToList();
				}
			}
		}

		public Organizable(Organizable o)
		{
			this.Name = o.Name;
			this.isNumber = o.isNumber;
			this.data = new Dictionary<string, List<int>>(o.data);
			this.invertedData = new Dictionary<int, string>(o.invertedData);
			this.AvailableForGroupping = o.AvailableForGroupping;
			this.ListOfGroups = new List<Group>(o.ListOfGroups);
			this.dataWithStacks = new Dictionary<object, List<int>>(o.dataWithStacks);
			this.stacks = new Dictionary<int, List<int>>(o.stacks);
			//this.stacksForCanvas
		}


		public virtual Group GetGroupContainingKey(int k)
		{
			String id = this.Id(k);
			if (!String.IsNullOrEmpty(id))
			{
				return ListOfGroups.First(g => g.name.CompareTo(id) == 0);
			}
			return null;
		}

		/// <summary>
		/// Total of Images
		/// </summary>
		public virtual int ItemCount
		{
			get
			{
				return Ids.Count;
			}
		}

		/// <summary>
		/// Total of Groups
		/// </summary>
		public virtual int GroupCount
		{
			get
			{
				return GroupList().Count;
			}
		}

		public Organizable(String name)
		{
			this.Name = name;
			data = new Dictionary<string, List<int>>();
			invertedData = new Dictionary<int, string>();
			dataWithStacks = new Dictionary<Object, List<int>>();
			stacksForCanvas = new Dictionary<int, List<CanvasItem>>();
			Dispositions.Add("Group");
		}

		public Boolean Import(String s) { return false; }

		/// <summary>
		/// Returns a list of GROUPS. It's converted from the GetGroups thing
		/// </summary>
		/// <returns></returns>
		public List<Group> GroupList()
		{
			if (HasFilter)
			{
				if (filteredListOfGroups == null)
				{
					filteredListOfGroups = new List<Group>();
					List<KeyValuePair<string, List<int>>> gs = GetGroups();
					foreach (KeyValuePair<string, List<int>> kv in gs)
					{
						List<int> l = kv.Value.Intersect(filter).ToList();
						if (l.Count > 0)
						{
							Group g = new Group(kv.Key, l);
							filteredListOfGroups.Add(g);
						}
					}
				}
				return filteredListOfGroups;
			}


			if (ListOfGroups == null)
			{
				ListOfGroups = new List<Group>();
				List<KeyValuePair<string, List<int>>> gs = GetGroups();
				foreach (KeyValuePair<string, List<int>> kv in gs)
				{
					Group g;
					if (HasFilter)
					{
						g = new Group(kv.Key, kv.Value.Intersect(filter).ToList());
					}
					else
					{
						g = new Group(kv.Key, kv.Value);
					}
					ListOfGroups.Add(g);
				}
			}
			return ListOfGroups;
		}

		public virtual List<KeyValuePair<string, List<int>>> GetGroups()
		{
			filteredData = new Dictionary<string, List<int>>();
			if (HasFilter)
			{
				foreach (KeyValuePair<string, List<int>> kv in data)
				{
					List<int> l = kv.Value.Intersect(filter).ToList();
					if (l.Count > 0)
					{
						filteredData.Add(kv.Key, l);
					}
				}
			}
			else
			{
				filteredData = data;
			}

			if (isNumber)
			{
				return filteredData.OrderBy(x => Convert.ToInt32(x.Key)).ToList();
			}
			else
			{
				return filteredData.OrderBy(x => x.Key).ToList();
			}
		}

		public virtual List<KeyValuePair<String, List<int>>> GetGroups(List<int> subset)
		{
			List<KeyValuePair<string, int>> filter = subset.Join(
				invertedData,
				id => id,
				data => data.Key,
				(id, data) => new KeyValuePair<string, int>(data.Value, id)
				).ToList();

			ILookup<string, int> lookup = filter.ToLookup(kv => kv.Key, kv => kv.Value);

			List<KeyValuePair<string, List<int>>> outList = new List<KeyValuePair<string, List<int>>>();
			foreach (IGrouping<string, int> group in lookup)
			{
				outList.Add(new KeyValuePair<string, List<int>>(group.Key, group.ToList()));
			}
			return outList;
		}

		public virtual void Add(int k, string p)
		{
			if (!data.ContainsKey(p))
			{
				data.Add(p, new List<int>());
			}
			data[p].Add(k);

			invertedData.Add(k, p);
		}


		/// <summary>
		/// Given an image id, returns its value for this organizable
		/// </summary>
		/// <param name="k">The MSI-Id for the image</param>
		/// <returns></returns>
		public virtual string Id(int k)
		{
			if (invertedData.ContainsKey(k))
			{
				return invertedData[k];
			}
			else
			{
				return null;
			}
		}

		public virtual Boolean ContainsId(int k)
		{
			return invertedData.ContainsKey(k);
		}

		public override string ToString()
		{
			return this.Name;
		}

		internal void ReplaceFilter(IEnumerable<int> i)
		{
			this.filter = null;
			AddFilter(i);
		}

		internal void AddFilter(IEnumerable<int> iEnumerable)
		{
			if (this.filter == null)
			{
				this.filter = iEnumerable;

			}
			else
			{
				this.filter.Concat(iEnumerable);
			}
			filteredData = null;
			filteredIds = null;
			filteredListOfGroups = null;
		}

		internal void ClearFilter()
		{
			this.filter = null;
			filteredData = null;
			filteredIds = null;
			filteredListOfGroups = null;
		}

		internal virtual IEnumerable<AutocompleteOption> RelatedKeys(String k)
		{
			if (this.isNumber)
			{
				int n;
				try
				{
					n = Convert.ToInt32(k);
					return new List<AutocompleteOption>() { new AutocompleteOption(n.ToString(), this.Name + ": " + n, this) };
				}
				catch (FormatException)
				{
					return new List<AutocompleteOption>();
				}
			}
			else
			{
				String[] aaa = data.Keys.Where(s => s.IndexOf(k, StringComparison.InvariantCultureIgnoreCase) != -1).ToArray();
				return data.Keys.Where(s => s.IndexOf(k, StringComparison.InvariantCultureIgnoreCase) != -1).Select(s => new AutocompleteOption(s, this.Name + ": " + s, this));
			}
		}
	}
}
