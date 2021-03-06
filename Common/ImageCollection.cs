﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EagleEye.Common {

	public delegate string ImageToStringDelegate(Image i);

	public class ImageCollection {
		internal Dictionary<long, Image> collection;
		private long currId = 0;
		private SortedImageCollection cachedSortedImageCollection;
		private Dictionary<String, long> paths = new Dictionary<string, long>();

		public ImageCollection() {
			collection = new Dictionary<long, Image>();
		}

		public ImageCollection(List<Image> list) {
			collection = new Dictionary<long, Image>();
			AddNew(list);
		}

		public ImageCollection(Dictionary<long, Image> c) {
			collection = c;
			currId = collection.Keys.Max() + 1;
		}

		internal void Replace(Dictionary<long, Image> c) {
			if (c.Count > 0) {
				collection = c;
				currId = collection.Keys.Max() + 1;
			}
//			List<long> duplicatedKeys = new List<long>();
			foreach (KeyValuePair<long, Image> kv in collection) {
/*				if (paths.ContainsKey(kv.Value.path)) {
					duplicatedKeys.Add(kv.Key);
					continue;
				}
*/				paths.Add(kv.Value.path, kv.Value.id);
			}
//			duplicatedKeys.ForEach(id => Remove(id));
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
			paths.Add(i.path, i.id);
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

		public Boolean PathExists(String path) {
			return paths.ContainsKey(System.IO.Path.GetFullPath(path));
		}

		public void AddNew(List<Image> list) {
			cachedSortedImageCollection = null;
			foreach (Image i in list) {
				i.id = currId;
				collection.Add(currId, i);
				paths.Add(i.path, i.id);
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

		/// <summary>
		/// Images that contain the specified Exif key
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public ImageCollection ImagesWithExifKey(string key) {
			ImageCollection newIC = new ImageCollection();
			foreach (Image i in collection.Values) {
				if (i.ContainsExif(key)) {
					newIC.Add(i);
				}
			}
			return newIC;
		}

		/// <summary>
		/// Images that contain any of the specified Exif keys
		/// </summary>
		/// <param name="keys">Array with the keys that each Image must contain.</param>
		/// <returns></returns>
		public ImageCollection ImagesWithAnyExifKeys(string[] keys) {
			ImageCollection newIC = new ImageCollection();
			foreach (Image i in collection.Values) {
				foreach (string key in keys) {
					if (i.ContainsExif(key)) {
						newIC.Add(i);
						continue;
					}
				}
			}
			return newIC;
		}

		/// <summary>
		/// Images that contain all of the specified Exif keys
		/// </summary>
		/// <param name="keys">Array with the keys that each Image must contain.</param>
		/// <returns></returns>
		public ImageCollection ImagesWithAllExifKeys(string[] keys) {
			ImageCollection newIC = new ImageCollection();
			foreach (Image i in collection.Values) {
				Boolean accepted = true;
				foreach (string key in keys) {
					if (!i.ContainsExif(key)) {
						accepted = false;
						break;
					}
				}
				if (accepted) 
					newIC.Add(i);
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

		public virtual void Remove(long id) {
			if (paths.ContainsValue(id)) {
				paths.Remove(collection[id].path);
			}
			collection.Remove(id);
		}

		#region Output

		public List<Image> ToList() {
			return collection.Values.ToList<Image>();
		}

		public virtual string ToStringWithExif(string key) {
			string txt = "";

			foreach (KeyValuePair<long, Image> kv in collection) {
				txt += kv.Key.ToString() + "\t" + kv.Value.path + " > ";
				if (kv.Value.ContainsExif(key)) {
					txt += kv.Value.Exif(key).ToString() + "\n";
				} else {
					txt += "[no " + key + "]\n";
				}
			}
			return txt;
		}


		public override string ToString() {
			string txt = "";
			int max = 50;
			if (collection.Count < max)
				max = collection.Count;
			for (int i = 0; i < max; i++) {
				txt += collection[i].id + ": "+ collection[i].path + "\n";
			}
			return txt;
		}

		/// <summary>
		/// Returns a (possibly cached) copy of the collection that can be sorted. Internally is a List
		/// </summary>
		/// <returns></returns>
		public SortedImageCollection ToSortable() {
			if (cachedSortedImageCollection == null)
				cachedSortedImageCollection = new SortedImageCollection(this);
			return cachedSortedImageCollection;
		}


		public Dictionary<long, Image> TheDictionary() {
			return collection;
		}

		#endregion Output


		public Image Get(long item) {
			return Get(Convert.ToInt32(item));
		}
	}
}
