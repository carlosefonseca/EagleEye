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
    public abstract class CanvasItem
    {
        public Point Position { get; set; }
        public int ImageId { get; set; }
        public virtual MultiScaleSubImage MainImage { get; set; }
        protected MyCanvas canvas;
        public double Side = 1;
        public bool autoCenter = true;

        public String PositionAsString()
        {
            return Position.X + "-" + Position.Y;
        }

        public CanvasItem(int img, Point p, MultiScaleSubImage sub) : this(img, sub)
        {
            Position = p;
        }

        public CanvasItem(int img, MultiScaleSubImage sub) : this(img)
        {
            MainImage = sub;
            MainImage.Opacity = 0;
        }

        public CanvasItem(int id)
        {
            this.ImageId = id;
        }

        //public virtual void Place(MyCanvas c)
        //{
        //    canvas = c;
        //    canvas.PositionImage(this, 1);
        //}

        public virtual bool Place(MyCanvas canvas, double x, double y)
        {
            this.canvas = canvas;
            Position = new Point(x, y);
            autoCenter = true;
            return true;
        }

        internal bool Place(MyCanvas canvas, double x, double y, double side)
        {
            this.Place(canvas, x, y);
            this.Side = side;
            autoCenter = false;
            return true;
        }
    }
}
