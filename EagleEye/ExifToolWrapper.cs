using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using EagleEye.Common;

namespace EagleEye {
	class ExifToolWrapper {
		private static Process p;
		private static ProcessStartInfo start;
		private static StreamReader reader;

		public static List<Image> CrawlDir(string dir) {
			start = new ProcessStartInfo();
			start.StandardOutputEncoding = Encoding.GetEncoding(1252);
			start.FileName = @"exiftool.exe";
			start.Arguments = "-j -q -r -ext jpg -L \"" + dir + "\"";
			start.UseShellExecute = false;
			start.RedirectStandardOutput = true;

			p = Process.Start(start);
			reader = p.StandardOutput;

			Console.WriteLine("Running exiftool...");
			string txt = reader.ReadToEnd();
			Console.WriteLine("Converting data...");
			List<Dictionary<string, object>> exifdata = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(txt);

			Console.WriteLine("Adding to the DB.");
			List<Image> images = new List<Image>();
			Image im;
			if (exifdata == null) {
			} else {
				foreach (Dictionary<string, object> it in exifdata) {
					im = new Image(it["SourceFile"].ToString());
					im.Exif(it);
					images.Add(im);
				}
			}

			return images;
		}

		public static void CrawlDir(string dir, ImageCollection collection) {
			start = new ProcessStartInfo();
			start.FileName = @"exiftool.exe";
			start.Arguments = "-j -q -r -ext jpg " + dir;
			start.UseShellExecute = false;
			start.RedirectStandardOutput = true;

			p = Process.Start(start);
			reader = p.StandardOutput;

			Console.WriteLine("Running exiftool...");
			string txt = reader.ReadToEnd();
			Console.WriteLine("Converting data...");
			List<Dictionary<string, object>> exifdata = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(txt);

			Console.WriteLine("Adding to the DB.");
			List<Image> images = new List<Image>();
			Image im;
			foreach (Dictionary<string, object> it in exifdata) {
				im = new Image(it["SourceFile"].ToString());
				im.Exif(it);
				collection.Add(im);
			}
		}

	}
}
