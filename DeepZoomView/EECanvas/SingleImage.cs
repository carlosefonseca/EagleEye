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

namespace DeepZoomView.EECanvas
{
    public class SingleImage : CanvasItem
    {
        public SingleImage(int imgId, Point point, MultiScaleSubImage currentImage) : base(imgId,point,currentImage)
        {
        }

        public SingleImage(int i) : base(i)
        {
        }

        public SingleImage(int id, MultiScaleSubImage i) : base(id, i)
        {
        }
    }
}
