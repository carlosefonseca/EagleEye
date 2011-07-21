using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;


namespace DeepZoomView {
	public class TreeMapper {
		public static RectWithRects TreeMap(IEnumerable<Group> groups, RectWithRects rect) {
            if (groups.Count() == 1 && rect.Fits(groups.First().images.Count))
            {
                rect.Group = groups.First();
                rect.Group.rectangle = rect;
                return rect;
            }

			Random r = new Random();
			int id = r.Next(100);
			Debug.WriteLine(id + ": TreeMap with " + groups.Count() + " groups on rect " + rect.Rect);

			if (!rect.Fits(groups.First().images.Count)) {
				Debug.WriteLine(id + ": !!! Group doesn't fit on the space...");
				return rect;
			}


			Boolean originalIsHorizontal = rect.isHorizontal();
			RectSide insertionSide = RectSide.Left;
			if (!originalIsHorizontal) {
				insertionSide = RectSide.Top;
				//rect.MakeHorizontal();
				//Debug.WriteLine(id + ": Rect is now Horizontal. " + rect.Rect);
			}

			// Left //////////////////////////////////////////
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
					break;	// Don't try to select more groups than available cells
				}
				d = CalculateSideFilling(groups.Take(n), fixedSide);
				Debug.WriteLine("{0}: MakeRect({1}/{2}) > Waste:{3} Width:{4} AR:{5}", id, n, groups.Count(), d.wastedSpace, d.calculatedSideLength, d.aspectRatioAverage);

				if (prevD.aspectRatioAverage < d.aspectRatioAverage) {
					break;	// If the new calculation yields a larger A.R., use the previous one.
				}

				if (groups.Count() <= n) {
					prevD = d;	// If there are no more groups, accept the current calculation.
					break;
				}
			} while (true);

			if (prevD.calculatedSideLength == 0) {
				Debug.WriteLine("{0}: FAIL! Width = 0!", id);
			}

			// prevD is better 
			Debug.WriteLine("{0}: Decided on {1}", id, prevD.calculatedSideLength);
			n--;
			// wasted space //////////////////////////////////////////
			if (prevD.wastedSpace > 0) {
				RectWithRects wastedSpaceRect = new RectWithRects(0, 0, 0, 0);
				if (insertionSide == RectSide.Left) {
					wastedSpaceRect.Width = prevD.calculatedSideLength;
					wastedSpaceRect.Height = prevD.wastedSpace / wastedSpaceRect.Width;
					wastedSpaceRect.Y = fixedSide - wastedSpaceRect.Height;
				} else if (insertionSide == RectSide.Top) {
					wastedSpaceRect.Height = prevD.calculatedSideLength;
					wastedSpaceRect.Width = prevD.wastedSpace / wastedSpaceRect.Height;
					wastedSpaceRect.X = fixedSide - wastedSpaceRect.Width;
				} else {
					throw new NotImplementedException();
				}
				wastedSpaceRect = FindGroupsForWastedSpace(groups.Skip(n), wastedSpaceRect);
				Debug.WriteLine("#### Wasted space results: {0}", wastedSpaceRect);
				// remove all groups placed on the wasted space from the current groups
				IEnumerable<Group> groupsAddedToWastedSpace = wastedSpaceRect.GetAllGroups();
				groups = groups.Except(groupsAddedToWastedSpace);
				rect.Add(wastedSpaceRect);
			}
			MakeRectsForGroupsToFillSide(insertionSide, groups.Take(n), prevD.calculatedSideLength, rect);
			String acc = "";
			foreach (RectWithRects minirects in rect.Children()) {
				acc += Environment.NewLine + "      " + minirects.ToString();
			}
			Debug.WriteLine("{0}: Generated rect: {1}", id, acc);

			// Rest //////////////////////////////////////////
			IEnumerable<Group> restOfGroups = groups.Skip(n);
			Debug.WriteLine("{0}: Rest of groups count: {1}", id, restOfGroups.Count());

			RectWithRects rest;
			if (insertionSide == RectSide.Left) {
				if (rect.Width - prevD.calculatedSideLength > 0 && rect.Height > 0) {
					rest = new RectWithRects(prevD.calculatedSideLength, 0, rect.Width - prevD.calculatedSideLength, rect.Height);
					Debug.WriteLine("{0}: Rect for rest: {1}", id, rest.Rect);
					if (restOfGroups.Count() > 0) {
						Debug.WriteLine("{0}: starting treemap on the rest...", id);
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

			Debug.WriteLine("{0}: returning: {1}", id, rect.Rect);
			return rect;
		}

		private static RectWithRects FindGroupsForWastedSpace(IEnumerable<Group> groups, RectWithRects rect) {
			int space = (int)(rect.Width * rect.Height);
			IEnumerable<Group> groupsThatFit = groups.SkipWhile(g => g.images.Count > space);
			if (groupsThatFit.Count() != 0) {
				Debug.WriteLine("#### Trying to place {0} groups on wasted space ({1})", groupsThatFit.Count(), space);
				return TreeMap(groupsThatFit, rect);
			} else {
				return rect;
			}
		}

		private enum RectSide { Left, Top }

		private static void MakeRectsForGroupsToFillSide(RectSide side, IEnumerable<Group> l, double fixedLength, RectWithRects r) {
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
				//placedGroups.Add(g);
			}
		}

		private struct DescriptionRectsTreemap {
			public double aspectRatioAverage;
			public int wastedSpace;
			public int calculatedSideLength;
		}


		private static DescriptionRectsTreemap CalculateSideFilling(IEnumerable<Group> l, int fixedSide) {
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
	}
}