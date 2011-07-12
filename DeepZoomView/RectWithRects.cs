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
	public class RectWithRects {
		public double X { get; set; }
		public double Y { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }
		private List<RectWithRects> children;
		public Boolean leaf;
		public Group group;
		public RectWithRects Parent { get; set; }

		public Group Group {
			get {
				return group;
			}
			set {
				group = value;
				children = null;
				leaf = true;
				Adjust();
			}
		}

		public Rect Rect {
			get {
				return new Rect(X, Y, Width, Height);
			}
			set {
				X = value.X;
				Y = value.Y;
				Width = value.Width;
				Height = value.Height;
			}
		}

		public Boolean isLeaf() {
			return leaf;
		}


		public RectWithRects(Rect r)
			: this(r.X, r.Y, r.Width, r.Height) {
		}

		public RectWithRects(RectWithRects r)
			: this(r.X, r.Y, r.Width, r.Height) {
		}

		public RectWithRects(int x, int y, int w, int h)
			: this((double)x, (double)y, (double)w, (double)h) {
		}

		public RectWithRects(double x, double y, double w, double h) {
			if (w < 0) throw new Exception("Width can't be less than zero");
			if (h < 0) throw new Exception("Height can't be less than zero");
			X = x;
			Y = y;
			Width = w;
			Height = h;
			group = null;
			leaf = false;
		}

		public RectWithRects(Rect r, Group g)
			: this(r) {
			group = g;
			leaf = true;
		}

		public RectWithRects(RectWithRects r, Group g)
			: this(r) {
			group = g;
			leaf = true;
		}

		public RectWithRects(int x, int y, int w, int h, Group g)
			: this((double)x, (double)y, (double)w, (double)h, g) {
		}

		public RectWithRects(double x, double y, double w, double h, Group g)
			: this(x, y, w, h) {
			group = g;
			leaf = true;
		}

		public RectWithRects(double x, double y, double? w, double h, Group g)
			: this(x, y, (w.HasValue ? w.Value : (g.images.Count / h)), h) {
			group = g;
			leaf = true;
		}

		public RectWithRects(double x, double y, double w, double? h, Group g)
			: this(x, y, w, (h.HasValue ? h.Value : (g.images.Count / w))) {
			group = g;
			leaf = true;
		}

		public Boolean Fits(double count) {
			return (Width * Height >= count);
		}

		public Boolean Fits(int count) {
			return Fits((double)count);
		}

		public void Adjust() {
			if (leaf) {
				if (!Fits(Group.images.Count)) {
					Adjust(this.Group.images.Count);
				}
			} else {
				throw new Exception("Adjust() only works on leaf nodes");
			}
		}

		public void Adjust(int count) {
			double aR = AspectRatio;
			int canHold = 1;
			double Hcells = 1;
			double Vcells = 1;
			while (canHold < count) {
				Hcells++;
				Vcells = Convert.ToInt32(Math.Floor(Hcells / aR));
				canHold = Convert.ToInt32(Hcells * Vcells);
			}
			Width = Hcells;
			Height = Vcells;
		}

		public override String ToString() {
			if (children != null) {
				return new Rect(X, Y, Width, Height).ToString() + " Count=" + children.Count;
			} else if (leaf && group != null) {
				return new Rect(X, Y, Width, Height).ToString() + " Group=" + group.ToString();
			} else {
				return new Rect(X, Y, Width, Height).ToString() + " No Children or Group";
			}
		}

		public void Add(RectWithRects r) {
			if (children == null) {
				children = new List<RectWithRects>();
			}
			children.Add(r);
			r.Parent = this;
			Width = Math.Max(r.X + r.Width, Width);
			Height = Math.Max(r.Y + r.Height, Height);
		}

		public List<RectWithRects> Children() {
			if (children == null)
				children = new List<RectWithRects>();
			return children;
		}

		public String TreeView() {
			int level = 0;
			return TreeView(level);
		}

		public String TreeView(int level) {
			String ident = "∟";
			ident = ident.PadLeft(level * 2 + 1);
			String str = ToString();
			if (children != null) {
				foreach (RectWithRects r in children) {
					str += Environment.NewLine + ident + r.TreeView(level + 1);
				}
			}
			return str;
		}

		public double AspectRatio {
			get {
				return Width / Height;
			}
		}

		public void IncreaseSize() {
			if (AspectRatio >= 1) {
				Width++;
				Height = Math.Floor(Width / AspectRatio);
			} else {
				Height++;
				Width = Math.Floor(Height * AspectRatio);
			}			
		}
	}
}
