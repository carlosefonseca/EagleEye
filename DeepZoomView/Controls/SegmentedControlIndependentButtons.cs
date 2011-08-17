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
	/// <summary>
	/// A set of buttons next to each other that mimic the radio button behaviour.
	/// </summary>
	public class SegmentedControlIndependentButtons : Canvas
	{
		/// <summary>
		/// Event to run when the selected button changes. The handler will receive the text of the selected button.
		/// </summary>
		public event EvHandler OnChangeSelected;

		List<Button> buttons = new List<Button>();
		Border border;
		StackPanel stack;


		/// <summary>
		/// Creates a new Segmented Control element.
		/// </summary>
		public SegmentedControlIndependentButtons()
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

		/// <summary>
		/// Handles the size change of the stack element (Buttons added or removed).
		/// </summary>
		/// <param name="sender">The Stack panel that changed (also accessible by this.stack)</param>
		/// <param name="e"></param>
		private void StackSizeChanged(object sender, SizeChangedEventArgs e)
		{
			StackClip((StackPanel)sender);
			ResizeCanvas();
		}

		/// <summary>
		/// Resizes the outter canvas to fit the contents.
		/// </summary>
		private void ResizeCanvas()
		{
			this.Width = border.ActualWidth;
			this.Height = border.ActualHeight;
		}

		/// <summary>
		/// Clips the Stack so it has round corners.
		/// </summary>
		/// <param name="stack"></param>
		void StackClip(StackPanel stack)
		{
			RectangleGeometry r = new RectangleGeometry();
			r.Rect = new Rect(0, 0, stack.ActualWidth, stack.ActualHeight);
			r.RadiusX = 4;
			r.RadiusY = 4;
			stack.Clip = r;
		}

		/// <summary>
		/// Creates the buttons on the element.
		/// </summary>
		/// <param name="buttonNames">A list with all button names.</param>
		public void SetButtons(List<String> buttonNames)
		{
			buttonNames.ForEach(n => buttons.Add(NewButton(n)));
			foreach (Button b in buttons)
			{
				if (this.stack.Children.Count != 0)
				{
					this.stack.Children.Add(MakeSplitLine());
				}
				this.stack.Children.Add(b);
			}
		}
		/// <summary>
		/// Creates the buttons on the element.
		/// </summary>
		/// <param name="buttonNames">A list with all button names.</param>
		public void SetButtons(Dictionary<String, RoutedEventHandler> buttonsInfo)
		{
			buttonsInfo.ToList().ForEach(kv => buttons.Add(NewButton(kv.Key, kv.Value)));
			foreach (Button b in buttons)
			{
				if (this.stack.Children.Count != 0)
				{
					this.stack.Children.Add(MakeSplitLine());
				}
				this.stack.Children.Add(b);
			}
		}

		/// <summary>
		/// Creates the line that splits the buttons
		/// </summary>
		/// <returns></returns>
		private UIElement MakeSplitLine()
		{
			Rectangle r = new Rectangle();
			r.Width = 1;
			r.Height = 20;
			r.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
			r.Fill = new SolidColorBrush(Color.FromArgb(255, 80, 80, 80));
			return r;
		}

		/// <summary>
		/// Creates the visible button element for the given name.
		/// </summary>
		/// <param name="name">The text that will be displayed on the button</param>
		/// <returns></returns>
		private Button NewButton(string name)
		{
			Button r = new Button();
			r.Content = name;
			// Couldn't make it work any other way :(
			StreamReader stream = new StreamReader(App.GetResourceStream(new Uri("SegmentedControlStyle.xaml", UriKind.Relative)).Stream);
			ControlTemplate ct = (ControlTemplate)XamlReader.Load(stream.ReadToEnd());
			r.Template = ct;
			r.Click += new RoutedEventHandler(b_Pressed);
			return r;
		}


		private Button NewButton(string name, RoutedEventHandler evHandler)
		{
			Button r = new Button();
			r.Content = name;
			// Couldn't make it work any other way :(
			StreamReader stream = new StreamReader(App.GetResourceStream(new Uri("SegmentedControlStyle.xaml", UriKind.Relative)).Stream);
			ControlTemplate ct = (ControlTemplate)XamlReader.Load(stream.ReadToEnd());
			r.Template = ct;
			r.MinWidth = 1;
			r.Click += evHandler;
			return r;
		}
		
		/// <summary>
		/// Handles the selection of a button and calls the user defined event with the text of the newly selected button.
		/// </summary>
		/// <param name="sender">The pressed button</param>
		/// <param name="e"></param>
		private void b_Pressed(object sender, RoutedEventArgs e)
		{
			if (OnChangeSelected != null)
			{
				String txt = (String)(((Button)sender).Content);
				OnChangeSelected(this, new MyEventArgs(txt));
			}
		}
	}
}
