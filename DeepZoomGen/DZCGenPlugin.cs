using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.DeepZoomTools;
using System.IO;
using System.Diagnostics;
using EagleEye.Common;
using EagleEye.Plugins.FeatureExtraction;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;

namespace EEPlugin {
	public class DZCGenerator : EEPluginInterface {
		private Persistence persistence;
		private Dictionary<long, string> PluginData; //id > xml path
		private string DZDir;

		#region EEPluginInterface Boring

		public String Id() {
			return "dzcg";
		}
		public override String ToString() {
			return "DeepZoomCollection Generator";
		}

		public String ImageInfo(EagleEye.Common.Image i) {
			return PluginData.ContainsKey(i.id) ? "OK" : "Not Processed";
		}

		public String ImageToString(EagleEye.Common.Image i) {
			return i.ToString();
		}

		public void Load() {
			PluginData = persistence.Read<long, string>(Converters.ReadLong, Converters.ReadString);
		}

		public void Save() {
			if (persistence == null)
				persistence = new Persistence(this.Id() + ".eep.db");

			foreach (KeyValuePair<long, string> kv in PluginData) {
				persistence.Put(kv.Key.ToString(), kv.Value);
			}
		}
		#endregion EEPluginInterface Boring

		/// <summary>
		/// Plugin initialization (called from manager)
		/// </summary>
		public void Init() {
			if (persistence == null) {
				persistence = new Persistence(Id() + ".eep");
			}
			if (persistence.existed) {
				Load();
			} else {
				PluginData = new Dictionary<long, string>();
			}
			DZDir = Persistence.RootFolder() + "DZC\\";
		}

		/// <summary>
		/// Process images (called from manager)
		/// </summary>
		/// <param name="ic">The colection of images</param>
		/// <returns></returns>
		public ImageCollection processImageCollection(ImageCollection ic) {
			long times = 0;
			int count = 0;
			foreach (EagleEye.Common.Image i in ic.ToSortable().SortById().TheList()) {
				if (PluginData.ContainsKey(i.id)) {
					Console.WriteLine("  " + i.path);
					continue;
				}
				if (File.Exists(i.path)) {
							Console.WriteLine("> " + i.path + "... ");
					string target = DZDir + "images\\" + i.id.ToString() + ".xml";
							Stopwatch st = Stopwatch.StartNew();
					GenerateDZC(i.path, target);
							st.Stop();
							times += st.ElapsedMilliseconds;
							count++;
					PluginData.Add(i.id, target);
							Console.WriteLine(i.id.ToString());
				}
				Save();
			}
			if (count > 0) {
				Console.WriteLine(count + " images. Mean processing time of " + times / count + " ms.");
			}

			Console.WriteLine("Generating Collection...");
			GenerateCollection();
			GenerateAndSaveMetadata(ic, DZDir+"metadata");

			return null;
		}


		/// <summary>
		/// Generates and writes image metadata and sorting information to the destination, to be read by the Viewer
		/// </summary>
		/// <param name="ic">Image Collection to process</param>
		/// <param name="destination">Folder to write the files</param>
		private void GenerateAndSaveMetadata(ImageCollection ic, String destination) {
			if (!Directory.Exists(destination)) {
				Directory.CreateDirectory(destination);
			}
			Console.Write("Sorting by Date. ");
			SortedImageCollection collectionByDate = new SortedImageCollection(ic);
			collectionByDate.SortByDate();
			Console.Write("Writting string. ");
			String txt = "";
			DateTime lastDateWritten = new DateTime(0);
			foreach (EagleEye.Common.Image i in collectionByDate.TheList()) {
				if (i.Date().Date.CompareTo(lastDateWritten.Date) != 0) {
					lastDateWritten = i.Date();
					txt += "\n" + lastDateWritten.ToShortDateString() + ":";
				}
				txt += i.id + ";";
			}
			Console.Write("Writing file. ");
			File.WriteAllText(Path.Combine(destination, "datetime.sorted.db"), txt.TrimStart('\n'));
			Console.WriteLine("Done!");
		}



		#region Helpers

		/// <summary>
		/// Generates the MultiScaleImage for the specified image
		/// </summary>
		/// <param name="source">Image to process</param>
		/// <param name="target">Destination for the processed stuff</param>
		private void GenerateDZC(string source, string target) {
			ImageCreator ic = new ImageCreator();
			ic.TileSize = 256;
			ic.TileFormat = ImageFormat.Jpg;
			ic.ImageQuality = 0.7;
			ic.TileOverlap = 0;
			ic.Create(source, target);
		}

		/// <summary>
		/// Generates the DeepZoomCollection file
		/// </summary>
		public void GenerateCollection() {
			CollectionCreator cc = new CollectionCreator();

			cc.TileSize = 256;
			cc.TileFormat = ImageFormat.Jpg;
			cc.MaxLevel = 5;
			cc.ImageQuality = 0.8;

			cc.Create(PluginData.Values, DZDir + "collection");
		}

		/// <summary>
		/// Returns all .jpg files in the specified directory and all its subdirectories
		/// </summary>
		/// <param name="path">Directory</param>
		/// <returns>List of filenames</returns>
		private static List<string> GetImagesInDirectory(string path) {
			return GetImagesInDirectory(new DirectoryInfo(path));
		}

		/// <summary>
		/// Returns all .jpg files in the specified directory and all its subdirectories
		/// </summary>
		/// <param name="di">Directory</param>
		/// <returns>List of filenames</returns>
		private static List<string> GetImagesInDirectory(DirectoryInfo di) {
			List<string> images = new List<string>();

			// get all the images in this directory first
			foreach (var fi in di.GetFiles("*.jpg")) {
				images.Add(fi.FullName);
			}

			// get all the directories with their images
			foreach (var sub in di.GetDirectories()) {
				images.AddRange(GetImagesInDirectory(sub));
			}

			return images;
		}
		#endregion Helpers
	}
}
