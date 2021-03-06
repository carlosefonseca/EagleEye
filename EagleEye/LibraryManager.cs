﻿using System;
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

		public static LibraryManager Get() {
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
			_lib = new LibraryManager(LibraryDir);
			return _lib;
		}
		#endregion Class Methods



		#region Instance Methods
		/// <summary>
		/// The Folder where everything about this library will be saved
		/// </summary>
		public readonly string path;
		private Dictionary<string, string> settings;
		public PersistedImageCollection collection;
		public PluginManager PlugMan;
		private string LibraryDir = "EagleEyeDB";
		private Thumbnails Thumbs;



		private LibraryManager(string dir) : this(dir, false) { }

		private LibraryManager(string dir, bool create) {
			path = Persistence.SetRootFolder(dir);

			Persistence setts = new Persistence("EagleEye");
			if (!setts.existed) {
				setts.Put("CreateDate", DateTime.Now.ToString());
			} else {
				setts.ReadStrings();
			}
			collection = new PersistedImageCollection("Images");
			Console.WriteLine("Collection Size: " + collection.Count());
			PlugMan = PluginManager.Get();
			Thumbs = Thumbnails.Get();
			//GenerateThumbnails();
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


		internal void Save() {
			throw new NotImplementedException();
		}

		public void GenerateThumbnails() {
			Console.WriteLine("Generating thumbnails...");
			double size = collection.Count();
			double items = 0;
			foreach (Image i in collection.ToList()) {
				if (!Thumbs.ThumbnailExists(i) && File.Exists(i.path)) {
					Thumbs.GenerateAndSaveThumbnail(i);
				}
				Console.Write("\r{0:0.00%}   ", ++items / size);
			}
			Console.WriteLine("Generation completed.");
		}
	}
}
