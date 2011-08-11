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
using System.Collections.ObjectModel;

namespace DeepZoomView.Controls
{
	// it was a good idea to change the listbox to use the POPUP class instead of a canvas on the doc root...
	public class FilterBar : RichTextBox
	{
		private ListBox optionList;
		private Run placeholderText;
		public event EvHandler OnTextInsertion;
		private ObservableCollection<AutocompleteOption> acOptions = new ObservableCollection<AutocompleteOption>();
		//IEnumerable<AutocompleteOption> acOptions = new List<AutocompleteOption>();


		public ObservableCollection<AutocompleteOption> AutocompleteOptions
		{
			get { return acOptions; }
			//set { acOptions = value; UpdateAutocomplete(); }
		}

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
			this.AcceptsReturn = false;
			this.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
			this.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
			this.Padding = new Thickness(1);
			this.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
			this.Height = 25;


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
			optionList.Visibility = System.Windows.Visibility.Collapsed;
		}

		private void UpdateAutocomplete()
		{
			if (acOptions.Count() > 0)
			{
				if (optionList.Visibility == System.Windows.Visibility.Collapsed)
				{
					optionList.Visibility = System.Windows.Visibility.Visible;
				}
			}
			else
			{
				optionList.Visibility = System.Windows.Visibility.Collapsed;
			}
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
				if (!Inlines.Any(i => i.GetType() == typeof(Run)))
				{
					return;
				}

				CreateAndAddNewFilterButton();
			}
			else if (e.Key == Key.Down)
			{
				if (optionList.SelectedIndex + 1 < optionList.Items.Count())
				{
					optionList.SelectedIndex++;
				}
			}
			else if (e.Key == Key.Up)
			{
				if (optionList.SelectedIndex > -1)
				{
					optionList.SelectedIndex--;
				}
			}
			else
			{	// This should be on a text changed event
				IEnumerable<Inline> runs = Inlines.Where(i => i.GetType() == typeof(Run));
				if (runs.Count() > 0)
				{
					Run txt = (Run)(runs.Last());
					OnTextInsertion(this, new MyEventArgs(txt.Text));

					if (optionList.Visibility == System.Windows.Visibility.Collapsed)
					{
						optionList.Visibility = System.Windows.Visibility.Visible;
						Rect position = GetElementPosition(this);
						optionList.Width = this.Width;
						optionList.Margin = new Thickness(position.Left, position.Bottom + 4, 0, 0);
					}
				}
				else
				{
					optionList.Visibility = System.Windows.Visibility.Collapsed;
				}
			}
		}

		private void CreateAndAddNewFilterButton()
		{
			IEnumerable<Inline> runs = this.Inlines.Where(i => i.GetType() == typeof(Run));
			Run txt = (Run)(runs.Last());

			InlineUIContainer uic = new InlineUIContainer();

			if (optionList.SelectedIndex != -1)
			{
				uic.Child = new FilterButton(((AutocompleteOption)optionList.SelectedItem).DisplayName);
			}
			else
			{
				uic.Child = new FilterButton(txt.Text);
			}
			int index = Inlines.IndexOf(txt);
			Inlines.RemoveAt(index);
			Inlines.Insert(index, uic);
			optionList.Visibility = System.Windows.Visibility.Collapsed;
		}

		public void PaintButtonRed(String t)
		{
			FilterButtons.First(b => t.Equals((String)b.Content)).Paint(Colors.Red);
		}

		public ListBox AutoCompleteElement
		{
			get { return optionList; }
			set { optionList = value; ConfigureAutocomplete(); }
		}

		private void ConfigureAutocomplete()
		{
			Rect position = GetElementPosition(this);

			optionList.Width = this.Width;
			Canvas.SetZIndex(optionList, 999);
			optionList.MaxHeight = 300;
			optionList.VerticalAlignment = System.Windows.VerticalAlignment.Top;
			optionList.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;

			optionList.DisplayMemberPath = "AutoCompleteText";
			optionList.ItemsSource = this.acOptions;
			/*
			ListBoxItem lbi = new ListBoxItem();
			lbi.Content = "aaaa";
			optionList.Items.Add(lbi);
			lbi = new ListBoxItem();
			lbi.Content = "bbbb aaaa";
			optionList.Items.Add(lbi);*/
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
	} // FilterBar


	/// <summary>
	/// The buttons used inside the Filter Bar
	/// </summary>
	public class FilterButton : Button
	{
		public String type = "Text";
		public String text;
		public String tooltip = "Test";

		const String sb =
			"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
				"xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"xmlns:data='clr-namespace:System.Windows.Controls;assembly=System.Windows.Controls.Data' " +
				"xmlns:mc='http://schemas.openxmlformats.org/markup-compatibility/2006' " +
				"TargetType='Button' >" +
				"<Border x:Name=\"Border\" Background=\"{{TemplateBinding Background}}\" CornerRadius=\"7\" Padding=\"3,0,3,0\" Margin=\"1,0,0,1\" " +
														 "ToolTipService.ToolTip=\"{0}\"  ToolTipService.Placement=\"Mouse\" >" +
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
			ControlTemplate ct = (ControlTemplate)XamlReader.Load(String.Format(sb, "Tooltip placeholder for " + txt));
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

	public class AutocompleteOption
	{
		private String autocompleteText = null;
		public String DisplayName;
		private String key = null;
		public Organizable Organizable = null;

		public AutocompleteOption(String displayName)
		{
			DisplayName = displayName;
		}

		public AutocompleteOption(String displayName, Organizable organizable)
			: this(displayName)
		{
			Organizable = organizable;
		}

		public AutocompleteOption(String displayName, String autocompleteText, Organizable organizable)
			: this(displayName, organizable)
		{
			this.autocompleteText = autocompleteText;
		}

		public AutocompleteOption(String displayName, String autocompleteText, String key, Organizable organizable)
			: this(displayName, autocompleteText, organizable)
		{
			this.key = key;
		}

		public String AutoCompleteText { get { return autocompleteText ?? DisplayName; } }

		public String Key { get { return key ?? DisplayName; } }
	}
}
