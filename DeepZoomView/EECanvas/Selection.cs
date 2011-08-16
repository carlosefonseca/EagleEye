using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeepZoomView.EECanvas
{
	internal class Selection : List<CanvasItem>
	{
		public Selection(IEnumerable<CanvasItem> iEnumerable)
		{
			this.AddRange(iEnumerable);
		}

		public Selection() : base()
		{
		}
	}
}
