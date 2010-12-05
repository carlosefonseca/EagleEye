using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using BerkeleyDB;
using System.Diagnostics;
using System.Timers;
using System.Drawing;
using System.Runtime.Serialization.Formatters.Binary;

namespace EagleEye.Common {
	public class Persistence {
		private static string dir;
		string filename;
		BTreeDatabase btreeDB;
		BTreeDatabaseConfig btreeConfig;
		Timer timer;
		const double timeout = 1000;
		public readonly bool existed;

		public static string SetRootFolder(string f) {
			f = Path.GetFullPath(f);
			if (!Directory.Exists(f)) {
				Directory.CreateDirectory(f);
			}
			if (!f.EndsWith("\\"))
				f += "\\";
			dir = f;
			return dir;
		}

		/// <summary>
		/// Transforms a filename into a full path and adds the .db extension, if needed
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		public string FullFilename(string filename) {
			if (dir == null) {
				throw new Exception("Class var 'dir' must be set first");
			}
			if (Path.GetPathRoot(filename) == "") {
				filename = dir + filename;
			}
			if (!filename.EndsWith(".db")) {
				filename += ".db";
			}
			return filename;
		}

		public Persistence(string fn) {
			filename = FullFilename(fn);

			// Configure the database.
			btreeConfig = new BTreeDatabaseConfig();
			btreeConfig.Duplicates = DuplicatesPolicy.NONE;
			btreeConfig.ErrorPrefix = filename;
			btreeConfig.Creation = CreatePolicy.IF_NEEDED;
			btreeConfig.CacheSize = new CacheInfo(0, 64 * 1024, 1);
			btreeConfig.PageSize = 8 * 1024;

			existed = System.IO.File.Exists(filename);
			btreeDB = BTreeDatabase.Open(filename, btreeConfig);
		}

		private void SetTimer() {
			Console.WriteLine("Setting timer");
			timer = new Timer(timeout);
			timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
			timer.AutoReset = false;
			timer.Start();
		}

		public void timer_Elapsed(object sender, ElapsedEventArgs e) {
			timer = null;
			btreeDB.Sync();
			//Save();
			Console.WriteLine("Flushing");
		}


		void Put(byte[] k, byte[] d) {
			btreeDB.Put(new DatabaseEntry(k), new DatabaseEntry(d));
			if (timer != null)
				timer.Interval = timeout;
			else
				SetTimer();
		}

		public void Put(string key, string obj) {
			System.Text.Encoding enc = System.Text.Encoding.ASCII;
			byte[] k = enc.GetBytes(key);
			byte[] v = enc.GetBytes(obj);
			Put(k, v);
		}


		public void Put<T>(string key, EEPersistable<T> obj) {
			System.Text.Encoding enc = System.Text.Encoding.ASCII;
			byte[] k = enc.GetBytes(key);
			byte[] v = obj.GetBytes();
			Put(k, v);
		}

		public void Put(string key, byte[] bytes) {
			System.Text.Encoding enc = System.Text.Encoding.ASCII;
			byte[] k = enc.GetBytes(key);
			Put(k, bytes);
		}

		/*


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
		/*
		public Dictionary<TK, TV> Read<TK, TV>() {
			Dictionary<TK, TV> output = new Dictionary<TK, TV>();

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
					TK key = ReadField<TK>(dbc.Current.Key.Data);
					TV val = ReadField<TV>(dbc.Current.Value.Data);
					output.Add(key, val);
				}
			}
			return output;
		}

		private T ReadField<T>(byte[] bytes) {
			T field;
			Console.WriteLine("IMPLEMENTED INTERFACES\n" + field.GetType().GetInterfaces().ToString());
			if (field.GetType().Equals(typeof(long))) {
				System.Text.Encoding enc = System.Text.Encoding.ASCII;
			} else if (field.GetType().Equals(typeof(string))) {
				System.Text.Encoding enc = System.Text.Encoding.ASCII;
				field = enc.GetString(bytes);
			} else if (field.GetType().GetInterface("EEPersistable")) {
				(EEPersistable<TV>)field.Read(bytes);
			} else
				throw new NotSupportedException("Only strings, longs and EEPersistable types are supported");
			return field;
		}*/

		public Dictionary<TK, TV> Read<TK, TV>(ConvertFromBytes<TK> DK, ConvertFromBytes<TV> DV) {
			Dictionary<TK, TV> output = new Dictionary<TK, TV>();

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
					TK key = DK(dbc.Current.Key.Data);
					TV val = DV(dbc.Current.Value.Data);
					output.Add(key, val);
				}
			}
			return output;
		}



		public Dictionary<string, string> ReadStrings() {
			Dictionary<string, string> output = new Dictionary<string, string>();

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
					System.Text.Encoding enc = System.Text.Encoding.ASCII;
					//byte[] myByteArray = enc.GetBytes("a text string);
					string k = enc.GetString(dbc.Current.Key.Data);
					string v = enc.GetString(dbc.Current.Value.Data);
					output.Add(k, v);
				}
			}

			Console.WriteLine("BDB loaded");
			return output;
		}

		public void Put(Image i) {
			System.Text.Encoding enc = System.Text.Encoding.ASCII;
			byte[] id = enc.GetBytes(i.id.ToString());
			Put(id, i.GetBytes());
		}
	}

	public interface EEPersistable<T> {
		byte[] GetBytes();

		T Set(byte[] bytes);
	}

	
	
	public delegate T ConvertFromBytes<T>(byte[] bytes);
	
	public class Converters {
		public static ConvertFromBytes<long> ReadLong = delegate(byte[] bytes) {
			System.Text.Encoding enc = System.Text.Encoding.ASCII;
			string k = enc.GetString(bytes);
			return long.Parse(k);
		};
		
		public static ConvertFromBytes<Image> ReadImage = delegate(byte[] bytes) {
			return new Image(bytes);
		};

		public static ConvertFromBytes<Rectangle[]> ReadRectangleArray = delegate(byte[] bytes) {
			BinaryFormatter formatter = new BinaryFormatter();
			MemoryStream memStream = new MemoryStream(bytes);
			Rectangle[] tmp = (Rectangle[])formatter.Deserialize(memStream);
			memStream.Close();
			return tmp;
		};
	}
}
