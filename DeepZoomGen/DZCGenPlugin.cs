using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.DeepZoomTools;
using System.IO;
using System.Diagnostics;
using EagleEye.Common;
using EagleEye.Plugins.FeatureExtraction;

namespace EEPlugin {
	public class DZCGenerator : EEPluginInterface {
		private Persistence persistence;
		private Dictionary<long, string> PluginData; //id > xml path
		private string DZDir;

		#region EEPluginInterface Members

		public String Id() {
			return "dzcg";
		}
		public override String ToString() {
			return "DeepZoomCollection Generator";
		}

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
		#endregion EEPluginInterface Members

		/// <summary>
		/// Process images (called from manager)
		/// </summary>
		/// <param name="ic">The colection of images</param>
		/// <returns></returns>
		public ImageCollection processImageCollection(ImageCollection ic) {
			long times = 0;
			int count = 0;
			foreach (EagleEye.Common.Image i in ic.ToList()) {
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

			return null;
		}
		



		private void GenerateDZC(string source, string target) {
			ImageCreator ic = new ImageCreator();
			ic.TileSize = 256;
			ic.TileFormat = ImageFormat.Jpg;
			ic.ImageQuality = 0.7;
			ic.TileOverlap = 0;
			ic.Create(source, target);
		}


		public void GenerateCollection() {
			CollectionCreator cc = new CollectionCreator();

			cc.TileSize = 256;
			cc.TileFormat = ImageFormat.Jpg;
			cc.MaxLevel = 5;
			cc.ImageQuality = 0.8;

			cc.Create(PluginData.Values, DZDir + "collection");
		}

		private static List<string> GetImagesInDirectory(string path) {
			return GetImagesInDirectory(new DirectoryInfo(path));
		}

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
	}
}
