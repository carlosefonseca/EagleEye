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
using DeepZoomView.EECanvas.Dispositions;
using System.Threading;
using System.Diagnostics;

namespace DeepZoomView.EECanvas
{
	public class MyCanvas
	{
		const bool ANIMATE = true;

		internal Dictionary<int, CanvasItem> items = new Dictionary<int, CanvasItem>();
		internal Dictionary<String, CanvasItem> canvasIndex = new Dictionary<string, CanvasItem>();
		internal List<CanvasItem> placedItems = null;
		internal Point diagonal;
		internal Page page;
		internal Overlays AllOverlays;
		internal Disposition disposition;
		internal Organizable organizable;
		internal Boolean hasStacks = false;
		public bool HasGroups { get; set; }
		private int blocks = -1;
		internal Dictionary<int, Shape> groupBorders = new Dictionary<int, Shape>();

		internal double aspectRatio { get { return page.msi.ActualWidth / page.msi.ActualHeight; } }

		internal System.Collections.ObjectModel.ReadOnlyCollection<MultiScaleSubImage> msis
		{
			get
			{
				return page.msi.SubImages;
			}
		}

		private MyCanvas(Page p, Disposition d, int blockCount)
		{
			if (blockCount == 0)
			{
				throw new ArgumentException("Nothing to show.");
			}
			this.page = p;
			this.disposition = d;
			this.blocks = blockCount;
			this.placedItems = new List<CanvasItem>();
		}

		/// <summary>
		/// Constructs a Canvas, makes the association with the page and the Disposition type,
		/// converts the list of image ids to canvasitems and places the images.
		/// </summary>
		/// <param name="p">Page</param>
		/// <param name="d">Disposition</param>
		/// <param name="l">List of ids</param>
		public MyCanvas(Page p, Disposition d, List<int> l)
			: this(p, d, l.Count)
		{
			this.items = ConvertIdsToItems(l);
			GenerateViewAndBorders();
		}

		/// <summary>
		/// Constructs a Canvas, makes the association with the page and the Disposition type,
		/// converts the organizable to canvasitems and places the images.
		/// </summary>
		/// <param name="p">Page</param>
		/// <param name="d">Disposition</param>
		/// <param name="o">Organizable</param>
		public MyCanvas(Page p, Disposition d, Organizable o)
			: this(p, d, o.ItemCount)
		{
			this.organizable = o;
			this.hasStacks = o.hasStacks;
			this.items = ConvertIdsToItems(o);
			GenerateViewAndBorders();
			if (this.organizable.GroupCount > 1)
			{
				this.HasGroups = true;
			}
		}

		//////////////////////////////////////

		private void CreateBorders()
		{
			this.AllOverlays = disposition.MakeOverlays();

			/* foreach (KeyValuePair<int, Group> kv in disposition.invertedGroups)
			 {
                
			 }


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
					 txt.TextAlignment = TextAlignment.Center;
					 txt.TextWrapping = TextWrapping.Wrap;
					 txt.VerticalAlignment = VerticalAlignment.Center;
					 txt.FontWeight = FontWeights.Bold;
					 txt.Foreground = new SolidColorBrush(Colors.White);
					 if (Group.DisplayType == "Groups")
					 {
						 border = new Border();
						 bounds = g.rectangle.Rect;
						 if (g.name.StartsWith("#"))
						 {
							 String[] parts = g.name.Split(new char[] { ' ', '#' }, StringSplitOptions.RemoveEmptyEntries);
							 Byte R = Byte.Parse(parts[0]);
							 Byte G = Byte.Parse(parts[1]);
							 Byte B = Byte.Parse(parts[2]);
							 border.Background = new SolidColorBrush(Color.FromArgb((byte)150, R, G, B));
						 }
						 else
						 {
							 txt.Text = g.name;
							 border.Background = new SolidColorBrush(Color.FromArgb((byte)150, (byte)rand.Next(255), (byte)rand.Next(255), (byte)rand.Next(255)));
						 }
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
			 destination.Children.Add(groupNamesOverlay);*/
		}

		private void GenerateViewAndBorders()
		{
			disposition.Place(this);
			CreateBorders();
			//Thread bordersThread = new Thread(CreateBorders);
			//bordersThread.Start();
		}


		private Dictionary<int, CanvasItem> ConvertIdsToItems(Organizable o)
		{
			return ConvertIdsToItems(o.Ids);
			switch (o.GetType().Name)
			{
				case "OrganizableByColor":
					return ConvertIdsToItems(((OrganizableByColor)o).invertedData.Keys.ToList());
				case "OrganizableByDate":
					return ConvertIdsToItems(((OrganizableByDate)o).Ids);
				case "OrganizableByHSB":
					return ConvertIdsToItems(((OrganizableByHSB)o).invertedData.Keys.ToList());
				case "OrganizableByKeyword":
					return ConvertIdsToItems(((OrganizableByKeyword)o).invertedData.Keys.ToList());
				case "OrganizableByPath":
					return ConvertIdsToItems(((OrganizableByPath)o).invertedData.Keys.ToList());
				case "Organizable":
					return ConvertIdsToItems(o.invertedData.Keys.ToList());
				default: throw new NotImplementedException();
			}
		}

		private Dictionary<int, CanvasItem> ConvertIdsToItems(List<int> l)
		{
			Dictionary<int, CanvasItem> list = new Dictionary<int, CanvasItem>();
			CanvasItem element;
			foreach (int id in l)
			{
				if (id >= 0)
				{
					element = new SingleImage(id, msis[id]);
					list.Add(id, element);
				}
				else if (hasStacks)
				{
					element = new Stack(id, msis, organizable.stacks);
					list.Add(id, element);
				}
				else
				{
					throw new Exception();
				}
			}
			return list;
		}

		//////////////////////////////////////

		/// <summary>
		/// Places all Canvas Items on the MSI, sets it's dimention and rullers accordingly.
		/// </summary>
		public void Display()
		{
			diagonal = new Point(0, 0);
			foreach (CanvasItem ci in placedItems)
			{
				PositionImage(ci);
				//ci.Place(this);
			}
			diagonal.X++;
			diagonal.Y++;
			page.msi.ViewportWidth = this.diagonal.X;
			Debug.WriteLine("Canvas Width: " + this.diagonal.X);

			page.Overlays.Width = page.msi.ActualWidth;
			page.Overlays.Height = page.msi.ActualHeight;

			page.makeRullerCells(this.diagonal.X, this.diagonal.Y);
			
			page.BorderOverlay.Children.Clear();
			page.GroupNamesOverlay.Children.Clear();
			this.AllOverlays.SetLayers(page.BorderOverlay, page.GroupNamesOverlay);
		}



		/// <summary>
		/// Returns a String identifying a coordinate
		/// </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y coordinate</param>
		/// <returns>A string in the format "X-Y"</returns>
		private static String p(int x, int y)
		{
			return x + ";" + y;
		}

		/// <summary>
		/// Returns a String identifying a coordinate
		/// </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y coordinate</param>
		/// <returns>A string in the format "X-Y"</returns>
		private static String p(double x, double y)
		{
			/*if (Math.Round(x) == x && Math.Round(y) == y)
			{
				return p((int)x, (int)y);
			}
			else
			{*/
			return Math.Round(x, 4) + ";" + Math.Round(y, 3);
			//}
		}

		private static String p(Point point)
		{
			return p(point.X, point.Y);
		}


		internal void ShowGroupBorderFromImg(CanvasItem item, Canvas Holder)
		{
			AllOverlays.ShowBorder(item, Holder);
		}

		internal void ShowGroupBorderFromImg(int index, Canvas Holder)
		{
			AllOverlays.ShowBorder(index, Holder);
		}

		internal void SetGroupNamesOverlay()
		{
			AllOverlays.ShowAll();
		}



		public void PositionImage(CanvasItem e)
		{
			int imgId = e.ImageId;
			double x = e.Position.X;
			double y = e.Position.Y;
			double side = e.Side;

			MultiScaleSubImage currentImage = e.MainImage;// page.msi.SubImages[imgId];

			currentImage.Opacity = 1;

			double newWidth;

			if (side == 1)
			{
				diagonal.X = Math.Max(diagonal.X, x);
				diagonal.Y = Math.Max(diagonal.Y, y);
			}

			//msi.SubImages[imgId].ViewportWidth = Math.Max(side, side / currentImage.AspectRatio);
			if (e.autoCenter)
			{
				if (currentImage.AspectRatio < 1)
				{
					x *= (1 / currentImage.AspectRatio);
					x += ((1 / currentImage.AspectRatio) - 1) / 2;
					y *= (1 / currentImage.AspectRatio);
				}
				else
				{
					y += (currentImage.AspectRatio - 1) / 2;
				}

				newWidth = Math.Max(side, side / currentImage.AspectRatio);
			}
			else
			{
				newWidth = side;
				x *= side;
				y *= side;
			}

			Point currentPosition = currentImage.ViewportOrigin;
			Point futurePosition = new Point(-x, -y);

			if (!ANIMATE)
			{
				currentImage.ViewportOrigin = futurePosition;
				currentImage.ViewportWidth = newWidth;
			}
			else
			{
				// Set up the animation to layout in grid
				Storyboard moveStoryboard = new Storyboard();
				Storyboard scaleStoryboard = new Storyboard();

				// Create Animation
				//PointAnimationUsingKeyFrames moveAnimation = new PointAnimationUsingKeyFrames();
				PointAnimation moveAnimation = new PointAnimation();

				QuadraticEase easeing = new QuadraticEase();
				easeing.EasingMode = EasingMode.EaseInOut;
				moveAnimation.EasingFunction = easeing;
				moveAnimation.Duration = new Duration(TimeSpan.FromSeconds(1));
				moveAnimation.To = futurePosition;

				DoubleAnimation scaleAnimation = new DoubleAnimation();
				scaleAnimation.EasingFunction = easeing;
				scaleAnimation.Duration = new Duration(TimeSpan.FromSeconds(1));
				scaleAnimation.To = newWidth;

				Storyboard.SetTarget(moveAnimation, currentImage);
				Storyboard.SetTargetProperty(moveAnimation, new PropertyPath("ViewportOrigin"));

				Storyboard.SetTarget(scaleAnimation, currentImage);
				Storyboard.SetTargetProperty(scaleAnimation, new PropertyPath("ViewportWidth"));

				moveStoryboard.Children.Add(moveAnimation);
				scaleStoryboard.Children.Add(scaleAnimation);
				page.msi.Resources.Add("unique_id", moveStoryboard);
				page.msi.Resources.Add("unique_id2", scaleStoryboard);

				// Play Storyboard
				moveStoryboard.Begin();
				scaleStoryboard.Begin();

				// Now that the Storyboard has done it's work, clear the MultiScaleImage resources.
				page.msi.Resources.Clear();
			}
		}

		internal CanvasItem SetItemPosition(int id, double x, double y)
		{
			CanvasItem i = items[id];
			if (i.Place(this, x, y))
				placedItems.Add(i);

			//add to index
			String newCanvasID = p(x, y);
			if (canvasIndex.ContainsKey(newCanvasID))
			{
				canvasIndex.Remove(newCanvasID);
			}
			canvasIndex.Add(newCanvasID, i);

			return i;
		}

		public override String ToString() // falta o aspect ratio
		{
			return KeyForCanvas(disposition, organizable, blocks, aspectRatio);
		}

		public static String KeyForCanvas(Disposition d, Organizable o, int i, double ar)
		{
			String format = "Canvas D:{0} O:{1} #:{2} AR:{3}";
			if (o == null)
			{
				return String.Format(format, d, '-', i, ar);
			}
			return String.Format(format, d, o, i, ar);
		}
	}
}
