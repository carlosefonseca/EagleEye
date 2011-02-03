using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EagleEye.Common {
	public class SortedImageCollection {
		private List<Image> collection;

		public SortedImageCollection() {
		}

		public SortedImageCollection(List<Image> imgs) {
			collection = imgs;
		}

		public SortedImageCollection(ImageCollection ic) {
			collection = ic.ToList();
		}

		public int Count() {
			return collection.Count;
		}

		public string ToStringWithExif(string key) {
			string txt = "";

			foreach (Image i in collection) {
				txt += i.id + "\t" + i.path + "\t>  ";
				if (i.ContainsExif(key)) {
					txt += i.Exif(key).ToString() + "\n";
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


		public SortedImageCollection SortByExif(string key) {
			if (key == "date") {
				ImageDateComparer c;
				c = new ImageDateComparer();
				collection.Sort(c);
			} else {
				ImageExifComparer c;
				Console.WriteLine("Comparing by " + key);
				c = new ImageExifComparer(key);
				collection.Sort(c);
			}
			return this;
		}
		
	}
}
