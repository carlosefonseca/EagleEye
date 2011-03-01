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
				throw new Exception("DB: Class var 'dir' must be set first");
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
			//Console.WriteLine("DB: Setting timer");
			timer = new Timer(timeout);
			timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
			timer.AutoReset = false;
			timer.Start();
		}

		public void timer_Elapsed(object sender, ElapsedEventArgs e) {
			timer = null;
			Console.WriteLine("DB: Flushing... ");
			btreeDB.Sync();
			//Save();
			Console.WriteLine("Flushed.");
		}


		void Put(byte[] k, byte[] d) {
			btreeDB.Put(new DatabaseEntry(k), new DatabaseEntry(d));
			Snooze();
		}

		private void Snooze() {
			if (timer == null) {
				SetTimer();
			} else {
				//timer.Interval = timeout;
			}
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
					Console.WriteLine("#ERRO a ler entrada " + DK(dbc.Current.Key.Data).ToString() + " da BDB. Deleting");
					dbc.Delete();
				} else {
					try {
						TK key = DK(dbc.Current.Key.Data);
						TV val = DV(dbc.Current.Value.Data);
						output.Add(key, val);
					} catch {
						Console.WriteLine("Error loading data...");
					}
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


		/// <summary>
		/// Obtains the value of a key in a DB
		/// </summary>
		/// <typeparam name="TV">Type of the return object</typeparam>
		/// <param name="id">id of the object</param>
		/// <param name="DV">Delegate to convert the value to TV</param>
		/// <returns>Value as TV</returns>
		public TV Get<TV>(long id, ConvertFromBytes<TV> DV) {
			if (btreeDB == null)
				throw new Exception("DB Not Initialized");

			System.Text.Encoding enc = System.Text.Encoding.ASCII;
			byte[] key = enc.GetBytes(id.ToString());
			DatabaseEntry DbKey = new DatabaseEntry(key);

			KeyValuePair<DatabaseEntry, DatabaseEntry> kv;
			kv = btreeDB.Get(DbKey);
			return DV(kv.Value.Data);
		}

		public bool ExistsKey(string p) {
			if (btreeDB == null)
				throw new Exception("DB Not Initialized");

			System.Text.Encoding enc = System.Text.Encoding.ASCII;
			byte[] key = enc.GetBytes(p);
			DatabaseEntry DbKey = new DatabaseEntry(key);

			return btreeDB.Exists(DbKey);
		}
	}

	public interface EEPersistable<T> {
		byte[] GetBytes();

		T Set(byte[] bytes);
	}



	public delegate T ConvertFromBytes<T>(byte[] bytes);

	public class Converters {
		public static ConvertFromBytes<Int32> ReadInt32 = delegate(byte[] bytes) {
			System.Text.Encoding enc = System.Text.Encoding.ASCII;
			string k = enc.GetString(bytes);
			return Int32.Parse(k);
		};

		public static ConvertFromBytes<long> ReadLong = delegate(byte[] bytes) {
			System.Text.Encoding enc = System.Text.Encoding.ASCII;
			string k = enc.GetString(bytes);
			return long.Parse(k);
		};

		public static ConvertFromBytes<double> ReadDouble = delegate(byte[] bytes) {
			System.Text.Encoding enc = System.Text.Encoding.ASCII;
			string k = enc.GetString(bytes);
			return double.Parse(k);
		};

		public static ConvertFromBytes<Image> ReadImage = delegate(byte[] bytes) {
			return new Image(bytes);
		};

		public static ConvertFromBytes<System.Drawing.Bitmap> ReadBitmap = delegate(byte[] bytes) {
			MemoryStream memStream = new MemoryStream(bytes);
			return new System.Drawing.Bitmap(memStream);
		};

		public static ConvertFromBytes<Rectangle[]> ReadRectangleArray = delegate(byte[] bytes) {
			BinaryFormatter formatter = new BinaryFormatter();
			MemoryStream memStream = new MemoryStream(bytes);
			Rectangle[] tmp = (Rectangle[])formatter.Deserialize(memStream);
			memStream.Close();
			return tmp;
		};

		public static ConvertFromBytes<Color> ReadColor = delegate(byte[] bytes) {
			System.Text.Encoding enc = System.Text.Encoding.ASCII;
			string k = enc.GetString(bytes);
			return Color.FromArgb(Int32.Parse(k));
		};

		public static ConvertFromBytes<SortedDictionary<double, List<long>>> ReadSortedDicDoubleListlong = delegate(byte[] bytes) {
			BinaryFormatter formatter = new BinaryFormatter();
			MemoryStream memStream = new MemoryStream(bytes);
			SortedDictionary<double, List<long>> tmp = (SortedDictionary<double, List<long>>)formatter.Deserialize(memStream);
			memStream.Close();
			return tmp;
		};

	}
}
