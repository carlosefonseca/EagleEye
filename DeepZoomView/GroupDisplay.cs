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
using System.Diagnostics;

namespace DeepZoomView {
	/// <summary>
	/// Provides a TreeMap-sort-of-like view of a set of grouped images
	/// </summary>
	public class GroupDisplay {
		public static List<String> DisplayOptions = new List<string>() { "Linear", "Groups" };
		public String Display;
		private MultiScaleImage msi;
		private List<KeyValuePair<string, List<int>>> groups;
		private double pxHeight, pxWidth, aspectRatio;
		private int imgHeight, imgWidth, imgCount;
		Dictionary<int, Group> invertedGroups = new Dictionary<int, Group>();
		Dictionary<string, Group> map = new Dictionary<string, Group>();
		List<Group> placedGroups = new List<Group>();
		List<Group> groupsNotPlaced = new List<Group>();
		Rectangle groupBorder = null;

		/// <summary>
		/// Creates a new GroupDisplay
		/// </summary>
		/// <param name="msi">The MSI where the images should be displayed</param>
		/// <param name="groups">The Groups to display</param>
		public GroupDisplay(MultiScaleImage msi, List<KeyValuePair<string, List<int>>> groups) {
			this.msi = msi;
			this.groups = groups;

			pxHeight = msi.ActualHeight;
			pxWidth = msi.ActualWidth;
			aspectRatio = pxWidth / pxHeight;
			imgCount = msi.SubImages.Count;
			CalculateCanvas();
		}

		/// <summary>
		/// Image distribution using the Quantum TreeMap algorithm
		/// </summary>
		/// <param name="groups">Groups to be ordered</param>
		/// <param name="parentRect">The area where to distribute the groups</param>
		/// <returns>A RectWithRects containing all the rects and associated groups</returns>
		internal RectWithRects TreeMap(IEnumerable<Group> groups, RectWithRects parentRect) {
			// 1. if one group, return size for that group
			if (groups.Count() == 1) {
				parentRect.Group = groups.First();
				parentRect.Adjust();
				groups.First().rectangle = parentRect;
				placedGroups.Add(groups.First());
				return parentRect;
			} else if (groups.Count() == 2) {
				RectWithRects R1, R2;
				if (parentRect.Height > parentRect.Width) {
					R1 = new RectWithRects(0, 0, parentRect.Width, null, groups.First());
					R2 = new RectWithRects(0, R1.Height, parentRect.Width, null, groups.Last());
				} else {
					R1 = new RectWithRects(0, 0, null, parentRect.Height, groups.First());
					R2 = new RectWithRects(R1.Width, 0, null, parentRect.Height, groups.Last());
				}
				groups.First().rectangle = R1;
				groups.Last().rectangle = R2;
				placedGroups.AddRange(groups);
				parentRect.Add(R1);
				parentRect.Add(R2);
				return parentRect;
			}
			// pick pivot and the lists before it and after it
			int total = groups.Sum(g => g.images.Count);
			int median = total / 2;
			int sum = 0;
			Group pivot = null;
			List<Group> La = new List<Group>();
			List<Group> Ltmp = groups.ToList();
			List<Group> Lb = new List<Group>();
			List<Group> Lc = new List<Group>();
			{
				Group firstG = groups.First();
				if (firstG.images.Count >= median) {
					La.Add(firstG);
					Ltmp.Remove(firstG);
					pivot = Ltmp.First();
					Ltmp.Remove(pivot);
				} else {
					foreach (Group g in groups) {
						sum += g.images.Count;
						if (sum < median) {
							La.Add(g);
							Ltmp.Remove(g);
							continue;
						}
						pivot = g;
						Ltmp.Remove(g);
						break;
					}
				}
			}

			// Distribute La on the available space
			int LaCount = La.Sum(g => g.images.Count);

			// Rects for Rp, Rb, Rc
			RectWithRects Ra, Rp, Rb, Rc;

			if (parentRect.Width >= parentRect.Height) { // Landscape
				Ra = new RectWithRects(0, 0, Math.Ceiling(LaCount / parentRect.Height + (int)(La.Count * 0.5)), parentRect.Height);
				// Rects for Rp, Rb, Rc
				int RpW, RpH;
				CalculateDistribution(pivot.images.Count, parentRect.Width / parentRect.Height, new Point(parentRect.Width - Ra.Width, parentRect.Height), out RpW, out RpH);
				Rp = new RectWithRects(Ra.Width, 0, RpW, RpH);
				Rb = new RectWithRects(Rp.X, Rp.Height, Rp.Width, Math.Max(1, parentRect.Height - Rp.Height));
				Rc = new RectWithRects(Rp.X + Rp.Width, 0, Math.Max(1, parentRect.Width - Rp.X - Rp.Width), parentRect.Height);
			} else {	// Portrait
				Ra = new RectWithRects(0, 0, parentRect.Width, Math.Ceiling(LaCount / parentRect.Width + (int)(La.Count * 0.5)));
				// Rects for Rp, Rb, Rc
				int RpW, RpH;
				CalculateDistribution(pivot.images.Count, parentRect.Width / parentRect.Height, new Point(parentRect.Width, parentRect.Height - Ra.Height), out RpW, out RpH);
				Rp = new RectWithRects(0, Ra.Height, RpW, RpH);
				Rb = new RectWithRects(Rp.Width, Rp.Y, Math.Max(1, parentRect.Width - Rp.Width), Rp.Height);
				Rc = new RectWithRects(0, Rp.Y + Rp.Height, parentRect.Width, Math.Max(1, parentRect.Height - Ra.Height - Rp.Height));
			}
			// Split Ltmp in Lb and Lc
			double toFill = Rb.Width * Rb.Height;
			Lb.Clear();
			Lc.Clear();
			int LcCount = 0;
			foreach (Group g in Ltmp) {
				if (g.images.Count <= toFill) {
					toFill -= g.images.Count;
					Lb.Add(g);
				} else if (Rc.Fits(LcCount + g.images.Count)) {
					Lc.Add(g);
					LcCount += g.images.Count;
				}
			}

			// 	Recursively apply the Ordered Treemap algorithm to LA in R1, LB in R2, and LC in R3.
			if (La.Count == 0 || Lb.Count == 0)
				Debug.WriteLine("A list was found empty.");
			Ra = TreeMap(La, Ra);
			if (Lb.Count > 0) {
				Rb = TreeMap(Lb, Rb);
			}
			Rp.Group = pivot;
			placedGroups.Add(pivot);
			if (Lc.Count > 0) {
				Rc = TreeMap(Lc, Rc);
			}
			
			// Even out the rectangles in the sub-regions
			if (false && parentRect.Width >= parentRect.Height) { // Landscape

				if (Rp.Width != Rb.Width) {
					double newWidth = Math.Max(Rp.Width, Rb.Width);
					Rp.Width = newWidth;
					Rb.Width = newWidth;
				}

				if (Ra.Height < Rp.Height + Rb.Height) {
					Ra.Height = Rp.Height + Rb.Height;
				} else {
					Rb.Height = Ra.Height - Rp.Height;
				}
				if (Ra.Height >= Rc.Height) {
					Rc.Height = Ra.Height;
				} else {
					Ra.Height = Rc.Height;
					Rb.Height = Ra.Height - Rp.Height;
				}

				Rp.X = Ra.X + Ra.Width;
				Rb.X = Ra.X + Ra.Width;
				Rc.X = Rp.X + Rp.Width;
			}
			parentRect.Add(Ra);
			parentRect.Add(Rp);
			parentRect.Add(Rb);
			Rp.Group.rectangle = Rp;
			if (Lc.Count > 0) {
				parentRect.Add(Rc);
			}
			return parentRect;
		}


		/// <summary>
		/// For debugging pourposes. Outputs the Rects as a set of Applescript properties.
		/// </summary>
		/// <param name="Ra"></param>
		/// <param name="Rb"></param>
		/// <param name="Rc"></param>
		/// <param name="Rp"></param>
		/// <param name="P"></param>
		internal void Output(RectWithRects Ra, RectWithRects Rb, RectWithRects Rc, RectWithRects Rp, RectWithRects P) {
			System.Globalization.CultureInfo c = new System.Globalization.CultureInfo("en-US");
			Debug.WriteLine("property RaO : {" + Ra.X.ToString("0.00", c) + ", " + Ra.Y.ToString("0.00", c) + "} \r\n  property RaS : {" + Ra.Width.ToString("0.00", c) + ", " + Ra.Height.ToString("0.00", c) + "} \r\n   property RbO : {" + Rb.X.ToString("0.00", c) + ", " + Rb.Y.ToString("0.00", c) + "} \r\n property RbS : {" + Rb.Width.ToString("0.00", c) + ", " + Rb.Height.ToString("0.00", c) + "}  \r\n  property RcO : {" + Rc.X.ToString("0.00", c) + ", " + Rc.Y.ToString("0.00", c) + "} \r\n property RcS : {" + Rc.Width.ToString("0.00", c) + ", " + Rc.Height.ToString("0.00", c) + "}  \r\n  property RpO : {" + Rp.X.ToString("0.00", c) + ", " + Rp.Y.ToString("0.00", c) + "} \r\n property RpS : {" + Rp.Width.ToString("0.00", c) + ", " + Rp.Height.ToString("0.00", c) + "}  \r\n  property PO : {" + P.X.ToString("0.00", c) + ", " + P.Y.ToString("0.00", c) + "} \r\n  property PS : {" + P.Width.ToString("0.00", c) + ", " + P.Height.ToString("0.00", c) + "}  ");
		}


		/// <summary>
		/// Takes the set of groups and arranges them on the MSI using a the Quantum TreeMap algorithm
		/// </summary>
		/// <param name="max">This contains the width and height of the display</param>
		/// <returns>A list of "X,Y"=>ImgID for the mouse-over identification</returns>
		public Dictionary<string, int> DisplayGroupsOnScreen(out Point max) {
			IOrderedEnumerable<KeyValuePair<string, List<int>>> orderedGroup = groups.OrderByDescending(kv => kv.Value.Count);
			foreach (KeyValuePair<string, List<int>> kv in orderedGroup) {
				Group g = new Group(kv.Key, kv.Value);
				groupsNotPlaced.Add(g);
			}
			RectWithRects result = TreeMap(groupsNotPlaced, new RectWithRects(0, 0, imgWidth, imgHeight));
			Debug.WriteLine(result.TreeView());
			PositionCorrection(result);
			groupsNotPlaced = groupsNotPlaced.Except(placedGroups).ToList();
			HideNotPlacedImages();
			return PositionImages(out max);
			
			#region old
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
							Rect r = map[pos].rectangle.Rect;
							if (r.Top + r.Height - 1 == y) {
								x++;
							} else {
								x = x + (int)(map[pos].rectangle.Width - (x - map[pos].rectangle.X));
							}
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
			#endregion old
		}


		/// <summary>
		/// Changes the positions of child rects to be related to the outter rect instead of its parent.
		/// </summary>
		/// <param name="node">The root Rect whose childs need fixing.</param>
		private void PositionCorrection(RectWithRects node) {
			foreach (RectWithRects r in node.Children()) {
				r.X += node.X;
				r.Y += node.Y;
				if (r.isLeaf()) {
					r.group.rectangle = r;
				} else {
					PositionCorrection(r);
				}
			}
		}

		/// <summary>
		/// Distributes images of a group on the canvas.
		/// </summary>
		/// <param name="g">The group whose images need distribution</param>
		/// <param name="ix">Initial X position</param>
		/// <param name="iy">Initial Y position</param>
		/// <returns>True if the group was succefully placed. False otherwise.</returns>
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
					if (y > imgHeight || y > 2 * H || map.ContainsKey(p(x, y))) {
						return false;
					}
				}
			}
			foreach (string pos in mapTmp) {
				map.Add(pos, g);
			}
			g.rectangle = new RectWithRects(ix, iy, H, y - iy + 1);
			return true;
		}

		/// <summary>
		/// Returns a String identifying a coordinate
		/// </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y coordinate</param>
		/// <returns>A string in the format "X-Y"</returns>
		private String p(int x, int y) {
			return x + "-" + y;
		}

		/// <summary>
		/// Used by the constructor to determine an aproximation to the rows and columns need to display the images.
		/// </summary>
		private void CalculateCanvas() {
			int cols;
			int rows;
			CalculateDistribution((int)Math.Ceiling(imgCount * 1.1), out cols, out rows);
			imgWidth = cols;
			imgHeight = rows;
		}

		/// <summary>
		/// Discovers a rectangle that can hold "amount" elements. Uses the aspect ratio set in the instance.
		/// </summary>
		/// <param name="amount">The number of elements to hold</param>
		/// <param name="max">Maximum width and height values. Ignored if the amout of elements can't fit.</param>
		/// <param name="cols">Out: The number of columns</param>
		/// <param name="rows">Out: The number of rows</param>
		private void CalculateDistribution(int amount, out int cols, out int rows) {
			CalculateDistribution(amount, aspectRatio, null, out cols, out rows);
		}

		/// <summary>
		/// Discovers a rectangle that can hold "amount" elements.
		/// </summary>
		/// <param name="amount">The number of elements to hold</param>
		/// <param name="aR">The aspect ratio of the rectangle</param>
		/// <param name="cols">Out: The number of columns</param>
		/// <param name="rows">Out: The number of rows</param>
		private void CalculateDistribution(int amount, double aR, out int cols, out int rows) {
			CalculateDistribution(amount, aR, null, out cols, out rows);
		}

		/// <summary>
		/// Discovers a rectangle that can hold "amount" elements.
		/// </summary>
		/// <param name="amount">The number of elements to hold</param>
		/// <param name="aR">The aspect ratio of the rectangle</param>
		/// <param name="max">Maximum width and height values. Ignored if the amout of elements can't fit.</param>
		/// <param name="cols">Out: The number of columns</param>
		/// <param name="rows">Out: The number of rows</param>
		private void CalculateDistribution(int amount, double aR, Point? max, out int cols, out int rows) {
			int canHold = 1;
			cols = 1;
			rows = 1;
			while (canHold < amount) {
				if (!max.HasValue) {
					cols++;
					rows = Convert.ToInt32(Math.Floor(cols / aR));
				} else {
					if (cols < max.Value.X) {
						cols++;
						rows = Convert.ToInt32(Math.Min(Math.Floor(cols / aR), max.Value.Y));
					} else if (rows < max.Value.Y) {
						rows++;
					} else {
						cols++;
						rows = Convert.ToInt32(Math.Floor(cols / aR));
					}
				}
				canHold = Convert.ToInt32(cols * rows);
			}
		}

		/// <summary>
		/// Transverses the set of groups that were correctly placed and assigns each image to a position of the MSI
		/// </summary>
		/// <param name="max">This contains the width and height of the display</param>
		/// <returns>A list of "X,Y"=>ImgID for the mouse-over identification</returns>
		public Dictionary<string, int> PositionImages(out Point max) {
			Dictionary<string, int> positions = new Dictionary<string, int>();
			max = new Point(0, 0);
			foreach (Group g in placedGroups) {
				int x = (int)Math.Round(g.rectangle.X);
				int y = (int)Math.Round(g.rectangle.Y);
				foreach (int id in g.images) {
					if (!positions.ContainsKey(x + ";" + y)) {
						positions.Add(x + ";" + y, id);
						msi.SubImages[id].Opacity = 1;
						invertedGroups.Add(id, g);
						try {
							msi.SubImages[id].ViewportOrigin = new Point(-x, -y);
						} catch {
							Debug.WriteLine("On PositionImages, id " + id + " was not found on msi (which contains " + msi.SubImages.Count + ")");
							continue;
						}
						max = new Point(Math.Max(max.X, x), Math.Max(max.Y, y));
					}
					if (++x >= g.rectangle.X + g.rectangle.Width) {
						x = (int)Math.Round(g.rectangle.X);
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
		/// <summary>
		/// Transverses the set of groups that where not placed and moves the images out of the view.
		/// </summary>
		private void HideNotPlacedImages() {
			foreach (Group g in groupsNotPlaced) {
				foreach (int id in g.images) {
					Point p = msi.SubImages[id].ViewportOrigin;
					msi.SubImages[id].ViewportOrigin = new Point(-p.X, -p.Y);
					msi.SubImages[id].Opacity = 0.5;
				}
			}
		}

		/// <summary>
		/// Given an image id, discovers in which group the images belongs,
		/// obtains the rectangle of that group and displays it inside the "element".
		/// Currently also displays the parents of the selected group's rectangle
		/// </summary>
		/// <param name="img">Image id</param>
		/// <param name="element">Canvas element which will receive the Rectangle</param>
		public void ShowGroupBorderFromImg(int img, Canvas element) {
			if (!invertedGroups.ContainsKey(img)) return;

			//			element.Children.Remove(groupBorder);
			element.Children.Clear();
			if (element.Children.Count > 0) {
				groupBorder = (Rectangle)element.Children.First(x => (((String)x.GetValue(Canvas.TagProperty)) == "Group"));
			} else {
				groupBorder = null;
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

			Color[] cs = new Color[] { Colors.Black, Colors.Blue, Colors.Cyan, Colors.Green, Colors.Yellow, Colors.Orange, Colors.Red, Colors.Magenta, Colors.Purple, Colors.Brown };
			RectWithRects p = g.rectangle.Parent;
			int n = 1;
			while (p != null) {
				Rectangle pBorder = new Rectangle();
				pBorder.SetValue(Canvas.TagProperty, "ParentGroup");
				element.Children.Add(pBorder);
				pBorder.SetValue(Canvas.TopProperty, p.Y * cellHeight - n);
				pBorder.SetValue(Canvas.LeftProperty, p.X * cellWidth - n);
				pBorder.Stroke = new SolidColorBrush(cs[n]);
				pBorder.StrokeThickness = 1.0;
				pBorder.Width = p.Width * cellHeight + 2 * n;
				pBorder.Height = p.Height * cellWidth + 2 * n;
				p = p.Parent;
				n++;
			}
		}
	} // closes GroupDisplay

	public class Group {
		internal String name { get; set; }
		internal RectWithRects rectangle { get; set; }
		internal List<int> images { get; set; }

		public Group(String n, Rect r, List<int> l)
			: this(n, l) {
			images = l;
		}

		public Group(String n, List<int> l) {
			name = n;
			images = l;
		}

		public override string ToString() {
			return "Group Name=" + name + " Rect=" + rectangle.Rect.ToString() + " ImgCount=" + images.Count;
		}
	}

	internal class RectWithRects {
		public double X { get; set; }
		public double Y { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }
		private List<RectWithRects> children;
		public Boolean leaf;
		public Group group;
		public RectWithRects Parent { get; set; }

		public Group Group {
			get {
				return group;
			}
			set {
				group = value;
				children = null;
				leaf = true;
				Adjust();
			}
		}

		public Rect Rect {
			get {
				return new Rect(X, Y, Width, Height);
			}
			set {
				X = value.X;
				Y = value.Y;
				Width = value.Width;
				Height = value.Height;
			}
		}

		public Boolean isLeaf() {
			return leaf;
		}


		public RectWithRects(Rect r)
			: this(r.X, r.Y, r.Width, r.Height) {
		}

		public RectWithRects(RectWithRects r)
			: this(r.X, r.Y, r.Width, r.Height) {
		}

		public RectWithRects(int x, int y, int w, int h)
			: this((double)x, (double)y, (double)w, (double)h) {
		}

		public RectWithRects(double x, double y, double w, double h) {
			if (w < 0) throw new Exception("Width can't be less than zero");
			if (h < 0) throw new Exception("Height can't be less than zero");
			X = x;
			Y = y;
			Width = w;
			Height = h;
			group = null;
			leaf = false;
		}

		public RectWithRects(Rect r, Group g)
			: this(r) {
			group = g;
			leaf = true;
		}

		public RectWithRects(RectWithRects r, Group g)
			: this(r) {
			group = g;
			leaf = true;
		}

		public RectWithRects(int x, int y, int w, int h, Group g)
			: this((double)x, (double)y, (double)w, (double)h, g) {
		}

		public RectWithRects(double x, double y, double w, double h, Group g)
			: this(x, y, w, h) {
			group = g;
			leaf = true;
		}

		public RectWithRects(double x, double y, double? w, double h, Group g)
			: this(x, y, (w.HasValue ? w.Value : (g.images.Count / h)), h) {
			group = g;
			leaf = true;
		}

		public RectWithRects(double x, double y, double w, double? h, Group g)
			: this(x, y, w, (h.HasValue ? h.Value : (g.images.Count / w))) {
			group = g;
			leaf = true;
		}

		public Boolean Fits(double count) {
			return (Width * Height >= count);
		}

		public Boolean Fits(int count) {
			return Fits((double)count);
		}

		public void Adjust() {
			if (leaf) {
				if (!Fits(Group.images.Count)) {
					Adjust(this.Group.images.Count);
				}
			} else {
				throw new Exception("Adjust() only works on leaf nodes");
			}
		}

		public void Adjust(int count) {
			double aR = Width / Height;
			int canHold = 1;
			double Hcells = 1;
			double Vcells = 1;
			while (canHold < count) {
				Hcells++;
				Vcells = Convert.ToInt32(Math.Floor(Hcells / aR));
				canHold = Convert.ToInt32(Hcells * Vcells);
			}
			Width = Hcells;
			Height = Vcells;
		}

		public override String ToString() {
			if (children != null) {
				return new Rect(X, Y, Width, Height).ToString() + " Count=" + children.Count;
			} else if (leaf && group != null) {
				return new Rect(X, Y, Width, Height).ToString() + " Group=" + group.ToString();
			} else {
				return new Rect(X, Y, Width, Height).ToString() + " No Children or Group";
			}
		}

		public void Add(RectWithRects r) {
			if (children == null) {
				children = new List<RectWithRects>();
			}
			children.Add(r);
			r.Parent = this;
			Width = Math.Max(r.X + r.Width, Width);
			Height = Math.Max(r.Y + r.Height, Height);
		}

		public List<RectWithRects> Children() {
			if (children == null)
				children = new List<RectWithRects>();
			return children;
		}

		public String TreeView() {
			int level = 0;
			return TreeView(level);
		}

		public String TreeView(int level) {
			String ident = "∟";
			ident = ident.PadLeft(level * 2 + 1);
			String str = ToString();
			if (children != null) {
				foreach (RectWithRects r in children) {
					str += Environment.NewLine + ident + r.TreeView(level + 1);
				}
			}
			return str;
		}
	}
}
