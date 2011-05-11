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

namespace DeepZoomView {
	public class DateCollection {
		private Dictionary<int, Dictionary<int, Dictionary<int, List<int>>>> root;

		public DateCollection() {
			root = new Dictionary<int, Dictionary<int, Dictionary<int, List<int>>>>();
		}

		public void Add(DateTime date, int id) {
			int y = date.Year;
			int m = date.Month;
			int d = date.Day;

			try {
				root[y][m][d].Add(id);
			} catch (KeyNotFoundException) {
				if (!root.ContainsKey(y)) {
					root.Add(y, new Dictionary<int, Dictionary<int, List<int>>>());
				}
				if (!root[y].ContainsKey(m)) {
					root[y].Add(m, new Dictionary<int, List<int>>());
				}
				if (!root[y][m].ContainsKey(d)) {
					root[y][m].Add(d, new List<int>());
				}
				root[y][m][d].Add(id);
			}
		}

		public Dictionary<int, Dictionary<int, Dictionary<int, List<int>>>> Get() {
			return root;
		}

		public void Add(DateTime date, string[] ids) {
			int y = date.Year;
			int m = date.Month;
			int d = date.Day;

			int id;

			List<int> list;

			try {
				list = root[y][m][d];
			} catch (KeyNotFoundException) {
				if (!root.ContainsKey(y)) {
					root.Add(y, new Dictionary<int, Dictionary<int, List<int>>>());
				}
				if (!root[y].ContainsKey(m)) {
					root[y].Add(m, new Dictionary<int, List<int>>());
				}
				if (!root[y][m].ContainsKey(d)) {
					root[y][m].Add(d, new List<int>());
				}
				list = root[y][m][d];
			}
			int count = root[y][m][d].Count;
			foreach (String s in ids) {
				id = int.Parse(s);
				list.Add(id);
			}
			if (root[y][m][d].Count == count) {
				throw new Exception("A LISTA NÃO FOI BEM MODIFICADA!!!!1");
			}
		}
	}
}
