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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DeepZoomView.EECanvas
{
    public class Overlays
    {
        List<Overlay> Shapes;
        Dictionary<int, Overlay> ImagesToShapes = new Dictionary<int, Overlay>();
        private Overlay currentBorder = null;
		public Canvas BorderLayer { get; set; }
		public Canvas ColorLayer { get; set; }

        public Overlays()
        {
            Shapes = new List<Overlay>();
        }

        public void ShowBorder(CanvasItem item, Canvas holder)
        {
            ShowBorder(item.ImageId, holder);
        }

        public void ShowBorder(int imgId, Canvas holder)
        {
            //holder.Children.Clear();
            if (currentBorder != null)
            {
                currentBorder.HideBorder();
            }

            if (ImagesToShapes.ContainsKey(imgId))
            {
                ImagesToShapes[imgId].ShowBorder();
                currentBorder = ImagesToShapes[imgId];
            }
            else
            {
                //throw new ArgumentOutOfRangeException();
                Debug.WriteLine("Failed to find overlay id {0}", imgId);
            }
        }

        public void AddOverlay(Overlay o, List<int> l)
        {
            Shapes.Add(o);
            foreach (int i in l)
            {
                ImagesToShapes.Add(i, o);
            }
        }

        internal void ShowAll()
        {
            foreach (Overlay s in this.Shapes)
            {
                s.ShowColor();
            }
        }

		internal void SetLayers(Canvas border, Canvas color)
		{
			foreach (Overlay o in Shapes)
			{
				o.SetLayers(border, color);
			}
		}
	}
}
