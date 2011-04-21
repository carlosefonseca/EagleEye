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

namespace DeepZoomView {
	public partial class Page : UserControl {
		// Based on prior work done by Lutz Gerhard, Peter Blois, and Scott Hanselman
		double zoom = 1;
		bool duringDrag = false;
		bool mouseDown = false;
		Point lastMouseDownPos = new Point();
		Point lastMousePos = new Point();
		Point lastMouseViewPort = new Point();
		int Hcells;
		double Vcells;
		long _LastIndex = -1;
		Dictionary<long, string> _Metadata = new Dictionary<long, string>();
		Dictionary<string, int> canvasIndex = new Dictionary<string, int>();

		public double ZoomFactor {
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

			this.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e) {
				lastMouseDownPos = e.GetPosition(msi);
				lastMouseViewPort = msi.ViewportOrigin;

				mouseDown = true;

				msi.CaptureMouse();
			};

			this.MouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e) {
				if (!duringDrag) {
					bool shiftDown = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
					bool ctrlDown = true;// (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Control;
					double newzoom = zoom;

					if (ctrlDown) {
						int index = GetSubImageIndex(e.GetPosition(msi));
						msi.ViewportWidth = 1.5;
						msi.ViewportOrigin = new Point(-msi.SubImages[index].ViewportOrigin.X, -msi.SubImages[index].ViewportOrigin.Y);
						//zoom = Hcells / msi.ViewportWidth;
						//						updateOverlay();
					} else if (shiftDown) {
						newzoom /= 2;
					} else {
						newzoom *= 2;
					}

					Zoom(newzoom, msi.ElementToLogicalPoint(this.lastMousePos));
				}
				duringDrag = false;
				mouseDown = false;

				msi.ReleaseMouseCapture();
			};

			this.MouseMove += delegate(object sender, MouseEventArgs e) {
				lastMousePos = e.GetPosition(msi);
				if (mouseDown && !duringDrag) {
					duringDrag = true;
					/*double w = msi.ViewportWidth;
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
				}
			};

			new MouseWheelHelper(this).Moved += delegate(object sender, MouseWheelEventArgs e) {
				e.Handled = true;

				double newzoom = zoom;

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
			};
			msi.ViewportChanged += delegate {
				Caption.Text = "Viewport Changed!!!1";
			};
		}

		void msi_ImageOpenSucceeded(object sender, RoutedEventArgs e) {
			double visWidth = msi.RenderSize.Width;
			double visHeight = msi.RenderSize.Height;

			double ratio = visHeight / visWidth;

			Hcells = msi.SubImages.Count;
			Vcells = 1;

			while (Vcells < Hcells * ratio) {
				Hcells--;
				Vcells = Math.Ceiling(msi.SubImages.Count / Hcells);
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
		}

		void msi_Loaded(object sender, RoutedEventArgs e) {
			// Hook up any events you want when the image has successfully been opened
		}

		private void Zoom(double newzoom, Point p) {
			if (newzoom < 1) {
				GoHomeClick(null, null);
			} else {
				msi.ZoomAboutLogicalPoint(newzoom / zoom, p.X, p.Y);
				zoom = newzoom;
			}
		}

		private void ZoomInClick(object sender, System.Windows.RoutedEventArgs e) {
			//Zoom(zoom * 1.3, msi.ElementToLogicalPoint(new Point(.5 * msi.ActualWidth, .5 * msi.ActualHeight)));
			ArrangeIntoGrid();
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

		public Rect ZoomAboutPoint(Rect img, double zAmount, Point pt) {
			return new Rect(pt.X + (img.X - pt.X) / zAmount, pt.Y + (img.Y - pt.Y) / zAmount, img.Width / zAmount, img.Height / zAmount);
		}

		public void LayoutDZI(Rect rect) {
			double ar = msi.AspectRatio;
			msi.ViewportWidth = 1 / rect.Width;
			msi.ViewportOrigin = new Point(-rect.Left / rect.Width, -rect.Top / rect.Width);
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
			double imgLogicalX = Math.Floor(msi.ViewportOrigin.X + msi.ViewportWidth * (point.X / msi.ActualWidth));
			double imgLogicalY = Math.Floor(msi.ViewportOrigin.Y + (msi.ViewportWidth * (msi.ActualHeight / msi.ActualWidth)) * (point.Y / msi.ActualHeight));
			try {
				return canvasIndex[imgLogicalX + ";" + imgLogicalY];
			} catch {
				return -1;
			}
		}

		private int GetSubImageIndex2(Point point) {
			// Hit-test each sub-image in the MultiScaleImage control to determine
			// whether  "point " lies within a sub-image
			for (int i = msi.SubImages.Count - 1; i >= 0; i--) {
				MultiScaleSubImage image = msi.SubImages[i];
				double width = msi.ActualWidth /
					(msi.ViewportWidth * image.ViewportWidth);
				double height = msi.ActualWidth /
					(msi.ViewportWidth * image.ViewportWidth * image.AspectRatio);



				Point pos = msi.LogicalToElementPoint(new Point(
					-image.ViewportOrigin.X / image.ViewportWidth,
					-image.ViewportOrigin.Y / image.ViewportWidth)
				);
				Rect rect = new Rect(pos.X, pos.Y, width, height);

				if (rect.Contains(point)) {
					long i2 = GetSubImageIndex2(point);
					// Return the image index
					return i;
				}
			}

			// No corresponding sub-image
			return -1;
		}

		private void updateOverlay() {
			zoom = Hcells / msi.ViewportWidth;
			double newX = (msi.ViewportOrigin.X * (msi.ActualWidth / Hcells)) * zoom;
			double newY = (msi.ViewportOrigin.Y * (msi.ActualHeight / Vcells)) * zoom;
			double newH = msi.ActualHeight * zoom;
			double newW = msi.ActualWidth * zoom;
			
			if ((Double)Overlays.GetValue(Canvas.TopProperty) != -newY) {
				Overlays.SetValue(Canvas.TopProperty, -newY);
			}
			if ((Double)Overlays.GetValue(Canvas.LeftProperty) != -newX) {
			Overlays.SetValue(Canvas.LeftProperty, -newX);
			}
			OverlaysScale.ScaleX = zoom;
			OverlaysScale.ScaleY = zoom;
			/*
			if (Overlays.Height != newH) {
				Overlays.Height = newH;
			}
			if (Overlays.Width != newW) {
				Overlays.Width = newW;
			}*/
		}
	}
}
