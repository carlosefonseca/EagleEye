using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using ColorUtils;
using System.IO;

namespace DeepZoomView {
	public class OrganizableByPath : Organizable {

		/// <summary>
		/// Constructor
		/// </summary>
		public OrganizableByPath()
			: base("Path") {
		}

		public override void Add(int k, string p) {
			String dir = Path.GetDirectoryName(p);

			if (!data.ContainsKey(dir)) {
				data.Add(dir, new List<int>());
			}
			data[dir].Add(k);
	
			invertedData[k] = p;
		}

		public override Group GetGroupContainingKey(int k)
		{
			String id = this.Id(k);
			if (!String.IsNullOrEmpty(id))
			{
				return ListOfGroups.First(g => id.StartsWith(g.name));
			}
			return null;
		}
	}
}
