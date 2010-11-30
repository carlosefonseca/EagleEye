using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BerkeleyDB;
using System.IO;
using EagleEye.Common;

namespace EagleEye {
	class Persistence {
		#region Singleton
		private static Persistence instance;

		public static Persistence Get() {
			if (instance == null) {
				instance = new Persistence();
			}
			return instance;
		}
		#endregion Singleton

		string root = "db\\";
		string DBImages = "images.db";
		BTreeDatabase btreeDB;
		BTreeDatabaseConfig btreeConfig;


		public Boolean DbExists() {
			return File.Exists(root + DBImages);
		}

		public void InitDB(string dbFileName) {
			//CanWriteTest(dbFileName)) {

			InitDB(dbFileName, false);
		}

		public void InitDB(string dbFileName, bool overwrite) {
			string dbFinalFileName = "";
			if (overwrite) {
				dbFinalFileName = dbFileName;
				dbFileName = dbFileName + ".tmp";
				File.Delete(dbFileName);
			}

			/* Configure the database. */
			btreeConfig = new BTreeDatabaseConfig();
			btreeConfig.Duplicates = DuplicatesPolicy.SORTED;
			btreeConfig.ErrorPrefix = dbFileName;
			btreeConfig.Creation = CreatePolicy.IF_NEEDED;
			btreeConfig.CacheSize = new CacheInfo(0, 64 * 1024, 1);
			btreeConfig.PageSize = 8 * 1024;

			/* Create and open a new database in the file. */
			try {
				btreeDB = BTreeDatabase.Open(dbFileName, btreeConfig);
				if (overwrite) {
					File.Delete(dbFinalFileName);
					File.Move(dbFileName, dbFinalFileName);
				}
			} catch (Exception e) {
				Console.WriteLine("Error opening {0}.", dbFileName);
				Console.WriteLine(e.Message);
				throw e;
			}
		}

		private bool CanWriteTest(string dbFileName) {
			if (File.Exists(dbFileName)) {
				string buff;
				while (true) {
					Console.Write("{0} already exists.  Overwrite? (y/n) ", dbFileName);
					buff = Console.ReadLine().ToLower();
					if (buff == "y" || buff == "n")
						break;
				}

				if (buff == "y") {
					try {
						File.Delete(dbFileName);
						return true;
					} catch (Exception e) {
						Console.WriteLine("Unable to delete {0}.\n  {1}", dbFileName, e);
						return false;
					}
				}
				return false;
			}
			return true;
		}

		public void WriteCollection(ImageCollection images) {
			this.InitDB(root + DBImages, true);

			DatabaseEntry key, data;

			foreach (KeyValuePair<long, Image> kv in images.TheDictionary()) {
				key = new DatabaseEntry();
				data = new DatabaseEntry();

				try {
					key.Data = System.Text.Encoding.ASCII.GetBytes(kv.Key.ToString());
					data.Data = kv.Value.getBytes();

					btreeDB.Put(key, data);
				} catch {
					Console.WriteLine("DB ERROR while saving image: " + kv.Value.ToString());
				}
			}
			btreeDB.Close();
			btreeDB = null;
		}

		public ImageCollection ReadCollection() {
			Console.WriteLine("Loading BDB");
			if (btreeDB == null)
				this.InitDB(root + DBImages);
			ImageCollection collection = new ImageCollection();
			/* Acquire a cursor for the database. */
			BTreeCursor dbc;
			dbc = btreeDB.Cursor();
			/* Walk through the database and print out key/data pairs. */
			Image i;
			while (dbc.MoveNext()) {
				if (dbc.Current.Value.Data == null) {
					Console.WriteLine("#ERRO a ler entrada da BDB");
				} else {
					i = new Image(dbc.Current.Value.Data);
					collection.Add(i);
					Console.WriteLine(i);
				}
			}

			Console.WriteLine("BDB loaded");
			return collection;
		}


		public void read() {
			if (btreeDB == null)
				this.InitDB(root + DBImages);
			/* Acquire a cursor for the database. */
			Cursor dbc;
			using (dbc = btreeDB.Cursor()) {
				/* Walk through the database and print out key/data pairs. */
				int count = 0;
				foreach (KeyValuePair<DatabaseEntry, DatabaseEntry> p in dbc)
					count++;
				Console.WriteLine(count + " items in DB");
			}
		}
	}
}
