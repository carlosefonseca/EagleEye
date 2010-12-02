using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BerkeleyDB;
using System.IO;

namespace EagleEye.Common {
	public class Persistence {
		string filename;
		BTreeDatabase btreeDB;
		BTreeDatabaseConfig btreeConfig;


		public Persistence(string filename) {
			this.filename = filename;

			// Configure the database.
			btreeConfig = new BTreeDatabaseConfig();
			btreeConfig.Duplicates = DuplicatesPolicy.SORTED;
			btreeConfig.ErrorPrefix = filename;
			btreeConfig.Creation = CreatePolicy.IF_NEEDED;
			btreeConfig.CacheSize = new CacheInfo(0, 64 * 1024, 1);
			btreeConfig.PageSize = 8 * 1024;
		}

		public void OpenOrCreate() {
			btreeDB = BTreeDatabase.Open(filename, btreeConfig);
			return;
		}


		public void Put(byte[] k, byte[] d) {
			btreeDB.Put(new DatabaseEntry(k), new DatabaseEntry(d));
		}

		public void Put<T>(EEPersistable<T> obj) {
		}
		//Put(obj.




		/*

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
					// Acquire a cursor for the database.
					BTreeCursor dbc;
					dbc = btreeDB.Cursor();
					// Walk through the database and print out key/data pairs.
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
					// Acquire a cursor for the database.
					Cursor dbc;
					using (dbc = btreeDB.Cursor()) {
						// Walk through the database and print out key/data pairs.
						int count = 0;
						foreach (KeyValuePair<DatabaseEntry, DatabaseEntry> p in dbc)
							count++;
						Console.WriteLine(count + " items in DB");
					}
				}*/

		public Dictionary<TK, TV> Read<TK,TV>() {
			Dictionary<string,string> output = new Dictionary<string,string>();

			if (btreeDB == null)
				throw new Exception("DB Not Initialized");

			// Acquire a cursor for the database.
			BTreeCursor dbc;
			dbc = btreeDB.Cursor();

			// Walk through the database and print out key/data pairs.
			while (dbc.MoveNext()) {
				if (dbc.Current.Value.Data == null) {
					Console.WriteLine("#ERRO a ler entrada da BDB");
				} else {
					string k, v;
					Decoder d = System.Text.Encoding.ASCII.GetDecoder();
					char chars = new char[dbc.Current.Value.Data.Length];
					int nBytes,nChars;
					bool completed;
					d.Convert(dbc.Current.Value.Data,dbc.Current.Value.Data.Length,chars,dbc.Current.Value.Data.Length,true,nBytes,nChars,completed);
					k = chars.ToString();
				}
			}

			Console.WriteLine("BDB loaded");
			return collection;
		}
	}

	public interface EEPersistable<T> {
		byte[] Key();
		byte[] GetBytes();

		T Set(byte[] bytes);
	}
}
