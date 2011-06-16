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
		Dictionary<int, Group> invertedGroups = new Dictionary<int, Group>();
		Dictionary<string, Group> map = new Dictionary<string, Group>();
		List<Group> placedGroups = new List<Group>();
		List<Group> groupsNotPlaced = new List<Group>();
		Rectangle groupBorder = null;

		public GroupDisplay(MultiScaleImage msi, List<KeyValuePair<string, List<int>>> groups) {
			this.msi = msi;
			this.groups = groups;

			pxHeight = msi.ActualHeight;
			pxWidth = msi.ActualWidth;
			aspectRatio = pxWidth / pxHeight;
			imgCount = msi.SubImages.Count;
			CalculateCanvas();
		}


		public Dictionary<string, int> DisplayGroupsOnScreen(out Point max) {
			IOrderedEnumerable<KeyValuePair<string, List<int>>> orderedGroup = groups.OrderByDescending(kv => kv.Value.Count);
			foreach (KeyValuePair<string, List<int>> kv in orderedGroup) {
				Group g = new Group(kv.Key, kv.Value);
				groupsNotPlaced.Add(g);
			}

			int x = 0, y = 0;
			String pos;
			int countGroupsPlaced = -1;
			while (groupsNotPlaced.Count != 0 && (x < imgWidth && y < imgHeight)) {
				countGroupsPlaced = -1;
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
						} else {
						}
					}
				}
				// caso ele não consiga meter nenhum grupo do ponto livre, experimenta outro ponto
				x++;
				if (x >= imgWidth) {
					x = 0;
					y++;
				}
			}
			HideNotPlacedImages();
			return PositionImages(out max);
		}

		private Boolean Fill(Group g, int ix, int iy) {
			int H, V,
				x = ix, y = iy,
				n = g.images.Count;
			CalculateDistribution(n, out H, out V);

			// test against the canvas limits
			if (V > imgHeight - iy) {
				V = imgHeight - iy;
				if (V < 1) {
					return false;
				}
				H = (int)Math.Ceiling(g.images.Count / (1.0 * V));

				if (H > imgWidth - ix) {
					// it doesn't fit! what now? search for other space? for now let's just not add it
					return false;
				}
			}
			if (H > imgWidth - ix) {
				H = imgWidth - ix;
				if (H < 1) {
					return false;
				}
				V = (int)Math.Ceiling(g.images.Count / (1.0 * H));

				if (V > imgHeight - iy) {
					// it doesn't fit! what now? search for other space? for now let's just not add it
					return false;
				}
			}
			if (g.name == "10-04-2010") {
				g.ToString();
			}
			List<string> mapTmp = new List<string>();
			while (n > 0) {
				if (x - ix < H) {
					if (!map.ContainsKey(p(x, y))) {
						mapTmp.Add(p(x, y));
						n--;
						x++;
					} else {
						H = x - ix;
					}
				} else {
					y++;
					x = ix;
					if (y > imgHeight || map.ContainsKey(p(x, y))) {
						return false;
					}
				}
			}
			foreach (string pos in mapTmp) {
				map.Add(pos, g);
			}
			g.rectangle = new Rect(ix, iy, H, y-iy+1);
			return true;
		}

		private String p(int x, int y) {
			return x + "-" + y;
		}

		private void CalculateCanvas() {
			int Hcells;
			int Vcells;
			CalculateDistribution((int)Math.Ceiling(imgCount * 1.1), out Hcells, out Vcells);
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

		public Dictionary<string, int> PositionImages(out Point max) {
			Dictionary<string, int> positions = new Dictionary<string, int>();
			max = new Point(0, 0);
			foreach (Group g in placedGroups) {
				double x = g.rectangle.X;
				double y = g.rectangle.Y;
				foreach (int id in g.images) {
					if (!positions.ContainsKey(x + ";" + y)) {
						positions.Add(x + ";" + y, id);
						invertedGroups.Add(id, g);
						msi.SubImages[id].ViewportOrigin = new Point(-x, -y);
						max = new Point(Math.Max(max.X, x), Math.Max(max.Y, y));
					}
					if (++x >= g.rectangle.X + g.rectangle.Width) {
						x = g.rectangle.X;
						y++;
					}
				}
			}
			if ((int)(max.X / aspectRatio) < max.Y) {
				msi.ViewportWidth = max.Y * aspectRatio;
			} else {
				msi.ViewportWidth = max.X;
			}
			max.X++;
			max.Y++;
			imgWidth = (int)max.X;
			imgHeight = (int)max.Y;
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

		public void ShowGroupBorderFromImg(int img, Canvas element) {
			if (!invertedGroups.ContainsKey(img)) return;

			//			element.Children.Remove(groupBorder);
			if (element.Children.Count > 0) {
				groupBorder = (Rectangle)element.Children.First(x => (((String)x.GetValue(Canvas.TagProperty)) == "Group"));
			}

			double cellHeight = pxHeight / imgHeight;
			double cellWidth = pxWidth / imgWidth;
			cellHeight = cellWidth;

			Group g = invertedGroups[img];
			if (groupBorder == null) {
				groupBorder = new Rectangle();
				groupBorder.SetValue(Canvas.TagProperty, "Group");
				element.Children.Add(groupBorder);
			}
			groupBorder.SetValue(Canvas.TopProperty, g.rectangle.Y * cellHeight);
			groupBorder.SetValue(Canvas.LeftProperty, g.rectangle.X * cellWidth);
			groupBorder.Stroke = new SolidColorBrush(Colors.White);
			groupBorder.StrokeThickness = 1.0;
			//groupBorder.Fill = new SolidColorBrush(Colors.Red);
			groupBorder.Width = g.rectangle.Width * cellHeight;
			groupBorder.Height = g.rectangle.Height * cellWidth;

			//g.rectangle;
		}

		public void Test(Canvas element) {
			double cellHeight = pxHeight / imgHeight;
			double cellWidth = pxWidth / imgWidth;
			foreach (Group g in placedGroups) {
				Rectangle r = new Rectangle();
				r.Stroke = new SolidColorBrush(Colors.Red);
				r.SetValue(Canvas.LeftProperty, g.rectangle.X);
				r.SetValue(Canvas.TopProperty, g.rectangle.Y);
				r.Width = cellWidth;
				r.Height = cellHeight;
			}
		}
	} // closes GroupDisplay

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
