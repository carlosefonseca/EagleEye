using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DeepZoomView.EECanvas;


namespace DeepZoomView.EECanvas.Dispositions
{
    public class TreeMapDisposition : Disposition
    {
        const bool DEBUG_TREEMAP = false;

        private Organizable organizable;
        private List<Group> placedGroups;
        List<Group> groupsNotPlaced = new List<Group>();

        public TreeMapDisposition() { }

        internal override void Place(MyCanvas c)
        {
            canvas = c;

			IEnumerable<Group> groups;

			if (canvas.organizable == null)
			{
				groups = new List<Group>();
				Group g = new Group("Group", canvas.items.Values.SelectMany(ci => ci.getAllIds()).ToList());
				((List<Group>)groups).Add(g);
			}
			else
			{
				organizable = canvas.organizable;

				groups = organizable.GroupList().OrderByDescending(g => g.images.Count);
			}
			int itemCount = groups.Sum(g => g.images.Count);

            double increment = 1;
            RectWithRects r = null;
            while (r == null)
            {
				CalculateDistribution((int)Math.Ceiling(itemCount * increment), out cols, out rows);
                try
                {
					r = TreeMap(groups, new RectWithRects(0, 0, cols, rows));
                    break;
                }
                catch (NotEnoughSpaceException)
                {
                    increment += 0.02;
                    Debug.WriteLine("Increment is now " + increment);
                }
            }
            cellSide = canvas.page.msi.ActualWidth / cols;
            placedGroups = new List<Group>();
            placedGroups.AddRange(r.GetAllGroups());
            PositionCorrection(r);
            PositionImages();
        }


        /// <summary>
        /// Transverses the set of groups that were correctly placed and assigns each image to a position of the MSI
        /// </summary>
        /// <param name="max">This contains the width and height of the display</param>
        /// <returns>A list of "X,Y"=>ImgID for the mouse-over identification</returns>
        public void PositionImages()
        {
            foreach (Group g in placedGroups)
            {
                int x = (int)Math.Round(g.rectangle.X);
                int y = (int)Math.Round(g.rectangle.Y);
                foreach (int id in g.images)
                {
                    canvas.SetItemPosition(id, x, y);

                    if (++x >= g.rectangle.X + g.rectangle.Width)
                    {
                        x = (int)Math.Round(g.rectangle.X);
                        y++;
                    }
                }
            }
        }
#pragma warning disable

		public static RectWithRects TreeMap(IEnumerable<Group> groups, RectWithRects rect)
		{
			return TreeMap(groups, rect, true);
		}

		public static RectWithRects TreeMap(IEnumerable<Group> groups, RectWithRects rect, Boolean throwException)
        {
            if (groups.Count() == 1 && rect.Fits(groups.First().images.Count))
            {
                rect.Group = groups.First();
                rect.Group.rectangle = rect;
                return rect;
            }

            Random r = new Random();
            int id = r.Next(100);
            if (DEBUG_TREEMAP) Debug.WriteLine(id + ": TreeMap with " + groups.Count() + " groups on rect " + rect.Rect);

            if (!rect.Fits(groups.First().images.Count))
            {
                if (DEBUG_TREEMAP) Debug.WriteLine(id + ": !!! Group doesn't fit on the space...");
                if (throwException) throw new NotEnoughSpaceException();
                return rect;
            }


            Boolean originalIsHorizontal = rect.isHorizontal();
            RectSide insertionSide = RectSide.Left;
            if (!originalIsHorizontal)
            {
                insertionSide = RectSide.Top;
                //rect.MakeHorizontal();
                //Debug.WriteLine(id + ": Rect is now Horizontal. " + rect.Rect);
            }

            // Left //////////////////////////////////////////
            int n = 1;
            DescriptionRectsTreemap d, prevD = new DescriptionRectsTreemap();
            int fixedSide;
            if (insertionSide == RectSide.Left)
            {
                fixedSide = (int)rect.Height;
            }
            else if (insertionSide == RectSide.Top)
            {
                fixedSide = (int)rect.Width;
            }
            else
            {
                throw new NotImplementedException();
            }
			
            d = CalculateSideFilling(groups.Take(n), fixedSide);
            if (DEBUG_TREEMAP) Debug.WriteLine("{0}: First MakeRect({1}) Waste:{2} Width:{3} AR:{4}", id, fixedSide, d.wastedSpace, d.calculatedSideLength, d.aspectRatioAverage);
            do
            {
                prevD = d;
                n++;
                if (n > fixedSide)
                {
                    break;	// Don't try to select more groups than available cells
                }
                d = CalculateSideFilling(groups.Take(n), fixedSide);
                if (DEBUG_TREEMAP) Debug.WriteLine("{0}: MakeRect({1}/{2}) > Waste:{3} Width:{4} AR:{5}", id, n, groups.Count(), d.wastedSpace, d.calculatedSideLength, d.aspectRatioAverage);

//				if (Math.Abs(rect.AspectRatio - prevD.aspectRatioAverage) < Math.Abs(rect.AspectRatio - d.aspectRatioAverage))
                if (prevD.aspectRatioAverage < d.aspectRatioAverage)
                {
                    break;	// If the new calculation yields a larger A.R., use the previous one.
                }

                if (groups.Count() <= n)
                {
                    prevD = d;	// If there are no more groups, accept the current calculation.
                    break;
                }
            } while (true);

            if (prevD.calculatedSideLength == 0)
            {
                if (DEBUG_TREEMAP) Debug.WriteLine("{0}: FAIL! Width = 0!", id);
            }

            // prevD is better 
            if (DEBUG_TREEMAP) Debug.WriteLine("{0}: Decided on {1}", id, prevD.calculatedSideLength);
            n--;
            // wasted space //////////////////////////////////////////
            if (prevD.wastedSpace > 0)
            {
                RectWithRects wastedSpaceRect = new RectWithRects(0, 0, 0, 0);
                if (insertionSide == RectSide.Left)
                {
                    wastedSpaceRect.Width = prevD.calculatedSideLength;
                    wastedSpaceRect.Height = prevD.wastedSpace / wastedSpaceRect.Width;
                    wastedSpaceRect.Y = fixedSide - wastedSpaceRect.Height;
                }
                else if (insertionSide == RectSide.Top)
                {
                    wastedSpaceRect.Height = prevD.calculatedSideLength;
                    wastedSpaceRect.Width = prevD.wastedSpace / wastedSpaceRect.Height;
                    wastedSpaceRect.X = fixedSide - wastedSpaceRect.Width;
                }
                else
                {
                    throw new NotImplementedException();
                }
                wastedSpaceRect = FindGroupsForWastedSpace(groups.Skip(n), wastedSpaceRect);
                if (DEBUG_TREEMAP) Debug.WriteLine("#### Wasted space results: {0}", wastedSpaceRect);
                // remove all groups placed on the wasted space from the current groups
                IEnumerable<Group> groupsAddedToWastedSpace = wastedSpaceRect.GetAllGroups();
                groups = groups.Except(groupsAddedToWastedSpace);
                rect.Add(wastedSpaceRect);
            }
            MakeRectsForGroupsToFillSide(insertionSide, groups.Take(n), prevD.calculatedSideLength, rect);
            String acc = "";
            foreach (RectWithRects minirects in rect.Children())
            {
                acc += Environment.NewLine + "      " + minirects.ToString();
            }
            if (DEBUG_TREEMAP) Debug.WriteLine("{0}: Generated rect: {1}", id, acc);

            // Rest //////////////////////////////////////////
            IEnumerable<Group> restOfGroups = groups.Skip(n);
            if (DEBUG_TREEMAP) Debug.WriteLine("{0}: Rest of groups count: {1}", id, restOfGroups.Count());

            RectWithRects rest;
            if (insertionSide == RectSide.Left)
            {
                if (rect.Width - prevD.calculatedSideLength > 0 && rect.Height > 0)
                {
                    rest = new RectWithRects(prevD.calculatedSideLength, 0, rect.Width - prevD.calculatedSideLength, rect.Height);
                    if (DEBUG_TREEMAP) Debug.WriteLine("{0}: Rect for rest: {1}", id, rest.Rect);
                    if (restOfGroups.Count() > 0)
                    {
                        if (DEBUG_TREEMAP) Debug.WriteLine("{0}: starting treemap on the rest...", id);
                        rest = TreeMap(restOfGroups, rest, throwException);
                        if (DEBUG_TREEMAP) Debug.WriteLine("{0}: treemap ended: {1}", id, rest);
                        rect.Add(rest);
                    }
                }
                else
                {
                    if (DEBUG_TREEMAP) Debug.WriteLine(id + ": Ups! Got no more space! " + restOfGroups.Count() + " groups left to display...");
                    if (DEBUG_TREEMAP) Debug.WriteLine("{0}: {1}-{2}({3}) > 0?  && {4} > 0", id, rect.Width, prevD.calculatedSideLength, rect.Width - prevD.calculatedSideLength, rect.Height);
                }
            }
            else if (insertionSide == RectSide.Top)
            {
                if (rect.Height - prevD.calculatedSideLength > 0 && rect.Width > 0)
                {
                    rest = new RectWithRects(0, prevD.calculatedSideLength, rect.Width, rect.Height - prevD.calculatedSideLength);
                    if (DEBUG_TREEMAP) Debug.WriteLine("{0}: Rect for rest: {1}", id, rest.Rect);
                    if (restOfGroups.Count() > 0)
                    {
                        if (DEBUG_TREEMAP) Debug.WriteLine("{0}: starting treemap...", id);
                        rest = TreeMap(restOfGroups, rest, throwException);
                        if (DEBUG_TREEMAP) Debug.WriteLine("{0}: treemap ended: {1}", id, rest);
                        rect.Add(rest);
                    }
                }
                else
                {
                    if (DEBUG_TREEMAP) Debug.WriteLine(id + ": Ups! Got no more space! " + restOfGroups.Count() + " groups left to display...");
                    if (DEBUG_TREEMAP) Debug.WriteLine("{0}: {1}-{2}({3}) > 0?  && {4} > 0", id, rect.Width, prevD.calculatedSideLength, rect.Width - prevD.calculatedSideLength, rect.Height);
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            if (DEBUG_TREEMAP) Debug.WriteLine("{0}: returning: {1}", id, rect.Rect);
            return rect;
        }

        private static RectWithRects FindGroupsForWastedSpace(IEnumerable<Group> groups, RectWithRects rect)
        {
            int space = (int)(rect.Width * rect.Height);
            IEnumerable<Group> groupsThatFit = groups.SkipWhile(g => g.images.Count > space);
            if (groupsThatFit.Count() != 0)
            {
                if (DEBUG_TREEMAP) Debug.WriteLine("#### Trying to place {0} groups on wasted space ({1})", groupsThatFit.Count(), space);
                return TreeMap(groupsThatFit, rect, false);
            }
            else
            {
                return rect;
            }
        }
#pragma warning restore

        private enum RectSide { Left, Top }

        private static void MakeRectsForGroupsToFillSide(RectSide side, IEnumerable<Group> l, double fixedLength, RectWithRects r)
        {
            double position;
            position = 0;
            foreach (Group g in l)
            {
                if (side == RectSide.Left)
                {
                    g.rect = new RectWithRects(0, position, fixedLength, Math.Ceiling(g.images.Count / fixedLength), g);
                    position += g.rect.Height;
                }
                else if (side == RectSide.Top)
                {
                    g.rect = new RectWithRects(position, 0, Math.Ceiling(g.images.Count / fixedLength), fixedLength, g);
                    position += g.rect.Width;
                }
                else
                {
                    throw new NotImplementedException();
                }
                r.Add(g.rectangle);
                //placedGroups.Add(g);
            }
        }

        private struct DescriptionRectsTreemap
        {
            public double aspectRatioAverage;
            public int wastedSpace;
            public int calculatedSideLength;
        }


        private static DescriptionRectsTreemap CalculateSideFilling(IEnumerable<Group> l, int fixedSide)
        {
            if (l.Count() == 0)
            {
                throw new ArgumentException("Zero Groups!");
            }
            if (fixedSide <= 0)
            {
                throw new ArgumentException("Invalid Height!");
            }

            double varSide = Math.Ceiling(l.Sum(g => g.images.Count) * 1.0 / fixedSide);
            double rectSide, position;
            double aspectRatioAcc = 0;
            do
            {
                aspectRatioAcc = 0;
                position = 0;
                foreach (Group g in l)
                {
                    rectSide = Math.Ceiling(g.images.Count / varSide);
                    position += rectSide;
                    aspectRatioAcc += Math.Max(varSide / rectSide, rectSide / varSide);
                    if (position > fixedSide)
                    {
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
        /// <summary>
        /// Changes the positions of child rects to be related to the outter rect instead of its parent.
        /// </summary>
        /// <param name="node">The root Rect whose childs need fixing.</param>
        private void PositionCorrection(RectWithRects node)
        {
            foreach (RectWithRects r in node.Children())
            {
                r.X += node.X;
                r.Y += node.Y;
                if (r.isLeaf())
                {
                    r.Group.rectangle = r;
                }
                else
                {
                    PositionCorrection(r);
                }
            }
        }

        internal override Overlays MakeOverlays()
        {
            Overlays overlays = new Overlays();

            Random rand = new Random();
            Rectangle shape;
            Rect bounds;
            Color c;

            if (placedGroups == null)
            {
                return overlays;
            }

            foreach (Group g in placedGroups)
            {
                c = ColorFromName(g.name);
                bounds = g.rectangle.Rect;
                shape = new Rectangle();
                shape.Width = bounds.Width * cellSide;
                shape.Height = bounds.Height * cellSide;
                Canvas.SetLeft(shape, bounds.X * cellSide);
                Canvas.SetTop(shape, bounds.Y * cellSide);

				Overlay overlay;
				if (organizable.Name.CompareTo("Color") == 0)
				{
					overlay = new Overlay(ColorUtils.HslColor.FromColor(c).Name, shape, c);
				}
				else
				{
					overlay = new Overlay(g.name, shape, c);
				}
				overlays.AddOverlay(overlay, g.images);
            }


            return overlays;
        }

        public override string ToString()
        {
            return "TreeMapDisposition";
        }
    }
}