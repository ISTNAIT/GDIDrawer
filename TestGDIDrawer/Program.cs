using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using GDIDrawer;
namespace TestGDIDrawer
{
    class Program
    {
        static Random s_rnd = new Random();

        static void Main(string[] args)
        {
            //PositionTests();
            //SubscriberTests();
            //SLines();
            //Lines();
            //SBlocks();
            //Bezier();
            //RandomBlocks();
            //ClickEllipses();
            //Background();
            CenteredRectangleTest();
        }

        static void CenteredRectangleTest()
        {            
            CDrawer can = new CDrawer(800, 600, false);
            can.AddCenteredRectangle(400, 300, 796, 596, Color.Red);
            for (int i = 0; i < 500; ++i)
                can.AddCenteredRectangle(s_rnd.Next(100, 700), s_rnd.Next(100, 500), s_rnd.Next(5, 190), s_rnd.Next(5, 190), RandColor.GetColor(), s_rnd.Next(6), RandColor.GetColor());
            can.Render();
            Console.ReadKey();
        }

        static void PositionTests()        
        {
            CDrawer A = new CDrawer(200, 200);           
            CDrawer B = new CDrawer(200, 300);            

            Console.ReadKey();
            A.Position = new Point(100, 50);
            B.Position = new Point(A.Position.X + A.DrawerWindowSize.Width + 10, 50);
            Console.ReadKey();
        }

        static void SLines()
        {
            CDrawer can = new CDrawer(800, 600, false);            

            can.AddLine(10, 10, 790, 590, Color.Red, 2);

            for (double d = 0; d < Math.PI * 2; d += Math.PI / 32)
                can.AddLine(new Point (400, 300), 50 * d, d);

            for (int x = 0; x < 600; x += 5)
            {
                can.AddLine(0, 600 - x, x, 0, RandColor.GetColor(), 1);
            }

            can.Render();
            Console.ReadKey();
        }

        static void SubscriberTests()
        {
            CDrawer can = new CDrawer();
            can.MouseMove += new GDIDrawerMouseEvent(MouseMove);
            can.MouseLeftClick += new GDIDrawerMouseEvent(LeftClick);

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            while (sw.ElapsedMilliseconds < 10000)
                System.Threading.Thread.Sleep(10);
        }

        static void LeftClick(Point pos, CDrawer dr)
        {
            dr.AddEllipse(pos.X, pos.Y, 10, 10, Color.Yellow);
        }

        static void MouseMove(Point pos, CDrawer dr)
        {
            dr.AddEllipse(pos.X, pos.Y, 5, 5, Color.Red);
        }

        static void Bezier()
        {
            CDrawer can = new CDrawer(800, 600, false);

            for (int ix = 0; ix < 800; ix += 50)
            {
                can.AddBezier(0, 600, ix, 0, 800 - ix, 600, 800, 0, Color.Red, 2);
                can.AddBezier(0, 0, ix, 0, 800 - ix, 600, 800, 600, Color.Red, 2);
            }

            can.Render();
            Console.ReadKey();
        }

        static void SBlocks()
        {
            CDrawer can = new CDrawer(800, 600, false);

            for (int i = 0; i < 500; ++i)
            {
                can.AddCenteredEllipse(s_rnd.Next(0, 800), s_rnd.Next(0, 800), s_rnd.Next(0, 800), s_rnd.Next(0, 800), RandColor.GetColor(), 1, RandColor.GetColor());
                can.AddCenteredEllipse(s_rnd.Next(0, 800), s_rnd.Next(0, 800), s_rnd.Next(0, 800), s_rnd.Next(0, 800), RandColor.GetColor(), 1);
                can.AddCenteredEllipse(s_rnd.Next(0, 800), s_rnd.Next(0, 800), s_rnd.Next(0, 800), s_rnd.Next(0, 800), RandColor.GetColor());
                can.AddCenteredEllipse(s_rnd.Next(0, 800), s_rnd.Next(0, 800), s_rnd.Next(0, 800), s_rnd.Next(0, 800));

                can.AddEllipse(s_rnd.Next(0, 800), s_rnd.Next(0, 800), s_rnd.Next(0, 800), s_rnd.Next(0, 800), RandColor.GetColor(), 1, RandColor.GetColor());
                can.AddEllipse(s_rnd.Next(0, 800), s_rnd.Next(0, 800), s_rnd.Next(0, 800), s_rnd.Next(0, 800), RandColor.GetColor(), 1);
                can.AddEllipse(s_rnd.Next(0, 800), s_rnd.Next(0, 800), s_rnd.Next(0, 800), s_rnd.Next(0, 800), RandColor.GetColor());
                can.AddEllipse(s_rnd.Next(0, 800), s_rnd.Next(0, 800), s_rnd.Next(0, 800), s_rnd.Next(0, 800));

                try
                {
                    can.AddPolygon(s_rnd.Next(0, 800), s_rnd.Next(0, 800), s_rnd.Next(0, 300), s_rnd.Next(0, 64), s_rnd.NextDouble() * Math.PI * 2, RandColor.GetColor(), 1, RandColor.GetColor());
                    can.AddPolygon(s_rnd.Next(0, 800), s_rnd.Next(0, 800), s_rnd.Next(0, 300), s_rnd.Next(0, 64), s_rnd.NextDouble() * Math.PI * 2, RandColor.GetColor(), 1);
                    can.AddPolygon(s_rnd.Next(0, 800), s_rnd.Next(0, 800), s_rnd.Next(0, 300), s_rnd.Next(0, 64), s_rnd.NextDouble() * Math.PI * 2, RandColor.GetColor());
                    can.AddPolygon(s_rnd.Next(0, 800), s_rnd.Next(0, 800), s_rnd.Next(0, 300), s_rnd.Next(0, 64), s_rnd.NextDouble() * Math.PI * 2);
                    can.AddPolygon(s_rnd.Next(0, 800), s_rnd.Next(0, 800), s_rnd.Next(0, 300), s_rnd.Next(0, 64));
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message);
                }

                try
                {
                    can.AddRectangle(s_rnd.Next(-10, 810), s_rnd.Next(-10, 610), s_rnd.Next(-10, 810), s_rnd.Next(-10, 610), RandColor.GetColor(), 1, RandColor.GetColor());
                    can.AddRectangle(s_rnd.Next(-10, 810), s_rnd.Next(-10, 610), s_rnd.Next(-10, 810), s_rnd.Next(-10, 610), RandColor.GetColor(), 1);
                    can.AddRectangle(s_rnd.Next(-10, 810), s_rnd.Next(-10, 610), s_rnd.Next(-10, 810), s_rnd.Next(-10, 610), RandColor.GetColor());
                    can.AddRectangle(s_rnd.Next(-10, 810), s_rnd.Next(-10, 610), s_rnd.Next(-10, 810), s_rnd.Next(-10, 610));
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message);
                }

                try
                {
                    can.AddText("Rats", s_rnd.Next(0, 100), s_rnd.Next (0, 800), s_rnd.Next (0, 600), s_rnd.Next(0, 200), s_rnd.Next (0, 200), RandColor.GetColor());
                    can.AddText("Rats", s_rnd.Next(0, 100), s_rnd.Next(0, 800), s_rnd.Next(0, 600), s_rnd.Next(0, 200), s_rnd.Next(0, 200));
                    can.AddText("Rats", s_rnd.Next(0, 100), RandColor.GetColor());
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message);
                }
            }

            can.Render();
            Console.ReadKey();
        }
        
        static void Lines()
        {
            CDrawer can = new CDrawer(800, 600, false);
            can.Scale = 10;
            for (int i = -10; i < can.ScaledWidth + 1; i += 5)
            {
                for (int j = -10; j < can.ScaledHeight + 1; j += 5)
                {
                    can.AddLine(i, j, can.ScaledWidth + 1 - i, can.ScaledHeight + 1 - j, RandColor.GetKnownColor(), 1);
                }
            }
            can.AddText("check...check.. ", 48);
            can.AddText("one two three", 12, -10, -10, 100, 50, Color.White);
            can.Render();
            Console.ReadKey();
        }
        static void RandomBlocks()
        {
            Random rnd = new Random();
            CDrawer can = new CDrawer();
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Reset();
            watch.Start();
            can.AddText("Random Known Colors SetBBPixel : 2s", 28, 0, 0, can.ScaledWidth, can.ScaledHeight, Color.White);
            can.AddText("Random Known Colors SetBBPixel : 2s", 28, 2, 2, can.ScaledWidth + 2, can.ScaledHeight + 2, Color.Black);
            while (watch.ElapsedMilliseconds < 2000)
            {
                can.SetBBPixel(rnd.Next(can.ScaledWidth), rnd.Next(can.ScaledHeight), RandColor.GetKnownColor());
            }
            can.Close();

            can = new CDrawer(800, 800);
            can.Scale = 10;
            Console.WriteLine("Random Known Colors SetBBScaledPixel : 2s");
            watch.Reset();
            watch.Start();
            can.AddText("Random Known Colors SetBBScaledPixel : 2s", 24);
            while (watch.ElapsedMilliseconds < 2000)
            {
                can.SetBBScaledPixel(rnd.Next(can.ScaledWidth), rnd.Next(can.ScaledHeight), RandColor.GetKnownColor());
            }
            can.Close();

        }
        static void ClickEllipses()
        {
            Random rnd = new Random();
            CDrawer can = new CDrawer();
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Reset();
            watch.Start();
            can.AddText("Random Bounding Box Ellipses : 2s", 28, 0, 0, can.ScaledWidth, can.ScaledHeight, Color.White);
            can.AddText("Random Bounding Box Ellipses : 2s", 28, 2, 2, can.ScaledWidth + 2, can.ScaledHeight + 2, Color.Black);
            while (watch.ElapsedMilliseconds < 5000)
            {
                Point p = new Point(rnd.Next(-50, can.ScaledWidth + 50), rnd.Next(-50, can.ScaledHeight - 50));
                switch (rnd.Next(6))
                {
                    case 0:
                        can.AddEllipse(p.X, p.Y, 100, 100);
                        break;
                    case 1:
                        can.AddEllipse(p.X, p.Y, 100, 100, RandColor.GetKnownColor(), rnd.Next(1, 4), RandColor.GetKnownColor());
                        break;
                    case 2:
                        can.AddPolygon(p.X, p.Y, 100, rnd.Next(3, 8));
                        break;
                    case 3:
                        can.AddPolygon(p.X, p.Y, 100, rnd.Next(3, 8), rnd.NextDouble() * Math.PI, RandColor.GetKnownColor(), 2, RandColor.GetKnownColor());
                        break;
                    case 4:
                        can.AddRectangle(p.X, p.Y, 100, 100);
                        break;
                    case 5:
                        can.AddRectangle(p.X, p.Y, 100, 100, RandColor.GetKnownColor(), rnd.Next(1, 4), RandColor.GetKnownColor());
                        break;
                    default:
                        break;
                }
                System.Threading.Thread.Sleep(100);

            }
            can.Close();

            can = new CDrawer(1000, 400, false);
            //System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Reset();
            watch.Start();
            can.AddText("Random Bounding Box Ellipses : 2s", 28, 0, 0, can.ScaledWidth, can.ScaledHeight, Color.White);
            can.AddText("Random Bounding Box Ellipses : 2s", 28, 2, 2, can.ScaledWidth + 2, can.ScaledHeight + 2, Color.Black);
            while (watch.ElapsedMilliseconds < 2000)
            {
                Point p = new Point(rnd.Next(50, can.ScaledWidth - 50), rnd.Next(50, can.ScaledHeight - 50));
                can.AddCenteredEllipse(p.X, p.Y, 100, 100, RandColor.GetKnownColor(), 2, Color.White);
                can.AddCenteredEllipse(p.X, p.Y, 5, 5, RandColor.GetKnownColor(), 1, Color.Red);
                System.Threading.Thread.Sleep(100);

            }
            can.Render();
            System.Threading.Thread.Sleep(1000);
            can.Close();

        }
        static void Background()
        {
            Console.WriteLine("Resource Picture");
            Bitmap bm = new Bitmap(Properties.Resources.jupiter);
            CDrawer dr = new CDrawer(bm.Width, bm.Height);
            dr.ContinuousUpdate = false;
            dr.SetBBPixel(bm.Width / 2, bm.Height / 2, Color.Wheat);

            for (int y = 0; y < bm.Height; ++y)
                for (int x = 0; x < bm.Width; ++x)
                    dr.SetBBPixel(x, y, bm.GetPixel(x, y));
            dr.Render();
            System.Threading.Thread.Sleep(1000);
            dr.Close();
        }
    }
}
