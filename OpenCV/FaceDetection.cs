using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EagleEye.Common;
using EagleEye.Plugins.FeatureExtraction;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace EEPlugin {
	public class FaceDetection : EEPluginInterface {
		//ATENCAO: Valores dos Rectangulos referem-se a uma imagem redimensionada para 1500px de largura
		private Dictionary<long, Rectangle[]> PluginIndex;
		#region EEPluginInterface Members

		public void Init() {
			if (PluginIndex == null) {
				PluginIndex = new Dictionary<long, Rectangle[]>();
			}
		}

		public ImageCollection processImageCollection(ImageCollection ic) {
			try {
				if (PluginIndex == null) {
					PluginIndex = new Dictionary<long, Rectangle[]>();
				}
				if (p == null) {
					Prepare();
				}
				foreach (EagleEye.Common.Image i in ic.ToList()) {
					if (PluginIndex.ContainsKey(i.id)) {
						Console.WriteLine("Skipping " + i.path);
						continue;
					}
					Console.Write("Face Detecting " + i.path + "... ");
					Rectangle[] result = RunDetection(i.path);
					PluginIndex.Add(i.id, result);
					Console.WriteLine(result != null ? result.Length : 0);
				}
			} finally {
				Kill();
			}
			return null;
		}

		public String Id() {
			return "faces";
		}

		public override String ToString() {
			return "Face Detection";
		}

		public String ImageToString(EagleEye.Common.Image i) {
			return i.ToString();
		}

		public String ImageInfo(EagleEye.Common.Image i) {
			if (PluginIndex.ContainsKey(i.id)) {
				Rectangle[] faces = PluginIndex[i.id];
				if (faces == null) return "No faces.";
				int n = PluginIndex[i.id].Length;
				return n + " face" + (n == 1 ? "" : "s");
			}
			return "Not analyzed";
		}

		#endregion EEPluginInterface Members


		private ProcessStartInfo start;
		private Process p;

		private StreamReader reader;
		private StreamWriter writer;

		private void Prepare() {
			string currDir = Path.GetDirectoryName(Assembly.GetAssembly(typeof(FaceDetection)).CodeBase);
			currDir = currDir.Substring(6);

			start = new ProcessStartInfo();
			start.FileName = currDir + "\\FaceDetect.exe";
			start.UseShellExecute = false;
			start.RedirectStandardOutput = true;
			start.RedirectStandardInput = true;

			p = Process.Start(start);
			reader = p.StandardOutput;
			writer = p.StandardInput;
		}

		private Rectangle[] RunDetection(string fn) {
			writer.WriteLine(fn);
			writer.Flush();

			string txt = reader.ReadLine();
			if (txt == "none") {
				return null;
			}
			char[] chars = new char[1];
			chars[0]=';';
			string[] txts = txt.Split(chars, StringSplitOptions.RemoveEmptyEntries);
			Rectangle[] faces = new Rectangle[txts.Length];
			int i = 0;
			foreach (string t in txts) {
				string[] ts = t.Split(@"{}:=,".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
				int x = int.Parse(ts[1]);
				int y = int.Parse(ts[3]);
				int w = int.Parse(ts[5]);
				int h = int.Parse(ts[7]);
				Rectangle r = new Rectangle(x, y, w, h);
				faces[i] = r;
				i++;
			}
			return faces;
		}

		private void Kill() {
			if (p != null) {
				p.Kill();
				p = null;
			}
		}
	}
}