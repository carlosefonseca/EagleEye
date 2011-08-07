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
		/// <summary>
		/// The selected button
		/// </summary>
		public string active;
		public MyEventArgs(String s)
		{
			active = s;
		}
	}

	/// <summary>
	/// A set of buttons next to each other that mimic the radio button behaviour.
	/// </summary>
	public class SegmentedControl : Canvas
	{
		/// <summary>
		/// Event to run when the selected button changes. The handler will receive the text of the selected button.
		/// </summary>
		public event EvHandler OnChangeSelected;

		List<RadioButton> buttons = new List<RadioButton>();
		Border border;
		StackPanel stack;

		/// <summary>
		/// Gets or sets the currently selected button.
		/// </summary>
		public string Selected
		{
			get
			{
				return (String)buttons.First(r => r.IsChecked.Value).Content;
			}
			set
			{
				buttons.First(r => value.Equals(r.Content)).IsChecked = true;
				OnChangeSelected(this, new MyEventArgs(value));
			}
		}

		/// <summary>
		/// Creates a new Segmented Control element.
		/// </summary>
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
			foreach (RadioButton b in buttons)
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
			r.Height = 24;
			r.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
			r.Fill = new SolidColorBrush(Color.FromArgb(255, 80, 80, 80));
			return r;
		}

		/// <summary>
		/// Creates the visible button element for the given name.
		/// </summary>
		/// <param name="name">The text that will be displayed on the button</param>
		/// <returns></returns>
		private RadioButton NewButton(string name)
		{
			RadioButton r = new RadioButton();
			r.Content = name;
			// Couldn't make it work any other way :(
			StreamReader stream = new StreamReader(App.GetResourceStream(new Uri("SegmentedControlStyle.xaml", UriKind.Relative)).Stream);
			ControlTemplate ct = (ControlTemplate)XamlReader.Load(stream.ReadToEnd());
			r.Template = ct;
			r.Checked += new RoutedEventHandler(b_Checked);
			return r;
		}
		
		/// <summary>
		/// Handles the selection of a button and calls the user defined event with the text of the newly selected button.
		/// </summary>
		/// <param name="sender">The pressed button</param>
		/// <param name="e"></param>
		private void b_Checked(object sender, RoutedEventArgs e)
		{
			if (OnChangeSelected != null)
			{
				String txt = (String)(((RadioButton)sender).Content);
				OnChangeSelected(this, new MyEventArgs(txt));
			}
		}
	}
}
