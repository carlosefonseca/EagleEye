using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using EagleEye.Common;

namespace EagleEye {
	public class LibraryManager {
		#region Singleton
		private static LibraryManager _lib;

		private LibraryManager() { }

		public LibraryManager Get() {
			if (_lib == null) {
				_lib = new LibraryManager();
			}
			return _lib;
		}
		#endregion Singleton

		#region Class Methods

		private const string libraryName = "EagleEye";
		private const string collectionName = "Photos";


		/// <summary>
		/// Creates or Loads a Library.
		/// </summary>
		/// <param name="LibraryDir">Directory of the Library</param>
		/// <returns>The library</returns>
		public static LibraryManager Init(string LibraryDir) {
			if (Directory.Exists(LibraryDir) && File.Exists(LibraryDir + "\\" + libraryName)) {
				//LOAD
				_lib = new LibraryManager(LibraryDir);
			} else {
				//CREATE
				_lib = new LibraryManager(LibraryDir, true);
			}
			return _lib;
		}
		#endregion Class Methods



		#region Instance Methods

		private string path;
		private Dictionary<string, string> settings;
		public PersistedImageCollection collection;
		private string LibraryDir = "EagleEyeDB";


		private LibraryManager(string dir) { LibraryManager(dir, false); }

		private LibraryManager(string dir, bool create) {
			dir = Path.GetDirectoryName(dir);
			if (!Directory.Exists(dir)) {
				Directory.CreateDirectory(dir);
			}

			if (!dir.EndsWith("\\"))
				dir += "\\";
			path = dir;

			if (create) {
				File.Delete(dir + "EagleEye.db");
				File.Delete(dir + "Images.db");

				settings = new Dictionary<string, string>();
				collection = new PersistedImageCollection(dir + "Images.db");

				Setting("CreateDate", DateTime.Now.ToString());
			} else {
				Persistence p = new Persistence(libraryName);
				p.OpenOrCreate();
				settings = p.Read<string,string>();

		}


		#region Settings Getter/Setter
		public string Setting(string k) {
			return settings[k];
		}

		public void Setting(string k, string v) {
			settings[k] = v;
		}
		#endregion Settings Getter/Setter

		#endregion Instance Methods

	}
}
