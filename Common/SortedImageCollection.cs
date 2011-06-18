using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Linq;

namespace EagleEye.Common {
	public class SortedImageCollection : ImageCollection {
		private new List<Image> collection;

		public SortedImageCollection() {
		}

		public SortedImageCollection(List<Image> imgs) {
			collection = imgs;
		}

		public SortedImageCollection(ImageCollection ic) {
			collection = ic.ToList();
		}

		public new int Count() {
			return collection.Count;
		}

		public new string ToStringWithExif(string key) {
			string txt = "";

			foreach (Image i in collection) {
				txt += i.id + "\t" + i.path + "\t>  ";
				if (i.ContainsExif(key)) {
					object exifdata = i.Exif(key);
					if (exifdata is List<object>) {
						txt += "[";
						((List<object>)exifdata).ForEach(delegate(object name) {
							txt += name.ToString() + ",";
						});
						txt = txt.TrimEnd(",".ToCharArray())+"]\n";
					} else {
						txt += i.Exif(key).ToString() + "\n";
					}
				} else {
					txt += "[no " + key + "]\n";
				}
			}
			return txt;
		}

		public SortedImageCollection SortById() {
			ImageIdComparer c = new ImageIdComparer();
			collection.Sort(c);
			return this;
		}


		public SortedImageCollection SortByDate() {
			ImageDateComparer c = new ImageDateComparer();
			collection.Sort(c);
			return this;
		}

		public SortedImageCollection SortByExif(string key) {
			if (key == "date") {
				SortByDate();
			} else {
				ImageExifComparer c;
				Console.WriteLine("Comparing by " + key);
				c = new ImageExifComparer(key);
				IEnumerable<Image> filtered = collection.Where(i => i.ContainsExif(key));
				collection = filtered.ToList();
				collection.Sort(c);
			}
			return this;
		}

		public List<Image> TheList() {
			return collection;
		}
	}
}
