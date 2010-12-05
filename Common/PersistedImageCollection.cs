using System;

namespace EagleEye.Common {
	public class PersistedImageCollection : ImageCollection {

		private Persistence persistence;

		public PersistedImageCollection(string filename)
			: base() {
			persistence = new Persistence(filename);
			if (persistence.existed) { // true if opened
				Load();
			}
		}


		public void Load() {
			Console.WriteLine("Loading image collection...");
			Replace(persistence.Read<long, Image>(Converters.ReadLong, Converters.ReadImage));
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