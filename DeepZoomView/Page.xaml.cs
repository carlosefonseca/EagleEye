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


namespace DeepZoomView {
	public partial class Page : UserControl {
		// Based on prior work done by Lutz Gerhard, Peter Blois, and Scott Hanselman
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
		Double Hcells;
		Double Vcells;
		long _LastIndex = -1;
		Dictionary<long, string> _Metadata = new Dictionary<long, string>();
		Dictionary<string, int> canvasIndex = new Dictionary<string, int>();
		DateCollection dateCollection;
		//Dictionary<String, Organizable> Organizations = new Dictionary<string, Organizable>();
		MetadataCollection metadataCollection = new MetadataCollection();
		ObservableCollection<String> CbItems = null;

		public Double ZoomFactor {
			get { return zoom; }
			set { zoom = value; }
		}

		public Page() {
			InitializeComponent();

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
							//if (metadataCollection.GetOrganized("color")) {
							//	OrganizableByColor colors = (OrganizableByColor)Organizations["color"];
							OrganizableByColor colordata = (OrganizableByColor)metadataCollection.GetOrganized("color");
							Color c;
							String ctxt = "";
							if (colordata != null && colordata.ContainsId(index)) {
								c = colordata.Color(index);
								ctxt = Environment.NewLine + "Color: [" + c.R + "," + c.G + "," + c.B + "]";
															}
							MouseTitle.Text = "ID: " + index + ctxt;
							
/*							for (int i = 0; i <= index % 4; i++) {
								MouseTitle.Text += "Image" + index + " " + "Image" + index + "\n";
							}
*/							MouseTitle.Text = MouseTitle.Text.TrimEnd(new char[1] { '\n' });
							//}
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
					selectedImagesIds = new List<int>();
					MultiScaleSubImage img;
					int id;
					for (double x = p1LogicalX; x <= p2LogicalX; x++) {
						for (double y = p1LogicalY; y <= p2LogicalY; y++) {
							if (canvasIndex.ContainsKey(x + ";" + y)) {
								img = msi.SubImages[canvasIndex[x + ";" + y]];
								img.Opacity = 0.3;
								//img.SetValue(BorderBrushProperty, new SolidColorBrush(Colors.Green));
								selectedImages.Add(img);
								selectedImagesIds.Add(canvasIndex[x + ";" + y]);
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

			msi.ViewportChanged += delegate {
				updateOverlay();
			};
			msi.MotionFinished += delegate {
				showOverlay();
				updateOverlay();
				showOverlay();
			};
			msi.ViewportChanged += delegate {
				hideOverlay();
			};

			App.Current.Host.Content.Resized += new EventHandler(Content_Resized);
			Vorganize_Update();
		}

		void Content_Resized(object sender, EventArgs e) {

		}

		private void showOverlay() {
			Overlays.Opacity = 1;
		}

		private void hideOverlay() {
			Overlays.Opacity = 0;
		}

		void msi_ImageOpenSucceeded(object sender, RoutedEventArgs e) {
			for (int j = 0; j < msi.SubImages.Count; j++) {
				allImageIds.Add(j);
			}
			canvasIndex = new Dictionary<string, int>();
			Double imgAR = msi.SubImages[0].AspectRatio;
			Double imgWidth = msi.SubImages[0].ViewportWidth;
			Double imgHeight = imgWidth * imgAR;
			CalculateHcellsVcells(true);

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

		private KeyValuePair<double, double> CalculateHcellsVcells() {
			return CalculateHcellsVcells(0, false);
		}

		private KeyValuePair<double, double> CalculateHcellsVcells(int imgCount) {
			return CalculateHcellsVcells(imgCount, false);
		}

		private KeyValuePair<double, double> CalculateHcellsVcells(Boolean setGlobals) {
			return CalculateHcellsVcells(0, setGlobals);
		}

		private KeyValuePair<double, double> CalculateHcellsVcells(int imgCount, Boolean setGlobals) {
			Double canvasWidth = msi.ActualWidth;
			Double canvasHeight = msi.ActualHeight;

			Double ratio = canvasWidth / canvasHeight;
			Double canvasRatio = canvasWidth / canvasHeight;

			if (imgCount <= 0) {
				imgCount = msi.SubImages.Count;
			}

			int canHold = 1;
			double Hcells = 1;
			double Vcells = 1;
			while (canHold < imgCount) {
				Hcells++;
				Vcells = Convert.ToInt32(Math.Floor(Hcells / ratio));
				canHold = Convert.ToInt32(Hcells * Vcells);
			}
			if (setGlobals) {
				this.Hcells = Hcells;
				this.Vcells = Vcells;
			}
			return new KeyValuePair<double, double>(Hcells, Vcells);
		}

		void msi_Loaded(object sender, RoutedEventArgs e) {
		}


		private void Zoom(Double newzoom, Point p) {
			if (newzoom < 1) {
				ShowAllContent();
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

		private void makeRullerCells(double Hcells, double Vcells) {
			XaxisGrid.ColumnDefinitions.Clear();
			YaxisGrid.RowDefinitions.Clear();
			XaxisGrid.Children.Clear();
			YaxisGrid.Children.Clear();

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
			b.Child = txt;

			return b;
		}

		private bool AskForMetadata() {
			List<String> failed = new List<string>();

			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Filter = "All Files (*.*)|*.*";
			ofd.FilterIndex = 1;

			String s;
			try {
				if (ofd.ShowDialog() != true) {
					return false;
				}
			} catch (SecurityException e) {
				System.Windows.Browser.HtmlPage.Window.Alert("Not allowed to open the Open File Dialog Box :(" + Environment.NewLine + e.Message);
				return false;
			}
			foreach (FileInfo file in ofd.Files) {
				StreamReader stream = file.OpenText();
				metadataCollection.ParseXML(stream);
				Vorganize_Update();
				stream.Close();
			}
			if (failed.Count == 1) {
				System.Windows.Browser.HtmlPage.Window.Alert("Metadata reading failed on the file " + failed[0]);
			} else if (failed.Count > 1) {
				String failedNames = "";
				foreach (String fn in failed) {
					failedNames += Environment.NewLine + " - " + fn;
				}
				System.Windows.Browser.HtmlPage.Window.Alert("Metadata reading failed on the files: " + failedNames);
			}
				return true;
		}


		private void orderByGroupsVertically(List<KeyValuePair<String, List<int>>> Groups) {
			List<int> groupSizes = new List<int>();
			List<string> groupNames = new List<string>();
			int total = 0;
			foreach (KeyValuePair<String, List<int>> group in Groups) {
				groupNames.Add(group.Key);
				groupSizes.Add(group.Value.Count);
				total += group.Value.Count;
			}

			Hcells = 1;
			Vcells = 1;

			foreach (int row in groupSizes) {
				if (row > Vcells) {
					Vcells = row;
				}
			}

			Hcells = groupSizes.Count;
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

			foreach (KeyValuePair<String, List<int>> group in Groups) {
				foreach (int id in group.Value) {
					msi.SubImages[id].ViewportOrigin = new Point(-x, -y);
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
			Hcells = Math.Max(Hcells, HcellsTmp);
			msi.ViewportWidth = Hcells;
			zoom = 1;
			ShowAllContent();

			List<KeyValuePair<string, int>> groups = new List<KeyValuePair<string, int>>();
			for (int i = 0; i < groupNames.Count; i++) {
				groups.Add(new KeyValuePair<string, int>(groupNames[i], Convert.ToInt32(Math.Ceiling(groupSizes[i] / Hcells))));
			}
			makeAnAxis("X", groups);
			makeAnAxis("Y", Vcells);
		}


		private void orderByGroupsHorizontally() {
			if (dateCollection == null && !AskForMetadata()) {
				return;
			}

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
			double rowsNeeded = 0;
			double NewVcells;
			while (true) {
				rowsNeeded = 0;
				// Determina o que acontece ao reduzir a largura
				foreach (int row in groupSizes) {
					rowsNeeded += Math.Ceiling(row / (Hcells - 1));
				}

				// se for viavel reduzir a largura, faz isso mesmo.
				NewVcells = Convert.ToInt32(Math.Ceiling(msi.ActualHeight * (Hcells - 1) / msi.ActualWidth));
				if (rowsNeeded <= NewVcells) {
					Hcells--;
					Vcells = NewVcells;
				} else {
					//Vcells = Math.Max(rowsNeeded, Convert.ToInt32(Math.Ceiling(msi.ActualHeight * Hcells / msi.ActualWidth)));
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
			ShowAllContent();

			List<KeyValuePair<string, int>> groups = new List<KeyValuePair<string, int>>();
			for (int i = 0; i < groupNames.Count; i++) {
				groups.Add(new KeyValuePair<string, int>(groupNames[i], Convert.ToInt32(Math.Ceiling(groupSizes[i] / Hcells))));
			}
			makeAnAxis("Y", groups);
			makeAnAxis("X", Hcells);
		}

		private void makeAnAxis(String XorY, double n) {
			List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
			for (int i = 1; i <= n; i++) {
				list.Add(new KeyValuePair<string, int>(i.ToString(), 1));
			}
			makeAnAxis(XorY, list);
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
			ShowAllContent();
		}

		private void ShowAllContent() {
			if (Math.Round(Hcells) == 1 && Math.Round(Vcells) == 1 && (msi.ActualHeight < msi.ActualWidth)) {
				msi.ViewportWidth = msi.ActualWidth / msi.ActualHeight;
				this.msi.ViewportOrigin = new Point(-(((msi.ActualWidth / msi.ActualHeight) - 1) / 2), 0);
			} else {
				this.msi.ViewportWidth = Hcells;
				this.msi.ViewportOrigin = new Point(0, 0);
			}
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
		private void ArrangeIntoGrid(List<int> imgList, double totalColumns, double totalRows) {
			canvasIndex = new Dictionary<string, int>();
			int numberOfImages = imgList.Count();

			int totalImagesAdded = 0;

			for (int row = 0; row < totalRows; row++) {
				for (int col = 0; col < totalColumns; col++) {
					if (numberOfImages != totalImagesAdded) {
						int imgId = imgList[totalImagesAdded];
						MultiScaleSubImage currentImage = msi.SubImages[imgId];

						Point currentPosition = currentImage.ViewportOrigin;
						Point futurePosition = new Point(-col, -row);
						canvasIndex.Add(col + ";" + row, imgId);

						// Set up the animation to layout in grid
						Storyboard moveStoryboard = new Storyboard();

						// Create Animation
						//PointAnimationUsingKeyFrames moveAnimation = new PointAnimationUsingKeyFrames();
						PointAnimation moveAnimation = new PointAnimation();

						QuadraticEase easeing = new QuadraticEase();
						easeing.EasingMode = EasingMode.EaseInOut;
						moveAnimation.EasingFunction = easeing;
						moveAnimation.Duration = new Duration(TimeSpan.FromSeconds(1));
						moveAnimation.To = futurePosition;

						Storyboard.SetTarget(moveAnimation, currentImage);
						Storyboard.SetTargetProperty(moveAnimation, new PropertyPath("ViewportOrigin"));

						moveStoryboard.Children.Add(moveAnimation);
						msi.Resources.Add("unique_id", moveStoryboard);

						// Play Storyboard
						moveStoryboard.Begin();

						// Now that the Storyboard has done it's work, clear the MultiScaleImage resources.
						msi.Resources.Clear();

						totalImagesAdded++;
					} else {
						break;
					}
				}
			}
		}

		private List<int> RandomizedListOfImages(List<int> idList) {
			Random ranNum = new Random();

			int numImages = idList.Count;

			// Randomize Image List
			for (int i = 0; i < numImages; i++) {
				int tempImage = idList[i];
				idList.RemoveAt(i);
				int ranNumSelect = ranNum.Next(idList.Count);
				idList.Insert(ranNumSelect, tempImage);
			}
			return idList;
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
			if (Hcells == 0 || Vcells == 0) {
				return;
			}
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
			foreach (Border border in YaxisGrid.Children) {
				elmHeight = border.RenderSize.Height;
				elmEnd = elmStart + elmHeight;
				labelHeight = border.Child.RenderSize.Height;
				label = (TextBlock)border.Child;

				if (elmStart >= visibleStart) {					// inicio visivel
					if (elmEnd > visibleEnd) {					// fim !visivel
						newTop = (visibleEnd - elmStart - labelHeight) / 2;
						CustomLabelPosition(label, newTop);
						break;			// toda a área visivel está preenchida, o resto n interessa
					} else {			// elemento completamente visivel -> posição auto
						CustomLabelPosition(label, -1);
						elmStart = elmEnd;
					}
				} else if (elmEnd > visibleStart) {		// elemento não está completamente out
					if (elmEnd <= visibleEnd) {			// inicio !visivel, fim visivel
						newTop = (elmEnd - visibleStart - labelHeight) / 2 + visibleStart - elmStart;
						CustomLabelPosition(label, newTop);
						elmStart = elmEnd;
					} else {														// inicio e fim !visivel
						newTop = (visibleEnd - visibleStart - labelHeight) / 2 + visibleStart - elmStart;
						CustomLabelPosition(label, newTop);
						break;
					}
				} else {		// elemento está completamente fora de vista
					CustomLabelPosition(label, -1);
					elmStart = elmEnd;
				}
			}
		}

		private void CustomLabelPosition(TextBlock label, double newTop) {
			if (newTop == -1) {	//Reset
				label.Margin = new Thickness(0);
				label.VerticalAlignment = VerticalAlignment.Center;
			} else {
				label.Margin = new Thickness(0, Math.Max(0, newTop), 0, 0);
				label.VerticalAlignment = VerticalAlignment.Top;
			}
		}

		private void ghDate_Click(object sender, RoutedEventArgs e) {
			orderByGroupsHorizontally();
		}

		private void gvDate_Click(object sender, RoutedEventArgs e) {
			//orderByGroupsVertically();
		}

		private void OnlySelected_Click(object sender, RoutedEventArgs e) {
			if (selectedImagesIds.Count == 0) {
				return;
			}
			IEnumerable<int> notSelected = allImageIds.Except(selectedImagesIds);

			fadeImages(notSelected, FadeAnimation.Out);
			fadeImages(selectedImagesIds, FadeAnimation.In);
			CalculateHcellsVcells(selectedImagesIds.Count, true);
			selectedImagesIds.Sort();
			ArrangeIntoGrid(selectedImagesIds, Hcells, Vcells);
			ShowAllContent();
		}

		enum FadeAnimation { In, Out };

		private void fadeImages(IEnumerable<int> ids, FadeAnimation type) {
			MultiScaleSubImage image;
			foreach (int id in ids) {
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

		private void resetbtn_Click(object sender, RoutedEventArgs e) {
			selectedImages = new List<MultiScaleSubImage>();
			selectedImagesIds = new List<int>();
			CalculateHcellsVcells(true);
			fadeImages(allImageIds, FadeAnimation.In);
			ArrangeIntoGrid(allImageIds, Hcells, Vcells);
			ShowAllContent();
			makeAnAxis("Y", Vcells);
			makeAnAxis("X", Hcells);
		}

		private void random_Click(object sender, RoutedEventArgs e) {
			if (selectedImagesIds.Count != 0) {
				ArrangeIntoGrid(RandomizedListOfImages(selectedImagesIds), Hcells, Vcells);
			} else {
				ArrangeIntoGrid(RandomizedListOfImages(allImageIds), Hcells, Vcells);
			}
		}

		private void Vorganize_Update() {
			if (CbItems == null) {
				CbItems = new ObservableCollection<string>();
				Vorganize.ItemsSource = CbItems;
			}
			CbItems.Clear();
			foreach (String s in metadataCollection.GetOrganizationOptions()) {
				CbItems.Add(s);
			}
		}

		private void Vorganize_DropDownOpened(object sender, EventArgs e) {
		}

		private void Vorganize_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			String selected = (String)Vorganize.SelectedItem;
			if (selected == null) {
				return;
			}
			if (selected == "Import Metadata") {
				AskForMetadata();
				Vorganize_Update();
			} else if (selected == "Not Sorted") {
			} else {
				if (selected == "Color") {
					orderByGroupsVertically(metadataCollection.GetOrganized("color").GetGroups());
				} else {
					orderByGroupsVertically(metadataCollection.GetOrganized(selected).GetGroups());
				}
				Vorganize.IsDropDownOpen = false;
			}
		}

		private void LoadMetadata(object sender, RoutedEventArgs e) {
			AskForMetadata();
		}
	}
}
