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
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;


namespace EEPlugin {
	public class DZCGenerator : EEPluginInterface {
		//private Persistence persistence;
		private Dictionary<long, string> PluginData; //id > xml path
		private string DZDir;

		#region EEPluginInterface Boring

		/// <summary>
		/// Plugin ID
		/// </summary>
		/// <returns></returns>
		public String Id() {
			return "dzcg";
		}

		/// <summary>
		/// Full Plugin Name
		/// </summary>
		/// <returns></returns>
		public override String ToString() {
			return "DeepZoomCollection Generator";
		}

		/// <summary>
		/// Textual Plugin data for this image
		/// </summary>
		/// <param name="i">Image</param>
		/// <returns></returns>
		public String ImageInfo(EagleEye.Common.Image i) {
			return PluginData.ContainsKey(i.id) ? "OK" : "Not Processed";
		}

		public String ImageToString(EagleEye.Common.Image i) {
			return i.ToString();
		}

		public void Load() {
			//PluginData = persistence.Read<long, string>(Converters.ReadLong, Converters.ReadString);
		}

		public void Save() {
			//if (persistence == null)
			//    persistence = new Persistence(this.Id() + ".eep.db");

			//foreach (KeyValuePair<long, string> kv in PluginData) {
			//    persistence.Put(kv.Key.ToString(), kv.Value);
			//}
		}
		#endregion EEPluginInterface Boring

		/// <summary>
		/// Plugin initialization (called from manager)
		/// </summary>
		public void Init() {
			//if (persistence == null) {
			//    persistence = new Persistence(Id() + ".eep");
			//}
			//if (persistence.existed) {
			//    Load();
			//} else {
			PluginData = new Dictionary<long, string>();
			//}
			DZDir = Persistence.RootFolder();// +"DZC\\";
		}

		/// <summary>
		/// Process images (called from manager)
		/// </summary>
		/// <param name="ic">The colection of images</param>
		/// <returns></returns>
		public ImageCollection processImageCollection(ImageCollection ic) {
			long times = 0;
			int count = 0;
			PluginData.Clear();
			Boolean generationOK;
			Boolean collectionGenerationRequired = true;
			foreach (EagleEye.Common.Image i in ic.ToSortable().SortById().TheList()) {
				string relativeTarget = "images\\" + i.id.ToString() + ".xml";
				string target = DZDir + relativeTarget;

				if (File.Exists(target)) {
					Console.WriteLine(" "+i.id.ToString() + " " + i.path + "... ");
					PluginData.Add(i.id, relativeTarget);
				} else {
					// Creation needed
					Console.Write(">" + i.id.ToString() + " " + i.path + "... ");
					Stopwatch st = Stopwatch.StartNew();
					// GERACAO DE CADA IMAGEM
					generationOK = GenerateMSI(i, target);
					st.Stop();
					times += st.ElapsedMilliseconds;
					count++;
					if (generationOK) {
						PluginData.Add(i.id, relativeTarget);
						collectionGenerationRequired = true;
					}
					//Save();
				}
			}
			if (count > 0) {
				Console.WriteLine(count + " images. Mean processing time of " + times / count + " ms.");
			}

			if (collectionGenerationRequired || !File.Exists(DZDir + "collection.xml")) {
				Console.WriteLine("Generating Collection...");
				GenerateCollection();
			} else {
				Console.WriteLine("Generation not required.");
			}

			// DZ XML file
			IncludeMetadata(ic, DZDir + "collection.xml");

			return null;
		}

		/// <summary>
		/// Includes Image Metadata inside the DZC XML file
		/// </summary>
		/// <param name="ic">Collection of Images to add</param>
		/// <param name="p">Path to XML file to use</param>
		private void IncludeMetadata(ImageCollection ic, string p) {
			XElement xml = XElement.Load(p);
			Console.WriteLine("Modifying the XML");
			Dictionary<string, string> tags;
			EagleEye.Common.Image i;
			int count = 0;
			foreach (XElement a in xml.Elements().First().Elements()) {
				tags = new Dictionary<string, string>();
				String id = a.Attribute("Source").Value.Split(new Char[] { '/', '.' })[1];
				try {
					i = ic.Get(Convert.ToInt32(id));
				} catch {
					a.Remove();
					continue;
				}
				tags.Add("id", id);
				tags.Add("date", i.Date().ToString());

				Dictionary<string, string> plugins = i.GetPluginData();
				if (plugins != null) {
					count++;
					foreach (KeyValuePair<string, string> kv in plugins) {
						tags.Add(kv.Key, kv.Value);
					}
				}

				if (a.Elements("Tag").Count() != 0) {
					a.Elements("Tag").Remove();
				}
				String tagtext = JsonConvert.SerializeObject(tags);
				a.Add(new XElement("Tag", tagtext));
				Console.WriteLine(tagtext);
			}

			Console.WriteLine(xml.Elements().Last().Elements().Last().ToString());
			xml.Save(p, SaveOptions.None);
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
			DateMetadata(ic, destination);
			ColorMetadata(ic, destination);
		}

		private static void ColorMetadata(ImageCollection ic, String destination) {

		}

		private static void DateMetadata(ImageCollection ic, String destination) {
			Console.Write("Sorting by Date. ");
			SortedImageCollection collectionByDate = new SortedImageCollection(ic);
			collectionByDate.SortByDate();
			Console.Write("Writting string. ");
			String txt = "";
			DateTime lastDateWritten = new DateTime(0);
			foreach (EagleEye.Common.Image i in collectionByDate.TheList()) {
				if (i.Date().Date.CompareTo(lastDateWritten.Date) != 0) {
					lastDateWritten = i.Date();
					txt += Environment.NewLine + lastDateWritten.ToShortDateString() + ":";
				}
				txt += i.id + ";";
			}
			Console.Write("Writing file. ");
			File.WriteAllText(Path.Combine(destination, "datetime.sorted.db"), txt.TrimStart(Environment.NewLine.ToCharArray()));
			Console.WriteLine("Done!");
		}

		public String generateMetadata() {
			return "not generated";
		}


		#region Helpers

		/// <summary>
		/// Generates the MultiScaleImage for the specified image
		/// </summary>
		/// <param name="source">Image to process</param>
		/// <param name="target">Destination for the processed stuff</param>
		private Boolean GenerateMSI(string source, string target) {
			ImageCreator ic = new ImageCreator();
			ic.TileSize = 256;
			ic.TileFormat = ImageFormat.Jpg;
			ic.ImageQuality = 0.7;
			ic.TileOverlap = 0;
			Console.WriteLine(Path.GetFileName(source) + "->" + Path.GetFileName(target));
			try {
				ic.Create(source, target);
				return true;
			} catch (ArgumentOutOfRangeException e) {
				Console.WriteLine("Error on DZGen... is the image smaller than 150px?");
				return false;
			}
		}

		private bool GenerateMSI(EagleEye.Common.Image i, string target) {
			try {
				if (i.ContainsExif("Orientation")) {
					Int64 o = (Int64)i.Exif("Orientation");
					Console.Write(" R{0} ", o);
					if (o == 3 || o == 6 || o == 8) {
						String path = null;
						switch (o) {
							case 3: path = GenerateRotatedImage(i.path, RotateFlipType.Rotate180FlipNone); break;
							case 6: path = GenerateRotatedImage(i.path, RotateFlipType.Rotate90FlipNone); break;
							case 8: path = GenerateRotatedImage(i.path, RotateFlipType.Rotate270FlipNone); break;
							default: Console.Write("!"); break;
						}

						if (path != null) {
							Boolean ret = GenerateMSI(path, target);

							return ret;
						}
					}
				}
			} catch (Exception e) {
				Console.WriteLine(e);
			}
			//File.Delete("tmp.jpg");
			return GenerateMSI(i.path, target);
		}

		private string GenerateRotatedImage(string path, RotateFlipType r) {
			Stream sr = new FileStream(path, FileMode.Open);
			Bitmap b = new Bitmap(sr);
			sr.Close();
			b.RotateFlip(r);
			Stream w = null;
			String fn = null;
			while (w == null) {
				try {
					fn = "tmp.jpg";
					w = new FileStream(fn, FileMode.Create);
				} catch {
					try {
						fn = "tmp2.jpg";
						w = new FileStream(fn, FileMode.Create);
					} catch {
						Thread.Sleep(100);
					}
				}
			}
			b.Save(w, System.Drawing.Imaging.ImageFormat.Jpeg);
			b.Dispose();
			w.Close();
			return fn;
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
			List<string> modifiedPD = new List<string>();
			foreach (string s in PluginData.Values) {
				modifiedPD.Add(DZDir + s);
			}
			cc.Create(modifiedPD, DZDir + "collection");
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
