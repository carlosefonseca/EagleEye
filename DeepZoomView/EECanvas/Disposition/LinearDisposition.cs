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
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;

namespace DeepZoomView.EECanvas.Dispositions
{
    public class LienarDisposition : Disposition
    {
        private Organizable organizable;
        private List<Group> placedGroups = new List<Group>();
        List<Group> groupsNotPlaced = new List<Group>();
        private Overlays overlays = new Overlays();

        public LienarDisposition() { }

        internal override void Place(MyCanvas c)
        {
            canvas = c;
            organizable = canvas.organizable;

            CalculateDistribution(organizable.ItemCount, out cols, out rows);

            //IOrderedEnumerable<KeyValuePair<string, List<int>>> orderedGroup = groups.OrderBy(kv => kv.Key);
            List<Group> orderedGroup = organizable.GroupList();

            orderByGroupsVertically(orderedGroup);


            /*
            canvas = c;
            double ar = canvas.page.msi.ActualWidth / canvas.page.msi.ActualHeight;
            base.CalculateDistribution(canvas.items.Count, ar, null, out cols, out rows);

            var x = 0.0;
            var y = 0.0;

            foreach (KeyValuePair<int, CanvasItem> kv in canvas.items)
            {
                canvas.SetItemPosition(kv.Key, x, y);
                x += 1;

                if (x >= cols)
                {
                    y += 1;
                    x = 0.0;
                }
            }*/
        }

        private void orderByGroupsVertically(List<Group> groupsNotPlaced)
        {
            int height = (int)rows;
            Boolean heightIsIncreasing;

            placedGroups.Clear();

            double prevPAR = 0;
            int width = TestVerticalGroupDistribution(groupsNotPlaced, height);
            int prevWidth = 0;

            double pAR = 1.0 * width / height;

            // TODO: Melhorar isto para que a ultima linha nao fique cortada
            if (canvasAspectRatio < pAR)
            {
                heightIsIncreasing = true;
            }
            else
            {
                heightIsIncreasing = false;
            }

            while (true)
            {
                if ((heightIsIncreasing && canvasAspectRatio > pAR) || (!heightIsIncreasing && canvasAspectRatio < pAR))
                    break;

                if (heightIsIncreasing) height++;
                else height--;

                prevWidth = width;
                width = TestVerticalGroupDistribution(groupsNotPlaced, height);

                prevPAR = pAR;
                pAR = 1.0 * width / height;
            }

            if (Math.Abs(canvasAspectRatio - pAR) > Math.Abs(canvasAspectRatio - prevPAR))
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
            DistributeGroupsVertically(groupsNotPlaced, height, width);
            cols = width; // (int)Math.Ceiling(height * pAR);
            rows = height;
        }


        private static int TestVerticalGroupDistribution(List<Group> groupsNotPlaced, int height)
        {
            Debug.WriteLine("TEST");
            int x = 0, sx = 0;
            int y = 0, sy = 0;
            foreach (Group g in groupsNotPlaced)
            {
                if (y != 0 && g.images.Count > height - y)
                {
                    x++;
                    y = 0;
                }
                sx = x;
                sy = y;
                x += (int)Math.Ceiling(g.images.Count / height);
                y += (int)Math.Ceiling(g.images.Count % height);

                Debug.WriteLine("Testing: Group: {0} : {1},{2} - {3},{4}", g.name, sx, sy, x, y);

                if (y != 0)
                    y++;
            }
            return x;
        }

        private Dictionary<string, int> DistributeGroupsVertically(List<Group> groupsNotPlaced, int height, int width)
        {
            Random rand = new Random();
            int x = 0, sx = 0;
            int y = 0, sy = 0;
            width++;
            cellSide = canvas.page.msi.ActualWidth / (width);
            Debug.WriteLine("Predicted Width: " + width);
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

                //int fx = x + (int)Math.Ceiling(g.images.Count * 1.0 / height);
                //int fy = y + (int)Math.Ceiling(g.images.Count * 1.0 % height);

                foreach (int id in g.images)
                {
                    canvas.SetItemPosition(id, x, y);
                    y++;
                    if (y >= height)
                    {
                        y = 0;
                        x++;
                    }
                }

                //if (fx != x || fy != y)
                //{
                //    Debug.WriteLine("ERRO!!!!!!!1 fx=" + fx + " x=" + x + " fy=" + fy + " y=" + y);
                //}

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
                Color c = Color.FromArgb((byte)150, (byte)rand.Next(255), (byte)rand.Next(255), (byte)rand.Next(255));
                Overlay overlay = new Overlay(g.name, p, ColorFromName(g.name));
                //g.shape = p;
                overlays.AddOverlay(overlay, g.images);
                Debug.WriteLine("Real: Group: {0} : {1},{2} - {3},{4}", g.name, sx, sy, x, y);
                if (y != 0)
                    y = y + 1;
                placedGroups.Add(g);
            }
            return canvasIndex;
        }


        internal override Overlays MakeOverlays()
        {
            return overlays;
        }

        public override string ToString()
        {
            return "LinearDisposition";
        }
    }
}
