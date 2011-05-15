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

namespace DeepZoomView {
	public partial class Page : UserControl {
		// Based on prior work done by Lutz Gerhard, Peter Blois, and Scott Hanselman
		Double zoom = 1;
		bool duringDrag = false;
		bool duringDragSelection = false;
		Point selectionStart = new Point();
		Rectangle selection = null;
		List<MultiScaleSubImage> selectedImages = new List<MultiScaleSubImage>();
		bool mouseDown = false;
		Point lastMouseDownPos = new Point();
		Point lastMousePos = new Point();
		Point lastMouseViewPort = new Point();
		Double Hcells;
		int Vcells;
		long _LastIndex = -1;
		Dictionary<long, string> _Metadata = new Dictionary<long, string>();
		Dictionary<string, int> canvasIndex = new Dictionary<string, int>();
		DateCollection dateCollection;

		public Double ZoomFactor {
			get { return zoom; }
			set { zoom = value; }
		}

		public Page() {
			InitializeComponent();

			Caption.DataContext = zoom;

			// Firing an event when the MultiScaleImage is Loaded
			this.msi.Loaded += new RoutedEventHandler(msi_Loaded);

			// Firing an event when all of the images have been Loaded
			this.msi.ImageOpenSucceeded += new RoutedEventHandler(msi_ImageOpenSucceeded);

			// Handling all of the mouse and keyboard functionality
			this.MouseMove += delegate(object sender, MouseEventArgs e) {
				lastMousePos = e.GetPosition(msi);


				if (duringDrag) {
					Point newPoint = lastMouseViewPort;
					newPoint.X += (lastMouseDownPos.X - lastMousePos.X) / msi.ActualWidth * msi.ViewportWidth;
					newPoint.Y += (lastMouseDownPos.Y - lastMousePos.Y) / msi.ActualWidth * msi.ViewportWidth;
					msi.ViewportOrigin = newPoint;
				} else {
					int index = GetSubImageIndex(e.GetPosition(msi));

					MouseTitle.Parent.SetValue(Canvas.TopProperty, e.GetPosition(msi).Y + 20);
					MouseTitle.Parent.SetValue(Canvas.LeftProperty, e.GetPosition(msi).X + 20);


					//updateOverlay();

					if (index != _LastIndex) {
						_LastIndex = index;

						if (index != -1) {
							//Caption.Text = _Metadata[index].Caption;
							//Caption.Text = msi.SubImages[index].ViewportOrigin.ToString();
							//Caption.Text = "Image" + index;
							MouseTitle.Text = "";
							for (int i = 0; i <= index % 4; i++) {
								MouseTitle.Text += "Image" + index + " " + "Image" + index + "\n";
							}
							MouseTitle.Text = MouseTitle.Text.TrimEnd(new char[1] { '\n' });
							//Description.Text = _Metadata[index].Description;
							//FadeIn.Begin();
						} else {
							//FadeOut.Begin();
						}
					}
				}
			};

			// CLICK
			this.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e) {
				lastMouseDownPos = e.GetPosition(msi);
				lastMouseViewPort = msi.ViewportOrigin;

				mouseDown = true;

				msi.CaptureMouse();
			};

			// RELEASE
			this.MouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e) {
				if (!duringDrag && !duringDragSelection) {
					bool shiftDown = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
					Double newzoom = zoom;

					if (!shiftDown) {
						int index = GetSubImageIndex(e.GetPosition(msi));
						if (index != -1) {
							msi.ViewportWidth = 1.5;
							msi.ViewportOrigin = new Point(-msi.SubImages[index].ViewportOrigin.X, -msi.SubImages[index].ViewportOrigin.Y);
						}
						//zoom = Hcells / msi.ViewportWidth;
						//						updateOverlay();
					} else if (shiftDown) {
						newzoom /= 2;
					} else {
						newzoom *= 2;
					}

					Zoom(newzoom, msi.ElementToLogicalPoint(this.lastMousePos));
				}
				if (duringDragSelection) {
					duringDragSelection = false;
					//do stuff
					Point p1 = new Point((double)selection.GetValue(Canvas.LeftProperty), (double)selection.GetValue(Canvas.TopProperty));
					Point p2 = new Point(p1.X + selection.Width, p1.Y + selection.Height);
					Double p1LogicalX = Math.Floor(msi.ViewportOrigin.X + msi.ViewportWidth * (p1.X / msi.ActualWidth));
					Double p1LogicalY = Math.Floor(msi.ViewportOrigin.Y + (msi.ViewportWidth * (msi.ActualHeight / msi.ActualWidth)) * (p1.Y / msi.ActualHeight));
					Double p2LogicalX = Math.Floor(msi.ViewportOrigin.X + msi.ViewportWidth * (p2.X / msi.ActualWidth));
					Double p2LogicalY = Math.Floor(msi.ViewportOrigin.Y + (msi.ViewportWidth * (msi.ActualHeight / msi.ActualWidth)) * (p2.Y / msi.ActualHeight));
					selectedImages = new List<MultiScaleSubImage>();
					MultiScaleSubImage img;
					int id;
					for (double x = p1LogicalX; x <= p2LogicalX; x++) {
						for (double y = p1LogicalY; y <= p2LogicalY; y++) {
							if (canvasIndex.ContainsKey(x + ";" + y)) {
								img = msi.SubImages[canvasIndex[x + ";" + y]];
								img.Opacity = 0.3;
								//img.SetValue(BorderBrushProperty, new SolidColorBrush(Colors.Green));
								selectedImages.Add(img);
							}
						}
					}
					Mouse.Children.Remove(selection);
				}
				duringDrag = false;
				mouseDown = false;

				msi.ReleaseMouseCapture();
			};

			// MOVE
			this.MouseMove += delegate(object sender, MouseEventArgs e) {
				lastMousePos = e.GetPosition(msi);
				if (mouseDown && !duringDrag && !duringDragSelection) {
					bool shiftDown = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
					if (shiftDown) {
						duringDragSelection = true;
						foreach (MultiScaleSubImage img in selectedImages) {
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
					} else {
						duringDrag = true;
					}
					/*Double w = msi.ViewportWidth;
					Point o = new Point(msi.ViewportOrigin.X, msi.ViewportOrigin.Y);
					msi.UseSprings = false;
					msi.ViewportOrigin = new Point(o.X, o.Y);
					msi.ViewportWidth = w;
					zoom = 1 / w;
					msi.UseSprings = true;*/
				}

				if (duringDrag) {
					Point newPoint = lastMouseViewPort;
					newPoint.X += (lastMouseDownPos.X - lastMousePos.X) / msi.ActualWidth * msi.ViewportWidth;
					newPoint.Y += (lastMouseDownPos.Y - lastMousePos.Y) / msi.ActualWidth * msi.ViewportWidth;
					msi.ViewportOrigin = newPoint;

				} else if (duringDragSelection) {
					selection.SetValue(Canvas.LeftProperty, Math.Min(lastMousePos.X, selectionStart.X));
					selection.SetValue(Canvas.TopProperty, Math.Min(lastMousePos.Y, selectionStart.Y));

					selection.Width = Math.Abs(selectionStart.X - lastMousePos.X);
					selection.Height = Math.Abs(selectionStart.Y - lastMousePos.Y);
				}
			};

			// WHEEL
			new MouseWheelHelper(this).Moved += delegate(object sender, MouseWheelEventArgs e) {
				e.Handled = true;

				Double newzoom = zoom;

				if (e.Delta < 0)
					newzoom /= 1.3;
				else
					newzoom *= 1.3;

				Zoom(newzoom, msi.ElementToLogicalPoint(this.lastMousePos));
				msi.CaptureMouse();
			};

			//TextBlock test2 = new TextBlock();
			//test2.Name = "test2";
			//test2.Foreground = new SolidColorBrush(Colors.Red);
			//test2.MaxHeight = 100;
			//test2.MaxWidth = 100;
			//test2.Text = "Test2";
			//Overlays.Children.Add(test2);

			/*this.msi.ViewportChanged += delegate {
				updateOverlay();
			};*/
			msi.MotionFinished += delegate {
				updateOverlay();
				showOverlay();
			};
			msi.ViewportChanged += delegate {
				hideOverlay();
			};
		}

		private void showOverlay() {
			Overlays.Opacity = 1;
		}

		private void hideOverlay() {
			Overlays.Opacity = 0;
		}

		void msi_ImageOpenSucceeded(object sender, RoutedEventArgs e) {
			canvasIndex = new Dictionary<string, int>();
			Double imgAR = msi.SubImages[0].AspectRatio;
			Double imgWidth = msi.SubImages[0].ViewportWidth;
			Double imgHeight = imgWidth * imgAR;
			Double canvasWidth = msi.ActualWidth;
			Double canvasHeight = msi.ActualHeight;

			Double ratio = canvasWidth / canvasHeight;
			Double canvasRatio = canvasWidth / canvasHeight;

			int imgCount = msi.SubImages.Count;

			int canHold = 1;
			Hcells = 1;
			Vcells = 1;
			while (canHold < imgCount) {
				Hcells++;
				Vcells = Convert.ToInt32(Math.Floor(Hcells / ratio));
				canHold = Convert.ToInt32(Hcells * Vcells);
			}

			var x = 0.0;
			var y = 0.0;
			int i = 0;
			foreach (MultiScaleSubImage subImage in msi.SubImages) {
				subImage.ViewportWidth = 1;
				subImage.ViewportOrigin = new Point(-x, -y);
				canvasIndex.Add(x + ";" + y, i++);
				x += 1;

				if (x >= Hcells) {
					y += 1;
					x = 0.0;
				}
			}
			msi.ViewportWidth = Hcells;

			Overlays.Width = msi.ActualWidth;
			Overlays.Height = msi.ActualHeight;
			Overlays.GetValue(Canvas.TopProperty);

			makeRullerCells(Hcells, y + 1);
		}

		void msi_Loaded(object sender, RoutedEventArgs e) {
			// Hook up any events you want when the image has successfully been opened
			StartDownloadDateData("SmallDB\\DZC\\metadata\\datetime.sorted.db");
		}

		private void Zoom(Double newzoom, Point p) {
			if (newzoom < 1) {
				GoHomeClick(null, null);
			} else {
				msi.ZoomAboutLogicalPoint(newzoom / zoom, p.X, p.Y);
				zoom = newzoom;
			}
		}

		private void ZoomInClick(object sender, System.Windows.RoutedEventArgs e) {
			//Zoom(zoom * 1.3, msi.ElementToLogicalPoint(new Point(.5 * msi.ActualWidth, .5 * msi.ActualHeight)));
			//ArrangeIntoGrid();
			orderImagesByDate();
		}



		private void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e) {
			if (e.Cancelled == false && e.Error == null) {
				string s = e.Result;

			}
		}


		private void makeRullerCells(double Hcells, double Vcells) {
			XaxisGrid.ColumnDefinitions.Clear();
			YaxisGrid.RowDefinitions.Clear();
			XaxisGrid.Children.Clear();
			YaxisGrid.Children.Clear();

			Xaxis.Width = msi.ActualWidth;
			Yaxis.Height = msi.ActualHeight;


			RowDefinition rowD;
			ColumnDefinition colD;
			TextBlock txt;
			for (int i = 0; i < Hcells; i++) {
				colD = new ColumnDefinition();
				XaxisGrid.ColumnDefinitions.Add(colD);
				UIElement elm = makeRullerLabel((i + 1).ToString(), Grid.ColumnProperty, i);
				XaxisGrid.Children.Add(elm);
			}
			for (int i = 0; i < Vcells; i++) {
				rowD = new RowDefinition();
				YaxisGrid.RowDefinitions.Add(rowD);
				UIElement elm = makeRullerLabel((i + 1).ToString(), Grid.RowProperty, i);
				YaxisGrid.Children.Add(elm);
			}
		}


		private FrameworkElement makeRullerLabel(String text, DependencyProperty dp, Object dpv) {
			TextBlock txt = new TextBlock();
			txt.Text = text;
			txt.Foreground = new SolidColorBrush(Colors.White);
			txt.TextAlignment = TextAlignment.Center;
			txt.HorizontalAlignment = HorizontalAlignment.Center;
			txt.VerticalAlignment = VerticalAlignment.Center;

			Border b = new Border();
			b.SetValue(dp, dpv);
			b.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 200, 200, 200));

			if (dp == Grid.RowProperty) {	// Y
				b.Width = 50;
				b.BorderThickness = new Thickness(0, 0, 0, 1);
			} else {	// X
				b.Height = 50;
				b.BorderThickness = new Thickness(0, 0, 1, 0);
			}
			b.HorizontalAlignment = HorizontalAlignment.Stretch;
			b.VerticalAlignment = VerticalAlignment.Stretch;
			b.Child = txt;

			return b;
		}


		private void StartDownloadDateData(string p) {
			String newP = App.Current.Host.Source.AbsoluteUri.Substring(0, App.Current.Host.Source.AbsoluteUri.LastIndexOf('/') + 1) + p.Replace("\\", "/");
			Uri uri = new Uri(newP);

			Stream stream = this.GetType().Assembly.GetManifestResourceStream("DeepZoomView.datetime.sorted.db");
			byte[] bytes = new byte[stream.Length];
			stream.Read(bytes, 0, (int)stream.Length);
			String s = System.Text.Encoding.UTF8.GetString(bytes, 0, (int)stream.Length);


			/*
			WebClient wc = new WebClient();
			wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(processDateData);
			wc.DownloadStringAsync(uri);
			 * */

			dateCollection = new DateCollection();

			String[] lines = s.Split(new Char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

			foreach (String line in lines) {
				String[] split = line.Split(':');
				DateTime date = DateTime.Parse(split[0]);
				String[] ids = split[1].Split(new Char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				dateCollection.Add(date, ids);
			}
		}


		private void orderByGroupsHorizontally() {
			List<int> groupSizes = new List<int>();
			List<string> groupNames = new List<string>();
			int total = 0;
			foreach (KeyValuePair<int, Dictionary<int, Dictionary<int, List<int>>>> years in dateCollection.Get()) {
				foreach (KeyValuePair<int, Dictionary<int, List<int>>> month in years.Value) {
					groupNames.Add(years.Key.ToString() + "\n" + (new DateTime(1, month.Key, 1)).ToString("MMM"));
					int count = 0;
					foreach (KeyValuePair<int, List<int>> day in month.Value) {
						count += day.Value.Count;
					}
					groupSizes.Add(count);
					total += count;
				}
			}

			Hcells = 1;
			Vcells = 1;

			foreach (int row in groupSizes) {
				if (row > Hcells) {
					Hcells = row;
				}
			}

			Vcells = groupSizes.Count;
			Vcells = Convert.ToInt32(Math.Floor(msi.ActualHeight * Hcells / msi.ActualWidth));
			double rowsNeeded = 0;

			while (true) {
				rowsNeeded = 0;
				// Determina o que acontece ao reduzir a largura
				foreach (int row in groupSizes) {
					rowsNeeded += Math.Ceiling(row / (Hcells - 1));
				}

				// se for viavel reduzir a largura, faz isso mesmo.
				Vcells = Convert.ToInt32(Math.Floor(msi.ActualHeight * (Hcells - 1) / msi.ActualWidth));
				if (rowsNeeded <= Vcells) {
					Hcells--;
				} else {
					Vcells = Convert.ToInt32(Math.Floor(msi.ActualHeight * Hcells / msi.ActualWidth));
					break;
				}
			}



			// put images in canvas
			double imgSize = msi.ActualWidth / Hcells;
			var x = 0.0;
			var y = 0.0;
			// era fixe guardar o anterior para repor
			canvasIndex = new Dictionary<string, int>();

			foreach (KeyValuePair<int, Dictionary<int, Dictionary<int, List<int>>>> years in dateCollection.Get()) {
				foreach (KeyValuePair<int, Dictionary<int, List<int>>> month in years.Value) {
					foreach (KeyValuePair<int, List<int>> day in month.Value) {
						foreach (int id in day.Value) {
							msi.SubImages[id].ViewportOrigin = new Point(-x, -y);
							canvasIndex.Add(x + ";" + y, id);
							x++;

							if (x >= Hcells) {
								y += 1;
								x = 0.0;
							}
						}
					}
					x = 0;
					y++;
				}
			}
			double HcellsTmp = msi.ActualWidth * Vcells / msi.ActualHeight;
			Hcells = Math.Max(Hcells, HcellsTmp);
			msi.ViewportWidth = Hcells;
			zoom = 1;

			List<KeyValuePair<string, int>> groups = new List<KeyValuePair<string, int>>();
			for (int i = 0; i < groupNames.Count; i++) {
				groups.Add(new KeyValuePair<string, int>(groupNames[i], Convert.ToInt32(Math.Ceiling(groupSizes[i] / Hcells))));
			}
			makeAnAxis("Y", groups);
		}


		private void makeAnAxis(String XorY, List<KeyValuePair<string, int>> groups) {
			Grid axisGrid;
			if (XorY == "X") {
				axisGrid = XaxisGrid;
			} else {
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

			if (XorY == "X") {
				foreach (KeyValuePair<string, int> group in groups) {
					for (int n = 0; n < group.Value; n++) {
						colD = new ColumnDefinition();
						XaxisGrid.ColumnDefinitions.Add(colD);
					}
					elm = makeRullerLabel(group.Key, Grid.ColumnProperty, i);
					Grid.SetColumnSpan(elm, group.Value);
					XaxisGrid.Children.Add(elm);
					i += group.Value;
				}
			} else {
				foreach (KeyValuePair<string, int> group in groups) {
					for (int n = 0; n < group.Value; n++) {
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

		private void orderImagesByDate() {
			orderByGroupsHorizontally();
			return;
			double imgSize = msi.ActualWidth / Hcells;
			var x = 0.0;
			var y = 0.0;
			// era fixe guardar o anterior para repor
			canvasIndex = new Dictionary<string, int>();
			foreach (KeyValuePair<int, Dictionary<int, Dictionary<int, List<int>>>> years in dateCollection.Get()) {
				foreach (KeyValuePair<int, Dictionary<int, List<int>>> month in years.Value) {
					TextBlock yearOverlay = new TextBlock();
					yearOverlay.Text = years.Key.ToString() + "\n" + (new DateTime(1, month.Key, 1)).ToString("MMMM");
					yearOverlay.Foreground = new SolidColorBrush(Colors.White);
					yearOverlay.SetValue(Canvas.LeftProperty, x * imgSize);
					yearOverlay.SetValue(Canvas.TopProperty, y * imgSize);
					Overlays.Children.Add(yearOverlay);
					x++;
					foreach (KeyValuePair<int, List<int>> day in month.Value) {
						foreach (int id in day.Value) {
							msi.SubImages[id].ViewportOrigin = new Point(-x, -y);
							canvasIndex.Add(x + ";" + y, id);
							x++;

							if (x >= Hcells) {
								y += 1;
								x = 0.0;
							}
						}
					}
					x = 0;
					y++;
				}
			}
		}

		private void ZoomOutClick(object sender, System.Windows.RoutedEventArgs e) {
			Zoom(zoom / 1.3, msi.ElementToLogicalPoint(new Point(.5 * msi.ActualWidth, .5 * msi.ActualHeight)));
		}

		private void GoHomeClick(object sender, System.Windows.RoutedEventArgs e) {
			this.msi.ViewportWidth = Hcells;
			this.msi.ViewportOrigin = new Point(0, 0);
			ZoomFactor = 1;
		}

		private void GoFullScreenClick(object sender, System.Windows.RoutedEventArgs e) {
			if (!Application.Current.Host.Content.IsFullScreen) {
				Application.Current.Host.Content.IsFullScreen = true;
			} else {
				Application.Current.Host.Content.IsFullScreen = false;
			}
		}

		// Handling the VSM states
		private void LeaveMovie(object sender, System.Windows.Input.MouseEventArgs e) {
			VisualStateManager.GoToState(this, "FadeOut", true);
		}

		private void EnterMovie(object sender, System.Windows.Input.MouseEventArgs e) {
			VisualStateManager.GoToState(this, "FadeIn", true);
		}


		// unused functions that show the inner math of Deep Zoom
		public Rect getImageRect() {
			return new Rect(-msi.ViewportOrigin.X / msi.ViewportWidth, -msi.ViewportOrigin.Y / msi.ViewportWidth, 1 / msi.ViewportWidth, 1 / msi.ViewportWidth * msi.AspectRatio);
		}

		public Rect ZoomAboutPoint(Rect img, Double zAmount, Point pt) {
			return new Rect(pt.X + (img.X - pt.X) / zAmount, pt.Y + (img.Y - pt.Y) / zAmount, img.Width / zAmount, img.Height / zAmount);
		}

		public void LayoutDZI(Rect rect) {
			Double ar = msi.AspectRatio;
			msi.ViewportWidth = 1 / rect.Width;
			msi.ViewportOrigin = new Point(-rect.Left / rect.Width, -rect.Top / rect.Width);
		}


		private void SortByFile(string path) {
			//BinaryFormatter formatter = new BinaryFormatter(); 
			//FileStream stream = File.OpenRead(path);

			Dictionary<String, long> sorted = new Dictionary<string, long>();

		}


		//
		// A small example that arranges all of your images (provided they are the same size) into a grid
		//
		private void ArrangeIntoGrid() {

			List<MultiScaleSubImage> randomList = RandomizedListOfImages();
			int numberOfImages = randomList.Count();

			int totalImagesAdded = 0;

			int totalColumns = 10;
			int totalRows = numberOfImages / (totalColumns - 1);


			for (int col = 0; col < totalColumns; col++) {
				for (int row = 0; row < totalRows; row++) {
					if (numberOfImages != totalImagesAdded) {
						MultiScaleSubImage currentImage = randomList[totalImagesAdded];

						Point currentPosition = currentImage.ViewportOrigin;
						Point futurePosition = new Point(-1.2 * col, -0.7 * row);

						// Set up the animation to layout in grid
						Storyboard moveStoryboard = new Storyboard();

						// Create Animation
						PointAnimationUsingKeyFrames moveAnimation = new PointAnimationUsingKeyFrames();

						// Create Keyframe
						SplinePointKeyFrame startKeyframe = new SplinePointKeyFrame();
						startKeyframe.Value = currentPosition;
						startKeyframe.KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero);

						startKeyframe = new SplinePointKeyFrame();
						startKeyframe.Value = futurePosition;
						startKeyframe.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1));

						KeySpline ks = new KeySpline();
						ks.ControlPoint1 = new Point(0, 1);
						ks.ControlPoint2 = new Point(1, 1);
						startKeyframe.KeySpline = ks;
						moveAnimation.KeyFrames.Add(startKeyframe);

						Storyboard.SetTarget(moveAnimation, currentImage);
						Storyboard.SetTargetProperty(moveAnimation, new PropertyPath("ViewportOrigin"));

						moveStoryboard.Children.Add(moveAnimation);
						msi.Resources.Add("unique_id", moveStoryboard);

						// Play Storyboard
						moveStoryboard.Begin();

						// Now that the Storyboard has done it's work, clear the 
						// MultiScaleImage resources.
						msi.Resources.Clear();

						totalImagesAdded++;
					} else {
						break;
					}
				}
			}
		}

		private List<MultiScaleSubImage> RandomizedListOfImages() {
			List<MultiScaleSubImage> imageList = new List<MultiScaleSubImage>();
			Random ranNum = new Random();

			// Store List of Images
			foreach (MultiScaleSubImage subImage in msi.SubImages) {
				imageList.Add(subImage);
			}

			int numImages = imageList.Count;

			// Randomize Image List
			for (int i = 0; i < numImages; i++) {
				MultiScaleSubImage tempImage = imageList[i];
				imageList.RemoveAt(i);

				int ranNumSelect = ranNum.Next(imageList.Count);

				imageList.Insert(ranNumSelect, tempImage);

			}

			return imageList;
		}

		private int GetSubImageIndex(Point point) {
			Double imgLogicalX = Math.Floor(msi.ViewportOrigin.X + msi.ViewportWidth * (point.X / msi.ActualWidth));
			Double imgLogicalY = Math.Floor(msi.ViewportOrigin.Y + (msi.ViewportWidth * (msi.ActualHeight / msi.ActualWidth)) * (point.Y / msi.ActualHeight));
			try {
				return canvasIndex[imgLogicalX + ";" + imgLogicalY];
			} catch {
				return -1;
			}
		}


		private void updateOverlay() {
			zoom = Math.Round(Hcells) / msi.ViewportWidth;
			Double newX = (msi.ViewportOrigin.X * (msi.ActualWidth / Math.Round(Hcells))) * zoom;
			Double newY = (msi.ViewportOrigin.Y * (((msi.ActualWidth / Hcells) * Vcells) / Vcells)) * zoom;
			Double newH = msi.ActualHeight * zoom;
			Double newW = msi.ActualWidth * zoom;

			if ((Double)Overlays.GetValue(Canvas.TopProperty) != -newY) {
				Overlays.SetValue(Canvas.TopProperty, -newY);
			}
			if ((Double)Overlays.GetValue(Canvas.LeftProperty) != -newX) {
				Overlays.SetValue(Canvas.LeftProperty, -newX);
			}

			XaxisGrid.SetValue(Canvas.LeftProperty, -newX);
			YaxisGrid.SetValue(Canvas.TopProperty, -newY);

			//Xaxis.Width = zoom * msi.ActualWidth;
			XaxisGrid.Width = zoom * msi.ActualWidth;
			//Yaxis.Height = zoom * ((msi.ActualWidth / Hcells) * Vcells);
			YaxisGrid.Height = zoom * ((msi.ActualWidth / Hcells) * Vcells);
		}

		private void ghDate_Click(object sender, RoutedEventArgs e) {
			orderByGroupsHorizontally();
		}

		private void gvDate_Click(object sender, RoutedEventArgs e) {
			orderByGroupsHorizontally();
		}
	}
}
