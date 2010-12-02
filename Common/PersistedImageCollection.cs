using System;

namespace EagleEye.Common {

	public class PersistedImageCollection : ImageCollection {

		private Persistence persistence;

		public PersistedImageCollection(string filename)
			: base() {
			persistence = new Persistence(filename);
		}


		public new long Add(Image i) {
			long id = base.Add(i);
			persistence.Put(i);
			return id;
		}



	}
}