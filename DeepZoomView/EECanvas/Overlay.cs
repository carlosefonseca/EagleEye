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
using System.Linq;

namespace DeepZoomView.EECanvas
{
	public class Overlay
	{
		private enum DisplayTypes { none, Border, ColorOverlay }

		public Shape OverlayShape;
		public String Name;
		public Color BackgroundColor;
		private Border textborder = null;
		private Canvas canvas = null;
		private DisplayTypes lastDisplay;
		private Shape placedBorder = null;
		private Shape placedColor = null;

		public Overlay(string name, Shape border, Color color)
		{
			this.Name = name;
			this.OverlayShape = border;
			this.OverlayShape.Stretch = Stretch.Fill;
			this.BackgroundColor = color;
			MakeBorder();
			MakeColor();
			Hide();
		}

		private Shape CopyShape()
		{
			Shape s;
			if (OverlayShape.GetType() == typeof(Rectangle))
			{
				s = new Rectangle();
				s.Width = OverlayShape.Width;
				s.Height = OverlayShape.Height;
			}
			else if (OverlayShape.GetType() == typeof(Polygon))
			{
				s = new Polygon();

				foreach (Point p in ((Polygon)OverlayShape).Points)
				{
					((Polygon)s).Points.Add(p);
				}
			}
			else
			{
				throw new Exception("Overlayshape type not recognized");
			}
			Canvas.SetLeft(s, Canvas.GetLeft(OverlayShape));
			Canvas.SetTop(s, Canvas.GetTop(OverlayShape));
			return s;
		}



		private void MakeBorder()
		{
			placedBorder = CopyShape();
			placedBorder.Stroke = new SolidColorBrush(Colors.White);
			placedBorder.StrokeThickness = 1;
		}

		private void MakeColor()
		{
			placedColor = CopyShape();
			placedColor.Stroke = null;
			placedColor.Fill = new SolidColorBrush(BackgroundColor);
			MakeText();
		}

		public void ShowBorder()
		{
			placedBorder.Visibility = Visibility.Visible;
		}

		public void ShowColor()
		{
			placedColor.Visibility = Visibility.Visible;
			textborder.Visibility = Visibility.Visible;
		}

		public void Hide()
		{
			placedBorder.Visibility = Visibility.Collapsed;
			placedColor.Visibility = Visibility.Collapsed;
			textborder.Visibility = Visibility.Collapsed;
		}


		public void MakeText()
		{
			textborder = new Border();
			TextBlock text = new TextBlock();
			text.Text = this.Name;
			if (ColorUtils.HslColor.FromColor(this.BackgroundColor).L > 0.5)
			{
				text.Foreground = new SolidColorBrush(Colors.Black);
			}
			else
			{
				text.Foreground = new SolidColorBrush(Colors.White);
			}
			text.FontWeight = FontWeights.Bold;
			textborder.Child = text;
			text.TextWrapping = TextWrapping.Wrap;

			if (OverlayShape.GetType() == typeof(Polygon))
			{
				PointCollection pc = ((Polygon)OverlayShape).Points;
				Point topleft = pc.First();
				Point bottomright = pc.First();
				foreach (Point p in pc.Skip(1))
				{
					if (p.X > bottomright.X)
					{
						bottomright.X = p.X;
					}
					if (p.Y > bottomright.Y)
					{
						bottomright.Y = p.Y;
					}
				}
				Canvas.SetLeft(textborder, topleft.X);
				Canvas.SetTop(textborder, topleft.Y);

				textborder.Width = bottomright.X - topleft.X;
				textborder.Height = bottomright.Y - topleft.Y;
			}
			else
			{
				Canvas.SetLeft(textborder, Canvas.GetLeft(OverlayShape));
				Canvas.SetTop(textborder, Canvas.GetTop(OverlayShape));

				textborder.Width = this.OverlayShape.Width;
				textborder.Height = this.OverlayShape.Height;
			}
			//txt.VerticalAlignment = VerticalAlignment.Center;
			//txt.HorizontalAlignment = HorizontalAlignment.Center;


			if (textborder.MinHeight < text.Width)
			{
				RotateTransform rt = new RotateTransform();
				rt.Angle = 90;
				rt.CenterY = 15;
				//txt.RenderTransformOrigin = new Point(0, -txt.Height);
				text.RenderTransform = rt;
			}
		}

		internal void SetLayers(Canvas border, Canvas color)
		{
			border.Children.Add(placedBorder);
			color.Children.Add(placedColor);
			color.Children.Add(textborder);
		}

		internal void HideBorder()
		{
			placedBorder.Visibility = Visibility.Collapsed;
		}
	}
}
