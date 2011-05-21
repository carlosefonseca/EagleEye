using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EagleEye.Plugins.FeatureExtraction;
using EagleEye.Common;
using System.Diagnostics;
using System.IO;
using System.Reflection;


namespace EagleEye {
	public class PluginManager {
		#region Singleton
		private static PluginManager instance;

		public static PluginManager Get() {
			if (instance == null)
				instance = new PluginManager();
			return instance;
		}

		private PluginManager() {
			plugins = new Dictionary<string, EEPluginInterface>();
		}
		#endregion Singleton

		private string DirToLib;
		private Dictionary<string, EEPluginInterface> plugins;

		public void LoadPlugins(string pluginDir) {
			plugins = new Dictionary<string, EEPluginInterface>();
			Console.Write("Loading Plugins: ");
			List<string> paths = new List<string>();
			foreach (string Filename in Directory.GetFiles(pluginDir , "*.eep.dll", SearchOption.AllDirectories)) {
				Assembly Asm = Assembly.LoadFile(Path.GetFullPath(Filename));
				foreach (Type AsmType in Asm.GetTypes()) {
					if (AsmType.GetInterface("EEPluginInterface") != null) {
						EEPluginInterface Plugin = (EEPluginInterface)Activator.CreateInstance(AsmType);
						plugins.Add(Plugin.Id(), Plugin);
						Plugin.Init();
						Console.Write(Plugin.ToString() + "; ");
					}
				}
			}
			Console.WriteLine();
		}


		public void RunPlugin(ImageCollection images) {
			if (plugins.Count == 0) {
				Console.WriteLine("No loaded plugins.");
				return;
			}
			Console.Write("Available Plugins: ");
			foreach (string p in plugins.Keys)
				Console.Write(p + "; ");
			string pluginId = Console.ReadLine();
			RunPlugin(images, pluginId);
			SaveMetadata(pluginId, Path.Combine(Persistence.RootFolder(), "DZC"));
		}

		public void RunPlugin(ImageCollection images, string pluginId) {
			if (plugins.ContainsKey(pluginId)) {
				EEPluginInterface p = plugins[pluginId];
				Console.WriteLine("Running plugin " + p.ToString());
				Stopwatch watch = Stopwatch.StartNew();
				ImageCollection ic = p.processImageCollection(images);
				watch.Stop();
				Console.WriteLine(watch.ElapsedMilliseconds + "ms. Results:");

				if (ic != null) {
					ImageToStringDelegate d = p.ImageToString;
					Console.WriteLine(ic.ToString(d));
				}
			} else {
				Console.WriteLine("Plugin not found.");
			}
		}

		public string PluginsInfoForImage(Image i) {
			string output = "";
			EEPluginInterface p;
			foreach (KeyValuePair<string,EEPluginInterface> kv in plugins) {
				p = kv.Value;
				output += p + " => " + p.ImageInfo(i)+"\n";
			}
			return output;
		}

		public void LoadAll() {
			if (DirToLib == null) {
				DirToLib = LibraryManager.Get().path;
			}
			foreach (KeyValuePair<string, EEPluginInterface> kv in plugins) {
				kv.Value.Load();
			}
		}

		public void SaveAll() {
			if (DirToLib == null) {
				DirToLib = LibraryManager.Get().path;
			}
			foreach (KeyValuePair<string,EEPluginInterface> kv in plugins) {
				kv.Value.Save();
			}
		}
		
		public void SaveMetadata(String pluginId, String folder) {
			if (plugins.ContainsKey(pluginId)) {
				EEPluginInterface p = plugins[pluginId];
				Console.Write("Generating metadata. ");
				String txt = p.generateMetadata();
				Console.Write("Writing file. ");
				File.WriteAllText(Path.Combine(folder, p.Id() + ".sorted.db"), txt);
				Console.WriteLine("Done!");
			}
		}
	}
}
