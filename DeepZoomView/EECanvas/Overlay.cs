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
    public class Overlay
    {
        private enum DisplayTypes { Border, ColorOverlay }

        public Shape OverlayShape;
        public String Name;
        public Brush BackgroundColor;
        private TextBlock text = null;
        private Canvas canvas = null;
        private DisplayTypes lastDisplay;

        public Overlay(string name, Shape border, SolidColorBrush solidColorBrush)
        {
            this.Name = name;
            this.OverlayShape = border;
            this.BackgroundColor = solidColorBrush;
            MakeText();
        }

        public void ShowBorder(Canvas c)
        {
            if (OverlayShape.Parent == c) { 
                return;
            }
            lastDisplay = DisplayTypes.Border;
            OverlayShape.Stroke = new SolidColorBrush(Colors.White);
            OverlayShape.StrokeThickness = 1;
            OverlayShape.Opacity = 1;
            canvas = c;
            c.Children.Add(OverlayShape);
        }

        public void ShowColorOverlay(Canvas c)
        {
            if (lastDisplay != DisplayTypes.ColorOverlay)
            {
                lastDisplay = DisplayTypes.ColorOverlay;
                OverlayShape.Stroke.Opacity = 0;
                OverlayShape.StrokeThickness = 0;

                OverlayShape.Fill = BackgroundColor;
                OverlayShape.Opacity = 1;
            }
            canvas = c;
            c.Children.Add(OverlayShape);
            c.Children.Add(text);
        }

        public void Hide()
        {
            try
            {
                canvas.Children.Remove(OverlayShape);
                canvas = null;
            }
            catch
            {
            }
        }


        public void MakeText()
        {
            text = new TextBlock();
            text.Text = this.Name;

            Canvas.SetLeft(text, Canvas.GetLeft(this.OverlayShape));
            Canvas.SetTop(text, Canvas.GetTop(this.OverlayShape));

            text.Width = this.OverlayShape.Width;
            text.Height = this.OverlayShape.Height;

            //RotateTransform rt = new RotateTransform();
            //rt.Angle = 90;
            //rt.CenterY = -txt.Height;
            //txt.RenderTransformOrigin = new Point(0, -txt.Height);
            //txt.RenderTransform = rt;
        }
    }
}
