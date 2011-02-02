using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EagleEye.Common;
using EagleEye.Plugins.FeatureExtraction;
using System.Drawing;
using AForge.Imaging;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace EEPlugin {
	public class ColorDetection : EEPluginInterface {
		private Persistence persistence;
		private Persistence persistence2;
		private Persistence persistence3;
		private Dictionary<long, Color> PluginData;
		private SortedDictionary<double, SortedDictionary<double, List<long>>> ColorMap;
		private HashSet<long> MappedImages;

		#region EEPluginInterface Members

		public String Id() {
			return "color";
		}
		public override String ToString() {
			return "Color Detection";
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
				PluginData = new Dictionary<long, Color>();
			}
			///////////////////////////////////
			if (persistence2 == null) {
				persistence2 = new Persistence(Id() + "2.eep");
			}
			if (persistence2.existed) {
				Load2();
			} else {
				ColorMap = new SortedDictionary<double, SortedDictionary<double, List<long>>>();
			}
			////////////////////////////////////
			if (persistence3 == null) {
				persistence3 = new Persistence(Id() + "3.eep");
			}
			if (persistence3.existed) {
				Load3();
			} else {
				MappedImages = new HashSet<long>();
			}

		}


		private void LoadAsm(String asmName) {
			using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(asmName)) {
				Byte[] assemblyData = new Byte[stream.Length];
				stream.Read(assemblyData, 0, assemblyData.Length);
				Assembly Asm = Assembly.Load(assemblyData);
			}
		}




		/// <summary>
		/// Process images (called from manager)
		/// </summary>
		/// <param name="ic">The colection of images</param>
		/// <returns></returns>
		public ImageCollection processImageCollection(ImageCollection ic) {
			// Obtem a cor mediana da imagem * Histograma RGB
			foreach (EagleEye.Common.Image i in ic.ToList()) {
				if (PluginData.ContainsKey(i.id)) {
					Console.WriteLine("Skipping " + i.path);
					continue;
				}
				Console.Write("Color Detecting " + i.path + "... ");
				Color result = RunDetection(i.path);
				PutData(i, result);
				Console.WriteLine(result.ToString());
			}
			Save();
			
			Console.WriteLine("\n-- Color detection completed. Now sorting.");


			// calcula a Saturaçao e a luminancia e ordena na árvore de cores * Histograma HSL
			if (ColorMap == null) ColorMap = new SortedDictionary<double, SortedDictionary<double, List<long>>>();

			foreach (EagleEye.Common.Image i in ic.ToList()) {
				if (MappedImages.Contains(i.id)) {
					Console.WriteLine("Skipping " + i.path);
					continue;
				}
				Bitmap img = new Bitmap(System.IO.Path.GetFullPath(i.path));
				AForge.Imaging.ImageStatisticsHSL histogram = new AForge.Imaging.ImageStatisticsHSL(img);
				double saturation = histogram.Saturation.Median;
				double luminance = histogram.Luminance.Median;
				if (!ColorMap.ContainsKey(saturation)) {
					ColorMap[saturation] = new SortedDictionary<double, List<long>>();
				}
				if (!ColorMap[saturation].ContainsKey(luminance)) {
					ColorMap[saturation][luminance] = new List<long>();
				}
				ColorMap[saturation][luminance].Add(i.id);
				MappedImages.Add(i.id);
			}

			SaveColorMap();

			

			// Percorre a árvore e imprime as imagens 
			foreach (KeyValuePair<double, SortedDictionary<double, List<long>>> row in ColorMap) {
				Console.WriteLine(row.Key.ToString());
				foreach (KeyValuePair<double, List<long>> col in row.Value) {
					Console.Write("   {0}: ", col.Key);
					foreach (long item in col.Value) {
						Console.Write("{0} ", item);
					}
					Console.WriteLine();
				}
			}



			// Cria um png com as imagens lá colocadas nas suas posições de acordo com a árvore
			// Altura > luminancia ; Largura > saturação
			System.Drawing.Bitmap pg = new System.Drawing.Bitmap(1050,1050);
			Graphics gr = Graphics.FromImage(pg);

			// clear the canvas to color
			Rectangle pgRect = new Rectangle(0, 0, pg.Width, pg.Height);
			SolidBrush solidWhite = new SolidBrush(Color.Black);
			gr.FillRectangle(solidWhite, pgRect);

			int x,y;
			Rectangle rect = new Rectangle(0,0,50,50);
			foreach (KeyValuePair<double, SortedDictionary<double, List<long>>> col in ColorMap) {
				x = Convert.ToInt16(col.Key*1000);
				foreach (KeyValuePair<double, List<long>> row in col.Value) {
					y = Convert.ToInt16(row.Key * 1000);
					foreach (long item in row.Value) {
						EagleEye.Common.Image i = ic.Get(item);
						rect.X = x+25;
						rect.Y = y+25;
						gr.DrawImage(new System.Drawing.Bitmap(i.Path()), rect);
					}
				}
			}

			pg.Save("colormap.png");




			return null;
		}

		private Color RunDetection(string p) {
			Bitmap img = new Bitmap(p);
			AForge.Imaging.ImageStatistics histogram = new AForge.Imaging.ImageStatistics(img);
			Color c = Color.FromArgb(histogram.Red.Median, histogram.Green.Median, histogram.Blue.Median);

			if (false) {
				//output debug
				System.Drawing.Bitmap pg = new System.Drawing.Bitmap(100, 100);
				Graphics gr = Graphics.FromImage(pg);

				// clear the canvas to color
				Rectangle pgRect = new Rectangle(0, 0, pg.Width, pg.Height);
				SolidBrush solidWhite = new SolidBrush(c);
				gr.FillRectangle(solidWhite, pgRect);

				pg.Save(System.IO.Path.ChangeExtension(p, "_.jpg"));
			}

			return c;
		}


		private void PutData(EagleEye.Common.Image i, Color result) {
			PluginData.Add(i.id, result);
		}




		public String ImageInfo(EagleEye.Common.Image i) {
			return PluginData.ContainsKey(i.id) ? PluginData[i.id].ToString() : "Not Processed";
		}

		public String ImageToString(EagleEye.Common.Image i) {
			return i.ToString();
		}

		public void Load() {
			PluginData = persistence.Read<long, Color>(Converters.ReadLong, Converters.ReadColor);
		}

		private void Load2() {
			Dictionary<double, SortedDictionary<double, List<long>>> tmp = persistence2.Read<double, SortedDictionary<double, List<long>>>(Converters.ReadDouble, Converters.ReadSortedDicDoubleListlong);
			ColorMap = new SortedDictionary<double, SortedDictionary<double, List<long>>>(tmp);
		}

		public void Load3() {
			Dictionary<long, Int32> tmp = persistence3.Read<long, Int32>(Converters.ReadLong, Converters.ReadInt32);
			MappedImages = new HashSet<long>(tmp.Keys);
		}

		public void Save() {
			if (persistence == null)
				persistence = new Persistence(this.Id() + ".eep.db");

			foreach (KeyValuePair<long, Color> kv in PluginData) {
				string id = kv.Key.ToString();
				persistence.Put(id, kv.Value.ToArgb().ToString());
			}
		}

		private byte[] SortedDicDoubleListlongToBytes(SortedDictionary<double, List<long>> coisa) {
			BinaryFormatter formatter = new BinaryFormatter();
			MemoryStream memStream = new MemoryStream();
			formatter.Serialize(memStream, coisa);
			byte[] bytes = memStream.GetBuffer();
			memStream.Close();
			return bytes;
		}

		public void SaveColorMap() {
			foreach (KeyValuePair<double, SortedDictionary<double, List<long>>> kv in ColorMap) {
				string id = kv.Key.ToString();
				persistence2.Put(id, SortedDicDoubleListlongToBytes(kv.Value));
			}
			foreach (long i in MappedImages) {
				persistence3.Put(i.ToString(), "0");
			}
		}

		#endregion EEPluginInterface Members
	}
}
