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
using System.Diagnostics;

namespace EEPlugin {
	public class ColorDetection : EEPluginInterface {
		private Persistence persistence;
		private Persistence persistence2;
		private Persistence persistence3;
		private Dictionary<long, Color> PluginData;
		private SortedDictionary<double, SortedDictionary<double, List<long>>> ColorMap;
		private HashSet<long> MappedImages;
		private Thumbnails thumbs;

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
				persistence = new Persistence(Id() + ".MainColor.eep");
			}
			if (persistence.existed) {
				Load();
			} else {
				PluginData = new Dictionary<long, Color>();
			}
			///////////////////////////////////
			if (persistence2 == null) {
				persistence2 = new Persistence(Id() + ".ColorMap.eep");
			}
			if (persistence2.existed) {
				Load2();
			} else {
				ColorMap = new SortedDictionary<double, SortedDictionary<double, List<long>>>();
			}
			////////////////////////////////////
			if (persistence3 == null) {
				persistence3 = new Persistence(Id() + ".MappedImages.eep");
			}
			if (persistence3.existed) {
				Load3();
			} else {
				MappedImages = new HashSet<long>();
			}
			thumbs = Thumbnails.Get();
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
			Boolean overrideData = false;

			// Obtem a cor mediana da imagem * Histograma RGB
			foreach (EagleEye.Common.Image i in ic.ToList()) {
				Color? result = null;
				if (overrideData || !i.ContainsPluginData("RGB")) {
					Console.WriteLine("Color Detecting " + i.path + "... ");
/*					if (thumbs.ThumbnailExists(i)) {
						continue;
					}
					Stopwatch s1 = Stopwatch.StartNew();
*/					result = RunDetection(thumbs.GetThumbnail(i));
/*					s1.Stop();
					Stopwatch s2 = Stopwatch.StartNew();
					result = RunDetection(i.path);
					s2.Stop();
					Console.WriteLine("Thumb: " + s1.ElapsedMilliseconds + "  Orig: " + s2.ElapsedMilliseconds);
*/					i.SetPluginData("RGB", result.Value.ToArgb().ToString());
					i.SetPluginData("HSB", "H:" + (int)result.Value.GetHue()
										+ " S:" + Math.Round(result.Value.GetSaturation(), 3)
										+ " B:" + Math.Round(result.Value.GetBrightness(), 3));
				}
			}
			return null;
		}

		public static Color FromStringToColor(string txt) {
			if (txt.StartsWith("Color")) {
				String[] p = txt.Split(new char[] { '[', ']', ',', ' ', '=' }, StringSplitOptions.RemoveEmptyEntries);
				return Color.FromArgb(Convert.ToByte(p[2]), Convert.ToByte(p[4]), Convert.ToByte(p[6]), Convert.ToByte(p[8]));
			} else {
				Byte[] bytes = BitConverter.GetBytes(Convert.ToInt32(txt));
				return Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0]);
			}
		}

		//    Console.WriteLine("\n-- Color detection completed. Now sorting.");

		//    // COLOR MAP
		//    // calcula a Hue e a luminancia e ordena na árvore de cores * Histograma HSL
		//    if (ColorMap == null) ColorMap = new SortedDictionary<double, SortedDictionary<double, List<long>>>();

		//    foreach (EagleEye.Common.Image i in ic.ToList()) {
		//        if (MappedImages.Contains(i.id)) {
		//            Console.WriteLine("Skipping " + i.path);
		//            continue;
		//        }

		//        System.Drawing.Bitmap img;
		//        try {
		//            img = thumbs.GetThumbnail(i.id);
		//        } catch {
		//            Console.WriteLine("ERROR: Thumbnail required!");
		//            return null;
		//        }

		//        Color c = PluginData[i.id];
		//        double hue = c.GetHue();
		//        double luminance = c.GetBrightness();

		//        if (!ColorMap.ContainsKey(hue)) {
		//            ColorMap[hue] = new SortedDictionary<double, List<long>>();
		//        }
		//        if (!ColorMap[hue].ContainsKey(luminance)) {
		//            ColorMap[hue][luminance] = new List<long>();
		//        }
		//        ColorMap[hue][luminance].Add(i.id);
		//        MappedImages.Add(i.id);
		//        Console.Write(".");
		//    }
		//    Console.WriteLine();
		//    SaveColorMap();

		//    //DrawMap2();

		//    return null;
		//}

		public String generateMetadata() {
			String o = "";
			Dictionary<int, List<long>> resorted = new Dictionary<int, List<long>>();

			foreach (KeyValuePair<long, Color> kv in PluginData) {
				int color = Convert.ToInt16(Math.Round(kv.Value.GetHue()));
				if (!resorted.ContainsKey(color)) {
					resorted.Add(color, new List<long>());
				}
				resorted[color].Add(kv.Key);
			}
			foreach (KeyValuePair<int, List<long>> kv in resorted) {
				o += kv.Key + ":";
				kv.Value.Sort();
				foreach (long id in kv.Value) {
					o += id + ";";
				}
				o += Environment.NewLine;
			}
			return o;
		}


		/*

				private void DrawMap() {
					Size canvas = new Size(3000, 1000);
					Size thumb = new Size(25, 25);

					// Cria um png com as imagens lá colocadas nas suas posições de acordo com a árvore
					// Altura > luminancia ; Largura > saturação
					System.Drawing.Bitmap pg = new System.Drawing.Bitmap(canvas.Width, canvas.Height);
					Graphics gr = Graphics.FromImage(pg);

					// paint the canvas black
					Rectangle pgRect = new Rectangle(0, 0, pg.Width, pg.Height);
					SolidBrush solidWhite = new SolidBrush(Color.Black);
					gr.FillRectangle(solidWhite, pgRect);

					int x, y;
					int xMax = 360;
					int marginX = thumb.Width / 2, marginY = thumb.Height / 2;
					Rectangle rect = new Rectangle();
					double totalItems = MappedImages.Count;
					double itemCount = 0;
					foreach (KeyValuePair<double, SortedDictionary<double, List<long>>> col in ColorMap) {
						x = Convert.ToInt16(col.Key * canvas.Width / xMax);
						foreach (KeyValuePair<double, List<long>> row in col.Value) {
							y = Convert.ToInt16(row.Key * canvas.Height);
							foreach (long item in row.Value) {
								System.Drawing.Bitmap img = thumbs.GetThumbnail(item);
								rect.X = x - marginX;
								rect.Y = canvas.Height - y - marginY;
								rect.Size = thumb;
								rect.Height = img.Height * thumb.Width / img.Width;
								gr.DrawImage(img, rect);
								itemCount++;
								Console.Write("\r{0:0%}    ", itemCount / totalItems);
							}
						}
					}
					Console.WriteLine();
					pg.Save(Path.GetFullPath("colormap.png"));
				}

				private void DrawMap2() {
					// Figure out what do we have to split
					Dictionary<int, int> image_counts = new Dictionary<int, int>();
					foreach (KeyValuePair<double, SortedDictionary<double, List<long>>> col_kv in ColorMap) {
						int k = Convert.ToInt32(col_kv.Key);
						if (image_counts.ContainsKey(k)) {
							image_counts[k] += col_kv.Value.Count;
						} else {
							image_counts.Add(k, col_kv.Value.Count);
						}
					}

					int destination_min = 0, destination_max = 1000;
					int[] destination = new int[destination_max];
					destination[destination_min] = image_counts.Keys.First<int>();
					destination[destination_max - 1] = image_counts.Keys.Last<int>();

					int arr_min = destination[destination_min];
					int arr_max = destination[destination_max - 1];

					Distribute(image_counts, arr_min, arr_max, ref destination, destination_min, destination_max);
					#region hide

					Size canvas = new Size(3000, 1000);
					Size thumb = new Size(25, 25);

					// Cria um png com as imagens lá colocadas nas suas posições de acordo com a árvore
					// Altura > luminancia ; Largura > saturação
					System.Drawing.Bitmap pg = new System.Drawing.Bitmap(canvas.Width, canvas.Height);
					Graphics gr = Graphics.FromImage(pg);

					// paint the canvas black
					Rectangle pgRect = new Rectangle(0, 0, pg.Width, pg.Height);
					SolidBrush solidWhite = new SolidBrush(Color.Black);
					gr.FillRectangle(solidWhite, pgRect);

					int x, y;
					int xMax = 360;
					int marginX = thumb.Width / 2, marginY = thumb.Height / 2;
					Rectangle rect = new Rectangle();
					double totalItems = MappedImages.Count;
					double itemCount = 0;
					foreach (KeyValuePair<double, SortedDictionary<double, List<long>>> col in ColorMap) {
						x = Convert.ToInt16(col.Key * canvas.Width / xMax);
						foreach (KeyValuePair<double, List<long>> row in col.Value) {
							y = Convert.ToInt16(row.Key * canvas.Height);
							foreach (long item in row.Value) {
								System.Drawing.Bitmap img = thumbs.GetThumbnail(item);
								rect.X = x - marginX;
								rect.Y = canvas.Height - y - marginY;
								rect.Size = thumb;
								rect.Height = img.Height * thumb.Width / img.Width;
								gr.DrawImage(img, rect);
								itemCount++;
								Console.Write("\r{0:0%}    ", itemCount / totalItems);
							}
						}
					}
					Console.WriteLine();
					pg.Save(Path.GetFullPath("colormap.png"));
					#endregion hid
				}
		



				private static void Distribute(Dictionary<int, int> image_counts, int arr_min, int arr_max,
											   ref int[] destination, int destination_min, int destination_max) {
					if (destination_min >= destination_max) return;

					int count = 0;
					for (int i = arr_min; i <= arr_max; i++) {
						if (image_counts.ContainsKey(i))
							count += image_counts[i];
					}

					int half_images = count / 2;
					int destination_middle = (destination_max - destination_min) / 2;
					int acc = 0;
					int key;
					for (key = arr_min; acc <= half_images; acc += image_counts[key++]) ;
					destination[destination_middle] = key;
					Distribute(image_counts, arr_min, key, ref destination, destination_min, destination_middle);
					Distribute(image_counts, key + 1, arr_max, ref destination, destination_middle, destination_max);
				}
				*/

		private Color RunDetection(string p) {
			Bitmap img = new Bitmap(p);
			AForge.Imaging.ImageStatistics histogram = new AForge.Imaging.ImageStatistics(img);
			Color c;
			if (histogram.IsGrayscale) {
				c = Color.FromArgb(histogram.Gray.Median, histogram.Gray.Median, histogram.Gray.Median);
			} else {
				c = Color.FromArgb(histogram.Red.Median, histogram.Green.Median, histogram.Blue.Median);
			}
			return c;
		}

		private Color RunDetection(Bitmap img) {
			AForge.Imaging.ImageStatistics histogram = new AForge.Imaging.ImageStatistics(img);
			Color c;
			if (histogram.IsGrayscale) {
				c = Color.FromArgb(histogram.Gray.Median, histogram.Gray.Median, histogram.Gray.Median);
			} else {
				c = Color.FromArgb(histogram.Red.Median, histogram.Green.Median, histogram.Blue.Median);
			}
			return c;
		}

		private void PutData(EagleEye.Common.Image i, Color result) {
			PluginData.Add(i.id, result);
		}

		public String ImageInfo(EagleEye.Common.Image i) {
			return PluginData.ContainsKey(i.id) ? PluginData[i.id].ToString() + " Hue: " + PluginData[i.id].GetHue().ToString() : "Not Processed";
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
