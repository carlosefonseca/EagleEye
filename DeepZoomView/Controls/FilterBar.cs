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

namespace DeepZoomView.Controls
{
	public class FilterBar : RichTextBox
	{
		private ListBox optionList = new ListBox();
		private Run placeholderText;
		

		/// <summary>
		/// Creates a new Filterbar
		/// </summary>
		public FilterBar()
			: base()
		{
			Paragraph p = new Paragraph();
			placeholderText = new Run();
			placeholderText.Text = " Filters";
			placeholderText.Foreground = new SolidColorBrush(Colors.LightGray);
			placeholderText.FontStyle = System.Windows.FontStyles.Italic;
			p.Inlines.Add(placeholderText);
			this.Blocks.Add(p);
		}

		/// <summary>
		/// When the control gets focus, clears the placeholder text
		/// </summary>
		/// <param name="e"></param>
		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);
			if (Inlines.Contains(placeholderText))
			{
				Inlines.Remove(placeholderText);
			}
			IsReadOnly = false;
		}

		/// <summary>
		/// When the control gets focus, places placeholder text
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLostFocus(RoutedEventArgs e)
		{
			base.OnLostFocus(e);
			if (Inlines.Count == 0)
			{
				Inlines.Add(placeholderText);
			}
			IsReadOnly = true;
		}

		/// <summary>
		/// Gets the paragraph inlines (Runs, buttons...)
		/// </summary>
		private InlineCollection Inlines { get { return ((Paragraph)this.Blocks.First()).Inlines; } }

		/// <summary>
		/// Gets all buttons in the filter bar. Text not transformed into button is not included.
		/// </summary>
		public IEnumerable<FilterButton> FilterButtons { get { return Inlines.Where(i => i.GetType() == typeof(InlineUIContainer)).Select(i => ((InlineUIContainer)i).Child).Cast<FilterButton>(); } }

		/// <summary>
		/// Gets the text of the buttons in the filter bar. Text not transformed into button is not included.
		/// </summary>
		public List<String> GetFilterElementsAsText
		{
			get
			{
				List<String> list = new List<String>();
				foreach (FilterButton i in FilterButtons)
				{
					list.Add(i.text);
				}
				return list;
			}
		}

		/// <summary>
		/// Handles key presses
		/// </summary>
		/// <param name="e">The key pressed</param>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			if (e == null)
			{
				Paragraph theParagraph = new Paragraph();
				this.Blocks.Add(theParagraph);

				InlineUIContainer uic = new InlineUIContainer();
				uic.Child = new FilterButton("b1");
				theParagraph.Inlines.Add(uic);

				uic = new InlineUIContainer();
				uic.Child = new FilterButton("Hello World"); ;
				theParagraph.Inlines.Add(uic);

				uic = new InlineUIContainer();
				uic.Child = new FilterButton("b3");
				theParagraph.Inlines.Add(uic);

				return;
			}

			if (e.Key == Key.Enter)
			{
				Paragraph theParagraph = ((Paragraph)this.Blocks.First());

				if (!theParagraph.Inlines.Any(i => i.GetType() == typeof(Run)))
				{
					return;
				}

				IEnumerable<Inline> runs = theParagraph.Inlines.Where(i => i.GetType() == typeof(Run));

				Run txt = (Run)(runs.Last());

				InlineUIContainer uic = new InlineUIContainer();
				uic.Child = new FilterButton(txt.Text);

				int index = theParagraph.Inlines.IndexOf(txt);
				theParagraph.Inlines.RemoveAt(index);
				theParagraph.Inlines.Insert(index, uic);
				//theParagraph.Inlines.Add(uic);
			}
		}

		public void PaintButtonRed(String t)
		{
			FilterButtons.First(b => t.Equals((String)b.Content)).Paint(Colors.Red);
		}
	} // FilterBar


	/// <summary>
	/// The buttons used inside the Filter Bar
	/// </summary>
	public class FilterButton : Button
	{
		public enum FilterType { Text, Keyword, Path, Date, Color }

		public FilterType type = FilterType.Text;
		public String text;
		public String tooltip = "Test";

		const String sb =
			"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
				"xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"xmlns:data='clr-namespace:System.Windows.Controls;assembly=System.Windows.Controls.Data' " +
				"xmlns:mc='http://schemas.openxmlformats.org/markup-compatibility/2006' " +
				"TargetType='Button' >" +
				"<Border x:Name=\"Border\" Background=\"{TemplateBinding Background}\" CornerRadius=\"7\" Padding=\"3,0,3,0\" Margin=\"1,0,0,1\" "+
														 "ToolTipService.ToolTip=\"ToolTip to the left.\"  ToolTipService.Placement=\"Mouse\" >" +
					"<ContentPresenter VerticalAlignment=\"Bottom\" HorizontalAlignment=\"Center\" />" +
				"</Border>" +
			"</ControlTemplate>";

		/// <summary>
		/// Creates a button with the included text
		/// </summary>
		/// <param name="txt">The text to include in the button</param>
		public FilterButton(String txt)
			: base()
		{
			Content = txt;
			text = txt;
			ControlTemplate ct = (ControlTemplate)XamlReader.Load(sb);
			Template = ct;
			Paint();
		}

		/// <summary>
		/// Gets the text of the button
		/// </summary>
		public String Text { get { return (String)Content; } }

		/// <summary>
		/// Paint the button with a color.
		/// </summary>
		/// <param name="c">The color to paint the button with.</param>
		public void Paint(Color c)
		{
			this.Background = new SolidColorBrush(c);
		}

		/// <summary>
		/// Paint the button with the default color.
		/// </summary>
		public void Paint()
		{
			this.Background = new SolidColorBrush(Color.FromArgb(255, 0xC0, 0xC0, 0xC0));
		}
	}
}
