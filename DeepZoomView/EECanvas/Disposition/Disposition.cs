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

namespace DeepZoomView.EECanvas.Dispositions
{
    public abstract class Disposition
    {
        private MyCanvas _canvas;
        protected double canvasAspectRatio;
        protected double cols = -1;
        protected double rows = -1;
        protected double cellSide = -1;
        internal Dictionary<int, Group> invertedGroups = new Dictionary<int, Group>();
        private Random random = new Random();

        protected MyCanvas canvas
        {
            get { return _canvas; }
            set
            {
                _canvas = value;
                canvasAspectRatio = _canvas.aspectRatio;
            }
        }

        public static List<String> DisplayOptions = new List<string>() { "Grid", "Groups", "Linear" };
		
        protected Disposition() { }

        /// <summary>
        /// Discovers a rectangle that can hold "amount" elements. The aspect ratio is determined from the canvas
        /// </summary>
        /// <param name="amount">The number of elements to hold</param>
        /// <param name="cols">Out: The number of columns</param>
        /// <param name="rows">Out: The number of rows</param>
        internal void CalculateDistribution(int amount, out double cols, out double rows)
        {
            CalculateDistribution(amount, canvasAspectRatio, null, out cols, out rows);
        }

        /// <summary>
        /// Discovers a rectangle that can hold "amount" elements.
        /// </summary>
        /// <param name="amount">The number of elements to hold</param>
        /// <param name="aR">The aspect ratio of the rectangle</param>
        /// <param name="max">Maximum width and height values. Ignored if the amout of elements can't fit.</param>
        /// <param name="cols">Out: The number of columns</param>
        /// <param name="rows">Out: The number of rows</param>
        internal void CalculateDistribution(int amount, double aR, Point? max, out double cols, out double rows)
        {
            int canHold = 1;
            cols = 1;
            rows = 1;
            while (canHold < amount)
            {
                if (!max.HasValue)
                {
                    cols++;
                    rows = Convert.ToInt32(Math.Floor(cols / aR));
                }
                else
                {
                    if (cols < max.Value.X)
                    {
                        cols++;
                        rows = Convert.ToInt32(Math.Min(Math.Floor(cols / aR), max.Value.Y));
                    }
                    else if (rows < max.Value.Y)
                    {
                        rows++;
                    }
                    else
                    {
                        cols++;
                        rows = Convert.ToInt32(Math.Floor(cols / aR));
                    }
                }
                canHold = Convert.ToInt32(cols * rows);
            }
        }


        internal abstract void Place(MyCanvas myCanvas);

        internal abstract Overlays MakeOverlays();

        protected Color ColorFromName(String name)
        {
            Color c;
            if (name.StartsWith("#"))
            {
				c = ColorUtils.ColorUtil.ParseCardinalRGBColor(name);
            }
            else
            {
                c = Color.FromArgb((byte)150,
                    (byte)random.Next(255),
                    (byte)random.Next(255),
                    (byte)random.Next(255));
            }
            return c;
        }
    }
}
