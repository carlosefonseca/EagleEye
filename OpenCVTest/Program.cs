using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace OpenCVTest {
	class Program {
		const string fdfile = "haarcascade_frontalface_alt_tree.xml";
		const string asmName = "OpenCVTest.EmguCV";
		const int smallSize = 1500;
		const bool DEBUG = true;

		private static string fullfdf, fdfres;
		private static HaarCascade face;
		
		static void Main(string[] a) {
			#region Load Embeded Libs
			foreach (string asm in Assembly.GetExecutingAssembly().GetManifestResourceNames()) {
				if (asm.Contains(fdfile)) fdfres = asm;
				if (DEBUG) Console.WriteLine(asm);
			}
			
			AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
				String resourceName = asmName + "." + new AssemblyName(args.Name).Name + ".dll";
//				Console.WriteLine("Trying to find " + resourceName);
				using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)) {
					Byte[] assemblyData = new Byte[stream.Length];
					stream.Read(assemblyData, 0, assemblyData.Length);
					return Assembly.Load(assemblyData);
				}
			};
			#endregion Load Embeded Libs

			string currDir = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Program)).CodeBase);
			currDir = currDir.Substring(6);
			fullfdf = currDir + "\\" + fdfile;

			if (!File.Exists(fullfdf)) {
				var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(fdfres);
				Byte[] assemblyData = new Byte[stream.Length];
				stream.Read(assemblyData, 0, assemblyData.Length);
				FileStream fd = new FileStream(fullfdf, FileMode.Create);
				fd.Write(assemblyData, 0, assemblyData.Length);
				fd.Close();
			}

			//Read the HaarCascade objects
			if (face == null)
				face = new HaarCascade(fullfdf);


			if (DEBUG) {
				Stopwatch s = new Stopwatch();
				string log = "", o;
				int i = 0, n;

				foreach (string filename in Directory.GetFiles("C:\\Fotos\\faces","*.jpg")) {
					Console.WriteLine(filename);
					i = 0;
					log = filename+"\n";
					do {
						s.Reset();
						Console.Write(i + ": ");
						s.Start();
						o = RunFD(filename, i);
						s.Stop();
						if (i == 0) Console.WriteLine(o);
						if (o.Contains("none")) n = 0;
						else n = o.Split(";".ToCharArray(),StringSplitOptions.RemoveEmptyEntries).Length;
						Console.WriteLine(n + " > " + s.ElapsedMilliseconds);
						log += i + ": " + n + " > " + s.ElapsedMilliseconds + "\n";
						drawRects(filename,i, o);
						if (i == 0)
							i = 2000;
						else
							i -= 100;
						File.WriteAllText(filename+".out", log);
					} while (n > 0 && i>100);
				}
				//File.WriteAllText("out", log);
			} else {
				string cmd;
				while ((cmd = Console.ReadLine()) != "exit") {
					if (File.Exists(cmd)) {
						Console.WriteLine(RunFD(cmd,smallSize));
					}
				}
			}
		}

		public static string RunFD(string path, int smallSize) {
			//Read the HaarCascade objects
			if (face == null)
				face = new HaarCascade(fullfdf);

			Image<Bgr, Byte> image = new Image<Bgr, byte>(path); //Read the files as an 8-bit Gray image  
			//Image<Gray, Byte> gray = new Image<Gray, byte>(path); //Read the files as an 8-bit Gray image  
			Image<Gray, Byte> gray = image.Convert<Gray, Byte>(); //Convert it to Grayscale
			gray._EqualizeHist();
			if (smallSize > 0)
				gray = gray.Resize(smallSize, smallSize, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC, true);

			//normalizes brightness and increases contrast of the image


			//Detect the faces  from the gray scale image and store the locations as rectangle
			//The first dimensional is the channel
			//The second dimension is the index of the rectangle in the specific channel
			//Console.WriteLine("Running");
			MCvAvgComp[][] facesDetected = gray.DetectHaarCascade(
			   face,
			   1.1,
			   10,
			   Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
			   new Size(20, 20));

			string o = "";
			foreach (MCvAvgComp f in facesDetected[0]) {
				o += f.rect.ToString() + ";";
#region NIU
				/*
				//draw the face detected in the 0th (gray) channel with blue color
				image.Draw(f.rect, new Bgr(Color.Blue), 2);

				//Set the region of interest on the faces
				gray.ROI = f.rect;
				MCvAvgComp[][] eyesDetected = gray.DetectHaarCascade(
				   eye,
				   1.1,
				   10,
				   Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
				   new Size(20, 20));
				gray.ROI = Rectangle.Empty;

				foreach (MCvAvgComp e in eyesDetected[0]) {
					Rectangle eyeRect = e.rect;
					eyeRect.Offset(f.rect.X, f.rect.Y);
					image.Draw(eyeRect, new Bgr(Color.Red), 2);
				}*/
#endregion NIU
			}
			return (o == "" ? "none" : o);
		}

		public static void drawRects(string path, int size, string rects) {
			if (rects == "none") return;
			
			Image<Bgr, Byte> image = new Image<Bgr, byte>(path); //Read the files as an 8-bit Gray image  

			if (size > 0)
				image = image.Resize(size, size, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC, true);

			string[] rectsarr = rects.Split(";".ToCharArray(),StringSplitOptions.RemoveEmptyEntries);
			foreach (string recttxt in rectsarr) {
				string[] ts = recttxt.Split(@"{}:=,".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
				int x = int.Parse(ts[1]);
				int y = int.Parse(ts[3]);
				int w = int.Parse(ts[5]);
				int h = int.Parse(ts[7]);
				Rectangle r = new Rectangle(x, y, w, h);

				//draw the face detected in the 0th (gray) channel with blue color
				image.Draw(r, new Bgr(Color.Red), 2);
			}
			image.Save(path + "-" + size + ".jpg");
		}
	}
}
