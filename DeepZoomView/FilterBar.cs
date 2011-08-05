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

		private InlineCollection Inlines { get { return ((Paragraph)this.Blocks.First()).Inlines; } }

		private IEnumerable<FilterButton> FilterButtons { get { return Inlines.Where(i => i.GetType() == typeof(InlineUIContainer)).Select(i => ((InlineUIContainer)i).Child).Cast<FilterButton>(); } }

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
			else
			{
				/*				Paragraph theParagraph = ((Paragraph)this.Blocks.First());
								IEnumerable<Inline> runs = theParagraph.Inlines.Where(i => i.GetType() == typeof(Run));
								Run txt = (Run)(runs.Last());
								txt.SetValue(TextBlock.PaddingProperty, new Thickness(5));
					*/
			}
		}


	}



	public class FilterButton : Button
	{
		public enum FilterType { Text, Keyword, Path, Date, Color }

		public FilterType type = FilterType.Text;
		public String text;

		const String sb =
			"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
				"xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"xmlns:data='clr-namespace:System.Windows.Controls;assembly=System.Windows.Controls.Data' " +
				"xmlns:mc='http://schemas.openxmlformats.org/markup-compatibility/2006' " +
				"TargetType='Button' >" +
				"<Border x:Name=\"Border\" Background=\"Silver\" CornerRadius=\"7\" Padding=\"3,0,3,0\" Margin=\"1,0,0,1\">" +
					"<ContentPresenter VerticalAlignment=\"Bottom\" HorizontalAlignment=\"Center\" />" +
				"</Border>" +
			"</ControlTemplate>";

		public FilterButton(String txt)
			: base()
		{
			Content = txt;
			text = txt;
			ControlTemplate ct = (ControlTemplate)XamlReader.Load(sb);
			Template = ct;
		}
	}
}
