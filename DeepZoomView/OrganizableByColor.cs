using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace DeepZoomView {
	public class OrganizableByColor : Organizable {
		Dictionary<int, List<int>> organization;

		/// <summary>
		/// Constructor
		/// </summary>
		public OrganizableByColor() : base("Color"){
			organization = new Dictionary<int, List<int>>();
		}

		/// <summary>
		/// Imports the contents of a metadata file into the this organizable
		/// </summary>
		/// <param name="s">The string with the contents of the file</param>
		/// <returns>True if the import is successful and false otherwise</returns>
		public override Boolean Import(String s) {
			try {
				String[] lines = s.Split(new string[1] { Environment.NewLine }, 
											StringSplitOptions.RemoveEmptyEntries);

				foreach (String line in lines) {
					String[] split = line.Split(':');
					String[] ids = split[1].Split(new Char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
					Add(Convert.ToInt16(split[0]), ids);
				}
			} catch {
				organization = new Dictionary<int, List<int>>();
				return false;
			}
			return true;
		}

		/// <summary>
		/// Adds a Color, Image pair to the organizable
		/// </summary>
		/// <param name="color">The Color</param>
		/// <param name="id">The image ID</param>
		public void Add(int color, int id) {
			if (!organization.ContainsKey(color)) {
				organization.Add(color, new List<int>());
			}
			organization[color].Add(id);
		}

		/// <summary>
		/// Adds a set of images to a color. Intended to be used with the Import
		/// </summary>
		/// <param name="color">The Color</param>
		/// <param name="ids">Array of ids in string format</param>
		private void Add(int color, String[] ids) {
			if (!organization.ContainsKey(color)) {
				organization.Add(color, new List<int>());
			}
			foreach (String id in ids) {
				organization[color].Add(Convert.ToInt16(id));
			}
		}

		/// <summary>
		/// Returns the number of groups in this organizable
		/// </summary>
		/// <returns></returns>
		public override int Count() {
			return organization.Count;
		}
	}
}
