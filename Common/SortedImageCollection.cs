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

		public string ToStringWithExif(string key) {
			string txt = "";
			int max = 50;
			if (collection.Count < max) {
				max = collection.Count;
				for (int i = 0; i < max; i++)
					txt += collection[i].path + " > " + collection[i].Exif(key).ToString() + "\n";
			} else {
				for (int i = 0; i < max / 2; i++)
					txt += collection[i].path + " > " + collection[i].Exif(key).ToString() + "\n";
				txt += "...\n";
				for (int i = collection.Count - max / 2; i < collection.Count; i++)
					txt += collection[i].path + " > " + collection[i].Exif(key).ToString() + "\n";
			}
			return txt;
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
