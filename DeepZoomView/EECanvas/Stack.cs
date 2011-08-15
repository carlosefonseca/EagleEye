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

namespace DeepZoomView.EECanvas
{
    public class Stack : CanvasItem
    {
        private List<CanvasItem> subImages = new List<CanvasItem>();
        private SingleImage first;
        private List<CanvasItem> rest;
        public override MultiScaleSubImage MainImage
        {
            get { return first.MainImage; }
        }

		public List<CanvasItem> SubImages
		{
			get
			{
				return subImages;
			}
		}

		
        public Stack(int id, System.Collections.ObjectModel.ReadOnlyCollection<MultiScaleSubImage> msis,
                                                                                       Dictionary<int, List<int>> stacks)
            : base(id)
        {
            foreach (int i in stacks[id])
            {
                CanvasItem e;
                if (i < 0)
                {
                    e = new Stack(i, msis, stacks);
                }
                else
                {
                    e = new SingleImage(i, msis[i]);
                }
                this.subImages.Add(e);
            }
            first = (SingleImage)subImages.First(ci => ci.GetType() == typeof(SingleImage));
            rest = subImages.Except(new CanvasItem[] { first }).ToList();
        }



        private const double stackSpace = 0.03;

        //public override void Place(MyCanvas canvas)
        //{
        //    this.canvas = canvas;
        //    int x = (int)Position.X;
        //    int y = (int)Position.Y;
        //    double ar = canvas.msis[first.ImageId].AspectRatio;
        //    // msi.SubImages[first].Opacity = 0.7;
        //    canvas.PositionImage(first.ImageId, x, y, Math.Max(1.0001, 1 / ar));

        //    if (ar > 1)
        //    {
        //        StackingImagesOnBottom(x, y, rest, ar);
        //    }
        //    else
        //    {
        //        StackingImagesOnRight(x, y, rest, ar);
        //    }
        //}


        public override bool Place(MyCanvas canvas, double x, double y)
        {
            base.Place(canvas, x, y);

            double ar = this.first.MainImage.AspectRatio; //canvas.msis[first.ImageId].AspectRatio;
            first.Place(canvas, x, y, Math.Max(1, 1 / ar));
            canvas.placedItems.Add(first);
            if (ar > 1)
            {
                StackingImagesOnBottom(x, y, rest, ar);
            }
            else
            {
                StackingImagesOnRight(x, y, rest, ar);
            }
            return false;
        }


        // Landscape  longside: x-axis ; shortside: y-axis
        private void StackingImagesOnBottom(double x, double y, List<CanvasItem> rest, double ar)
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
                shortSide = Math.Min(spaceForStack, longSide / rest.First().MainImage.AspectRatio);
                space = (itemsPerLine == 1 ? 0 : (stackSpace / (itemsPerLine - 1)));
            } while (shortSide * nLines + ((nLines - 1) * space) > spaceForStack);

            double cellLongSide, cellShortSide;
            cellLongSide = x;
            cellShortSide = y + 1 - shortSide - (spaceForStack - shortSide * nLines - space * (nLines - 1)) / 2;
            foreach (CanvasItem i in rest)
            {
                //canvas.msis[i.ImageId].Opacity = 1;
                // TODO: what if image is in diferent orientation?
                canvas.placedItems.Add(i);
                i.Place(canvas, cellLongSide, cellShortSide, 1 / longSide);
                //canvas.PositionImage(i.ImageId, cellLongSide, cellShortSide, 1 / longSide);
                cellLongSide += longSide + space;
                if (cellLongSide > x + 1)
                {
                    cellLongSide = x;
                    cellShortSide -= shortSide + space;
                }
            }
        }

        // Portrait  longside: y-axis ; shortside: x-axis
        private void StackingImagesOnRight(double x, double y, List<CanvasItem> rest, double ar)
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
                shortSide = Math.Min(spaceForStack, longSide * rest.First().MainImage.AspectRatio);
                space = (itemsPerLine == 1 ? 0 : (stackSpace / (itemsPerLine - 1)));
            } while (shortSide * nLines + ((nLines - 1) * space) > spaceForStack);

            double cellLongSide, cellShortSide;
            cellLongSide = y;
            cellShortSide = x + 1 - shortSide - (spaceForStack - shortSide * nLines - space * (nLines - 1)) / 2;
            foreach (CanvasItem i in rest)
            {
                //canvas.msis[i.ImageId].Opacity = 1;
                // TODO: what if image is in diferent orientation?
                canvas.placedItems.Add(i);
                i.Place(canvas, cellShortSide, cellLongSide, 1 / shortSide);
                //canvas.PositionImage(i.ImageId, cellShortSide, cellLongSide, 1 / shortSide);
                cellLongSide += longSide + space;
                if (cellLongSide > y + 1)
                {
                    cellLongSide = y;
                    cellShortSide -= shortSide + space;
                }
            }
        }

        public CanvasItem GetHoveredSubItem(Point p)
        {
            MultiScaleImage msi = canvas.page.msi;

            // Hit-test each sub-image in the MultiScaleImage control to determine
            // whether  "point " lies within a sub-image
            foreach (CanvasItem c in this.subImages.Concat(new CanvasItem[] { this.first }))
            {
                if (c.GetType() == typeof(Stack))
                {
                    CanvasItem cc = ((Stack)c).GetHoveredSubItem(p);
                    if (cc != null)
                    {
                        return cc;
                    }
                }
                MultiScaleSubImage image = c.MainImage;
                double width = msi.ActualWidth / (msi.ViewportWidth * image.ViewportWidth);
                double height = msi.ActualWidth / (msi.ViewportWidth * image.ViewportWidth * image.AspectRatio);

                Point pos = msi.LogicalToElementPoint(new Point(
                    -image.ViewportOrigin.X / image.ViewportWidth,
                    -image.ViewportOrigin.Y / image.ViewportWidth)
                );
                Rect rect = new Rect(pos.X, pos.Y, width, height);

                if (rect.Contains(p))
                {
                    // Return the image index
                    return c;
                }
            }

            // No corresponding sub-image
            return null;
        }

		public override void SetOpacity(double v)
		{
			this.first.SetOpacity(v);
			this.subImages.ForEach(ci => ci.SetOpacity(v));
		}

		public override List<int> AddId(List<int> l)
		{
			l.AddRange(this.getAllIds());
			return l;
		}

		internal override IEnumerable<int> getAllIds()
		{
			List<int> l = new List<int>();
            l.Add(this.ImageId);
			foreach (CanvasItem i in subImages)
			{
				if (i.IsLeaf)
				{
					l.Add(i.ImageId);
				}
				else
				{
					l.AddRange(((Stack)i).getAllIds());
				}
			}
			return l;
		}
    }
}
