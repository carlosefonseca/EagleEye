using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Linq;

namespace ColorUtils
{
    public class ColorUtil
    {
        public static Color FromStringToColor(string txt)
        {
            if (txt.StartsWith("Color"))
            {
                String[] p = txt.Split(new char[] { '[', ']', ',', ' ', '=' }, StringSplitOptions.RemoveEmptyEntries);
                return Color.FromArgb(Convert.ToByte(p[2]), Convert.ToByte(p[4]), Convert.ToByte(p[6]), Convert.ToByte(p[8]));
            }
            else
            {
                Byte[] bytes = BitConverter.GetBytes(Convert.ToInt32(txt));
                return Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0]);
            }
        }
    }

    public struct HsbColor
    {
        // value from 0 to 1 
        public double A;
        // value from 0 to 360 
        public int H;
        // value from 0 to 1 
        public double S;
        // value from 0 to 1 
        public double B;

        public static HsbColor Parse(String txt)
        {
            String[] splitted = txt.Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int h = Convert.ToInt16(splitted.First(x => x.StartsWith("H:")).Substring(2));
            double s = Convert.ToDouble(splitted.First(x => x.StartsWith("S:")).Substring(2));
            double b = Convert.ToDouble(splitted.First(x => x.StartsWith("B:")).Substring(2));

            HsbColor c = new HsbColor();
            c.A = 1;
            c.H = h;
            c.S = s;
            c.B = b;
            return c;
        }
        
        public override String ToString()
        {
            return String.Format("H:{0} S:{1} B:{2}", H, S, B);
        }
    }


    public struct HslColor
    {
        // value from 0 to 1 
        public double A;
        // value from 0 to 360 
        public double H;
        // value from 0 to 1 
        public double S;
        // value from 0 to 1 
        public double L;

        private static double ByteToPct(byte v)
        {
            double d = v;
            d /= 255;
            return d;
        }

        private static byte PctToByte(double pct)
        {
            pct *= 255;
            pct += .5;
            if (pct > 255) pct = 255;
            if (pct < 0) pct = 0;
            return (byte)pct;
        }

        public static HslColor FromColor(Color c)
        {
            return HslColor.FromArgb(c.A, c.R, c.G, c.B);
        }

        public static HslColor FromArgb(byte A, byte R, byte G, byte B)
        {
            HslColor c = FromRgb(R, G, B);
            c.A = ByteToPct(A);
            return c;
        }

        public static HslColor FromRgb(byte R, byte G, byte B)
        {
            HslColor c = new HslColor();
            c.A = 1;
            double r = ByteToPct(R);
            double g = ByteToPct(G);
            double b = ByteToPct(B);
            double max = Math.Max(b, Math.Max(r, g));
            double min = Math.Min(b, Math.Min(r, g));
            if (max == min)
            {
                c.H = 0;
            }
            else if (max == r && g >= b)
            {
                c.H = 60 * ((g - b) / (max - min));
            }
            else if (max == r && g < b)
            {
                c.H = 60 * ((g - b) / (max - min)) + 360;
            }
            else if (max == g)
            {
                c.H = 60 * ((b - r) / (max - min)) + 120;
            }
            else if (max == b)
            {
                c.H = 60 * ((r - g) / (max - min)) + 240;
            }

            c.L = .5 * (max + min);
            if (max == min)
            {
                c.S = 0;
            }
            else if (c.L <= .5)
            {
                c.S = (max - min) / (2 * c.L);
            }
            else if (c.L > .5)
            {
                c.S = (max - min) / (2 - 2 * c.L);
            }
            return c;
        }

        public HslColor Lighten(double pct)
        {
            HslColor c = new HslColor();
            c.A = this.A;
            c.H = this.H;
            c.S = this.S;
            c.L = Math.Min(Math.Max(this.L + pct, 0), 1);
            return c;
        }

        public HslColor Darken(double pct)
        {
            return Lighten(-pct);
        }

        private double norm(double d)
        {
            if (d < 0) d += 1;
            if (d > 1) d -= 1;
            return d;
        }

        private double getComponent(double tc, double p, double q)
        {
            if (tc < (1.0 / 6.0))
            {
                return p + ((q - p) * 6 * tc);
            }
            if (tc < .5)
            {
                return q;
            }
            if (tc < (2.0 / 3.0))
            {
                return p + ((q - p) * 6 * ((2.0 / 3.0) - tc));
            }
            return p;
        }

        public Color ToColor()
        {
            double q = 0;
            if (L < .5)
            {
                q = L * (1 + S);
            }
            else
            {
                q = L + S - (L * S);
            }
            double p = (2 * L) - q;
            double hk = H / 360;
            double r = getComponent(norm(hk + (1.0 / 3.0)), p, q);
            double g = getComponent(norm(hk), p, q);
            double b = getComponent(norm(hk - (1.0 / 3.0)), p, q);
            return Color.FromArgb(PctToByte(A), PctToByte(r), PctToByte(g), PctToByte(b));
        }

        public int SimpleHue()
        {
            return Convert.ToInt16(H);
        }

        public override String ToString()
        {
            return String.Format("H:{0} S:{1} L:{2}", H, S, L);
        }
    }
}
