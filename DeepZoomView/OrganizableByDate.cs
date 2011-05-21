﻿using System;
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
	public class OrganizableByDate : Organizable {
		public OrganizableByDate() : base("Date") { }

		public override int Count() { return 0; }
		public override Boolean Import(String s) { return false; }
	}
}
