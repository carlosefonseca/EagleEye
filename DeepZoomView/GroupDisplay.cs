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

namespace DeepZoomView
{
    /// <summary>
    /// Provides a TreeMap-sort-of-like view of a set of grouped images
    /// </summary>
    public class GroupDisplay
    {
        public String Display;
        public static List<String> DisplayOptions = new List<string>() { "Groups", "Linear" };


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
        public Dictionary<int, List<int>> stacks;
        public Page page;


        /// <summary>
        /// Creates a new GroupDisplay
        /// </summary>
        /// <param name="msi">The MSI where the images should be displayed</param>
        /// <param name="groups">The Groups to display</param>
        public GroupDisplay(MultiScaleImage msi, List<KeyValuePair<string, List<int>>> groups, Page page)
        {
            this.msi = msi;
            this.groups = groups;
            this.page = page;

            pxHeight = msi.ActualHeight;
            pxWidth = msi.ActualWidth;
            aspectRatio = pxWidth / pxHeight;
            imgCount = msi.SubImages.Count;
            CalculateCanvas();
        }

        /// <summary>
        /// Creates a new GroupDisplay
        /// </summary>
        /// <param name="msi">The MSI where the images should be displayed</param>
        /// <param name="groups">The Groups to display</param>
        public GroupDisplay(MultiScaleImage msi, List<KeyValuePair<string, List<int>>> groups, int imgC, Page page)
        {
            this.msi = msi;
            this.groups = groups;
            this.page = page;

            pxHeight = msi.ActualHeight;
            pxWidth = msi.ActualWidth;
            aspectRatio = pxWidth / pxHeight;
            imgCount = imgC;
            CalculateCanvas();
        }


        /// <summary>
        /// Takes the set of groups and arranges them on the MSI using a the Quantum TreeMap algorithm
        /// </summary>
        /// <param name="max">This contains the width and height of the display</param>
        /// <returns>A list of "X,Y"=>ImgID for the mouse-over identification</returns>
        public Dictionary<string, int> DisplayGroupsOnScreen(out Point max)
        {
            groupNamesOverlay = null;
            Dictionary<string, int> canvasIndex = null;

            groupsNotPlaced.Clear();
            placedGroups.Clear();
            invertedGroups.Clear();
            if (groupBorder != null && groupBorder.Parent != null)
            {
                ((Canvas)groupBorder.Parent).Children.Remove(groupBorder);
                groupBorder = null;
            }
            if (Display == "Linear")
            {
                IOrderedEnumerable<KeyValuePair<string, List<int>>> orderedGroup = groups.OrderBy(kv => kv.Key);
                foreach (KeyValuePair<string, List<int>> kv in orderedGroup)
                {
                    Group g = new Group(kv.Key, kv.Value);
                    groupsNotPlaced.Add(g);
                }
                Group.DisplayType = Display;
                int cols, rows;
                orderByGroupsVertically(groupsNotPlaced, out canvasIndex, out cols, out rows);
                max = new Point(cols, rows);
            }
            else if (Display == "Groups")
            {
                IOrderedEnumerable<KeyValuePair<string, List<int>>> orderedGroup = groups.OrderByDescending(kv => kv.Value.Count);
                foreach (KeyValuePair<string, List<int>> kv in orderedGroup)
                {
                    Group g = new Group(kv.Key, kv.Value);
                    groupsNotPlaced.Add(g);
                }
                Group.DisplayType = Display;
                RectWithRects result = TreeMapper.TreeMap(groupsNotPlaced, new RectWithRects(0, 0, imgWidth, imgHeight));
                placedGroups.AddRange(result.GetAllGroups());
                Debug.WriteLine(result.TreeView2());
                PositionCorrection(result);
                groupsNotPlaced = groupsNotPlaced.Except(placedGroups).ToList();
                HideNotPlacedImages();
                canvasIndex = PositionImages(out max);
            }
            else
            {
                throw new Exception("Incorrect display method");
            }
            //max.X = Math.Max(max.Y * aspectRatio, max.X);
            return canvasIndex;
        }

        public static Polygon DuplicatePolygon(Polygon o)
        {
            Polygon newP = new Polygon();
            foreach (Point p in o.Points)
            {
                newP.Points.Add(p);
            }
            return newP;
        }


        public List<KeyValuePair<string, int>> GetGroupsForAxis(int cols)
        {
            List<KeyValuePair<string, int>> theSet = new List<KeyValuePair<string, int>>();
            foreach (KeyValuePair<string, List<int>> g in groups)
            {
                theSet.Add(new KeyValuePair<string, int>(g.Key, Convert.ToInt32(Math.Ceiling(g.Value.Count / cols))));
            }
            return theSet;
        }

        /// <summary>
        /// Transverses the set of groups that were correctly placed and assigns each image to a position of the MSI
        /// </summary>
        /// <param name="max">This contains the width and height of the display</param>
        /// <returns>A list of "X,Y"=>ImgID for the mouse-over identification</returns>
        public Dictionary<string, int> PositionImages(out Point max)
        {
            invertedGroups.Clear();
            Dictionary<string, int> positions = new Dictionary<string, int>();
            max = new Point(0, 0);
            foreach (Group g in placedGroups)
            {
                int x = (int)Math.Round(g.rectangle.X);
                int y = (int)Math.Round(g.rectangle.Y);
                foreach (int id in g.images)
                {
                    if (!positions.ContainsKey(x + ";" + y))
                    {
                        if (id < 0)
                        {
                            Debug.WriteLine(id + " -> " + stacks[id].Count() + " [{0},{1}]", x, y);
                            StackImagePosition(id, x, y);
                        }
                        else
                        {
                            //Debug.WriteLine(id);
                            //positions.Add(x + ";" + y, id);
                            invertedGroups.Add(id, g);
                            try
                            {
                                page.PositionImageInMSI(msi, id, x, y);
                                //msi.SubImages[id].ViewportOrigin = new Point(-x, -y);
                                //msi.SubImages[id].Opacity = 0.5;
                            }
                            catch
                            {
                                //g.images.Remove(id);
                                Debug.WriteLine("On PositionImages, id " + id + " was not found on msi (which contains " + msi.SubImages.Count + ")");
                                continue;
                            }
                        }
                        max = new Point(Math.Max(max.X, x), Math.Max(max.Y, y));
                    }
                    if (++x >= g.rectangle.X + g.rectangle.Width)
                    {
                        x = (int)Math.Round(g.rectangle.X);
                        y++;
                    }
                }
            }
            if ((int)(max.X / aspectRatio) < max.Y)
            {
                msi.ViewportWidth = max.Y * aspectRatio;
            }
            else
            {
                msi.ViewportWidth = max.X;
            }
            max.X++;
            max.Y++;
            imgWidth = (int)max.X;
            imgHeight = (int)max.Y;
            Debug.WriteLine("Position ended");
            return positions;
        }
        public Canvas overlays;
        private int StackImagePosition(int id, int x, int y)
        {
            List<int> ids = stacks[id];

            int first = ids.First();
            List<int> rest = ids.Skip(1).ToList();

            double ar = msi.SubImages[first].AspectRatio;
            // msi.SubImages[first].Opacity = 0.7;
            page.PositionImageInMSI(msi, stacks[id].First(), x, y, Math.Max(1.0001, 1 / ar));

            if (ar > 1)
            {
                StackingImagesOnBottom(x, y, rest, ar);
            }
            else
            {
                StackingImagesOnRight(x, y, rest, ar);
            }
            return first;
        }


        private const double stackSpace = 0.03;

        // Landscape  longside: x-axis ; shortside: y-axis
        private void StackingImagesOnBottom(int x, int y, List<int> rest, double ar)
        {
            int nLines;
            int itemsPerLine;
            double longSide;
            double shortSide;
            double space;
            double spaceForStack = 1 - 1 / ar;
            double items = 3.0;
            do
            {
                items++;
                if (items > rest.Count * 10)
                {
                    throw new Exception("Fail...");
                }
                nLines = (int)Math.Ceiling(rest.Count() / items);
                itemsPerLine = (int)Math.Ceiling(rest.Count() * 1.0 / nLines);
                longSide = Math.Min((1 - stackSpace) / 3, (1 - stackSpace) / itemsPerLine);
                shortSide = Math.Min(spaceForStack, longSide / msi.SubImages[rest.First()].AspectRatio);
                space = (itemsPerLine == 1 ? 0 : (stackSpace / (itemsPerLine - 1)));
            } while (shortSide * nLines + ((nLines - 1) * space) > spaceForStack);

            double cellLongSide, cellShortSide;
            cellLongSide = x;
            cellShortSide = y + 1 - shortSide - (spaceForStack - shortSide * nLines - space * (nLines - 1)) / 2;
            foreach (int i in rest)
            {
                msi.SubImages[i].Opacity = 1;
                // TODO: what if image is in diferent orientation?
                page.PositionImageInMSI(msi, i, cellLongSide, cellShortSide, 1 / longSide);
                cellLongSide += longSide + space;
                if (cellLongSide > x + 1)
                {
                    cellLongSide = x;
                    cellShortSide -= shortSide + space;
                }
            }
        }

        private void oldStackingImagesOnBottom(int x, int y, List<int> rest, double ar)
        {
            int longSide = (int)Math.Ceiling(rest.Count() / 6.0);
            int shortSide = (int)Math.Ceiling(rest.Count() * 1.0 / longSide);
            double side;
            if (ar > 1)
            {
                side = Math.Min((1 - (1 / ar)) * ar, 0.9 / shortSide);
            }
            else
            {
                side = (1 - stackSpace) / shortSide;
            }
            double space = (shortSide == 1 ? 0 : (stackSpace / (shortSide - 1)));
            double cellLongSide, cellShortSide;
            if (ar > 1)
            { // Landscape  longside: x-axis ; shortside: y-axis
                cellLongSide = x;
                cellShortSide = y + 1 - (side / ar);
            }
            else
            { // Portrait  longside: y-axis ; shortside: x-axis
                cellLongSide = y;
                cellShortSide = x + 1 - (side / ar);
            }
            foreach (int i in rest)
            {
                msi.SubImages[i].Opacity = 1;
                if (ar > 1)
                { // Landscape  longside: x-axis ; shortside: y-axis
                    //msi.SubImages[i].Opacity = 0.3;
                    page.PositionImageInMSI(msi, i, cellLongSide, cellShortSide, 1 / side);
                }
                else
                { // Portrait  longside: y-axis ; shortside: x-axis
                    page.PositionImageInMSI(msi, i, cellShortSide, cellLongSide, 1 / side);
                }
                cellLongSide += side + space;
                if (cellLongSide > x + 1)
                {
                    cellLongSide = x;
                    cellShortSide -= side / ar;
                }
            }
        }

        // Portrait  longside: y-axis ; shortside: x-axis
        private void StackingImagesOnRight(int x, int y, List<int> rest, double ar)
        {
            int nLines;
            int itemsPerLine;
            double longSide;
            double shortSide;
            double space;
            double spaceForStack = 1 - ar;
            double items = 3.0;
            do
            {
                items++;
                if (items > rest.Count * 10)
                {
                    throw new Exception("Fail...");
                }
                nLines = (int)Math.Ceiling(rest.Count() / items);
                itemsPerLine = (int)Math.Ceiling(rest.Count() * 1.0 / nLines);
                longSide = Math.Min((1 - stackSpace) / 3, (1 - stackSpace) / itemsPerLine);
                shortSide = Math.Min(spaceForStack, longSide * msi.SubImages[rest.First()].AspectRatio);
                space = (itemsPerLine == 1 ? 0 : (stackSpace / (itemsPerLine - 1)));
            } while (shortSide * nLines + ((nLines - 1) * space) > spaceForStack);

            double cellLongSide, cellShortSide;
            cellLongSide = y;
            cellShortSide = x + 1 - shortSide - (spaceForStack - shortSide * nLines - space * (nLines - 1)) / 2;
            foreach (int i in rest)
            {
                msi.SubImages[i].Opacity = 1;
                // TODO: what if image is in diferent orientation?
                page.PositionImageInMSI(msi, i, cellShortSide, cellLongSide, 1 / shortSide);
                cellLongSide += longSide + space;
                if (cellLongSide > y + 1)
                {
                    cellLongSide = y;
                    cellShortSide -= shortSide + space;
                }
            }
        }

        public static void SetFrameworkElementBoundsFromOther(FrameworkElement e, FrameworkElement r)
        {
            Canvas.SetLeft(e, Canvas.GetLeft(r));
            Canvas.SetTop(e, Canvas.GetTop(r));
            e.Width = r.Width;
            e.Height = r.Height;
        }
        public static void SetFrameworkElementBoundsFromRect(FrameworkElement e, Rect r)
        {
            SetFrameworkElementBoundsFromRect(e, r, 1.0);
        }

        public static void SetFrameworkElementBoundsFromRect(FrameworkElement e, Rect r, double multiplier)
        {
            Canvas.SetLeft(e, r.X * multiplier);
            Canvas.SetTop(e, r.Y * multiplier);
            e.Width = r.Width * multiplier;
            e.Height = r.Height * multiplier;
        }


        public void SetGroupNamesOverlay(Canvas destination)
        {
            if (groupNamesOverlay == null)
            {
                groupNamesOverlay = new Canvas();
                groupNamesOverlay.Width = this.pxWidth;

                Border border;
                Polygon pBorder;
                Rect bounds;
                TextBlock txt;
                Random rand = new Random();
                double cellSide = pxWidth / imgWidth;

                foreach (Group g in placedGroups)
                {
                    txt = new TextBlock();
                    txt.Text = g.name;
                    txt.TextAlignment = TextAlignment.Center;
                    txt.TextWrapping = TextWrapping.Wrap;
                    txt.VerticalAlignment = VerticalAlignment.Center;
                    txt.FontWeight = FontWeights.Bold;
                    txt.Foreground = new SolidColorBrush(Colors.White);
                    if (Group.DisplayType == "Groups")
                    {
                        border = new Border();
                        bounds = g.rectangle.Rect;
                        border.Background = new SolidColorBrush(Color.FromArgb((byte)150, (byte)rand.Next(255), (byte)rand.Next(255), (byte)rand.Next(255)));
                        border.Width = bounds.Width * cellSide;
                        border.Height = bounds.Height * cellSide;
                        Canvas.SetLeft(border, bounds.X * cellSide);
                        Canvas.SetTop(border, bounds.Y * cellSide);
                        border.Child = txt;
                        groupNamesOverlay.Children.Add(border);
                    }
                    else if (Group.DisplayType == "Linear")
                    {
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
        public void ShowGroupBorderFromImg(int img, Canvas element)
        {
            if (!invertedGroups.ContainsKey(img)) return;

            //			element.Children.Remove(groupBorder);
            groupBorder = (Shape)element.Children.FirstOrDefault(x => (((String)x.GetValue(Canvas.TagProperty)) == "Group"));

            double cellHeight = pxHeight / imgHeight;
            double cellWidth = pxWidth / imgWidth;
            cellHeight = cellWidth;
            element.Children.Remove(groupBorder);
            Group g = invertedGroups[img];
            if (Display == "Linear")
            {

                g.shape.Stroke = new SolidColorBrush(Colors.White);
                g.shape.StrokeThickness = 1.0;
                g.shape.Tag = "Group";
                element.Children.Add(g.shape);
            }
            else if (Display == "Groups")
            {
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

            if (Display != "Groups")
                return;

            List<UIElement> toRemove = element.Children.Where(x => (String)x.GetValue(Canvas.TagProperty) == "ParentGroup").ToList();
            foreach (UIElement e in toRemove)
            {
                element.Children.Remove(e);
            }

            Color[] cs = new Color[] { Colors.Black, Colors.Blue, Colors.Cyan, Colors.Green, Colors.Yellow, Colors.Orange, Colors.Red, Colors.Magenta, Colors.Purple, Colors.Brown };
            RectWithRects p = g.rectangle.Parent;
            int n = 1;
            while (p != null)
            {
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


        /// <summary>
        /// For debugging pourposes. Outputs the Rects as a set of Applescript properties.
        /// </summary>
        /// <param name="Ra"></param>
        /// <param name="Rb"></param>
        /// <param name="Rc"></param>
        /// <param name="Rp"></param>
        /// <param name="P"></param>
        internal void Output(RectWithRects Ra, RectWithRects Rb, RectWithRects Rc, RectWithRects Rp, RectWithRects P)
        {
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
        /// Used by the constructor to determine an aproximation to the rows and columns need to display the images.
        /// </summary>
        private void CalculateCanvas()
        {
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
        private void CalculateDistribution(int amount, double aR, out int cols, out int rows)
        {
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
        private void CalculateDistribution(int amount, double aR, Point? max, out int cols, out int rows)
        {
            int canHold = 1;
            cols = 1;
            rows = 1;
            while (canHold < amount)
            {
                if (!max.HasValue)
                {
                    cols++;
                    rows = Convert.ToInt32(Math.Floor(cols / aR));
                }
                else
                {
                    if (cols < max.Value.X)
                    {
                        cols++;
                        rows = Convert.ToInt32(Math.Min(Math.Floor(cols / aR), max.Value.Y));
                    }
                    else if (rows < max.Value.Y)
                    {
                        rows++;
                    }
                    else
                    {
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
        private void CalculateDistribution(int amount, out int cols, out int rows)
        {
            CalculateDistribution(amount, aspectRatio, null, out cols, out rows);
        }

        private Dictionary<string, int> DistributeGroupsVertically(List<Group> groupsNotPlaced, int height, int width)
        {
            int x = 0, sx = 0;
            int y = 0, sy = 0;
            double cellSide = pxWidth / width;
            //double cellSide = pxHeight / height;
            invertedGroups.Clear();
            Polygon p;
            Dictionary<string, int> canvasIndex = new Dictionary<string, int>();
            foreach (Group g in groupsNotPlaced)
            {
                if (y != 0 && g.images.Count > height - y)
                {
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

                foreach (int id in g.images)
                {
                    try
                    {
                        if (id < 0)
                        {
                            invertedGroups.Add(StackImagePosition(id, x, y), g);
                        }
                        else
                        {
                            page.PositionImageInMSI(msi, id, x, y);
                            invertedGroups.Add(id, g);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("ERRO!!!!!!!1 " + e.Message);
                    }
                    y++;
                    if (y >= height)
                    {
                        y = 0;
                        x++;
                    }
                }

                if (fx != x || fy != y)
                {
                    Debug.WriteLine("ERRO!!!!!!!1 fx=" + fx + " x=" + x + " fy=" + fy + " y=" + y);
                }

                if (y == 0)
                {
                    p.Points.Add(new Point(x * cellSide, sy * cellSide));
                }
                else
                {
                    p.Points.Add(new Point((x + 1) * cellSide, sy * cellSide));
                    p.Points.Add(new Point((x + 1) * cellSide, y * cellSide));
                }
                p.Points.Add(new Point(x * cellSide, y * cellSide));

                if (x > sx)
                {
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
        /// Transverses the set of groups that where not placed and moves the images out of the view.
        /// </summary>
        private void HideNotPlacedImages()
        {
            foreach (Group g in groupsNotPlaced)
            {
                foreach (int id in g.images)
                {
                    Point p = msi.SubImages[id].ViewportOrigin;
                    page.PositionImageInMSI(msi, id, p.X, p.Y);
                    msi.SubImages[id].Opacity = 0.5;
                }
            }
        }



        private void orderByGroupsVertically(List<Group> groupsNotPlaced, out Dictionary<String, int> canvasIndex, out int cols, out int rows)
        {
            int height = imgHeight;
            Boolean heightIsIncreasing;

            placedGroups.Clear();

            double prevPAR = 0;
            int width = TestVerticalGroupDistribution(groupsNotPlaced, height), prevWidth = 0;

            double pAR = 1.0 * width / height;

            // TODO: Melhorar isto para que a ultima linha nao fique cortada
            if (aspectRatio < pAR)
            {
                heightIsIncreasing = true;
            }
            else
            {
                heightIsIncreasing = false;
            }

            while (true)
            {
                if ((heightIsIncreasing && aspectRatio > pAR) || (!heightIsIncreasing && aspectRatio < pAR))
                    break;

                if (heightIsIncreasing) height++;
                else height--;

                prevWidth = width;
                width = TestVerticalGroupDistribution(groupsNotPlaced, height);

                prevPAR = pAR;
                pAR = 1.0 * width / height;
            }

            if (Math.Abs(aspectRatio - pAR) > Math.Abs(aspectRatio - prevPAR))
            {
                pAR = prevPAR;
                width = prevWidth;
                if (heightIsIncreasing)
                {
                    height--;
                }
                else
                {
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
        private String p(int x, int y)
        {
            return x + "-" + y;
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

        private static int TestVerticalGroupDistribution(List<Group> groupsNotPlaced, int height)
        {
            int x = 0;
            int y = 0;
            foreach (Group g in groupsNotPlaced)
            {
                if (y != 0 && g.images.Count > height - y)
                {
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
