using System;

namespace EagleEye.Common {
	public delegate T ConvertFromBytes<T>(byte[] bytes);
	
	public class PersistedImageCollection : ImageCollection {

		private Persistence persistence;

		public PersistedImageCollection(string filename)
			: base() {
			persistence = new Persistence(filename);
			if (persistence.OpenOrCreate()) { // true if opened
				Load();
			}
		}

		ConvertFromBytes<Image> ReadValue = delegate(byte[] bytes) {
			return new Image(bytes);
		};

		ConvertFromBytes<long> ReadKey = delegate(byte[] bytes) {
			System.Text.Encoding enc = System.Text.Encoding.ASCII;
			string k = enc.GetString(bytes);
			return long.Parse(k);
		};

		public void Load() {
			Console.WriteLine("Loading image collection...");
			collection = persistence.Read<long, Image>(ReadKey, ReadValue);
		}


		public new long Add(Image i) {
			long id = base.Add(i);
			persistence.Put(i);
			return id;
		}

		public void Add(System.Collections.Generic.List<Image> list) {
			foreach (Image i in list) {
				Add(i);
			}
		}
	}
}