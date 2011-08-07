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
			invertedData[k] = p.Replace(",",", ");
		}
	}
}
