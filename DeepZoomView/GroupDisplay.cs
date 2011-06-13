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
using System.Linq;

namespace DeepZoomView {
	public class GroupDisplay {
		private MultiScaleImage msi;
		private List<KeyValuePair<string, List<int>>> groups;
		private double pxHeight, pxWidth, aspectRatio;
		private int imgHeight, imgWidth, imgCount;
		Dictionary<string, Group> map = new Dictionary<string, Group>();
		List<Group> placedGroups = new List<Group>();
		List<Group> groupsNotPlaced = new List<Group>();

		public GroupDisplay(MultiScaleImage msi, List<KeyValuePair<string, List<int>>> groups) {
			this.msi = msi;
			this.groups = groups;

			pxHeight = msi.ActualHeight;
			pxWidth = msi.ActualWidth;
			aspectRatio = pxWidth / pxHeight;
			imgCount = msi.SubImages.Count;
			CalculateCanvas();
		}


		public Dictionary<string, int> DisplayGroupsOnScreen() {
			IOrderedEnumerable<KeyValuePair<string, List<int>>> orderedGroup = groups.OrderByDescending(kv => kv.Value.Count);
			foreach (KeyValuePair<string, List<int>> kv in orderedGroup) {
				Group g = new Group(kv.Key, kv.Value);
				groupsNotPlaced.Add(g);
			}

			int x = 0, y = 0;
			String pos;
			int countGroupsPlaced = -1;

			while (placedGroups.Count > countGroupsPlaced) {
				countGroupsPlaced = placedGroups.Count;
				List<Group> groupsBeingPlaced = groupsNotPlaced.GetRange(0, groupsNotPlaced.Count);
				foreach (Group g in groupsBeingPlaced) {
					pos = p(x, y);
					while (map.ContainsKey(pos)) {
						x = x + (int)(map[pos].rectangle.Width - (x - map[pos].rectangle.X));
						if (x >= imgWidth) {
							x = 0;
							y++;
							if (y >= imgHeight) {
								break;
							}
						}
						pos = p(x, y);
					}
					if (Fill(g, x, y)) {
						placedGroups.Add(g);
						groupsNotPlaced.Remove(g);
					}
				}
			}
			HideNotPlacedImages();
			return PositionImages();
		}

		private Boolean Fill(Group g, int ix, int iy) {
			int H, V,
				x = ix, y = iy,
				n = g.images.Count;
			CalculateDistribution(n, out H, out V);

			// test against the canvas limits
			if (V > imgHeight - iy) {
				V = imgHeight - iy;
				H = (int)Math.Ceiling(g.images.Count / (1.0 * V));

				if (H > imgWidth - ix) {
					// it doesn't fit! what now? search for other space? for now let's just not add it
					return false;
				}
			}
			if (H > imgWidth - ix) {
				H = imgWidth - ix;
				V = (int)Math.Ceiling(g.images.Count / (1.0 * H));

				if (V > imgHeight - iy) {
					// it doesn't fit! what now? search for other space? for now let's just not add it
					return false;
				}
			}

			while (n > 0) {
				if (x - ix < H && !map.ContainsKey(p(x, y))) {
					map.Add(p(x, y), g);
					n--;
					x++;
				} else if (y - iy < V) {
					y++;
					x = ix;
				} else {
					//throw new NotImplementedException("why tha fuck am I where?");
					break;
				}
			}
			g.rectangle = new Rect(ix, iy, H, V);
			return true;
		}

		private String p(int x, int y) {
			return x + "-" + y;
		}

		private void CalculateCanvas() {
			int Hcells;
			int Vcells;
			CalculateDistribution((int)Math.Ceiling(imgCount * 1.05), out Hcells, out Vcells);
			imgWidth = Hcells;
			imgHeight = Vcells;
		}

		private void CalculateDistribution(int amount, out int Hcells, out int Vcells) {
			int canHold = 1;
			Hcells = 1;
			Vcells = 1;
			while (canHold < amount) {
				Hcells++;
				Vcells = Convert.ToInt32(Math.Floor(Hcells / aspectRatio));
				canHold = Convert.ToInt32(Hcells * Vcells);
			}
		}

		public Dictionary<string, int> PositionImages() {
			Dictionary<string, int> positions = new Dictionary<string, int>();
			foreach (Group g in placedGroups) {
				double x = g.rectangle.X;
				double y = g.rectangle.Y;
				foreach (int id in g.images) {
					if (!positions.ContainsKey(x + ";" + y)) {
						positions.Add(x + ";" + y, id);
						msi.SubImages[id].ViewportOrigin = new Point(-x, -y);
					}
					if (++x >= g.rectangle.X + g.rectangle.Width) {
						x = g.rectangle.X;
						y++;
					}
				}
			}
			return positions;
		}

		private void HideNotPlacedImages() {
			foreach (Group g in groupsNotPlaced) {
				foreach (int id in g.images) {
					Point p = msi.SubImages[id].ViewportOrigin;
					msi.SubImages[id].ViewportOrigin = new Point(-p.X, -p.Y);
					msi.SubImages[id].Opacity = 0.5;
				}
			}
		}
	}


	public class Group {
		internal String name { get; set; }
		internal Rect rectangle { get; set; }
		internal List<int> images { get; set; }

		public Group(String n, Rect r, List<int> l)
			: this(n, l) {
			images = l;
		}

		public Group(String n, List<int> l) {
			name = n;
			images = l;
		}


		//public static List<Group> 
	}
}
