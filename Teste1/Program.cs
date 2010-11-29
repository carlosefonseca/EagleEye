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
	class Program {
		private static ImageCollection images;
		private static Persistence persistence;
		private static PluginManager PlugMan;

		private static string PluginDir = "plugins/";

		static void Main(string[] args) {
			persistence = Persistence.Get();
			string tmp = "";
			if (persistence.DbExists()) {
				Console.Write("Load from DB? [y/n] ");
				do {
					tmp = Console.ReadLine();
				} while (tmp != "y" && tmp != "n");
			}
			if (tmp == "y") {
				persistence.read();
				images = persistence.ReadCollection();
			} else {
				String dir;
				if (args.Length == 1) {
					dir = args[0];
					Console.WriteLine("Processing " + dir);
					images = new ImageCollection(ExifToolWrapper.CrawlDir(dir));
					Console.WriteLine(images.Count() + " images in Mem.");
					//SaveCollection();
				} else {
					Console.WriteLine("Directory needed");
					return;
				}
			}
			PlugMan = PluginManager.Get();
			PlugMan.LoadPlugins(PluginDir);
			CommandLine();
		}

		public static void CommandLine() {
			string command;
			string cmds = @"Commands: show | list | sort | plugin | save | exit";
			Console.WriteLine(cmds);
			do {
				command = Console.ReadLine();
				switch (command) {
					case "show":
						Console.Write("Image Id: ");
						int id = int.Parse(Console.ReadLine());
						Console.WriteLine(ShowImageInfo(images.Get(id)));
						break;
					case "list": Console.WriteLine(images.ToStringWithExif("CreateDate")); break;
					case "sort": Sort(); break;
					case "plugin": PlugMan.RunPlugin(images); break;
					case "save": SaveCollection(); break;
					case "exit": Console.WriteLine("Bye"); break;
					default: Console.WriteLine("Unkown cmd. " + cmds); break;
				}
			} while (command != "exit");
		}

		public static void Sort() {
			Console.WriteLine("Sort by exif tag (Ex: CreateDate, Make, Model):");
			string key = Console.ReadLine();
			SortedImageCollection sortedImages = images.ToSortable().SortByExif(key);
			Console.WriteLine(sortedImages.ToStringWithExif(key));
			return;
		}

		public static void SaveCollection() {
			Console.WriteLine("Saving collection to BerkeleyDB");
			persistence.InitDB("db\\images.db");
			try {
				persistence.WriteCollection(images);
			} catch (Exception e) {
				Console.WriteLine("Could not save: " + e);
				return;
			}
			Console.Write("Save complete. ");
			persistence.read();
		}

		public static string ShowImageInfo(Image i) {
			return i.Details() + "\nPlugins:\n" + PlugMan.PluginsInfoForImage(i);
		}
	}
}
