using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeepZoomView
{
	class DisplaySetting
	{
		public String Key;
		public Organizable Organization;
		public Type Disposition;

		public DisplaySetting(String k, Organizable o, Type d)
		{
			Key = k;
			Organization = o;
			Disposition = d;
		}
	}
}
