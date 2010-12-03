using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using EagleEye.Common;
using EagleEye.Plugins.FeatureExtraction;
using System.Reflection;
using System.Diagnostics;


namespace EagleEye {
	class EagleEye {
		private static ImageCollection images;
		private static Persistence persistence;
		private static PluginManager PlugMan;
		private static LibraryManager LibMan;

		private static string PluginDir = "plugins/";
		private static string LibraryDir = "EagleEyeDB";

		static void Main(string[] a) {
			#region Load Embeded Libs
			foreach (string ass in Assembly.GetExecutingAssembly().GetManifestResourceNames())
				Console.WriteLine(ass);

			AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
				String resourceName = "EagleEye" + "." + new AssemblyName(args.Name).Name + ".dll";
				Console.WriteLine("Trying to find " + resourceName);
				using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)) {
					Byte[] assemblyData = new Byte[stream.Length];
					stream.Read(assemblyData, 0, assemblyData.Length);
					return Assembly.Load(assemblyData);
				}
			};
			#endregion Load Embeded Libs

			Console.Write("Folder for DB? [.\\EagleEyeDB\\] ");
			string buf = Console.ReadLine();
			if (buf != "") LibraryDir = buf;
			LibMan = LibraryManager.Init(LibraryDir);

			PlugMan = PluginManager.Get();
			PlugMan.LoadPlugins(PluginDir);

			CommandLine();
		}

		private static void CreateLib(string LibraryDir) {

		}

		public static void CommandLine() {
			string command;
			string cmds = @"Commands: show | list | sort | adddir | plugin | save | exit";
			Console.WriteLine(cmds);
			do {
				command = Console.ReadLine();
				string[] split = command.Split(' ');
				switch (split[0]) {
					case "show": CmdShowImageInfo(split); break;
					case "list": Console.WriteLine(images.ToStringWithExif("CreateDate")); break;
					case "sort": CmdSort(split); break;
					case "adddir": AddDir(split); break;
					case "plugin": PlugMan.RunPlugin(images); break;
					case "save": LibMan.Save(); break;
					case "exit": Console.WriteLine("Bye"); break;
					default: Console.WriteLine("Unkown cmd. " + cmds); break;
				}
			} while (command != "exit");
		}

		public static void CmdSort(string[] cmd) {
			string key;
			if (cmd.Length < 2) {
				Console.WriteLine("Sort by exif tag (Ex: CreateDate, Make, Model):");
				key = Console.ReadLine();
			} else {
				key = cmd[1];
			}
			SortedImageCollection sortedImages = images.ToSortable().SortByExif(key);
			Console.WriteLine(sortedImages.ToStringWithExif(key));
			return;
		}


		public static void SaveCollection() {
			/*Console.WriteLine("Saving collection to BerkeleyDB");
			persistence.InitDB("db\\images.db");
			try {
				persistence.WriteCollection(images);
			} catch (Exception e) {
				Console.WriteLine("Could not save: " + e);
				return;
			}
			Console.Write("Save complete. ");
			persistence.read();*/
		}

		public static void AddDir(string[] cmd) {
			String dir;
			if (cmd.Length < 2) {
				Console.Write("Enter a folder with photos: ");
				dir = Console.ReadLine();
			} else {
				dir = cmd[1];
			}
			Console.WriteLine("Processing " + dir);
			images = new ImageCollection(ExifToolWrapper.CrawlDir(dir));
			Console.WriteLine(images.Count() + " images in Mem.");
		}


		public static void CmdShowImageInfo(string[] cmd) {
			string idString;
			if (cmd.Length < 2) {
				Console.Write("Enter the Image ID: ");
				idString = Console.ReadLine();
			} else {
				idString = cmd[1];
			}
			int id = int.Parse(idString);
			Console.WriteLine(ShowImageInfo(images.Get(id)));
		}


		public static string ShowImageInfo(Image i) {
			return i.Details() + "\nPlugins:\n" + PlugMan.PluginsInfoForImage(i);
		}
	}
}
