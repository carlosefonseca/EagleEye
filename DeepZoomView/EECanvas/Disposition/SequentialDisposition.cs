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
using System.Collections.ObjectModel;
using System.Linq;

namespace DeepZoomView.EECanvas.Dispositions
{
    public class SequentialDisposition : Disposition
    {
        public SequentialDisposition() { }

        internal override void Place(MyCanvas c)
        {
            canvas = c;
            double ar = canvas.page.msi.ActualWidth / canvas.page.msi.ActualHeight;
            base.CalculateDistribution(canvas.items.Count, ar, null, out cols, out rows);

            var x = 0.0;
            var y = 0.0;

            foreach (KeyValuePair<int, CanvasItem> kv in canvas.items)
            {
                canvas.SetItemPosition(kv.Key, x, y);
                x += 1;

                if (x >= cols)
                {
                    y += 1;
                    x = 0.0;
                }
            }
        }

        internal override Overlays MakeOverlays()
        {
            return new Overlays();
        }

        public override string ToString()
        {
            return "SequentialDisposition";
        }
    }
}
