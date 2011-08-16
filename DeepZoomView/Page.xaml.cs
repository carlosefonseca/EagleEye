using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO;
using System.Runtime.Serialization;
using System.Collections;
using System.Security;
using System.Xml;
using System.Xml.Linq;
using System.Json;
using System.Text;
using System.Collections.ObjectModel;
using System.Diagnostics;
using DeepZoomView.EECanvas;
using DeepZoomView.EECanvas.Dispositions;
using System.Windows.Markup;
using DeepZoomView.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace DeepZoomView
{
	public partial class Page : UserControl
	{
		// CONSTS
		const double OPACITY_OF_SELECTED = 0.3;


		Double zoom = 1;
		bool duringDrag = false;
		bool duringDragSelection = false;
		Point selectionStart = new Point();
		Rectangle selection = null;
		List<MultiScaleSubImage> selectedImages = new List<MultiScaleSubImage>();
		List<int> selectedImagesIds = new List<int>();
		List<int> allImageIds = new List<int>();
		bool mouseDown = false;
		Point lastMouseDownPos = new Point();
		Point lastMousePos = new Point();
		Point lastMouseViewPort = new Point();
		CanvasItem LastItemHovered = null;
		Dictionary<long, string> _Metadata = new Dictionary<long, string>();
		MetadataCollection metadataCollection = new MetadataCollection();
		//ObservableCollection<String> CbItems = null;
		Boolean ignoreMouseClickOnCanvas = false;

		List<MyCanvas> CanvasHistory = new List<MyCanvas>();
		Dictionary<String, MyCanvas> CanvasCache = new Dictionary<string, MyCanvas>();
		MyCanvas CurrentCanvas;
		internal List<Selection> newSelections = new List<Selection>();
		internal IEnumerable<int> currentSelections = null;

		private Dictionary<String, DisplaySetting> DisplaySettings = new Dictionary<string, DisplaySetting>();
		public static Dictionary<String, Type> DisplayOptions = new Dictionary<string, Type>();
		private DisplaySetting currentDisplay = null;

		enum MouseMode { Navigate, ImageSelect, GroupSelect }
		MouseMode mouseMode;

		CanvasItem canvasItemUnderTheMouse;
		CanvasItem imageUnderTheMouse;

		public Double ZoomFactor
		{
			get { return zoom; }
			set { zoom = value; }
		}

		public Page()
		{
			InitializeComponent();

			// Firing an event when the MultiScaleImage is Loaded
			this.msi.Loaded += new RoutedEventHandler(msi_Loaded);

			// Firing an event when all of the images have been Loaded
			this.msi.ImageOpenSucceeded += new RoutedEventHandler(msi_ImageOpenSucceeded);

			// Handling all of the mouse and keyboard functionality
			this.MouseMove += delegate(object sender, MouseEventArgs e)
			{
				lastMousePos = e.GetPosition(msi);

				if (duringDrag)
				{
					Point newPoint = lastMouseViewPort;
					newPoint.X += (lastMouseDownPos.X - lastMousePos.X) / msi.ActualWidth * msi.ViewportWidth;
					newPoint.Y += (lastMouseDownPos.Y - lastMousePos.Y) / msi.ActualWidth * msi.ViewportWidth;
					msi.ViewportOrigin = newPoint;
				}
				else
				{
					canvasItemUnderTheMouse = GetSubImageIndex(e.GetPosition(msi));

					if (canvasItemUnderTheMouse != null && canvasItemUnderTheMouse.ImageId < 0)
					{
						imageUnderTheMouse = ((Stack)canvasItemUnderTheMouse).GetHoveredSubItem(e.GetPosition(msi));
					}
					else
					{
						imageUnderTheMouse = canvasItemUnderTheMouse;
					}

					if (imageUnderTheMouse == null)
					{
						if (imageUnderTheMouse != LastItemHovered)
						{
							LastItemHovered = null;
							HideTooltip();
						}
					}
					else
					{
						MouseTitle.Parent.SetValue(Canvas.TopProperty, e.GetPosition(msi).Y + 40);
						MouseTitle.Parent.SetValue(Canvas.LeftProperty, e.GetPosition(msi).X + 40);
						if (imageUnderTheMouse != LastItemHovered)
						{
							LastItemHovered = imageUnderTheMouse;
							MakeTooltipText(imageUnderTheMouse, e.GetPosition(msi));
							CurrentCanvas.ShowGroupBorderFromImg(canvasItemUnderTheMouse, Overlays);
						}
					}
				}
			};

			// CLICK
			this.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
			{
				lastMouseDownPos = e.GetPosition(msi);
				lastMouseViewPort = msi.ViewportOrigin;

				mouseDown = true;

				msi.CaptureMouse();
			};

			// RELEASE
			this.MouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e)
			{
				if (ignoreMouseClickOnCanvas)
				{
					ignoreMouseClickOnCanvas = false;
					return;
				}
				if (!duringDrag && !duringDragSelection && mouseMode != MouseMode.GroupSelect)
				{
					bool shiftDown = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
					Double newzoom = zoom;

					if (shiftDown)
					{
						CanvasItem item = GetSubImageIndex(e.GetPosition(msi));
						if (item != null)
						{
							msi.ViewportWidth = 1.5;
							msi.ViewportOrigin = new Point(-item.MainImage.ViewportOrigin.X, -item.MainImage.ViewportOrigin.Y);
						}
					}
					/*					else if (!shiftDown)
										{
											newzoom /= 2;
										}*/
					else
					{
						newzoom *= 2;
					}

					Zoom(newzoom, msi.ElementToLogicalPoint(this.lastMousePos));
				}
				// Image Selection
				else if (duringDragSelection)
				{
					duringDragSelection = false;
					//do stuff
					Point p1 = new Point((double)selection.GetValue(Canvas.LeftProperty), (double)selection.GetValue(Canvas.TopProperty));
					Point p2 = new Point(p1.X + selection.Width, p1.Y + selection.Height);
					Double p1LogicalX = Math.Floor(msi.ViewportOrigin.X + msi.ViewportWidth * (p1.X / msi.ActualWidth));
					Double p1LogicalY = Math.Floor(msi.ViewportOrigin.Y + (msi.ViewportWidth * (msi.ActualHeight / msi.ActualWidth)) * (p1.Y / msi.ActualHeight));
					Double p2LogicalX = Math.Floor(msi.ViewportOrigin.X + msi.ViewportWidth * (p2.X / msi.ActualWidth));
					Double p2LogicalY = Math.Floor(msi.ViewportOrigin.Y + (msi.ViewportWidth * (msi.ActualHeight / msi.ActualWidth)) * (p2.Y / msi.ActualHeight));
					selectedImages = new List<MultiScaleSubImage>();
					selectedImagesIds = new List<int>();

					Selection s = new Selection();

					for (double x = p1LogicalX; x <= p2LogicalX; x++)
					{
						for (double y = p1LogicalY; y <= p2LogicalY; y++)
						{
							if (CurrentCanvas.canvasIndex.ContainsKey(x + ";" + y))
							{
								CanvasItem ci = CurrentCanvas.canvasIndex[x + ";" + y];
								ci.SetOpacity(OPACITY_OF_SELECTED);
								s.Add(ci);
							}
						}
					}
					newSelections.Add(s);
					FilterButton f = SearchField.NewButtonAtEnd(s.Count + " images");
					f.type = "Selection";
					f.relatedObject = s;
					//Apply.IsEnabled = true;

					Mouse.Children.Remove(selection);
				}
				else if (mouseMode == MouseMode.GroupSelect && canvasItemUnderTheMouse != null)
				{
					Group g = currentDisplay.Organization.GetGroupContainingKey(canvasItemUnderTheMouse.ImageId);
					IEnumerable<CanvasItem> cil = g.images.Select(i => CurrentCanvas.items[i]);

					FilterButton b = SearchField.NewButtonAtEnd("Group " + g.name);
					b.type = "Selection";
					b.relatedObject = new Selection(cil);

					cil.ToList().ForEach(c => c.SetOpacity(OPACITY_OF_SELECTED));
				}
				duringDrag = false;
				mouseDown = false;

				msi.ReleaseMouseCapture();
			};

			// MOVE
			this.MouseMove += delegate(object sender, MouseEventArgs e)
			{
				lastMousePos = e.GetPosition(msi);
				if (mouseDown && !duringDrag && !duringDragSelection)
				{
					if (mouseMode == MouseMode.ImageSelect)
					{
						duringDragSelection = true;
						foreach (MultiScaleSubImage img in selectedImages)
						{
							img.Opacity = 1;
						}
						selectionStart = new Point(lastMouseDownPos.X, lastMouseDownPos.Y);
						selection = new Rectangle();
						selection.SetValue(Canvas.TopProperty, lastMouseDownPos.Y);
						selection.SetValue(Canvas.LeftProperty, lastMouseDownPos.X);
						selection.Width = 0.0;
						selection.Height = 0.0;
						selection.Fill = new SolidColorBrush(Colors.Blue);
						selection.Opacity = 0.5;
						Mouse.Children.Add(selection);
					}
					else
					{
						duringDrag = true;
					}
				}

				if (duringDrag)
				{
					Point newPoint = lastMouseViewPort;
					newPoint.X += (lastMouseDownPos.X - lastMousePos.X) / msi.ActualWidth * msi.ViewportWidth;
					newPoint.Y += (lastMouseDownPos.Y - lastMousePos.Y) / msi.ActualWidth * msi.ViewportWidth;
					msi.ViewportOrigin = newPoint;
				}
				else if (duringDragSelection)
				{
					selection.SetValue(Canvas.LeftProperty, Math.Min(lastMousePos.X, selectionStart.X));
					selection.SetValue(Canvas.TopProperty, Math.Min(lastMousePos.Y, selectionStart.Y));

					selection.Width = Math.Abs(selectionStart.X - lastMousePos.X);
					selection.Height = Math.Abs(selectionStart.Y - lastMousePos.Y);
				}
			};

			// WHEEL
			new MouseWheelHelper(this).Moved += delegate(object sender, MouseWheelEventArgs e)
			{
				e.Handled = true;

				Double newzoom = zoom;

				if (e.Delta < 0)
					newzoom /= 1.3;
				else
					newzoom *= 1.3;

				Zoom(newzoom, msi.ElementToLogicalPoint(this.lastMousePos));
				msi.CaptureMouse();
			};

			msi.ViewportChanged += delegate
			{
				updateOverlay();
			};
			msi.MotionFinished += delegate
			{
				showOverlay();
				updateOverlay();
				showOverlay();
			};
			msi.ViewportChanged += delegate
			{
				hideOverlay();
			};

			App.Current.Host.Content.Resized += new EventHandler(Content_Resized);
			DisplayOptions.Add("Grid", typeof(SequentialDisposition));
			DisplayOptions.Add("Group", typeof(TreeMapDisposition));
			DisplayOptions.Add("Linear", typeof(LinearDisposition));
		}

		private void HideTooltip()
		{
			MouseTitle.Parent.SetValue(Canvas.TopProperty, -5000.0);
			MouseTitle.Parent.SetValue(Canvas.LeftProperty, -5000.0);
		}

		private void MakeTooltipText(int index)
		{
			String tooltipTxt = "";
			foreach (String oName in metadataCollection.GetAllOrganizablesNames())
			{
				Organizable o = metadataCollection.GetOrganized(oName);
				if (o.ContainsId(index))
				{
					tooltipTxt += o.Name + ": " + o.Id(index) + Environment.NewLine;
				}
			}
			MouseTitle.Text = tooltipTxt.TrimEnd(Environment.NewLine.ToCharArray());
		}

		private void MakeTooltipText(CanvasItem item, Point p)
		{
			if (item.ImageId < 0 || (item.MainImage == null && item.GetType() == typeof(Stack)))
			{
				CanvasItem c = ((Stack)item).GetHoveredSubItem(p);
				if (c != null)
				{
					MakeTooltipText(c, p);
				}
			}
			else
			{
				MakeTooltipText(item.ImageId);
			}
		}


		void Content_Resized(object sender, EventArgs e)
		{
			UpdateView();
		}

		private void showOverlay()
		{
			Overlays.Opacity = 1;
		}

		private void hideOverlay()
		{
			Overlays.Opacity = 0;
		}

		void msi_ImageOpenSucceeded(object sender, RoutedEventArgs e)
		{
			for (int j = 0; j < msi.SubImages.Count; j++)
			{
				allImageIds.Add(j);
			}

			ArrangeIntoGrid(allImageIds);
			AppStartDebug();
		}

		void msi_Loaded(object sender, RoutedEventArgs e)
		{
		}


		private void Zoom(Double newzoom, Point p)
		{
			/*if (newzoom < 1) {
				ShowAllContent();
			} else*/
			{
				msi.ZoomAboutLogicalPoint(newzoom / zoom, p.X, p.Y);
				zoom = newzoom;
			}
		}

		private void ZoomInClick(object sender, System.Windows.RoutedEventArgs e)
		{
			//orderImagesByDate();
		}

		internal void makeRullerCells(double Hcells, double Vcells)
		{
			XaxisGrid.ColumnDefinitions.Clear();
			YaxisGrid.RowDefinitions.Clear();
			XaxisGrid.Children.Clear();
			YaxisGrid.Children.Clear();

			RowDefinition rowD;
			ColumnDefinition colD;
			for (int i = 0; i < Hcells; i++)
			{
				colD = new ColumnDefinition();
				XaxisGrid.ColumnDefinitions.Add(colD);
				UIElement elm = makeRullerLabel((i + 1).ToString(), Grid.ColumnProperty, i);
				XaxisGrid.Children.Add(elm);
			}
			for (int i = 0; i < Vcells; i++)
			{
				rowD = new RowDefinition();
				YaxisGrid.RowDefinitions.Add(rowD);
				UIElement elm = makeRullerLabel((i + 1).ToString(), Grid.RowProperty, i);
				YaxisGrid.Children.Add(elm);
			}
		}

		private FrameworkElement makeRullerLabel(String text, DependencyProperty dp, Object dpv)
		{
			TextBlock txt = new TextBlock();
			txt.Text = text;
			txt.Foreground = new SolidColorBrush(Colors.White);
			txt.TextAlignment = TextAlignment.Center;
			txt.HorizontalAlignment = HorizontalAlignment.Center;
			txt.VerticalAlignment = VerticalAlignment.Center;

			Border b = new Border();
			b.SetValue(dp, dpv);
			b.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 200, 200, 200));

			if (dp == Grid.RowProperty)
			{	// Y
				b.Width = 50;
				b.BorderThickness = new Thickness(0, 0, 0, 1);
			}
			else
			{	// X
				b.Height = 50;
				b.BorderThickness = new Thickness(0, 0, 1, 0);
			}
			b.Child = txt;

			return b;
		}

		private bool AskForMetadata()
		{
			List<String> failed = new List<string>();

			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Filter = "All Files (*.*)|*.*";
			ofd.FilterIndex = 1;

			try
			{
				if (ofd.ShowDialog() != true)
				{
					return false;
				}
			}
			catch (SecurityException e)
			{
				System.Windows.Browser.HtmlPage.Window.Alert("Not allowed to open the Open File Dialog Box :(" + Environment.NewLine + e.Message);
				return false;
			}
			foreach (FileInfo file in ofd.Files)
			{
				StreamReader stream = file.OpenText();
				metadataCollection.ParseXML(stream, msi.SubImages.Count);
				stream.Close();
			}
			if (failed.Count == 1)
			{
				System.Windows.Browser.HtmlPage.Window.Alert("Metadata reading failed on the file " + failed[0]);
			}
			else if (failed.Count > 1)
			{
				String failedNames = "";
				foreach (String fn in failed)
				{
					failedNames += Environment.NewLine + " - " + fn;
				}
				System.Windows.Browser.HtmlPage.Window.Alert("Metadata reading failed on the files: " + failedNames);
			}
			return true;
		}


		private void makeAnAxis(String XorY, double n)
		{
			List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
			for (int i = 1; i <= n; i++)
			{
				list.Add(new KeyValuePair<string, int>(i.ToString(), 1));
			}
			makeAnAxis(XorY, list);
		}


		private void makeAnAxis(String XorY, List<KeyValuePair<string, int>> groups)
		{
			Grid axisGrid;
			if (XorY == "X")
			{
				axisGrid = XaxisGrid;
			}
			else
			{
				axisGrid = YaxisGrid;
			}
			axisGrid.ColumnDefinitions.Clear();
			axisGrid.RowDefinitions.Clear();
			axisGrid.Children.Clear();

			ColumnDefinition colD;
			RowDefinition rowD;
			//UIElement elm;
			FrameworkElement elm;
			int i = 0;

			if (XorY == "X")
			{
				foreach (KeyValuePair<string, int> group in groups)
				{
					for (int n = 0; n < group.Value; n++)
					{
						colD = new ColumnDefinition();
						XaxisGrid.ColumnDefinitions.Add(colD);
					}
					elm = makeRullerLabel(group.Key, Grid.ColumnProperty, i);
					Grid.SetColumnSpan(elm, group.Value);
					XaxisGrid.Children.Add(elm);
					i += group.Value;
				}
			}
			else
			{
				foreach (KeyValuePair<string, int> group in groups)
				{
					for (int n = 0; n < group.Value; n++)
					{
						rowD = new RowDefinition();
						YaxisGrid.RowDefinitions.Add(rowD);
					}
					elm = makeRullerLabel(group.Key, Grid.RowProperty, i);
					Grid.SetRowSpan(elm, group.Value);
					YaxisGrid.Children.Add(elm);
					i += group.Value;
				}
			}
		}

		private void ZoomOutClick(object sender, System.Windows.RoutedEventArgs e)
		{
			Zoom(zoom / 1.3, msi.ElementToLogicalPoint(new Point(.5 * msi.ActualWidth, .5 * msi.ActualHeight)));
		}

		private void GoHomeClick(object sender, System.Windows.RoutedEventArgs e)
		{
			ShowAllContent();
		}

		private void ShowAllContent()
		{

			if (Math.Round(CurrentCanvas.diagonal.X) == 1 && Math.Round(CurrentCanvas.diagonal.Y) == 1 && (msi.ActualHeight < msi.ActualWidth))
			{
				msi.ViewportWidth = msi.ActualWidth / msi.ActualHeight;
				this.msi.ViewportOrigin = new Point(-(((msi.ActualWidth / msi.ActualHeight) - 1) / 2), 0);
			}
			else
			{
				this.msi.ViewportWidth = CurrentCanvas.diagonal.X;
				this.msi.ViewportOrigin = new Point(0, 0);
			}
			ZoomFactor = 1;
		}

		private void GoFullScreenClick(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!Application.Current.Host.Content.IsFullScreen)
			{
				Application.Current.Host.Content.IsFullScreen = true;
			}
			else
			{
				Application.Current.Host.Content.IsFullScreen = false;
			}
		}

		// Handling the VSM states
		private void LeaveMovie(object sender, System.Windows.Input.MouseEventArgs e)
		{
			VisualStateManager.GoToState(this, "FadeOut", true);
		}

		private void EnterMovie(object sender, System.Windows.Input.MouseEventArgs e)
		{
			VisualStateManager.GoToState(this, "FadeIn", true);
		}


		// unused functions that show the inner math of Deep Zoom
		public Rect getImageRect()
		{
			return new Rect(-msi.ViewportOrigin.X / msi.ViewportWidth, -msi.ViewportOrigin.Y / msi.ViewportWidth, 1 / msi.ViewportWidth, 1 / msi.ViewportWidth * msi.AspectRatio);
		}

		public Rect ZoomAboutPoint(Rect img, Double zAmount, Point pt)
		{
			return new Rect(pt.X + (img.X - pt.X) / zAmount, pt.Y + (img.Y - pt.Y) / zAmount, img.Width / zAmount, img.Height / zAmount);
		}

		public void LayoutDZI(Rect rect)
		{
			Double ar = msi.AspectRatio;
			msi.ViewportWidth = 1 / rect.Width;
			msi.ViewportOrigin = new Point(-rect.Left / rect.Width, -rect.Top / rect.Width);
		}


		private void SortByFile(string path)
		{
			//BinaryFormatter formatter = new BinaryFormatter(); 
			//FileStream stream = File.OpenRead(path);
			Dictionary<String, long> sorted = new Dictionary<string, long>();
		}


		//
		// A small example that arranges all of your images (provided they are the same size) into a grid
		//
		private void ArrangeIntoGrid(List<int> imgList, double totalColumns, double totalRows)
		{
			ArrangeIntoGrid(imgList);
		}
		private void ArrangeIntoGrid(List<int> imgList)
		{
			MyCanvas d = new MyCanvas(this, new SequentialDisposition(), imgList);
			CanvasHistory.Add(d);
			d.Display();
			CurrentCanvas = d;
		}


		private List<int> RandomizedListOfImages(List<int> idList)
		{
			Random ranNum = new Random();

			int numImages = idList.Count;

			// Randomize Image List
			for (int i = 0; i < numImages; i++)
			{
				int tempImage = idList[i];
				idList.RemoveAt(i);
				int ranNumSelect = ranNum.Next(idList.Count);
				idList.Insert(ranNumSelect, tempImage);
			}
			return idList;
		}

		private CanvasItem GetSubImageIndex(Point point)
		{
			Double imgLogicalX = Math.Floor(msi.ViewportOrigin.X + msi.ViewportWidth * (point.X / msi.ActualWidth));
			Double imgLogicalY = Math.Floor(msi.ViewportOrigin.Y + (msi.ViewportWidth * (msi.ActualHeight / msi.ActualWidth)) * (point.Y / msi.ActualHeight));

			if (CurrentCanvas != null && CurrentCanvas.canvasIndex.ContainsKey(imgLogicalX + ";" + imgLogicalY))
			{
				return CurrentCanvas.canvasIndex[imgLogicalX + ";" + imgLogicalY];
			}
			else
			{
				return null;
			}
		}


		private void updateOverlay()
		{
			if (CurrentCanvas == null)
			{
				return;
			}
			double Hcells = CurrentCanvas.diagonal.X;
			double Vcells = CurrentCanvas.diagonal.Y;
			if (Hcells == 0 || Vcells == 0)
			{
				return;
			}
			//zoom = Math.Round(Hcells) / msi.ViewportWidth;
			zoom = Hcells / msi.ViewportWidth;
			//Double newX = (msi.ViewportOrigin.X * (msi.ActualWidth / Math.Round(Hcells))) * zoom;
			Double newX = (msi.ViewportOrigin.X * (msi.ActualWidth / Hcells)) * zoom;
			Double newY = (msi.ViewportOrigin.Y * (((msi.ActualWidth / Hcells) * Vcells) / Vcells)) * zoom;
			Double newH = msi.ActualHeight * zoom;
			Double newW = msi.ActualWidth * zoom;

			// Overlays
			if ((Double)Overlays.GetValue(Canvas.TopProperty) != -newY)
			{
				Overlays.SetValue(Canvas.TopProperty, -newY);
			}
			if ((Double)Overlays.GetValue(Canvas.LeftProperty) != -newX)
			{
				Overlays.SetValue(Canvas.LeftProperty, -newX);
			}

			OverlaysScale.ScaleX = zoom;
			OverlaysScale.ScaleY = zoom;

			// Rullers
			XaxisGrid.SetValue(Canvas.LeftProperty, -newX);
			YaxisGrid.SetValue(Canvas.TopProperty, -newY);

			XaxisGrid.Width = zoom * msi.ActualWidth;
			YaxisGrid.Height = zoom * ((msi.ActualWidth / Hcells) * Vcells);

			double visibleStart = -(double)YaxisGrid.GetValue(Canvas.TopProperty);
			double visibleEnd = visibleStart + Yaxis.ActualHeight;
			double elmStart = 0;
			double elmHeight = 0;
			double elmEnd = 0;
			double labelHeight = 0;
			double newTop = 0;
			TextBlock label;
			foreach (Border border in YaxisGrid.Children)
			{
				elmHeight = border.RenderSize.Height;
				elmEnd = elmStart + elmHeight;
				labelHeight = border.Child.RenderSize.Height;
				label = (TextBlock)border.Child;

				if (elmStart >= visibleStart)
				{					// inicio visivel
					if (elmEnd > visibleEnd)
					{					// fim !visivel
						newTop = (visibleEnd - elmStart - labelHeight) / 2;
						CustomLabelPosition(label, newTop);
						break;			// toda a área visivel está preenchida, o resto n interessa
					}
					else
					{			// elemento completamente visivel -> posição auto
						CustomLabelPosition(label, -1);
						elmStart = elmEnd;
					}
				}
				else if (elmEnd > visibleStart)
				{		// elemento não está completamente out
					if (elmEnd <= visibleEnd)
					{			// inicio !visivel, fim visivel
						newTop = (elmEnd - visibleStart - labelHeight) / 2 + visibleStart - elmStart;
						CustomLabelPosition(label, newTop);
						elmStart = elmEnd;
					}
					else
					{														// inicio e fim !visivel
						newTop = (visibleEnd - visibleStart - labelHeight) / 2 + visibleStart - elmStart;
						CustomLabelPosition(label, newTop);
						break;
					}
				}
				else
				{		// elemento está completamente fora de vista
					CustomLabelPosition(label, -1);
					elmStart = elmEnd;
				}
			}
		}

		private void CustomLabelPosition(TextBlock label, double newTop)
		{
			if (newTop == -1)
			{	//Reset
				label.Margin = new Thickness(0);
				label.VerticalAlignment = VerticalAlignment.Center;
			}
			else
			{
				label.Margin = new Thickness(0, Math.Max(0, newTop), 0, 0);
				label.VerticalAlignment = VerticalAlignment.Top;
			}
		}


		private void OnlySelected_Click(object sender, RoutedEventArgs e)
		{
			if (newSelections.Count == 0)
			{
				return;
			}

			NewCanvasDispositionFromUI();
		}

		internal enum FadeAnimation { In, Out };

		internal void fadeImages(IEnumerable<int> ids, FadeAnimation type)
		{
			MultiScaleSubImage image;
			foreach (int id in ids)
			{
				image = msi.SubImages[id];
				// Set up the animation to layout in grid
				Storyboard fadeStoryboard = new Storyboard();

				// Create Animation
				DoubleAnimation fadeAnimation = new DoubleAnimation();

				Storyboard.SetTarget(fadeAnimation, image);
				Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath("Opacity"));
				fadeAnimation.Duration = new Duration(TimeSpan.FromSeconds(1));
				fadeAnimation.To = (type == FadeAnimation.In ? 1.0 : 0.0);
				fadeStoryboard.Children.Add(fadeAnimation);
				msi.Resources.Add("unique_id", fadeStoryboard);

				// Play Storyboard
				fadeStoryboard.Begin();

				// Now that the Storyboard has done it's work, clear the 
				// MultiScaleImage resources.
				msi.Resources.Clear();
			}
		}

		private void resetDisplay()
		{
			selectedImages = new List<MultiScaleSubImage>();
			selectedImagesIds = new List<int>();
			//CalculateHcellsVcells(true);
			fadeImages(allImageIds, FadeAnimation.In);
			ArrangeIntoGrid(allImageIds);//, Hcells, Vcells);
			/*ShowAllContent();
			makeAnAxis("Y", Vcells);
			makeAnAxis("X", Hcells);
			*/
		}

		private void random_Click(object sender, RoutedEventArgs e)
		{
			if (selectedImagesIds.Count != 0)
			{
				ArrangeIntoGrid(RandomizedListOfImages(selectedImagesIds));
			}
			else
			{
				ArrangeIntoGrid(RandomizedListOfImages(allImageIds));
			}
		}


		private void NewCanvasDispositionFromUI()
		{
			if (newSelections != null && newSelections.Count > 0)
			{
				currentSelections = newSelections.SelectMany(c => c).SelectMany(c => c.getAllIds());
				NewCanvasDispositionFromUI(currentSelections);
			}
			else if (currentSelections != null)
			{
				NewCanvasDispositionFromUI(currentSelections);
			}
			else
			{
				NewCanvasDispositionFromUI(new List<int>());
			}
		}

		private void NewCanvasDispositionFromUI(IEnumerable<int> filter)
		{
			if (msi.ActualHeight == 0 || msi.ActualWidth == 0)
			{
				return;
			}
			String sorting = "id";

			// Disposition (Treemap / Grid / Linear)
			Disposition d = new SequentialDisposition();

			// Get Organizable
			Organizable o = metadataCollection.GetOrganized(sorting);


			String s = "";
			// Create canvas
			MyCanvas canvas;
			if (o != null)
			{
				if (filter.Count() != 0)
				{
					// Filter Organizable
					o.ReplaceFilter(filter);
					// make filter key
					foreach (int i in filter)
					{
						s += i + ";";
					}
				}
				else
				{
					o.ClearFilter();
				}

				String key = MyCanvas.KeyForCanvas(d, o, o.ItemCount, msi.ActualWidth / msi.ActualHeight) + s;
				Debug.WriteLine(key + " " + s);
				if (CanvasCache.ContainsKey(key))
				{
					canvas = CanvasCache[key];
				}
				else
				{
					canvas = new MyCanvas(this, d, o);
					if (key != canvas.ToString() + s)
					{
						throw new Exception("Keys are different!");
					}
					CanvasCache.Add(key, canvas);
				}
			}
			else
			{
				String key = MyCanvas.KeyForCanvas(d, null, msi.SubImages.Count, msi.ActualWidth / msi.ActualHeight) + s;
				List<int> items = msi.SubImages.Select((m, i) => i).ToList();
				if (filter != null && filter.Count() != 0)
					items = items.Intersect(filter).ToList();

				if (CanvasCache.ContainsKey(key))
				{
					canvas = CanvasCache[key];
				}
				else
				{
					canvas = new MyCanvas(this, d, items);
					if (key != canvas.ToString() + s)
					{
						throw new Exception("Keys are different!");
					}
					CanvasCache.Add(key, canvas);
				}
			}
			CanvasHistory.Add(canvas);
			// Place
			IEnumerable<int> placedImages = canvas.Display();
			// hide rest
			fadeImages(msi.SubImages.Select((m, i) => i).Except(placedImages), FadeAnimation.Out);
			CurrentCanvas = canvas;
		}


		private void NewCanvasDispositionFromUI(DisplaySetting display, IEnumerable<int> filter)
		{
			if (msi.ActualHeight == 0 || msi.ActualWidth == 0)
			{
				return;
			}
			Disposition d = (Disposition)(display.Disposition.GetConstructor(new Type[] { }).Invoke(new Type[] { }));
			Organizable o = display.Organization;

			String s = "";
			// Create canvas
			MyCanvas canvas;
			if (o != null)		// The Organizable Exists
			{
				if (filter.Count() != 0)
				{
					// Filter Organizable
					o.ReplaceFilter(filter);
					// make filter key
					foreach (int i in filter)
					{
						s += i + ";";
					}
				}
				else
				{
					o.ClearFilter();
				}

				String key = MyCanvas.KeyForCanvas(d, o, o.ItemCount, msi.ActualWidth / msi.ActualHeight) + s;
				Debug.WriteLine(key + " " + s);
				if (CanvasCache.ContainsKey(key))
				{
					canvas = CanvasCache[key];
				}
				else
				{
					canvas = new MyCanvas(this, d, o);
					if (key != canvas.ToString() + s)
					{
						throw new Exception("Keys are different!");
					}
					CanvasCache.Add(key, canvas);
				}
			}
			else  // The Organizable DOES NOT Exist
			{
				String key = MyCanvas.KeyForCanvas(d, null, msi.SubImages.Count, msi.ActualWidth / msi.ActualHeight) + s;
				List<int> items = msi.SubImages.Select((m, i) => i).ToList();
				if (filter != null && filter.Count() != 0)
					items = items.Intersect(filter).ToList();

				if (CanvasCache.ContainsKey(key))
				{
					canvas = CanvasCache[key];
				}
				else
				{
					canvas = new MyCanvas(this, d, items);
					if (key != canvas.ToString() + s)
					{
						throw new Exception("Keys are different!");
					}
					CanvasCache.Add(key, canvas);
				}
			}
			CanvasHistory.Add(canvas);
			// Place
			IEnumerable<int> placedImages = canvas.Display();
			// hide rest
			fadeImages(msi.SubImages.Select((m, i) => i).Except(placedImages), FadeAnimation.Out);
			CurrentCanvas = canvas;
		}

		private void LoadMetadata(object sender, RoutedEventArgs e)
		{
			if (sender == null && e == null)
			{
				// Loading from embeded file, for debugging
				StreamReader stream = new StreamReader(App.GetResourceStream(new Uri("smalldb.xml", UriKind.Relative)).Stream);
				metadataCollection.ParseXML(stream, msi.SubImages.Count);
				stream.Close();
			}
			else if (AskForMetadata()) // normal loading
			{
				ignoreMouseClickOnCanvas = true;
			}
			else
			{
				// loading failed
				return;
			}
			// post loading actions
			// Switch visible controls
			((StackPanel)load.Parent).Children.ToList().ForEach(ui => ui.Visibility = System.Windows.Visibility.Visible);
			load.Visibility = Visibility.Collapsed;

			// set user options and display
			SetVisualizationUI();

			// Set the autocomplete stuff for the filter bar
			SearchField.AutoCompleteElement = SearchFieldAutocomplete;
			SearchField.OnTextInsertion += new EvHandler(SearchField_OnTextInsertion);
			SearchField.ApplyButton = Apply;

			selectionsButton.SelectionHandler += new EvHandler(selectionsButton_SelectionHandler);
			selectionsButton.SelectionCleared += new EvHandler(selectionsButton_SelectionCleared);

			UpdateView();
		}

		void selectionsButton_SelectionCleared(object sender, MyEventArgs e)
		{
			mouseMode = MouseMode.Navigate;
		}

		void selectionsButton_SelectionHandler(object sender, MyEventArgs e)
		{
			if (e.active.EndsWith("images"))
			{
				selectionsButton.SetActive("Select Images (click to stop)");
				mouseMode = MouseMode.ImageSelect;
			}
			else if (e.active.EndsWith("groups"))
			{
				selectionsButton.SetActive("Select Groups (click to stop)");
				mouseMode = MouseMode.GroupSelect;
			}
			else { return; }
			ignoreMouseClickOnCanvas = true;
		}

		void SearchField_OnTextInsertion(object sender, MyEventArgs e)
		{
			CalculateAndSetSearchFieldAutocompleteOptions(e.active);
		}

		private void CalculateAndSetSearchFieldAutocompleteOptions(string p)
		{
			SearchField.AutocompleteOptions.Clear();
			IEnumerable<AutocompleteOption> cena = metadataCollection.GetAllOrganizables.SelectMany(o => o.RelatedKeys(p));
			SearchField.AutocompleteOptions.Clear();
			cena.ToList().ForEach(a => SearchField.AutocompleteOptions.Add(a));
		}

		private void SetVisualizationUI()
		{
			// Set the visualization options and respective buttons
			IEnumerable<Organizable> options = metadataCollection.GetOrganizationOptions();
			foreach (Organizable o in options)
			{
				if (o.Dispositions.Count == 1)
				{
					DisplaySettings.Add(o.Name, new DisplaySetting(o.Name, o, DisplayOptions[o.Dispositions[0]]));
				}
				else
				{
					foreach (String d in o.Dispositions)
					{
						String name = o.Name;
						if (d != "Group")
						{
							name += String.Format(" ({0})", d);
						}
						DisplaySettings.Add(name, new DisplaySetting(name, o, DisplayOptions[d]));
					}
				}
			}

			DisplayOptionsButtons.SetButtons(DisplaySettings.Keys.ToList());
			DisplayOptionsButtons.OnChangeSelected += VizChangedHandler;
			if (DisplaySettings.ContainsKey("Date"))
			{
				DisplayOptionsButtons.Selected = "Date";
				UpdateView(DisplaySettings["Date"]);
			}
			else
			{
				DisplayOptionsButtons.Selected = DisplaySettings.Keys.First();
				UpdateView(DisplaySettings.First().Value);
			}
		}

		private void showgroups_Click(object sender, RoutedEventArgs e)
		{
			if (showgroups.IsChecked.HasValue && showgroups.IsChecked.Value && CurrentCanvas.HasGroups)
			{
				CurrentCanvas.SetGroupNamesOverlay();
				GroupNamesOverlay.Visibility = Visibility.Visible;
				BorderOverlay.Visibility = Visibility.Collapsed;
			}
			else
			{
				GroupNamesOverlay.Visibility = Visibility.Collapsed;
				BorderOverlay.Visibility = Visibility.Visible;
			}
		}


		private void Filter(String s)
		{

		}

		private void AppStartDebug()
		{
			LoadMetadata(null, null);
		}


		public void VizChangedHandler(object sender, MyEventArgs e)
		{
			if (DisplaySettings.ContainsKey(e.active))
			{
				UpdateView(DisplaySettings[e.active]);
			}
		}

		private enum LOGIC { OR, AND };

		private void UpdateView()
		{
			if (currentDisplay == null)
			{
				NewCanvasDispositionFromUI();
			}
			else
			{
				UpdateView(currentDisplay);
			}
			return;
		}

		/// <summary>
		/// Updates the image layout using a display settings object.
		/// Does all the filtering and sends it to be displayed.
		/// </summary>
		/// <param name="displaySetting"></param>
		private void UpdateView(DisplaySetting displaySetting)
		{
			currentDisplay = displaySetting;

			// FILTERS
			LOGIC logic = LOGIC.AND;
			IEnumerable<FilterButton> filterbarList = SearchField.FilterButtons.Where(f => f.type.CompareTo("Selection") != 0);
			List<int> filter = new List<int>();

			bool first = true;

			List<int> perSearchItem;
			foreach (FilterButton f in filterbarList)
			{
				String s = f.Text;
				perSearchItem = new List<int>();
				perSearchItem.AddRange(IdsFromMatchedKeysFromOrganizable("Keyword", s));
				perSearchItem.AddRange(IdsFromMatchedKeysFromOrganizable("Path", s));
				perSearchItem = perSearchItem.Distinct().ToList();

				if (perSearchItem.Count == 0)
				{
					f.Paint(Colors.Red);
				}
				else
				{
					f.Paint(Colors.Green);
				}

				if (first || logic == LOGIC.OR)
				{
					filter.AddRange(perSearchItem);
				}
				else if (logic == LOGIC.AND)
				{
					filter = filter.Intersect(perSearchItem).ToList();
				}
				first = false;
			}

			IEnumerable<FilterButton> selectionButtons = SearchField.FilterButtons.Where(f => f.type.CompareTo("Selection") == 0);
			if (selectionButtons.Count() > 0)
			{
				Selection selectedItems;
				if (selectionButtons.Count() > 1)
				{
					selectedItems = new Selection(selectionButtons.SelectMany(f => (Selection)f.relatedObject));
					SearchField.RemoveButtons(selectionButtons.Skip(1));
					selectionButtons.First().relatedObject = selectedItems;
					selectionButtons.First().Text = selectedItems.Count + " items";
				}
				else
				{
					selectedItems = (Selection)selectionButtons.First().relatedObject;
				}

				IEnumerable<int> selectedIds = selectedItems.SelectMany(c => c.getAllIds());
				if (filter.Count() != 0)
				{
					NewCanvasDispositionFromUI(displaySetting, filter.Distinct().Intersect(selectedIds));
				}
				else
				{
					NewCanvasDispositionFromUI(displaySetting, selectedIds);
				}
			}
			else
			{
				NewCanvasDispositionFromUI(displaySetting, filter.Distinct());
			}
			SearchField.Dirty = false;
		}

		/// <summary>
		/// Gathers the image ids from the specified Organizable instance that match the specified string
		/// </summary>
		/// <param name="organizable"></param>
		/// <param name="s"></param>
		/// <returns></returns>
		private IEnumerable<int> IdsFromMatchedKeysFromOrganizable(String organizable, String s)
		{
			if (metadataCollection.ContainsOrganizable(organizable))
			{
				Organizable o = metadataCollection.GetOrganized(organizable);
				return o.IdsForKey(o.KeysThatMatch(s));
			}
			return new List<int>();
		}


		private void SearchField_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				UpdateView();
			}
		}

		private void Apply_Click(object sender, RoutedEventArgs e)
		{
			UpdateView();
		}
	}
}
