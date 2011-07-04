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
			// Adjust median to the size of the nearest rectangle (future Ra)
			if (parentRect.Width >= parentRect.Height) { // Landscape
				median = (int)(Math.Round(median / parentRect.Height) * parentRect.Height);
			} else {	// Portrait
				median = (int)(Math.Round(median / parentRect.Width) * parentRect.Width);
			}
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
						if (sum + g.images.Count <= median) {
							sum += g.images.Count;
							La.Add(g);
							Ltmp.Remove(g);
							continue;
						} else if (sum == median) {
							break;
						}
					}
					pivot = Ltmp.First();
					Ltmp.Remove(Ltmp.First());
				}
			}

			// Distribute La on the available space
			int LaCount = La.Sum(g => g.images.Count);

			// Rects for Rp, Rb, Rc
			RectWithRects Ra = null, Rp = null, Rb = null, Rc = null;

			if (parentRect.Width >= parentRect.Height) { // Landscape
				Ra = new RectWithRects(0, 0, Math.Ceiling(LaCount / parentRect.Height/* + (int)(La.Count * 0.5)*/), parentRect.Height);
				// Rects for Rp, Rb, Rc
				int RpW, RpH;
				CalculateDistribution(pivot.images.Count, parentRect.Width / parentRect.Height, new Point(parentRect.Width - Ra.Width, parentRect.Height), out RpW, out RpH);
				Rp = new RectWithRects(Ra.Width, 0, RpW, RpH);
				if (Rp.Height + Rp.Y < parentRect.Height) {
					Rb = new RectWithRects(Rp.X, Rp.Height, Rp.Width, Math.Max(1, parentRect.Height - Rp.Height));
				}
				if (Rp.X + Rp.Width < parentRect.Width) {
					Rc = new RectWithRects(Rp.X + Rp.Width, 0, Math.Max(1, parentRect.Width - Rp.X - Rp.Width), parentRect.Height);
				}
			} else {	// Portrait
				Ra = new RectWithRects(0, 0, parentRect.Width, Math.Ceiling(LaCount / parentRect.Width/* + (int)(La.Count * 0.5)*/));
				// Rects for Rp, Rb, Rc
				int RpW, RpH;
				CalculateDistribution(pivot.images.Count, parentRect.Width / parentRect.Height, new Point(parentRect.Width, parentRect.Height - Ra.Height), out RpW, out RpH);
				Rp = new RectWithRects(0, Ra.Height, RpW, RpH);
				if (Rp.X + Rp.Width < parentRect.Width) {
					Rb = new RectWithRects(Rp.Width, Rp.Y, Math.Max(1, parentRect.Width - Rp.Width), Rp.Height);
				}
				if (Rp.Y + Rp.Height < parentRect.Height) {
					Rc = new RectWithRects(0, Rp.Y + Rp.Height, parentRect.Width, Math.Max(1, parentRect.Height - Ra.Height - Rp.Height));
				}
			}
			// Split Ltmp in Lb and Lc
			List<Group> Lrest, Lrest2;
			if (Rb != null) {
				FillWhileFits(out Lb, Rb.Width * Rb.Height, Ltmp, out Lrest);
				if (Rc != null) {
					FillWhileFits(out Lc, Rc.Width * Rc.Height, Lrest, out Lrest2);
				} else {
					groupsNotPlaced.AddRange(Lrest);
				}
			} else if (Rc != null) {
				FillWhileFits(out Lc, Rc.Width * Rc.Height, Ltmp, out Lrest);
				groupsNotPlaced.AddRange(Lrest);
			} else {
				groupsNotPlaced.AddRange(Ltmp);
			}

			// 	Recursively apply the Ordered Treemap algorithm to LA in R1, LB in R2, and LC in R3.
			Ra = TreeMap(La, Ra);
			if (Rb != null && Lb.Count > 0) {
				Rb = TreeMap(Lb, Rb);
			}
			Rp.Group = pivot;
			placedGroups.Add(pivot);
			if (Rc != null && Lc.Count > 0) {
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
			if (Rb != null && Lb.Count > 0) {
				parentRect.Add(Rb);
			}
			Rp.Group.rectangle = Rp;
			if (Rc != null && Lc.Count > 0) {
				parentRect.Add(Rc);
			}
			return parentRect;
		}

		/// <summary>
		/// Tries to add Groups from Source to Dest, as long as they fit on the available space given by ToFill.
		/// Runs the source list sequentially. Does not try to maximize the elements that fit on the destination.
		/// The Groups that didn't fit are on Rest.
		/// </summary>
		/// <param name="dest">Destination List</param>
		/// <param name="toFill">Available space</param>
		/// <param name="source">Source List</param>
		/// <param name="rest">List containing the groups that didn't fit</param>
		private void FillWhileFits(out List<Group> dest, double toFill, List<Group> source, out List<Group> rest) {
			dest = new List<Group>();
			foreach (Group g in source) {
				if (g.images.Count <= toFill) {
					toFill -= g.images.Count;
					dest.Add(g);
				}
			}
			rest = source.Except(dest).ToList();
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
			if (Rb == null) Rb = new RectWithRects(-1, -1, 0, 0);
			if (Rc == null) Rc = new RectWithRects(-1, -1, 0, 0);
			System.Globalization.CultureInfo c = new System.Globalization.CultureInfo("en-US");
			Double mult = 10;
			Double RaX = mult * (P.X + Ra.X);
			Double RbX = mult * (P.X + Rb.X);
			Double RcX = mult * (P.X + Rc.X);
			Double RpX = mult * (P.X + Rp.X);

			Double RaY = mult * (P.Y + Ra.Y);
			Double RbY = mult * (P.Y + Rb.Y);
			Double RcY = mult * (P.Y + Rc.Y);
			Double RpY = mult * (P.Y + Rp.Y);

			Double RaW = mult * Ra.Width;
			Double RbW = mult * Rb.Width;
			Double RcW = mult * Rc.Width;
			Double RpW = mult * Rp.Width;

			Double RaH = mult * Ra.Height;
			Double RbH = mult * Rb.Height;
			Double RcH = mult * Rc.Height;
			Double RpH = mult * Rp.Height;

			Double PX = mult * P.X;
			Double PY = mult * P.Y;
			Double PW = mult * P.Width;
			Double PH = mult * P.Height;

			Debug.WriteLine("property RaO : {" + RaX.ToString("0.00", c) + ", " + RaY.ToString("0.00", c) + "} \r\n  property RaS : {" + RaW.ToString("0.00", c) + ", " + RaH.ToString("0.00", c) +
				"} \r\n   property RbO : {" + RbX.ToString("0.00", c) + ", " + RbY.ToString("0.00", c) + "} \r\n property RbS : {" + RbW.ToString("0.00", c) + ", " + RbH.ToString("0.00", c) +
				"}  \r\n  property RcO : {" + RcX.ToString("0.00", c) + ", " + RcY.ToString("0.00", c) + "} \r\n property RcS : {" + RcW.ToString("0.00", c) + ", " + RcH.ToString("0.00", c) +
				"}  \r\n  property RpO : {" + RpX.ToString("0.00", c) + ", " + RpY.ToString("0.00", c) + "} \r\n property RpS : {" + RpW.ToString("0.00", c) + ", " + RpH.ToString("0.00", c) +
				"}  \r\n  property PO : {" + PX.ToString("0.00", c) + ", " + PY.ToString("0.00", c) + "} \r\n  property PS : {" + PW.ToString("0.00", c) + ", " + PH.ToString("0.00", c) + "}  ");
		}


		/// <summary>
		/// Takes the set of groups and arranges them on the MSI using a the Quantum TreeMap algorithm
		/// </summary>
		/// <param name="max">This contains the width and height of the display</param>
		/// <returns>A list of "X,Y"=>ImgID for the mouse-over identification</returns>
		public Dictionary<string, int> DisplayGroupsOnScreen(out Point max) {
			if (Display == DisplayOptions[0]) {
				Dictionary<String, int> canvasIndex;
				int cols, rows;
				orderByGroupsVertically(out canvasIndex, out cols, out rows);
				max = new Point(cols, rows);
				return canvasIndex;
			} else {
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
			}
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
		/// Takes the set of groups and arranges them on the MSI vertically
		/// </summary>
		/// <param name="Groups"></param>
		private void orderByGroupsVertically(out Dictionary<String, int> canvasIndex, out int cols, out int rows) {
			List<int> groupSizes = new List<int>();
			List<string> groupNames = new List<string>();
			int total = 0;
			foreach (KeyValuePair<String, List<int>> group in this.groups) {
				groupNames.Add(group.Key);
				groupSizes.Add(group.Value.Count);
				total += group.Value.Count;
			}

			cols = 1;
			rows = 1;

			foreach (int row in groupSizes) {
				if (row > rows) {
					rows = row;
				}
			}

			cols = groupSizes.Count;
			cols = Convert.ToInt32(Math.Floor(msi.ActualHeight * cols / msi.ActualWidth));
			double rowsNeeded = 0;

			while (true) {
				rowsNeeded = 0;
				// Determina o que acontece ao reduzir a largura
				foreach (int row in groupSizes) {
					rowsNeeded += Math.Ceiling(row / (rows - 1));
				}

				// se for viavel reduzir a largura, faz isso mesmo.
				cols = Convert.ToInt32(Math.Floor(msi.ActualWidth * (rows - 1) / msi.ActualHeight));
				if (rowsNeeded <= cols) {
					rows--;
				} else {
					cols = Convert.ToInt32(Math.Floor(msi.ActualWidth * rows / msi.ActualHeight));
					break;
				}
			}



			// put images in canvas
			double imgSize = msi.ActualHeight / rows;
			var x = 0.0;
			var y = 0.0;
			// era fixe guardar o anterior para repor
			canvasIndex = new Dictionary<string, int>();

			foreach (KeyValuePair<String, List<int>> group in this.groups) {
				foreach (int id in group.Value) {
					try {
						msi.SubImages[id].ViewportOrigin = new Point(-x, -y);
					} catch {
						continue;
					}
					canvasIndex.Add(x + ";" + y, id);
					y++;

					if (y >= rows) {
						x += 1;
						y = 0.0;
					}
				}
				y = 0;
				x++;
			}

			double HcellsTmp = msi.ActualWidth * rows / msi.ActualHeight;
			cols = (int)Math.Max(cols, HcellsTmp);
			msi.ViewportWidth = cols;
			double zoom = 1;
			//ShowAllContent();

			//makeAnAxis("X", groups);
			//makeAnAxis("Y", Vcells);
			return;
		}


		public List<KeyValuePair<string, int>> GetGroupsForAxis(int cols) {
			List<KeyValuePair<string, int>> theSet = new List<KeyValuePair<string, int>>();
			foreach (KeyValuePair<string, List<int>> g in groups) {
				theSet.Add(new KeyValuePair<string, int>(g.Key, Convert.ToInt32(Math.Ceiling(g.Value.Count / cols))));
			}
			return theSet;
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
			int rows;	////////////////////////////////////////\
			CalculateDistribution((int)Math.Ceiling(imgCount * 1.0), out cols, out rows);
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
						invertedGroups.Add(id, g);
						try {
							msi.SubImages[id].ViewportOrigin = new Point(-x, -y);
							msi.SubImages[id].Opacity = 1;
						} catch {
							//g.images.Remove(id);
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
				pBorder.Stroke = new SolidColorBrush(cs[n % cs.Count()]);
				pBorder.StrokeThickness = 1.0;
				pBorder.Width = p.Width * cellHeight + 2 * n;
				pBorder.Height = p.Height * cellWidth + 2 * n;
				p = p.Parent;
				n++;
			}
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

		internal void DisplayAllGroupNames(Canvas destination) {
			Border border;
			Rect bounds;
			TextBlock txt;
			Random rand = new Random();
			foreach (Group g in placedGroups) {
				border = new Border();
				bounds = g.rectangle.Rect;
				border.Background = new SolidColorBrush(Color.FromArgb((byte)100, (byte)rand.Next(255), (byte)rand.Next(255), (byte)rand.Next(255)));
				txt = new TextBlock();
				txt.Text = g.name;
				txt.TextAlignment = TextAlignment.Center;
				txt.VerticalAlignment = VerticalAlignment.Center;
				SetFrameworkElementBoundsFromRect(txt, bounds);
				border.Child = txt;
				destination.Children.Add(border);
			}
		}

		public static void SetFrameworkElementBoundsFromOther(FrameworkElement e, FrameworkElement r) {
			Canvas.SetLeft(e, Canvas.GetLeft(r));
			Canvas.SetTop(e, Canvas.GetTop(r));
			e.Width = r.Width;
			e.Height = r.Height;
		}
		public static void SetFrameworkElementBoundsFromRect(FrameworkElement e, Rect r) {
			Canvas.SetLeft(e, r.X);
			Canvas.SetTop(e, r.Y);
			e.Width = r.Width;
			e.Height = r.Height;
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
