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
		public String Display;
		public static List<String> DisplayOptions = new List<string>() { "Groups", "Linear" };

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
		/// Takes the set of groups and arranges them on the MSI using a the Quantum TreeMap algorithm
		/// </summary>
		/// <param name="max">This contains the width and height of the display</param>
		/// <returns>A list of "X,Y"=>ImgID for the mouse-over identification</returns>
		public Dictionary<string, int> DisplayGroupsOnScreen(out Point max) {
			groupNamesOverlay = null;
			Dictionary<string, int> canvasIndex = null;

			groupsNotPlaced.Clear();
			placedGroups.Clear();
			invertedGroups.Clear();
			if (groupBorder != null && groupBorder.Parent != null) {
				((Canvas)groupBorder.Parent).Children.Remove(groupBorder);
				groupBorder = null;
			}
			if (Display == "Linear") {
				IOrderedEnumerable<KeyValuePair<string, List<int>>> orderedGroup = groups.OrderBy(kv => kv.Key);
				foreach (KeyValuePair<string, List<int>> kv in orderedGroup) {
					Group g = new Group(kv.Key, kv.Value);
					groupsNotPlaced.Add(g);
				}
				Group.DisplayType = Display;
				int cols, rows;
				orderByGroupsVertically(groupsNotPlaced, out canvasIndex, out cols, out rows);
				max = new Point(cols, rows);
			} else if (Display == "Groups") {
				IOrderedEnumerable<KeyValuePair<string, List<int>>> orderedGroup = groups.OrderByDescending(kv => kv.Value.Count);
				foreach (KeyValuePair<string, List<int>> kv in orderedGroup) {
					Group g = new Group(kv.Key, kv.Value);
					groupsNotPlaced.Add(g);
				}
				Group.DisplayType = Display;
				RectWithRects result = TreeMap(groupsNotPlaced, new RectWithRects(0, 0, imgWidth, imgHeight));
				Debug.WriteLine(result.TreeView2());
				PositionCorrection(result);
				groupsNotPlaced = groupsNotPlaced.Except(placedGroups).ToList();
				HideNotPlacedImages();
				canvasIndex = PositionImages(out max);
			} else {
				throw new Exception("Incorrect display method");
			}
			//max.X = Math.Max(max.Y * aspectRatio, max.X);
			return canvasIndex;
			//////////////// (temporarily) DEAD CODE
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

		public static Polygon DuplicatePolygon(Polygon o) {
			Polygon newP = new Polygon();
			foreach (Point p in o.Points) {
				newP.Points.Add(p);
			}
			return newP;
		}


		public List<KeyValuePair<string, int>> GetGroupsForAxis(int cols) {
			List<KeyValuePair<string, int>> theSet = new List<KeyValuePair<string, int>>();
			foreach (KeyValuePair<string, List<int>> g in groups) {
				theSet.Add(new KeyValuePair<string, int>(g.Key, Convert.ToInt32(Math.Ceiling(g.Value.Count / cols))));
			}
			return theSet;
		}

		/// <summary>
		/// Transverses the set of groups that were correctly placed and assigns each image to a position of the MSI
		/// </summary>
		/// <param name="max">This contains the width and height of the display</param>
		/// <returns>A list of "X,Y"=>ImgID for the mouse-over identification</returns>
		public Dictionary<string, int> PositionImages(out Point max) {
			invertedGroups.Clear();
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
							Page.PositionImageInMSI(msi, id, x, y);
							//msi.SubImages[id].ViewportOrigin = new Point(-x, -y);
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

		public static void SetFrameworkElementBoundsFromOther(FrameworkElement e, FrameworkElement r) {
			Canvas.SetLeft(e, Canvas.GetLeft(r));
			Canvas.SetTop(e, Canvas.GetTop(r));
			e.Width = r.Width;
			e.Height = r.Height;
		}
		public static void SetFrameworkElementBoundsFromRect(FrameworkElement e, Rect r) {
			SetFrameworkElementBoundsFromRect(e, r, 1.0);
		}

		public static void SetFrameworkElementBoundsFromRect(FrameworkElement e, Rect r, double multiplier) {
			Canvas.SetLeft(e, r.X * multiplier);
			Canvas.SetTop(e, r.Y * multiplier);
			e.Width = r.Width * multiplier;
			e.Height = r.Height * multiplier;
		}


		public void SetGroupNamesOverlay(Canvas destination) {
			if (groupNamesOverlay == null) {
				groupNamesOverlay = new Canvas();
				groupNamesOverlay.Width = this.pxWidth;

				Border border;
				Polygon pBorder;
				Rect bounds;
				TextBlock txt;
				Random rand = new Random();
				double cellSide = pxWidth / imgWidth;

				foreach (Group g in placedGroups) {
					txt = new TextBlock();
					txt.Text = g.name;
					txt.TextAlignment = TextAlignment.Center;
					txt.TextWrapping = TextWrapping.Wrap;
					txt.VerticalAlignment = VerticalAlignment.Center;
					txt.FontWeight = FontWeights.Bold;
					txt.Foreground = new SolidColorBrush(Colors.White);
					if (Group.DisplayType == "Groups") {
						border = new Border();
						bounds = g.rectangle.Rect;
						border.Background = new SolidColorBrush(Color.FromArgb((byte)150, (byte)rand.Next(255), (byte)rand.Next(255), (byte)rand.Next(255)));
						border.Width = bounds.Width * cellSide;
						border.Height = bounds.Height * cellSide;
						Canvas.SetLeft(border, bounds.X * cellSide);
						Canvas.SetTop(border, bounds.Y * cellSide);
						border.Child = txt;
						groupNamesOverlay.Children.Add(border);
					} else if (Group.DisplayType == "Linear") {
						pBorder = DuplicatePolygon((Polygon)g.shape);
						pBorder.Fill = new SolidColorBrush(Color.FromArgb((byte)150, (byte)rand.Next(255), (byte)rand.Next(255), (byte)rand.Next(255)));
						Canvas.SetLeft(txt, pBorder.Points[0].X);
						Canvas.SetTop(txt, pBorder.Points[0].Y);

						txt.Width = pBorder.Width;
						txt.Height = pBorder.Height;

						RotateTransform rt = new RotateTransform();
						rt.Angle = 90;
						//rt.CenterY = -txt.Height;
						//txt.RenderTransformOrigin = new Point(0, -txt.Height);
						//txt.RenderTransform = rt;
						groupNamesOverlay.Children.Add(pBorder);
						groupNamesOverlay.Children.Add(txt);
					}
				}
			}
			destination.Children.Clear();
			destination.Children.Add(groupNamesOverlay);
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
			groupBorder = (Shape)element.Children.FirstOrDefault(x => (((String)x.GetValue(Canvas.TagProperty)) == "Group"));

			double cellHeight = pxHeight / imgHeight;
			double cellWidth = pxWidth / imgWidth;
			cellHeight = cellWidth;
			element.Children.Remove(groupBorder);
			Group g = invertedGroups[img];
			if (Display == "Linear") {

				g.shape.Stroke = new SolidColorBrush(Colors.White);
				g.shape.StrokeThickness = 1.0;
				g.shape.Tag = "Group";
				element.Children.Add(g.shape);
			} else if (Display == "Groups") {
				//if (groupBorder == null || (String)groupBorder.Tag == "") {
				groupBorder = new Rectangle();
				groupBorder.SetValue(Canvas.TagProperty, "Group");
				element.Children.Add(groupBorder);
				//}
				groupBorder.SetValue(Canvas.TopProperty, g.rectangle.Y * cellHeight);
				groupBorder.SetValue(Canvas.LeftProperty, g.rectangle.X * cellWidth);
				groupBorder.Stroke = new SolidColorBrush(Colors.White);
				groupBorder.StrokeThickness = 1.0;
				//groupBorder.Fill = new SolidColorBrush(Colors.Red);
				groupBorder.Width = g.rectangle.Width * cellHeight;
				groupBorder.Height = g.rectangle.Height * cellWidth;
			}

			//return;

			List<UIElement> toRemove = element.Children.Where(x => (String)x.GetValue(Canvas.TagProperty) == "ParentGroup").ToList();
			foreach (UIElement e in toRemove) {
				element.Children.Remove(e);
			}

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


		//	groups = groups.OrderByDescending(g => g.images.Count);

		#region Treemap v3

		private RectWithRects TreeMap(IEnumerable<Group> groups, RectWithRects rect) {
			Random r = new Random();
			int id = r.Next(100);
			Debug.WriteLine(id + ": TreeMap with " + groups.Count() + " groups on rect " + rect.Rect);

			Boolean originalIsHorizontal = rect.isHorizontal();
			RectSide insertionSide = RectSide.Left;
			if (!originalIsHorizontal) {
				insertionSide = RectSide.Top;
				//rect.MakeHorizontal();
				//Debug.WriteLine(id + ": Rect is now Horizontal. " + rect.Rect);
			}

			// Left
			int n = 1;
			DescriptionRectsTreemap d, prevD = new DescriptionRectsTreemap();
			int fixedSide;
			if (insertionSide == RectSide.Left) {
				fixedSide = (int)rect.Height;
			} else if (insertionSide == RectSide.Top) {
				fixedSide = (int)rect.Width;
			} else {
				throw new NotImplementedException();
			}
			d = CalculateSideFilling(groups.Take(n), fixedSide);
			Debug.WriteLine("{0}: First MakeRect({1}) Waste:{2} Width:{3} AR:{4}", id, fixedSide, d.wastedSpace, d.calculatedSideLength, d.aspectRatioAverage);
			do {
				prevD = d;
				n++;
				if (n > fixedSide) {
					break;
				}
				d = CalculateSideFilling(groups.Take(n), fixedSide);
				Debug.WriteLine("{0}: MakeRect({1}/{2}) > Waste:{3} Width:{4} AR:{5}", id, n, groups.Count(), d.wastedSpace, d.calculatedSideLength, d.aspectRatioAverage);

				if (/*prevD.wastedSpace == d.wastedSpace && */prevD.aspectRatioAverage < d.aspectRatioAverage) {
					break;
				}
				/*				if (prevD.wastedSpace < d.wastedSpace) {
									break;
								}
				*/
				if (groups.Count() <= n /*|| groups.Count() <= fixedSide*/) {
					prevD = d;
					break;
				}
			} while (true);

			if (prevD.calculatedSideLength == 0) {
				Debug.WriteLine("{0}: FAIL! Width = 0!", id);
			}

			// prevD is better 
			Debug.WriteLine("{0}: Decided on {1}", id, prevD.calculatedSideLength);
			MakeRectsForGroupsToFillSide(insertionSide, groups.Take(--n), prevD.calculatedSideLength, rect);
			String acc = "";
			foreach (RectWithRects minirects in rect.Children()) {
				acc += Environment.NewLine + "      " + minirects.ToString();
			}
			Debug.WriteLine("{0}: Generated rect: {1}", id, acc);

			// Do rest
			IEnumerable<Group> restOfGroups = groups.Skip(n);
			Debug.WriteLine("{0}: Rest of groups count: {1}", id, restOfGroups.Count());

			RectWithRects rest;
			if (insertionSide == RectSide.Left) {
				if (rect.Width - prevD.calculatedSideLength > 0 && rect.Height > 0) {
					rest = new RectWithRects(prevD.calculatedSideLength, 0, rect.Width - prevD.calculatedSideLength, rect.Height);
					Debug.WriteLine("{0}: Rect for rest: {1}", id, rest.Rect);
					if (restOfGroups.Count() > 0) {
						Debug.WriteLine("{0}: starting treemap...", id);
						rest = TreeMap(restOfGroups, rest);
						Debug.WriteLine("{0}: treemap ended: {1}", id, rest);
						rect.Add(rest);
					}
				} else {
					Debug.WriteLine(id + ": Ups! Got no more space! " + restOfGroups.Count() + " groups left to display...");
					Debug.WriteLine("{0}: {1}-{2}({3}) > 0?  && {4} > 0", id, rect.Width, prevD.calculatedSideLength, rect.Width - prevD.calculatedSideLength, rect.Height);
				}
			} else if (insertionSide == RectSide.Top) {
				if (rect.Height - prevD.calculatedSideLength > 0 && rect.Width > 0) {
					rest = new RectWithRects(0, prevD.calculatedSideLength, rect.Width, rect.Height - prevD.calculatedSideLength);
					Debug.WriteLine("{0}: Rect for rest: {1}", id, rest.Rect);
					if (restOfGroups.Count() > 0) {
						Debug.WriteLine("{0}: starting treemap...", id);
						rest = TreeMap(restOfGroups, rest);
						Debug.WriteLine("{0}: treemap ended: {1}", id, rest);
						rect.Add(rest);
					}
				} else {
					Debug.WriteLine(id + ": Ups! Got no more space! " + restOfGroups.Count() + " groups left to display...");
					Debug.WriteLine("{0}: {1}-{2}({3}) > 0?  && {4} > 0", id, rect.Width, prevD.calculatedSideLength, rect.Width - prevD.calculatedSideLength, rect.Height);
				}
			} else {
				throw new NotImplementedException();
			}



			if (!originalIsHorizontal) {
				//rect.MakeVertical();
				//Debug.WriteLine("{0}: reverting back to a vertical position", id);
			}

			Debug.WriteLine("{0}: returning: {1}", id, rect.Rect);
			return rect;
		}

		private enum RectSide { Left, Top }

		private void MakeRectsForGroupsToFillSide(RectSide side, IEnumerable<Group> l, int fixedLength, RectWithRects r) {
			double position;
			position = 0;
			foreach (Group g in l) {
				if (side == RectSide.Left) {
					g.rect = new RectWithRects(0, position, fixedLength, Math.Ceiling(g.images.Count / fixedLength), g);
					position += g.rect.Height;
				} else if (side == RectSide.Top) {
					g.rect = new RectWithRects(position, 0, Math.Ceiling(g.images.Count / fixedLength), fixedLength, g);
					position += g.rect.Width;
				} else {
					throw new NotImplementedException();
				}
				r.Add(g.rectangle);
				placedGroups.Add(g);
			}
		}

		private struct DescriptionRectsTreemap {
			public double aspectRatioAverage;
			public int wastedSpace;
			public int calculatedSideLength;
		}


		private DescriptionRectsTreemap CalculateSideFilling(IEnumerable<Group> l, int fixedSide) {
			if (l.Count() == 0) {
				throw new ArgumentException("Zero Groups!");
			}
			if (fixedSide <= 0) {
				throw new ArgumentException("Invalid Height!");
			}

			double varSide = Math.Ceiling(l.Sum(g => g.images.Count) * 1.0 / fixedSide);
			double rectSide, position;
			double aspectRatioAcc = 0;
			do {
				aspectRatioAcc = 0;
				position = 0;
				foreach (Group g in l) {
					rectSide = Math.Ceiling(g.images.Count / varSide);
					position += rectSide;
					aspectRatioAcc += Math.Max(varSide / rectSide, rectSide / varSide);
					if (position > fixedSide) {
						varSide++;
						break;
					}
				}
			} while (position > fixedSide);

			DescriptionRectsTreemap ret = new DescriptionRectsTreemap();
			ret.aspectRatioAverage = aspectRatioAcc / l.Count();
			ret.wastedSpace = (int)((fixedSide - position) * varSide);
			ret.calculatedSideLength = (int)varSide;
			return ret;
		}
		#endregion //Treemap v3





		/// <summary>
		/// Image distribution using the Quantum TreeMap algorithm
		/// </summary>
		/// <param name="groups">Groups to be ordered</param>
		/// <param name="parentRect">The area where to distribute the groups</param>
		/// <returns>A RectWithRects containing all the rects and associated groups</returns>
		private RectWithRects OldTreeMap(IEnumerable<Group> groups, RectWithRects parentRect) {
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

			int smallestGroupSizeOnLTemp = Ltmp.Min(g => g.images.Count);
			int LtempCount = Ltmp.Sum(g => g.images.Count);
			List<Group> Ltmp2 = new List<Group>(), Ltmp3 = new List<Group>();

			RectWithRects StartingParent = new RectWithRects(parentRect);

			Boolean parentIsVertical = false;
			if (parentRect.Width < parentRect.Height) { // Landscape
				parentIsVertical = true;
				parentRect.MakeHorizontal();
			}

			Boolean parentChanged;

			do {
				parentChanged = false;
				Ra = new RectWithRects(0, 0, Math.Ceiling(LaCount / parentRect.Height), parentRect.Height);
				Ra = TreeMap(La, Ra);
				parentRect.AdjustSizeToFitRect(Ra);

				// Rects for Rp, Rb, Rc

				// The objective of the following is to define Rp and Rb so that both fit in the parent.
				// This is done by reducing one side of Rp until Rb can fit the smallest group destined for Rb or Rc
				int RpW, RpH = (int)parentRect.Height;
				int oldRpH = -1;
				do {
					if (RpH > 1 && RpH != oldRpH) {
						oldRpH = RpH;
						RpH--;
					} else {
						parentRect.IncreaseSize();
						RpH = (int)parentRect.Height;
					}
					CalculateDistribution(pivot.images.Count,
										  parentRect.Width / parentRect.Height,
										  new Point(parentRect.Width - Ra.Width, RpH),
										  out RpW, out RpH);
					Rp = new RectWithRects(Ra.Width, 0, RpW, RpH);
					Rb = new RectWithRects(Rp.X, Rp.Height, Rp.Width, Math.Max(1, parentRect.Height - Rp.Height));
					//parentRect.Width = Math.Max(parentRect.Width, Rp.X + Rp.Width + 1);
					if (!Rb.Fits(smallestGroupSizeOnLTemp) || !Rp.Fits(pivot.images.Count)) {
						// doesn't fit... try again with a slightly different size
						continue;
					} else {
						// at least the smallest group fits
						Lb.Clear();
						Ltmp2.Clear();
						FillWhileFits(out Lb, Rb.Width * Rb.Height, Ltmp, out Ltmp2);
						if (Lb.Count > 0) {
							Rb = TreeMap(Lb, Rb);
							parentChanged = parentRect.AdjustSizeToFitRect(Rb);
							if (parentChanged) {
								Debug.WriteLine("Rb made the parent change size");
								goto STARTOVER; // countinue on the outter loop, to start over on the Ra
							}
							// shrink Rp & Rb to the smallest size possible, if Rb wasn't really treemapped
							if (Rb.Children().Count <= 2) {
								int LbSize = Lb.Sum(g => g.images.Count);
								Rect newRb, newRp = Rp.Rect;
								while (true) {
									newRp.Width--;
									newRp.Height = Math.Ceiling(pivot.images.Count / (Rp.Width - 1));
									if (parentRect.Height - newRp.Height < 1) break;
									newRb = new Rect(newRp.X,
														newRp.Height,
														newRp.Width,
														parentRect.Height - newRp.Height);
									if (Rb.Fits(LbSize) && newRb.Y + newRb.Height <= parentRect.Height) {
										Rp.Rect = newRp;
										Rb.Rect = newRb;
										continue;
									} else {
										break;
									}
								}
							}
							break;
						} else {
							throw new Exception("This shouldn't happen! I couldn't fit any groups in Rb");
						}
					}
				} while (true); // Rp & Rb passed

				if (parentRect.Width - Ra.Width - Rp.Width < 1) {
					parentRect.IncreaseSize();
					parentChanged = true;
					continue;
				}
				// Desnecessario -------------------------------.
				Rc = new RectWithRects(Rp.X + Rp.Width, 0, Math.Max(1, parentRect.Width - Ra.Width - Rp.Width), parentRect.Height);
				if (!Rc.Fits(LtempCount - Lb.Sum(g => g.images.Count))) {
					parentRect.IncreaseSize();
					parentChanged = true;
					continue;
				}
				Lc.Clear();
				Ltmp3.Clear();
				FillWhileFits(out Lc, Rc.Width * Rc.Height, Ltmp2, out Ltmp3);
				if (Ltmp3.Count > 0) {
					parentRect.IncreaseSize();
					parentChanged = true;
					continue;
				} else {
					if (Lc.Count > 0)
						Rc = TreeMap(Lc, Rc);
					break;
				}
			STARTOVER:
				;
			} while (parentChanged);

			/////////////////////////////////

			Output(Ra, Rb, Rc, Rp, parentRect);

			// Split Ltmp in Lb and Lc
			/*			List<Group> Lrest, Lrest2;
						if (Rb != null) {
							FillWhileFits(out Lb, Rb.Width * Rb.Height, Ltmp, out Lrest);
							if (Rc != null) {
								FillWhileFits(out Lc, Rc.Width * Rc.Height, Lrest, out Lrest2);
								if (Lrest2.Count != 0) {
									groupsNotPlaced.AddRange(Lrest2);
									Debug.WriteLine("After adding stuff to Rb & Rc, " + Lrest2.Count + " groups still didn't fit.");
								}
							} else {
								//throw new Exception("2small");

								groupsNotPlaced.AddRange(Lrest);
							}
						} else if (Rc != null) {
							FillWhileFits(out Lc, Rc.Width * Rc.Height, Ltmp, out Lrest);
							//throw new Exception("2small");
							groupsNotPlaced.AddRange(Lrest);
						} else {
							// Rb && Rc don't exist.

							//throw new Exception("2small");
							groupsNotPlaced.AddRange(Ltmp);
						}
			*/
			// 	Recursively apply the Ordered Treemap algorithm to LA in R1, LB in R2, and LC in R3.
			/*Ra = TreeMap(La, Ra);
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
			}*/
			parentRect.Add(Ra);
			Rp.Group = pivot;
			placedGroups.Add(pivot);
			parentRect.Add(Rp);
			//if (Rb != null && Lb.Count > 0) {
			parentRect.Add(Rb);
			//}
			Rp.Group.rectangle = Rp;
			if (Rc != null && Lc.Count > 0) {
				parentRect.Add(Rc);
			}
			parentRect.ResizeToChildren();
			if (parentIsVertical) {
				parentRect.MakeVertical();
			}
			return parentRect;
		}
		Shape groupBorder = null;
		private Canvas groupNamesOverlay = null;
		private List<KeyValuePair<string, List<int>>> groups;
		List<Group> groupsNotPlaced = new List<Group>();
		private int imgHeight, imgWidth, imgCount;
		Dictionary<int, Group> invertedGroups = new Dictionary<int, Group>();
		Dictionary<string, Group> map = new Dictionary<string, Group>();
		private MultiScaleImage msi;
		List<Group> placedGroups = new List<Group>();
		private double pxHeight, pxWidth, aspectRatio;

		/// <summary>
		/// Used by the constructor to determine an aproximation to the rows and columns need to display the images.
		/// </summary>
		private void CalculateCanvas() {
			int cols;
			int rows;	////////////////////////////////////////\
			CalculateDistribution((int)Math.Ceiling(imgCount * 1.02), out cols, out rows);
			imgWidth = cols;
			imgHeight = rows;
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
		/// Discovers a rectangle that can hold "amount" elements. Uses the aspect ratio set in the instance.
		/// </summary>
		/// <param name="amount">The number of elements to hold</param>
		/// <param name="max">Maximum width and height values. Ignored if the amout of elements can't fit.</param>
		/// <param name="cols">Out: The number of columns</param>
		/// <param name="rows">Out: The number of rows</param>
		private void CalculateDistribution(int amount, out int cols, out int rows) {
			CalculateDistribution(amount, aspectRatio, null, out cols, out rows);
		}

		private Dictionary<string, int> DistributeGroupsVertically(List<Group> groupsNotPlaced, int height, int width) {
			int x = 0, sx = 0;
			int y = 0, sy = 0;
			double cellSide = pxWidth / width;
			//double cellSide = pxHeight / height;
			invertedGroups.Clear();
			Polygon p;
			Dictionary<string, int> canvasIndex = new Dictionary<string, int>();
			foreach (Group g in groupsNotPlaced) {
				if (y != 0 && g.images.Count > height - y) {
					x++;
					y = 0;
				}

				p = new Polygon();
				Canvas.SetTop(p, 0);
				Canvas.SetLeft(p, 0);
				p.Points.Add(new Point(x * cellSide, y * cellSide));
				sx = x;
				sy = y;

				int fx = x + g.images.Count / height;
				int fy = y + g.images.Count % height;

				foreach (int id in g.images) {
					try {
						Page.PositionImageInMSI(msi, id, x, y);
						//msi.SubImages[id].ViewportOrigin = new Point(-x, -y);
						canvasIndex.Add(x + ";" + y, id);
						invertedGroups.Add(id, g);
					} catch (Exception e) {
						Debug.WriteLine("ERRO!!!!!!!1 " + e.Message);
					}
					y++;
					if (y >= height) {
						y = 0;
						x++;
					}
				}

				if (fx != x || fy != y) {
					Debug.WriteLine("ERRO!!!!!!!1 fx=" + fx + " x=" + x + " fy=" + fy + " y=" + y);
				}

				if (y == 0) {
					p.Points.Add(new Point(x * cellSide, sy * cellSide));
				} else {
					p.Points.Add(new Point((x + 1) * cellSide, sy * cellSide));
					p.Points.Add(new Point((x + 1) * cellSide, y * cellSide));
				}
				p.Points.Add(new Point(x * cellSide, y * cellSide));

				if (x > sx) {
					p.Points.Add(new Point(x * cellSide, height * cellSide));
					p.Points.Add(new Point(sx * cellSide, height * cellSide));
				}
				g.shape = p;
				if (y != 0)
					y = (y + 1) % height;
				placedGroups.Add(g);
			}
			return canvasIndex;
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
		/// Transverses the set of groups that where not placed and moves the images out of the view.
		/// </summary>
		private void HideNotPlacedImages() {
			foreach (Group g in groupsNotPlaced) {
				foreach (int id in g.images) {
					Point p = msi.SubImages[id].ViewportOrigin;
					Page.PositionImageInMSI(msi, id, p.X, p.Y);
					msi.SubImages[id].Opacity = 0.5;
				}
			}
		}

		//public Rectangle RectangleWithPositions(int X, int Y, int W, int H) {
		//    Rectangle r = new Rectangle();

		//}


		/// <summary>
		/// Takes the set of groups and arranges them on the MSI vertically
		/// </summary>
		/// <param name="Groups"></param>
		private void OLDorderByGroupsVertically(List<Group> groupsNotPlaced, out Dictionary<String, int> canvasIndex, out int cols, out int rows) {
			List<int> groupSizes = new List<int>();
			List<string> groupNames = new List<string>();
			int total = 0;
			foreach (KeyValuePair<String, List<int>> group in this.groups) {
				groupNames.Add(group.Key);
				groupSizes.Add(group.Value.Count);
				total += group.Value.Count;
			}

			int Hcells = 1;
			int Vcells = 1;

			// Greatest group size
			Vcells = groupsNotPlaced.Max(g => g.images.Count);
			/*foreach (int row in groupSizes) {
				if (row > Vcells) {
					Vcells = row;
				}
			}*/

			// cols = group count
			//Hcells = groupSizes.Count;
			Hcells = groupsNotPlaced.Count;
			Hcells = Convert.ToInt32(Math.Floor(msi.ActualHeight * Hcells / msi.ActualWidth));
			double rowsNeeded = 0;

			while (true) {
				rowsNeeded = 0;
				// Determina o que acontece ao reduzir a largura
				foreach (int row in groupSizes) {
					rowsNeeded += Math.Ceiling(row / (Vcells - 1));
				}

				// se for viavel reduzir a largura, faz isso mesmo.
				Hcells = Convert.ToInt32(Math.Floor(msi.ActualWidth * (Vcells - 1) / msi.ActualHeight));
				if (rowsNeeded <= Hcells) {
					Vcells--;
				} else {
					Hcells = Convert.ToInt32(Math.Floor(msi.ActualWidth * Vcells / msi.ActualHeight));
					break;
				}
			}



			// put images in canvas
			double imgSize = msi.ActualHeight / Vcells;
			var x = 0.0;
			var y = 0.0;
			// era fixe guardar o anterior para repor
			canvasIndex = new Dictionary<string, int>();

			foreach (KeyValuePair<String, List<int>> group in this.groups) {
				foreach (int id in group.Value) {
					Page.PositionImageInMSI(msi, id, x, y);
					//msi.SubImages[id].ViewportOrigin = new Point(-x, -y);
					canvasIndex.Add(x + ";" + y, id);
					y++;

					if (y >= Vcells) {
						x += 1;
						y = 0.0;
					}
				}
				y = 0;
				x++;
			}

			double HcellsTmp = msi.ActualWidth * Vcells / msi.ActualHeight;
			Hcells = (int)Math.Max(Hcells, HcellsTmp);
			msi.ViewportWidth = Hcells;
			//zoom = 1;
			//ShowAllContent();

			List<KeyValuePair<string, int>> groups = new List<KeyValuePair<string, int>>();
			for (int i = 0; i < groupNames.Count; i++) {
				groups.Add(new KeyValuePair<string, int>(groupNames[i], Convert.ToInt32(Math.Ceiling(groupSizes[i] / Hcells))));
			}
			//makeAnAxis("X", groups);
			//makeAnAxis("Y", Vcells);
			cols = Hcells;
			rows = Vcells;
			return;
		}


		private void orderByGroupsVertically(List<Group> groupsNotPlaced, out Dictionary<String, int> canvasIndex, out int cols, out int rows) {
			int height = imgHeight;
			int previousHeight = -1;
			Boolean heightIsIncreasing;

			placedGroups.Clear();

			double prevPAR = 0;
			int width = TestVerticalGroupDistribution(groupsNotPlaced, height), prevWidth = 0;

			double pAR = 1.0 * width / height;

			// TODO: Melhorar isto para que a ultima linha nao fique cortada
			if (aspectRatio < pAR) {
				heightIsIncreasing = true;
			} else {
				heightIsIncreasing = false;
			}

			while (true) {
				if ((heightIsIncreasing && aspectRatio > pAR) || (!heightIsIncreasing && aspectRatio < pAR))
					break;

				if (heightIsIncreasing) height++;
				else height--;

				prevWidth = width;
				width = TestVerticalGroupDistribution(groupsNotPlaced, height);

				prevPAR = pAR;
				pAR = 1.0 * width / height;
			}

			if (Math.Abs(aspectRatio - pAR) > Math.Abs(aspectRatio - prevPAR)) {
				pAR = prevPAR;
				width = prevWidth;
				if (heightIsIncreasing) {
					height--;
				} else {
					height++;
				}
			}
			canvasIndex = DistributeGroupsVertically(groupsNotPlaced, height, width);
			cols = width; // (int)Math.Ceiling(height * pAR);
			rows = height;
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

		private static int TestVerticalGroupDistribution(List<Group> groupsNotPlaced, int height) {
			int x = 0;
			int y = 0;
			foreach (Group g in groupsNotPlaced) {
				if (y != 0 && g.images.Count > height - y) {
					x++;
					y = 0;
				}
				x += g.images.Count / height;
				y += g.images.Count % height;
				y = (y + 1) % height;
			}
			return x;
		}
	} // closes GroupDisplay
}
