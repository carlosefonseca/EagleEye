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

namespace DeepZoomView {
	public class Group {
		public static String DisplayType;

		internal String name { get; set; }
		//internal RectWithRects rectangle { get; set; }
		internal Dictionary<String, Object> rectangles = new Dictionary<string, Object>();
		internal RectWithRects rect;
		internal List<int> images { get; set; }

		internal RectWithRects rectangle {
			get {
				return rect;
				//return (RectWithRects)rectangles[DisplayType];
			}
			set {
				//rectangles[DisplayType] = value;
				rect = value;
			}
		}

		internal Shape shape {
			get {
				return (Shape)rectangles[DisplayType];
			}
			set {
				rectangles[DisplayType] = value;
			}
		}

		public Group(String n, Rect r, List<int> l)
			: this(n, l) {
			images = l;
		}

		public Group(String n, List<int> l) {
			name = n;
			images = l;
		}

		public override string ToString() {
			if (rectangles.ContainsKey("Lienar")) {
				return "Group '" + name + "' Polygon=" + shape.ToString() + " ImgCount=" + images.Count;
			} else if (rectangles.ContainsKey("Group")) {
				return "Group '" + name + "' Rect=" + rectangle.Rect.ToString() + " ImgCount=" + images.Count;
			} else {
				return "Group '" + name + "' Shape=None ImgCount=" + images.Count;
			}
		}
	}
}
