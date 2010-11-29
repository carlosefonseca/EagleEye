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
		const string asmName = "OpenCVTest.bin.Debug";
		const int smallSize = 1500;
		const bool DEBUG = false;
		
		private static string fullfdf;
		private static HaarCascade face;
		
		static void Main(string[] a) {

			//foreach (string ass in Assembly.GetExecutingAssembly().GetManifestResourceNames())
			//    Console.WriteLine(ass);


			AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
				String resourceName = asmName + "." + new AssemblyName(args.Name).Name + ".dll";
				//Console.WriteLine("Trying to find " + resourceName);
				using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)) {
					Byte[] assemblyData = new Byte[stream.Length];
					stream.Read(assemblyData, 0, assemblyData.Length);
					return Assembly.Load(assemblyData);
				}
			};

			string currDir = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Program)).CodeBase);
			currDir = currDir.Substring(6);
			fullfdf = currDir + "\\" + fdfile;

			if (!File.Exists(fullfdf)) {
				var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(asmName + "." + fdfile);
				Byte[] assemblyData = new Byte[stream.Length];
				stream.Read(assemblyData, 0, assemblyData.Length);
				FileStream fd = new FileStream(fullfdf, FileMode.Create);
				fd.Write(assemblyData, 0, assemblyData.Length);
				fd.Close();
			}


			if (DEBUG) {
				Stopwatch s = new Stopwatch();
				string log = "";
				int i = 0;
				s.Reset();
				s.Start();
				Console.WriteLine(i + ": " + RunFD("C:\\Fotos\\smallset\\zy.jpg", i));
				s.Stop();
				log += i + ": " + RunFD("C:\\Fotos\\smallset\\zy.jpg", i).Split(';').Length + " > " + s.ElapsedMilliseconds+"\n";
				for (i = 1700; i > 1000; i -= 100) {
					s.Reset();
					s.Start();
					Console.WriteLine(i + ": " + RunFD("C:\\Fotos\\smallset\\zy.jpg", i));
					s.Stop();
					log += i + ": " + RunFD("C:\\Fotos\\smallset\\zy.jpg", i).Split(';').Length + " > " + s.ElapsedMilliseconds+"\n";
				}
				Console.WriteLine(log);
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
			}
			return (o == "" ? "none" : o);
		}
	}
}
