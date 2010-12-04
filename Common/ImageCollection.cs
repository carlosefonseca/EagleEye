using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EagleEye.Common {

	public delegate string ImageToStringDelegate(Image i);

	public class ImageCollection {
		internal Dictionary<long, Image> collection;
		private long currId = 0;
		private SortedImageCollection cachedSortedImageCollection;

		public ImageCollection() {
			collection = new Dictionary<long, Image>();
		}

		public ImageCollection(List<Image> list) {
			collection = new Dictionary<long, Image>();
			AddNew(list);
		}

		public ImageCollection(Dictionary<long, Image> c) {
			collection = c;
		}

		public long Add(Image i) {
			long k = currId;
			if (i.id != -1) {
				k = i.id;
				if (currId <= k)
					currId = k + 1;
			} else {
				i.id = k;
			}
			collection.Add(k, i);
			cachedSortedImageCollection = null;
			currId++;
			return k;
		}

		/*		public void Add(long k, Image i) {
					collection.Add(k, i);
					i.id = currId;
					cachedSortedImageCollection = null;
					if (currId <= k)
						currId = k + 1;
				}*/

		public void AddNew(List<Image> list) {
			cachedSortedImageCollection = null;
			foreach (Image i in list) {
				i.id = currId;
				collection.Add(currId, i);
				currId++;
			}
		}

		public List<long> GetKeys() {
			return collection.Keys.ToList();
		}

		public Image Get(int id) {
			return collection[id];
		}

		public int Count() {
			return collection.Count;
		}


		public ImageCollection ImagesWithExifKey(string key) {
			ImageCollection newIC = new ImageCollection();
			foreach (Image i in collection.Values) {
				if (i.ContainsExif(key)) {
					newIC.Add(i);
				}
			}
			return newIC;
		}

		public String ToString(ImageToStringDelegate d) {
			string txt = "";
			foreach (Image i in collection.Values) {
				txt += d(i) + "\n";
			}
			return txt;
		}



		#region Output

		public List<Image> ToList() {
			return collection.Values.ToList<Image>();
		}

		public virtual string ToStringWithExif(string key) {
			string txt = "";
			int max = 50;
			if (collection.Count < max)
				max = collection.Count;
			for (int i = 0; i < max; i++) {
				txt += collection[i].id + "\t" + collection[i].path + " > " + collection[i].Exif(key).ToString() + "\n";
			}
			return txt;
		}


		public override string ToString() {
			string txt = "";
			int max = 50;
			if (collection.Count < max)
				max = collection.Count;
			for (int i = 0; i < max; i++) {
				txt += collection[i].path + "\n";
			}
			return txt;
		}


		public SortedImageCollection ToSortable() {
			if (cachedSortedImageCollection == null)
				cachedSortedImageCollection = new SortedImageCollection(this);
			return cachedSortedImageCollection;
		}


		public Dictionary<long, Image> TheDictionary() {
			return collection;
		}

		#endregion Output

	}
}
