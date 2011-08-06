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
using System.Windows.Markup;
using System.IO;

namespace DeepZoomView.Controls
{
	public delegate void EvHandler(object sender, MyEventArgs e);

	public class MyEventArgs : EventArgs
	{
		public string active;
		public MyEventArgs(String s)
		{
			active = s;
		}
	}

	public class SegmentedControl : Canvas
	{
		public event EvHandler OnChangeSelected;

		List<RadioButton> buttons = new List<RadioButton>();
		Border border;
		StackPanel stack;

		public string Selected
		{
			set
			{
				buttons.First(r => value.Equals(r.Content)).IsChecked = true;
				OnChangeSelected(this, new MyEventArgs(value));
			}
		}

		public SegmentedControl()
			: base()
		{
			stack = new StackPanel();
			stack.Orientation = Orientation.Horizontal;

			border = new Border();
			border.Child = stack;
			border.VerticalAlignment = System.Windows.VerticalAlignment.Center;
			border.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
			border.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 80, 80, 80));
			border.BorderThickness = new Thickness(2);
			border.CornerRadius = new CornerRadius(5);

			this.Children.Add(border);

			stack.SizeChanged += new SizeChangedEventHandler(StackSizeChanged);
		}

		void StackSizeChanged(object sender, SizeChangedEventArgs e)
		{
			StackClip((StackPanel)sender);
			ResizeCanvas();
		}

		private void ResizeCanvas()
		{
			this.Width = border.ActualWidth;
			this.Height = border.ActualHeight;
		}

		void StackClip(StackPanel stack)
		{
			RectangleGeometry r = new RectangleGeometry();
			r.Rect = new Rect(0, 0, stack.ActualWidth, stack.ActualHeight);
			r.RadiusX = 4;
			r.RadiusY = 4;
			stack.Clip = r;
		}

		public SegmentedControl(List<String> buttonNames)
			: this()
		{
			buttonNames.ForEach(n => AddButton(n));
			buttons.ForEach(b => this.stack.Children.Add(b));
		}

		public void SetButtons(List<String> buttonNames)
		{
			buttonNames.ForEach(n => AddButton(n));
			foreach (RadioButton b in buttons)
			{
				if (this.stack.Children.Count != 0)
				{
					this.stack.Children.Add(MakeSplitLine());
				}
				this.stack.Children.Add(b);
			}
		}



		private UIElement MakeSplitLine()
		{
			Rectangle r = new Rectangle();
			r.Width = 1;
			r.Height = 24;
			r.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
			r.Fill = new SolidColorBrush(Color.FromArgb(255, 80, 80, 80));
			return r;
		}


		private void AddButton(String name)
		{
			buttons.Add(NewButton(name));
		}

		private RadioButton NewButton(string name)
		{
			RadioButton r = new RadioButton();
			r.Content = name;
			StreamReader stream = new StreamReader(App.GetResourceStream(new Uri("SegmentedControlStyle.xaml", UriKind.Relative)).Stream);
			ControlTemplate ct = (ControlTemplate)XamlReader.Load(stream.ReadToEnd());
			r.Template = ct;
			r.Checked += new RoutedEventHandler(b_Checked);
			return r;
		}
		
		
		void b_Checked(object sender, RoutedEventArgs e)
		{
			if (OnChangeSelected != null)
			{
				String txt = (String)(((RadioButton)sender).Content);
				OnChangeSelected(this, new MyEventArgs(txt));
			}
		}
	}
}
