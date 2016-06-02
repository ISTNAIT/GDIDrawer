using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace GDIDrawer
{
    internal partial class DrawerWnd : Form
    {
        string sVersion = "GDIDrawer:" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        internal readonly int m_ciWidth;
        internal readonly int m_ciHeight;

        // log file
        internal StatusLog _log = null;

        // position of the window (must be invoked)
        internal delegate void delSetPos(Point pos);
        internal void SetDTPos(Point pos)
        {
            DesktopLocation = pos;
        }

        // position of the window (must be invoked)
        internal delegate Point delGetPos();
        internal Point GetDTPos()
        {
            return DesktopLocation;
        }

        // size of the window (must be invoked)
        internal delegate Size delGetSize();
        internal Size GetWndSize()
        {
            return new Size(this.Width, this.Height);
        }

        // delegate types for owner callbacks
        internal delegate int delIntGraphics(Graphics gr);
        internal delegate void delVoidPoint(Point p);

        // event delegates (set by owner, null if not in use)
        internal delIntGraphics m_delRender;
        internal delVoidPoint m_delMouseMove;
        internal delVoidPoint m_delMouseLeftClick;
        internal delVoidPoint m_delMouseRightClick;

        // this flag indicates that the drawer window is fully initialized and ready for rendering
        internal bool m_bIsInitialized = false;

        // flag indicates that thread/form should terminate, flagged from CDrawer.Close(), checked in timer
        internal bool m_bTerminate = false;

        // image for layer underlay operations
        private Bitmap m_bmUnderlay;

        // back-buffer parts (created once and reused for efficiency)
        BufferedGraphicsContext m_bgc;
        BufferedGraphics m_bg;
        byte[] m_argbs = null;

        // stopwatch for render time calcs
        private System.Diagnostics.Stopwatch m_StopWatch = new System.Diagnostics.Stopwatch();
        private Queue<long> m_qRenderAvg = new Queue<long>();
        private bool m_bContinuousUpdate = true;
        public bool ContinuousUpdate
        {
            get { return m_bContinuousUpdate; }
            set { m_bContinuousUpdate = value; }
        }
        private bool m_bRenderNow = true;
        public bool RenderNow
        {
            get { return m_bRenderNow; }
            set { m_bRenderNow = value; }
        }

        public DrawerWnd(CDrawer dr)
        {
            InitializeComponent();

            // use the log as built from parent
            _log = dr._log;

            // save window size
            m_ciWidth = dr.m_ciWidth;
            m_ciHeight = dr.m_ciHeight;

            // cap delegates, this will be set by owner
            m_delRender = null;
            m_delMouseMove = null;
            m_delMouseLeftClick = null;
            m_delMouseRightClick = null;

            // cap/set references
            m_bgc = new BufferedGraphicsContext();
            m_bg = null;

            // create the bitmap for the underlay and clear it to whatever colour
            m_bmUnderlay = new Bitmap(dr.m_ciWidth, dr.m_ciHeight);    // docs say will use Format32bppArgb

            // fill the bitmap with the default drawer bb colour
            FillBB(Color.Black);

            // show that drawer is up and running
            _log.WriteLine("Drawer Started...");            
        }

        //testing version
        internal void FillBBRect(Rectangle rect, Color c)
        {
            // set region
            lock (m_bmUnderlay)
            {
                if (m_bmUnderlay.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                {
                    // for each Y, lock the row to reduce copying
                    for (int y = rect.Top; y < rect.Bottom; ++y)
                    {
                        // ensure it's worth doing
                        if (y >= 0 && y < m_ciHeight)
                        {
                            // grab this scan row
                            // lock up the parts of the image to write to (rect)
                            System.Drawing.Imaging.BitmapData bmd = m_bmUnderlay.LockBits(
                              new Rectangle(0, y, m_ciWidth, 1),
                              System.Drawing.Imaging.ImageLockMode.ReadWrite,
                              m_bmUnderlay.PixelFormat);

                            // copy data out to a buffer
                            if (m_argbs == null || m_argbs.Length != bmd.Stride)
                                m_argbs = new byte[bmd.Stride];

                            System.Runtime.InteropServices.Marshal.Copy(bmd.Scan0, m_argbs, 0, bmd.Stride);

                            // fill colours (only valid area)
                            int iPos = 0;
                            for (int x = rect.Left; x < rect.Right; ++x)
                            {
                                if (x >= 0 && x < m_ciWidth)
                                {
                                    iPos = x * 4;
                                    m_argbs[iPos++] = c.B;
                                    m_argbs[iPos++] = c.G;
                                    m_argbs[iPos++] = c.R;
                                    m_argbs[iPos++] = c.A;
                                }
                            }

                            // write back into array and unlock the image
                            System.Runtime.InteropServices.Marshal.Copy(m_argbs, 0, bmd.Scan0, bmd.Stride);
                            m_bmUnderlay.UnlockBits(bmd);
                        }
                    }
                }
                else
                {
                    throw new Exception("Unexpected BB Bitmap Format!");
                }
            }
        }

        /*
        // fill in a section of the backbuffer (rect), with a color (c)
        internal void FillBBRectLast(Rectangle rect, Color c)
        {            
            // set region
            lock (m_bmUnderlay)
            {
                if (m_bmUnderlay.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                {
                    // lock up the parts of the image to write to (rect)
                    System.Drawing.Imaging.BitmapData bmd = m_bmUnderlay.LockBits(
                      new Rectangle(0, 0, m_ciWidth, m_ciHeight),
                      System.Drawing.Imaging.ImageLockMode.ReadWrite,
                      m_bmUnderlay.PixelFormat);
                                        
                    // copy data out to a buffer
                    if (m_argbs == null || m_argbs.Length != bmd.Stride * bmd.Height)
                        m_argbs = new byte[bmd.Stride * bmd.Height];

                    // investigate doing this line by line?
                    //IntPtr ptr = bmd.Scan0;
                    //IntPtr b = new IntPtr (ptr.ToInt32() + bmd.Stride);

                    //Console.WriteLine("Stride is : " + bmd.Stride.ToString());
                    System.Runtime.InteropServices.Marshal.Copy(bmd.Scan0, m_argbs, 0, bmd.Stride * bmd.Height);

                    //Console.WriteLine("filling rectangle : " + rect.ToString());
                    // fill colours (only valid area)
                    int iPos = 0;
                    for (int y = rect.Top; y < rect.Bottom; ++y)
                    {
                        if (y >= 0 && y < m_ciHeight)
                        {
                            for (int x = rect.Left; x < rect.Right; ++x)
                            {
                                if (x >= 0 && x < m_ciWidth)
                                {
                                    iPos = (y * bmd.Stride) + (x * 4);
                                    m_argbs[iPos++] = c.B;
                                    m_argbs[iPos++] = c.G;
                                    m_argbs[iPos++] = c.R;
                                    m_argbs[iPos++] = c.A;
                                }
                            }
                        }
                    }

                    // write back into array and unlock the image
                    System.Runtime.InteropServices.Marshal.Copy(m_argbs, 0, bmd.Scan0, bmd.Stride * bmd.Height);
                    m_bmUnderlay.UnlockBits(bmd);
                }
                else
                {
                    throw new Exception("Unexpected BB Bitmap Format!");
                }
            }
        }
        */

        internal void FillBB(Color c)
        {
            lock (m_bmUnderlay)
            {
                if (m_bmUnderlay.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                {
                    // lock up the parts of the image to write to (all of it)
                    System.Drawing.Imaging.BitmapData bmd = m_bmUnderlay.LockBits(
                      new Rectangle(0, 0, m_ciWidth, m_ciHeight),
                      System.Drawing.Imaging.ImageLockMode.WriteOnly,
                      m_bmUnderlay.PixelFormat);

                    // copy data out to a buffer
                    if (m_argbs == null || m_argbs.Length != bmd.Stride * bmd.Height)
                        m_argbs = new byte[bmd.Stride * bmd.Height];

                    for (int i = 0; i < m_argbs.Length; )
                    {
                        m_argbs[i++] = c.B;
                        m_argbs[i++] = c.G;
                        m_argbs[i++] = c.R;
                        m_argbs[i++] = c.A;
                    }
                    // write back into array and unlock the image
                    System.Runtime.InteropServices.Marshal.Copy(m_argbs, 0, bmd.Scan0, m_argbs.Length);
                    m_bmUnderlay.UnlockBits(bmd);
                }
                else
                {
                    throw new Exception("Unexpected BB Bitmap Format!");
                }
            }
        }
        private void Render()
        {
            // check to ensure that there is actually a callback registered
            if (m_delRender != null)
            {
                int iNumRendered = 0;

                // reset and start the stopwatch
                m_StopWatch.Reset();
                m_StopWatch.Start();

                // stop the timer, could be a long time for rendering
                UI_TIM_RENDER.Enabled = false;

                // copy the layover bitmap to the backbuffer for erasure/layover (eating mem)
                try
                {
                    lock (m_bmUnderlay)
                        m_bg.Graphics.DrawImage(m_bmUnderlay, new Point(0, 0));
                }
                catch (Exception err)
                {
                    _log.WriteLine("DrawerWnd::Render (Underlay) : " + err.Message);
                }

                // invoke controller class rendering...
                try
                {
                    iNumRendered = m_delRender(m_bg.Graphics);
                }
                catch (Exception err)
                {
                    _log.WriteLine("DrawerWnd::Render (main) : " + err.Message);
                }

                // flip bb to fb
                try
                {
                    m_bg.Render();
                }
                catch (Exception err)
                {
                    _log.WriteLine("DrawerWnd::Render (Flip) : " + err.Message);
                }

                // stop the stopwatch
                m_StopWatch.Stop();

                // do avarage calculation
                m_qRenderAvg.Enqueue(m_StopWatch.ElapsedMilliseconds);
                while (m_qRenderAvg.Count > 75)
                    m_qRenderAvg.Dequeue();
                double dTot = 0;
                foreach (long l in m_qRenderAvg)
                    dTot += l;
                dTot = dTot / m_qRenderAvg.Count;

                // Show render time...
                if (iNumRendered == 1)
                    Text = sVersion + " - Render Time = " + dTot.ToString("f2") + "ms (" + iNumRendered.ToString() + " shape)";
                else
                    Text = sVersion + " - Render Time = " + dTot.ToString("f2") + "ms (" + iNumRendered.ToString() + " shapes)";

                // restart the timer
                UI_TIM_RENDER.Enabled = true;
            }
        }
        private void UI_TIM_RENDER_Tick(object sender, EventArgs e)
        {
            if (m_bTerminate) // that's it we are done,
            {
                UI_TIM_RENDER.Enabled = false;
                Close();
                return;
            }

            if (!m_bContinuousUpdate && !m_bRenderNow)
                return;

            m_bRenderNow = false;

            Render();
        }
        private void DrawerWnd_MouseMove(object sender, MouseEventArgs e)
        {
            // if delegate is registered, fire off coords
            if (m_delMouseMove != null && e.X >= 0 && e.X < m_ciWidth && e.Y > 0 && e.Y < m_ciHeight)
            {
                try
                {
                    m_delMouseMove(e.Location);
                }
                catch (Exception err)
                {
                    _log.WriteLine("Error in MouseMove event - " + err.Message);
                }
            }
        }

        private void DrawerWnd_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (m_delMouseLeftClick != null && e.X >= 0 && e.X < m_ciWidth && e.Y > 0 && e.Y < m_ciHeight)
                {
                    try
                    {
                        m_delMouseLeftClick(e.Location);
                    }
                    catch (Exception err)
                    {
                        _log.WriteLine("Error in MouseDown event - " + err.Message);
                    }
                }                
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (m_delMouseRightClick != null && e.X >= 0 && e.X < m_ciWidth && e.Y > 0 && e.Y < m_ciHeight)
                {
                    try
                    {
                        m_delMouseRightClick(e.Location);
                    }
                    catch (Exception err)
                    {
                        _log.WriteLine("Error in MouseDown event - " + err.Message);
                    }
                }                
            }
        }

        internal void SetBBPixel(Point p, Color c)
        {
            if (m_bmUnderlay != null && p.X >= 0 && p.X < m_ciWidth && p.Y >= 0 && p.Y < m_ciHeight)
            {
                try
                {
                    lock (m_bmUnderlay)
                        m_bmUnderlay.SetPixel(p.X, p.Y, c);
                }
                catch (Exception err)
                {
                    _log.WriteLine("DrawerWnd::SetBBPixel : " + err.Message);
                }
            }
        }
        private void DrawerWnd_Shown(object sender, EventArgs e)
        {
            // create frontbuffer
            Graphics gr = CreateGraphics();

            // create the re-useable back-buffer object from the context and current display surface
            // if the surface is lost, this will be a problem...
            m_bg = m_bgc.Allocate(gr, DisplayRectangle);

            // start the rendering timer (done here as other inits require time and a primary surface to work)
            UI_TIM_RENDER.Enabled = true;
        }

        // Locate the drawer to the bottom right of the primary display
        private void DrawerWnd_Load(object sender, EventArgs e)
        {
            // set the window size accordingly
            ClientSize = new Size(m_ciWidth, m_ciHeight);

            // set the window position (as best fit to bottom right)
            if (System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width >= Size.Width &&
                System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height >= Size.Height)
            {
                SetDesktopLocation(
                    System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width - Size.Width,
                    System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height - Size.Height);
            }
            else
            {
                // window position will be upper-left, or whatever Windows thinks is a good idea...
            }
        }

        private void DrawerWnd_Paint(object sender, PaintEventArgs e)
        {
            Render();
            // mark as all initialization of this object is done
            m_bIsInitialized = true;
        }
    }
}