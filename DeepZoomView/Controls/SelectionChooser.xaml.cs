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
using System.Windows.Controls.Primitives;

namespace DeepZoomView.Controls
{
	public partial class SelectionChooser : UserControl
	{
		public event EvHandler SelectionHandler;
		private String defaultButtontext;


		public SelectionChooser()
		{
			InitializeComponent();
			defaultButtontext = (String)button.Content;
		}

		private void showPopup_Click(object sender, RoutedEventArgs e)
		{
			if (!button.IsChecked.Value)
			{
				SetDefault();
			}
			else
			{

				if (popup.IsOpen)
				{
					popup.IsOpen = false;
				}
				else
				{
					popup.IsOpen = true;
					popuplist.Width = button.ActualWidth;
					popuplist.SelectedIndex = -1;
				}
			}
		}

		private Rect GetElementPosition(UIElement element)
		{
			// Obtain transform information based off root element
			GeneralTransform gt = element.TransformToVisual(Application.Current.RootVisual);

			// Find the four corners of the element
			Point topLeft = gt.Transform(new Point(0, 0));
			//Point topRight = gt.Transform(new Point(element.RenderSize.Width, 0));
			//Point bottomLeft = gt.Transform(new Point(0, element.RenderSize.Height));
			Point bottomRight = gt.Transform(new Point(element.RenderSize.Width, element.RenderSize.Height));

			return new Rect(topLeft, bottomRight);
		}

		private void popuplist_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (popuplist.SelectedIndex != -1)
			{
				SelectionHandler(this, new MyEventArgs((String)((ListBoxItem)this.popuplist.SelectedItem).Content));
				popup.IsOpen = false;
			}
		}

		public void SetActive(String btnText)
		{
			button.Content = btnText;
			button.IsChecked = true;	
		}

		private void SetDefault()
		{
			button.Content = defaultButtontext;
			button.IsChecked = false;
		}
	}
}
