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

namespace DeepZoomView {
	public abstract class Organizable {
		public readonly String Name;

		protected Organizable(String name) {
			this.Name = name;
		}

		public abstract int Count();
		public abstract Boolean Import(String s);
	}
}
