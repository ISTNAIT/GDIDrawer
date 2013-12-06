using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GDIDrawer
{
    // the drawer class is used as an interface for basic drawing functionality
    // users use an instance of this class and draw primitives through it
    ///////////////////////////////////////////////////////////////////////////////////////
    // Revision History
    // April 16th 2008 - Created (POC) - SLW
    // April 22nd 2008 - Added Mouse move/left-click/right-click info support
    // April 23rd 2008 - Added Back-Buffer (Underlay) image + set pixel/fill support
    // April 24th 2008 - Moved BB code to members to increase speed (req's lost surface testing)
    // 13 May 2008 - Added CDrawer.Clear(), moved const width, height to CDrawer
    // May 21 2008     - Added Scaled Mouse Query Methods
    // Feb 26 2009 - Added ContinuousUpdate, Render() to allow selective updates
    //               ** Caveate : if ContinuousUpdate set immediately upon creation of CDrawer,
    //                            operation will complete but condition will not be set in CDrawerWnd
    // May 11 2009 - HHV - Added Scaled Width and Height property, 
    //                     ** Will truncate to int, dependant on Scale as multiple of Width and Height
    //               Version 1.1 - Target .NET 3.5
    // October 23 2009 - SLW - Updated to use readonly width and height for windows (set in ctor overload)
    //               NOTE: Text Centering is now broken, as window size will not be known to drawing classes
    //               Version 1.2
    // October 27 2009 - SLW - Updated the back-buffer code to fill blocks faster, brought code to 'code'
    //               Version 1.3(.0.1)
    // Feb 24 2010 - SLW - Added RedundaMouse prop to allow redundant reporting of click/move events
    //               Version 1.3(.0.2)
    // Mar 17 2010 - SLW - Fixed initial/reset coords for mouse ops to not ignore 0,0 coord
    //               Version 1.3(.0.3)
    // May  7 2010 - HHV - Added Close() to CDrawer to allow custom drawer instances to come and go
    //               Version 1.3(.0.4)
    // Nov 1 2010 - HHV - Revised wait on initial render completion for CTOR completion to Sleep(0) to workaround
    //                    shell activated programs hanging on startup.
    //                    Revised Close() to flag the DrawerWnd timer which will close the app, thereby terminating the thread
    //              Version 1.3(.0.5)
    // May 9 2011 - HHV Version 1.4.0.0
    //            - Modified to use default and named arguments, added centered object placement
    // SEPT/OCT 2011 - Herb corrected ctor ignoring cont. update flag, and line excpetions on start/end @ 0 (Herb, please correct)
    // Oct 3 2011 - SLW - Fixed shape hierarchy and moved to IRender interface for collection
    // Oct 3 2011 - SLW - *** modified compareto in base to adjust for changes *** (not present in all shapes now)
    // Oct 4 2011 - SLW - Added Bezier Spline shape - pushed to version 1.4.0.3
    // Nov 7 2011 - HHV - Corrected Polygon CTOR improperly passing border color to base Shape CTOR, exception message to CPolygon (1.4.0.4)
    // Dec 01 2011 - SLW - Added mouse events for subscribers (1.4.0.5)
    // Dec 04 2013 - SLW - Added position and size props (1.4.0.6)
    //                   - Changed AddLine methods to remove ambiguous pt to pt vs. rotation overloads
    //                   - Added AddCenteredRectangle method + added Width/Height < 1 checks to both AddRectangle methods
    ///////////////////////////////////////////////////////////////////////////////////////

    // renderable interface definition for any type rendered in the drawer
    internal interface IRender
    {
        void Render (Graphics gr, int iScale);
    }

    /// <summary>
    /// Delegate used for mouse events with the drawer
    /// </summary>
    /// <param name="pos">The position of the mouse in the event</param>
    /// <param name="dr">The drawer that generated the event</param>
    public delegate void GDIDrawerMouseEvent (Point pos, CDrawer dr);

    /// <summary>
    /// CNT CDrawer
    /// </summary>
    public class CDrawer
    {
        /// <summary>
        /// Width of the drawer window
        /// </summary>
        public readonly int m_ciWidth;

        /// <summary>
        /// Height of the drawer window
        /// </summary>
        public readonly int m_ciHeight;

        /// <summary>
        /// Set the desktop position for the drawer window
        /// </summary>
        public Point Position
        {
            get
            {
                Point Pos = new Point();
                try
                {
                    if (m_wDrawer != null)
                    {
                        Pos = (Point)m_wDrawer.Invoke(new DrawerWnd.delGetPos(m_wDrawer.GetDTPos));
                    }
                }
                catch (Exception)
                {
                }

                return Pos;
            }
            set
            {
                try
                {
                    if (m_wDrawer != null)
                    {
                        m_wDrawer.Invoke(new DrawerWnd.delSetPos(m_wDrawer.SetDTPos), value);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// Get the size of the drawer window
        /// </summary>
        public Size DrawerWindowSize
        {
            get
            {
                Size sz = new Size();
                try
                {
                    if (m_wDrawer != null)
                    {
                        sz = (Size)m_wDrawer.Invoke(new DrawerWnd.delGetSize(m_wDrawer.GetWndSize));
                    }
                }
                catch (Exception)
                {
                }

                return sz;
            }
        }

        // the window (application) that does the actual drawing
        private DrawerWnd m_wDrawer;

        // the thread that the drawing window runs in
        private Thread m_tDrawerThread;

        // this is the linked list of polymorphic shapes that are to be rendered
        private LinkedList<IRender> m_llShapes;

        /// <summary>
        /// determines if mouse coords/state will generate redundant values 
        /// </summary>
        public bool RedundaMouse { set; get; }

        // mouse move state variables
        private Point m_pLastMouseMove = new Point(-1, -1);
        private bool m_bLastMouseMoveNew = false;
        private Point m_pLastMouseMoveScaled = new Point(-1, -1);
        private bool m_bLastMouseMoveNewScaled = false;

        // mouse click state variables
        private Point m_pLastMouseLeftClick = new Point(-1, -1);
        private bool m_bLastMouseLeftClickNew = false;
        private Point m_pLastMouseRightClick = new Point(-1, -1);
        private bool m_bLastMouseRightClickNew = false;
        private Point m_pLastMouseLeftClickScaled = new Point(-1, -1);
        private bool m_bLastMouseLeftClickNewScaled = false;
        private Point m_pLastMouseRightClickScaled = new Point(-1, -1);
        private bool m_bLastMouseRightClickNewScaled = false;

        // event delegates
        /// <summary>
        /// The mouse has moved over the drawer window
        /// </summary>
        public event GDIDrawerMouseEvent MouseMove = null;
        public event GDIDrawerMouseEvent MouseMoveScaled = null;
        public event GDIDrawerMouseEvent MouseLeftClick = null;
        public event GDIDrawerMouseEvent MouseLeftClickScaled = null;
        public event GDIDrawerMouseEvent MouseRightClick = null;
        public event GDIDrawerMouseEvent MouseRightClickScaled = null;

        /// <summary>
        /// Close the Drawer
        /// </summary>
        public void Close()
        {
            if (m_wDrawer != null && m_tDrawerThread != null)
            {
                m_bContinuousUpdate = false;
                m_wDrawer.ContinuousUpdate = false;
                m_wDrawer.m_bTerminate = true; // flag thread timer to bail out, closing the form
                m_tDrawerThread.Join(5000); // Wait til thread bails
            }
        }
        /// <summary>
        /// Retrieve last known mouse position in CDrawer coordinates 
        /// </summary>
        /// <param name="pCoords">out : last known point</param>
        /// <returns>true if coordinates are new since last read</returns>
        public bool GetLastMousePosition(out Point pCoords)
        {
            // if the mouse has moved since last read, return actual coords
            if (m_bLastMouseMoveNew)
            {
                m_bLastMouseMoveNew = false;
                pCoords = m_pLastMouseMove;
                return true;
            }
            // else return old coords, but indicate stale
            pCoords = m_pLastMouseMove;
            return false;
        }
        /// <summary>
        /// Retrieve Scaled last known mouse position in CDrawer coordinates 
        /// </summary>
        /// <param name="pCoords">out : last known scaled point</param>
        /// <returns>true if coordinates are new since last read</returns>
        public bool GetLastMousePositionScaled(out Point pCoords)
        {
            // if the mouse has moved since last read, return actual coords
            if (m_bLastMouseMoveNewScaled)
            {
                m_bLastMouseMoveNewScaled = false;
                pCoords = m_pLastMouseMoveScaled;
                return true;
            }
            // else return old coords, but indicate stale
            pCoords = m_pLastMouseMoveScaled;
            return false;
        }
        /// <summary>
        /// Retrieve last known point of Mouse Left Click in CDrawer
        /// </summary>
        /// <param name="pCoords">out : last known point of Left Click</param>
        /// <returns>true if point is new since last read</returns>
        public bool GetLastMouseLeftClick(out Point pCoords)
        {
            // if the mouse has left clicked since last read, return actual coords
            if (m_bLastMouseLeftClickNew)
            {
                m_bLastMouseLeftClickNew = false;
                pCoords = m_pLastMouseLeftClick;
                return true;
            }
            // else return old coords, but indicate stale
            pCoords = m_pLastMouseLeftClick;
            return false;
        }

        /// <summary>
        /// Retrieve Scaled last known point of Mouse Left Click in CDrawer
        /// </summary>
        /// <param name="pCoords">out : scaled last known point of Left Click</param>
        /// <returns>true if point is new since last read</returns>
        public bool GetLastMouseLeftClickScaled(out Point pCoords)
        {
            // if the mouse has moved since last read, return actual coords
            if (m_bLastMouseLeftClickNewScaled)
            {
                m_bLastMouseLeftClickNewScaled = false;
                pCoords = m_pLastMouseLeftClickScaled;
                return true;
            }
            // else return old coords, but indicate stale
            pCoords = m_pLastMouseLeftClickScaled;
            return false;
        }

        /// <summary>
        /// Retrieve last known point of Mouse Right Click in CDrawer
        /// </summary>
        /// <param name="pCoords">out : last known point of Right Click</param>
        /// <returns>true if point is new since last read</returns>
        public bool GetLastMouseRightClick(out Point pCoords)
        {
            // if the mouse has right clicked since last read, return actual coords
            if (m_bLastMouseRightClickNew)
            {
                m_bLastMouseRightClickNew = false;
                pCoords = m_pLastMouseRightClick;
                return true;
            }
            // else return old coords, but indicate stale
            pCoords = m_pLastMouseRightClick;
            return false;
        }

        /// <summary>
        /// Retrieve Scaled last known point of Mouse Right Click in CDrawer
        /// </summary>
        /// <param name="pCoords">out : scaled last known point of Right Click</param>
        /// <returns>true if point is new since last read</returns>
        public bool GetLastMouseRightClickScaled(out Point pCoords)
        {
            // if the mouse has moved since last read, return actual coords
            if (m_bLastMouseRightClickNewScaled)
            {
                m_bLastMouseRightClickNewScaled = false;
                pCoords = m_pLastMouseRightClickScaled;
                return true;
            }

            // else return old coords, but indicate stale
            pCoords = m_pLastMouseRightClickScaled;
            return false;
        }

        private int m_iScale;
        /// <summary>
        /// Get/Set the current CDrawer Scaling factor
        /// </summary>
        public int Scale
        {
            get { return m_iScale; }
            set
            {
                if (value < 1 || value > m_ciWidth)
                    throw new ArgumentException("CDrawer:Scale: Scale value must be between 1 and " + m_ciWidth.ToString());
                m_iScale = value;

                // reset scale bits for mouse query on scaled side
                m_bLastMouseLeftClickNewScaled = false;
                m_pLastMouseLeftClickScaled = new Point(-1, -1);
                m_bLastMouseRightClickNewScaled = false;
                m_pLastMouseRightClickScaled = new Point(-1, -1);
                m_bLastMouseMoveNewScaled = false;
                m_pLastMouseMoveScaled = new Point(-1, -1);
            }
        }
        // 
        /// <summary>
        /// Get current ScaledWidth ** May be truncated based on Scale value and Width
        /// </summary>
        public int ScaledWidth
        {
            get { return m_ciWidth / m_iScale; }
        }

        /// <summary>
        /// Get current ScaledHeight ** May be truncated based on Scale value
        /// </summary>
        public int ScaledHeight
        {
            get { return m_ciHeight / m_iScale; }
        }
        // Continuous Update, render flag
        /// <summary>
        /// Render all currently Added objects immediately
        /// </summary>
        public void Render()
        {
            if (m_wDrawer != null && m_tDrawerThread != null)
                m_wDrawer.RenderNow = true;
        }

        private bool m_bContinuousUpdate;
        /// <summary>
        /// Get/Set Whether the CDrawer will periodically Render automatically
        /// </summary>
        public bool ContinuousUpdate
        {
            get { return m_bContinuousUpdate; }
            set
            {
                if (m_wDrawer != null && m_tDrawerThread != null)
                {
                    m_bContinuousUpdate = value;
                    m_wDrawer.ContinuousUpdate = value;
                }
            }
        }
        /// <summary>
        /// Clear the current list of shapes to "wipe" the canvas 
        /// </summary>
        public void Clear()
        {
            lock (m_llShapes)
            {
                m_llShapes.Clear();
            }
        }
        /// <summary>
        /// Create a new CDrawer window
        /// </summary>
        /// <param name="iWindowXSize">Width of new CDrawer</param>
        /// <param name="iWindowYSize">Height of new CDrawer</param>
        /// <param name="bContinuousUpdate">Automatic rendering enabled</param>
        /// <param name="bRedundaMouse">Redunda-Mouse enabled</param>
        public CDrawer(int iWindowXSize = 800, int iWindowYSize = 600, bool bContinuousUpdate = true, bool bRedundaMouse = false)
        {
            // set window size, verify size
            m_ciWidth = iWindowXSize;
            m_ciHeight = iWindowYSize;
            RedundaMouse = bRedundaMouse;
            if (iWindowXSize < 1)
                throw new ArgumentException("CDrawer:CDrawer : Invalid window size! iWindowXSize must be > 0!");
            if (iWindowYSize < 1)
                throw new ArgumentException("CDrawer:CDrawer : Invalid window size! iWindowYSize must be > 0!");

            m_iScale = 1;

            // linked list of shapes init (empty)
            m_llShapes = new LinkedList<IRender>();

            // start the thread for the rendering window
            m_tDrawerThread = new Thread(TStart);
            m_tDrawerThread.IsBackground = true;
            m_tDrawerThread.Start(m_wDrawer);

            // silly, but required because object takes time to be created (in it's own thread)
            // can't assume instant (or blocking) creation...
            //System.Threading.Thread.Sleep(250);
            while (m_wDrawer == null || !m_wDrawer.m_bIsInitialized) // Wait for first Paint to complete
                System.Threading.Thread.Sleep(0); // Allow for Render() to complete

            System.Threading.Thread.Sleep(100); // Allow for potential 2nd Paint event to complete
            ContinuousUpdate = bContinuousUpdate;
        }

        /// <summary>
        /// Set a background persistent pixel in the CDrawer
        /// </summary>
        /// <param name="iX">X Coordinate</param>
        /// <param name="iY">Y Coordinate</param>
        /// <param name="colour">Color to set</param>
        public void SetBBPixel(int iX, int iY, Color colour)
        {
            if (iX < 0 || iX >= m_ciWidth)
                throw new ArgumentOutOfRangeException("CDrawer:SetBBPixel : iX must be between 0 and " + (m_ciWidth - 1).ToString() + " inclusive");
            if (iY < 0 || iY >= m_ciHeight)
                throw new ArgumentOutOfRangeException("CDrawer:SetBBPixel : iY must be between 0 and " + (m_ciHeight - 1).ToString() + " inclusive");

            if (m_wDrawer != null && m_tDrawerThread != null)
                m_wDrawer.SetBBPixel(new Point(iX, iY), colour);
        }

        /// <summary>
        /// Set a Scaled background persistent pixel in the CDrawer
        /// </summary>
        /// <param name="iX">Scaled X Coordinate</param>
        /// <param name="iY">Scaled Y Coordinate</param>
        /// <param name="colour">Color to set</param>
        public void SetBBScaledPixel(int iX, int iY, Color colour)
        {
            if (iX < 0 || iX >= m_ciWidth / m_iScale)
                throw new ArgumentOutOfRangeException("CDrawer:SetBBScaledPixel : iX must be between 0 and " + ScaledWidth.ToString());
            if (iY < 0 || iY >= m_ciHeight / m_iScale)
                throw new ArgumentOutOfRangeException("CDrawer:SetBBScaledPixel : iY must be between 0 and " + ScaledHeight.ToString());

            // attempt: use faster bb fill operation
            if (m_wDrawer != null && m_tDrawerThread != null)
                m_wDrawer.FillBBRect(new Rectangle(iX * m_iScale, iY * m_iScale, m_iScale, m_iScale), colour);
        }

        /// <summary>
        /// When set, this property will fill the back-buffer with the specified colour
        /// </summary>
        public Color BBColour
        {
            set
            {
                if (m_wDrawer != null && m_tDrawerThread != null)
                    m_wDrawer.FillBB(value);
            }
        }

        // thread entry point for drawer window to run in
        private void TStart(object o)
        {
            // create the drawing window and start it up
            m_wDrawer = new DrawerWnd(this);

            // setup delegate for callback in drawer window to use render in this
            m_wDrawer.m_delRender = new DrawerWnd.delIntGraphics(Render);

            // setup delegate for callback in drawer window to handle mouse move event
            m_wDrawer.m_delMouseMove = new DrawerWnd.delVoidPoint(CBMouseMove);

            // setup delegate for callback in drawer window to handle mouse click events
            m_wDrawer.m_delMouseLeftClick = new DrawerWnd.delVoidPoint(CBMouseLeftClick);
            m_wDrawer.m_delMouseRightClick = new DrawerWnd.delVoidPoint(CBMouseRightClick);

            // start the drawer window in this thread
            Application.Run(m_wDrawer);

            // thread will run out when drawing window application runs out
        }

        ///////////////////////////////////////////////////////////////////////////////////////
        // Callback from drawer window that handles a mouse move event
        ///////////////////////////////////////////////////////////////////////////////////////
        internal void CBMouseMove(Point pt)
        {
            // save the last known mouse move position and mark as changed
            m_pLastMouseMove = pt;
            m_bLastMouseMoveNew = true;

            // event stuff (call subscribers)
            try
            {
                if (MouseMove != null)
                    MouseMove(pt, this);
            }
            catch
            {
            }

            // scaled stuff
            Point pTemp = new Point(pt.X / m_iScale, pt.Y / m_iScale);
            if ((pTemp != m_pLastMouseMoveScaled) || RedundaMouse)
            {
                m_pLastMouseMoveScaled = pTemp;
                m_bLastMouseMoveNewScaled = true;

                // event stuff (call subscribers)
                try
                {
                    if (MouseMoveScaled != null)
                        MouseMoveScaled(pTemp, this);
                }
                catch
                {
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////
        // Callback from drawer window that handles a mouse left click event
        ///////////////////////////////////////////////////////////////////////////////////////
        internal void CBMouseLeftClick(Point pt)
        {
            // save the last known mouse click position and mark as changed
            m_pLastMouseLeftClick = pt;
            m_bLastMouseLeftClickNew = true;

            try
            {
                if (MouseLeftClick != null)
                    MouseLeftClick(pt, this);
            }
            catch
            {
            }

            Point pTemp = new Point(pt.X / m_iScale, pt.Y / m_iScale);
            if ((pTemp != m_pLastMouseLeftClickScaled) || RedundaMouse)
            {
                m_pLastMouseLeftClickScaled = pTemp;
                m_bLastMouseLeftClickNewScaled = true;

                try
                {
                    if (MouseLeftClickScaled != null)
                        MouseLeftClickScaled(pTemp, this);
                }
                catch
                {
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////
        // Callback from drawer window that handles a mouse right click event
        ///////////////////////////////////////////////////////////////////////////////////////
        internal void CBMouseRightClick(Point pt)
        {
            // save the last known mouse click position and mark as changed
            m_pLastMouseRightClick = pt;
            m_bLastMouseRightClickNew = true;

            try
            {
                if (MouseRightClick != null)
                    MouseRightClick(pt, this);
            }
            catch
            {
            }

            Point pTemp = new Point(pt.X / m_iScale, pt.Y / m_iScale);
            if ((pTemp != m_pLastMouseRightClickScaled) || RedundaMouse)
            {
                m_pLastMouseRightClickScaled = pTemp;
                m_bLastMouseRightClickNewScaled = true;

                try
                {
                    if (MouseRightClickScaled != null)
                        MouseRightClickScaled(pTemp, this);
                }
                catch
                {
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////
        // Rendering (intended to be used as a callback from the drawer window)
        ///////////////////////////////////////////////////////////////////////////////////////
        internal int Render(Graphics gr)
        {
            int iNum = 0;

            // walk through the linked list of renderables and instruct each to render
            lock (m_llShapes)
            {
                foreach (IRender renderme in m_llShapes)
                    renderme.Render(gr, m_iScale);

                iNum = m_llShapes.Count;
            }

            return iNum;
        }

        ///////////////////////////////////////////////////////////////////////////////////////
        // Rectangle Operations
        ///////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Add a Rectangle to the CDrawer
        /// </summary>
        /// <param name="iXStart">Bounding box Left/X start Coordinate</param>
        /// <param name="iYStart">Bounding box Top/Y start Coordinate</param>
        /// <param name="iWidth">Bounding box Width</param>
        /// <param name="iHeight">Bounding box Height</param>
        /// <param name="FillColor">Fill Color</param>
        /// <param name="iBorderThickness">Border Thickness</param>
        /// <param name="BorderColor">Border Color</param>
        public void AddRectangle(int iXStart, int iYStart, int iWidth, int iHeight, Color? FillColor = null, int iBorderThickness = 0, Color? BorderColor = null)
        {
            if (iWidth < 1 || iHeight < 1)
                throw new ArgumentException("The width and height of the rectangle must be greater than 1!");

            // add a rectangle to the list of shapes
            lock (m_llShapes)
                m_llShapes.AddLast(new CRectangle(iXStart, iYStart, iWidth, iHeight, FillColor, iBorderThickness, BorderColor));
        }

        /// <summary>
        /// Add a centered Rectangle to the CDrawer
        /// </summary>
        /// <param name="iXCenter">X Coordinate of Rectangle center point</param>
        /// <param name="iYCenter">Y Coordinate of Rectangle center point</param>
        /// <param name="iWidth">The width of the Rectangle</param>
        /// <param name="iHeight">The height of the Rectangle</param>
        /// <param name="FillColor">The Rectangle fill color</param>
        /// <param name="iBorderThickness">Thickness of the outside border</param>
        /// <param name="BorderColor">Color of the outside border</param>
        public void AddCenteredRectangle(int iXCenter, int iYCenter, int iWidth, int iHeight, Color? FillColor = null, int iBorderThickness = 0, Color? BorderColor = null)
        {
            if (iWidth < 1 || iHeight < 1)
                throw new ArgumentException("The width and height of the rectangle must be greater than 1!");

            // add a rectangle to the list of shapes
            lock (m_llShapes)
                m_llShapes.AddLast(new CRectangle(iXCenter - iWidth / 2, iYCenter - iHeight / 2, iWidth, iHeight, FillColor, iBorderThickness, BorderColor));
        }

        ///////////////////////////////////////////////////////////////////////////////////////
        // Ellipse Operations
        ///////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Add an Ellipse to the CDrawer
        /// </summary>
        /// <param name="iXStart">Bounding box Left/X start Coordinate</param>
        /// <param name="iYStart">Bounding box Top/Y start Coordinate</param>
        /// <param name="iWidth">Bounding box Width</param>
        /// <param name="iHeight">Bounding box Height</param>
        /// <param name="FillColor">Fill Color</param>
        /// <param name="iBorderThickness">Border Thickness</param>
        /// <param name="BorderColor">Border Color</param>
        public void AddEllipse(int iXStart, int iYStart, int iWidth, int iHeight, Color? FillColor = null, int iBorderThickness = 0, Color? BorderColor = null)
        {
            // add a rectangle to the list of shapes
            lock (m_llShapes)
            {
                m_llShapes.AddLast(new CEllipse(iXStart, iYStart, iWidth, iHeight, FillColor, iBorderThickness, BorderColor));
            }
        }
        /// <summary>
        /// Add a Centered Ellipse to the CDrawer
        /// </summary>
        /// <param name="iXCenter">X Coordinate of Ellipse center point</param>
        /// <param name="iYCenter">Y Coordinate of Ellipse center point</param>
        /// <param name="iWidth">Width</param>
        /// <param name="iHeight">Height</param>
        /// <param name="FillColor">Fill Color</param>
        /// <param name="iBorderThickness">Border thickness</param>
        /// <param name="BorderColor">Border Color</param>
        public void AddCenteredEllipse(int iXCenter, int iYCenter, int iWidth, int iHeight, Color? FillColor = null, int iBorderThickness = 0, Color? BorderColor = null)
        {
            // add a rectangle to the list of shapes
            lock (m_llShapes)
            {
                m_llShapes.AddLast(new CEllipse(iXCenter - iWidth / 2, iYCenter - iHeight / 2, iWidth, iHeight, FillColor, iBorderThickness, BorderColor));
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////
        // Line Operations
        ///////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Add a Line to the CDrawer where start and end points are defined
        /// </summary>
        /// <param name="iXStart">X Coordinate of Start Point</param>
        /// <param name="iYStart">Y Coordinate of Start Point</param>
        /// <param name="iXEnd">X Coordinate of End Point</param>
        /// <param name="iYEnd">Y Coordinate of End Point</param>
        /// <param name="LineColor">Line Color</param>
        /// <param name="iThickness">Line Thickness</param>
        public void AddLine(int iXStart, int iYStart, int iXEnd, int iYEnd, Color? LineColor = null, int iThickness = 1)
        {
            // add a line to the list of shapes
            lock (m_llShapes)
                m_llShapes.AddLast(new CLine(iXStart, iYStart, iXEnd, iYEnd, LineColor, iThickness));
        }
        /// <summary>
        /// Add a line segment to the CDrawer at a known start point using a length and rotation angle
        /// </summary>
        /// <param name="StartPos">Start Point of the Line</param>
        /// <param name="dLength">Length of Line segment</param>
        /// <param name="dRotation">Rotation around start in Radians ( 0 is Up )</param>
        /// <param name="LineColor">Line Color</param>
        /// <param name="iThickness">Line Thickness</param>
        public void AddLine(Point StartPos, double dLength, double dRotation = 0, Color? LineColor = null, int iThickness = 1)
        {
            if (dLength < 0)
                throw new ArgumentOutOfRangeException("dLength must be greater than or equal to 0");
            // add a line to the list of shapes
            lock (m_llShapes)
                m_llShapes.AddLast(new CLine(StartPos, dLength, dRotation, LineColor, iThickness));
        }

        /// <summary>
        /// Add a Bezier Spline to the CDrawer, with start point, control point 1, control point 2, and end point
        /// </summary>
        /// <param name="iXStart">Start X Position (start of line)</param>
        /// <param name="iYStart">Start Y Position (start of line)</param>
        /// <param name="iCtrlPt1X">Control X Position 1 (deflection)</param>
        /// <param name="iCtrlPt1Y">Control Y Position 1 (deflection)</param>
        /// <param name="iCtrlPt2X">Control X Position 2 (deflection)</param>
        /// <param name="iCtrlPt2Y">Control Y Position 2 (deflection)</param>
        /// <param name="iXEnd">End X Position (end of line)</param>
        /// <param name="iYEnd">End Y Position (end of line)</param>
        /// <param name="LineColor">Line Colour</param>
        /// <param name="iThickness">Line Thickness</param>
        public void AddBezier(int iXStart, int iYStart, int iCtrlPt1X, int iCtrlPt1Y, int iCtrlPt2X, int iCtrlPt2Y, int iXEnd, int iYEnd, Color? LineColor = null, int iThickness = 1)
        {
            lock (m_llShapes)
                m_llShapes.AddLast(new CBezier(iXStart, iYStart, iCtrlPt1X, iCtrlPt1Y, iCtrlPt2X, iCtrlPt2Y, iXEnd, iYEnd, LineColor, iThickness));
        }
        ///////////////////////////////////////////////////////////////////////////////////////
        // Polygon Operations
        ///////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Add a Polygon to the CDrawer
        /// </summary>
        /// <param name="iXStart">Start X Coordinate of bounding box</param>
        /// <param name="iYStart">Start Y Coordinate of bounding box</param>
        /// <param name="iVertexRadius">Distance of each vertex from center</param>
        /// <param name="iNumPoints">Number of points in polygon ( Triangle = 3 )</param>
        /// <param name="dRotation">Rotation in radians of polygon</param>
        /// <param name="FillColor">Fill Color of polygon</param>
        /// <param name="iBorderThickness">Border thickness</param>
        /// <param name="BorderColor">Border color</param>
        public void AddPolygon(int iXStart, int iYStart, int iVertexRadius, int iNumPoints, double dRotation = 0, Color? FillColor = null, int iBorderThickness = 0, Color? BorderColor = null)
        {
            // add a rectangle to the list of shapes
            lock (m_llShapes)
                m_llShapes.AddLast(new CPolygon(iXStart, iYStart, iVertexRadius, iNumPoints, dRotation, FillColor, iBorderThickness, BorderColor));
        }
        ///////////////////////////////////////////////////////////////////////////////////////
        // Text Operations
        ///////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Add text centered in the CDrawer
        /// </summary>
        /// <param name="sText">text to add</param>
        /// <param name="fTextSize">size of text, generally approximating point size</param>
        /// <param name="TextColor">Color of text</param>
        public void AddText(string sText, float fTextSize, Color ? TextColor = null)
        {
            // add a rectangle to the list of shapes
            lock (m_llShapes)
                m_llShapes.AddLast(new CText(sText, fTextSize, TextColor));
        }
        /// <summary>
        /// Add text centered in the defining bounding box, may clip
        /// </summary>
        /// <param name="sText">text to add</param>
        /// <param name="fTextSize">size of text, generally approximating point size</param>
        /// <param name="iXStart">X Coordinate of bounding box</param>
        /// <param name="iYStart">Y  Coordinate of bounding box</param>
        /// <param name="iWidth">Width of bounding box</param>
        /// <param name="iHeight">Height of bounding box</param>
        /// <param name="TextColor">Color of text</param>
        public void AddText(string sText, float fTextSize, int iXStart, int iYStart, int iWidth, int iHeight, Color ? TextColor = null)
        {
            // add a rectangle to the list of shapes
            lock (m_llShapes)
                m_llShapes.AddLast(new CText(sText, fTextSize, iXStart, iYStart, iWidth, iHeight, TextColor));
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////////
    // base class for all shapes contained by the drawer
    ///////////////////////////////////////////////////////////////////////////////////////    
    public abstract class CShape : IRender
    {
        protected int m_iXStart;
        protected int m_iYStart;
        protected Color m_Color;

        public CShape(int iXStart, int iYStart, Color? ShapeColor = null)
        {
            m_iXStart = iXStart;
            m_iYStart = iYStart;
            m_Color = ShapeColor != null ? (Color)ShapeColor : Color.Gray;
        }

        public int CompareTo(object obj)
        {
            if (obj is CShape)
            {
                CShape shape = (CShape)obj;
                if (this.m_iXStart == shape.m_iXStart && this.m_iYStart == shape.m_iYStart)
                    return 0;
                return 1;
            }
            else
                throw new ArgumentException("CShape:CompareTo : Object is not a CShape Object");
        }

        public abstract void Render(Graphics gr, int iScale);
    }

    public abstract class CBoundingRectWithBorderShape : CShape
    {
        protected int m_iWidth;
        protected int m_iHeight;

        protected int m_iBorderThickness;
        protected Color m_BorderColor;

        public CBoundingRectWithBorderShape(int iXStart, int iYStart, int iWidth, int iHeight, Color? FillColor = null, int iBorderThickness = 0, Color? BorderColor = null)
            : base (iXStart, iYStart, FillColor)
        {
            if (iWidth < 0)
                throw new ArgumentException("CShape:CShape : iWidth(" + iWidth.ToString() + ") must be >= 0");
            if (iHeight < 0)
                throw new ArgumentException("CShape:CShape : iHeight(" + iHeight.ToString() + ") must be >= 0");

            m_iWidth = iWidth;
            m_iHeight = iHeight;

            if (iBorderThickness < 0)
                throw new ArgumentException("CClosedShape : iBorderThickness(" + iBorderThickness.ToString() + ") must be >= 0");
            m_iBorderThickness = iBorderThickness;
            m_BorderColor = BorderColor != null ? (Color)BorderColor : m_Color;
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////////
    // rectangle class, for drawing simple rectangles
    ///////////////////////////////////////////////////////////////////////////////////////    
    public class CRectangle : CBoundingRectWithBorderShape
    {
        public CRectangle(int iXStart, int iYStart, int iWidth, int iHeight, Color? FillColor = null, int iBorderThickness = 0, Color? BorderColor = null)
            : base(iXStart, iYStart, iWidth, iHeight, FillColor, iBorderThickness, BorderColor)
        {
        }

        public override void Render(Graphics gr, int iScale)
        {
            gr.FillRectangle(new SolidBrush(m_Color), m_iXStart * iScale, m_iYStart * iScale, m_iWidth * iScale, m_iHeight * iScale);
            if (m_iBorderThickness > 0)
                gr.DrawRectangle(new Pen(m_BorderColor, m_iBorderThickness), m_iXStart * iScale, m_iYStart * iScale, m_iWidth * iScale, m_iHeight * iScale);
        }
    }
    ///////////////////////////////////////////////////////////////////////////////////////
    // Ellipse class, for drawing simple Ellipsess
    ///////////////////////////////////////////////////////////////////////////////////////    
    public class CEllipse : CBoundingRectWithBorderShape
    {
        public CEllipse(int iXStart, int iYStart, int iWidth, int iHeight, Color? FillColor = null, int iBorderThickness = 0, Color? BorderColor = null)
            : base(iXStart, iYStart, iWidth, iHeight, FillColor, iBorderThickness, BorderColor)
        {
        }

        public override void Render(Graphics gr, int iScale)
        {
            gr.FillEllipse(new SolidBrush(m_Color), m_iXStart * iScale, m_iYStart * iScale, m_iWidth * iScale, m_iHeight * iScale);
            if (m_iBorderThickness > 0)
                gr.DrawEllipse(new Pen(m_BorderColor, m_iBorderThickness), m_iXStart * iScale, m_iYStart * iScale, m_iWidth * iScale, m_iHeight * iScale);
        }
    }
    ///////////////////////////////////////////////////////////////////////////////////////
    // Polygon class, for drawing simple Polygonzes
    ///////////////////////////////////////////////////////////////////////////////////////    
    public class CPolygon : CShape
    {
        protected int m_iBorderThickness;
        protected Color m_BorderColor;
        protected int m_iNumPoints;
        protected double m_dRotation;
        protected int m_iVertexRadius;

        public CPolygon(int iXStart, int iYStart, int iVertexRadius, int iNumPoints, double dRotation = 0, Color? FillColor = null, int iBorderThickness = 0, Color? BorderColor = null)
            : base(iXStart, iYStart, FillColor)
        {
            if (iNumPoints < 3)
                throw new ArgumentException("CPolygon:CPolygon : iNumPoints(" + iNumPoints.ToString() + ") must be > 2");
            if (iBorderThickness < 0)
                throw new ArgumentException("CPolygon:CPolygon : iBorderThickness(" + iBorderThickness.ToString() + ") must be >= 0");
            m_iVertexRadius = iVertexRadius;
            m_iNumPoints = iNumPoints;
            m_dRotation = dRotation;
            m_iBorderThickness = iBorderThickness;
            m_BorderColor = BorderColor != null ? (Color)BorderColor : m_Color;
        }

        public override void Render(Graphics gr, int iScale)
        {
            Point[] points = new Point[m_iNumPoints];

            int iRad = m_iVertexRadius * iScale;
            for (int i = 0; i < m_iNumPoints; ++i)
            {
                points[i].X = (m_iXStart * iScale) + iRad + (int)(Math.Sin(2 * i * Math.PI / m_iNumPoints + m_dRotation) * iRad);
                points[i].Y = (m_iYStart * iScale) + iRad - (int)(Math.Cos(2 * i * Math.PI / m_iNumPoints + m_dRotation) * iRad);
            }
            gr.FillPolygon(new SolidBrush(m_Color), points);
            if (m_iBorderThickness > 0)
                gr.DrawPolygon(new Pen(m_BorderColor, m_iBorderThickness), points);
        }
    }
    ///////////////////////////////////////////////////////////////////////////////////////
    // Line class, for drawing simple Lines
    ///////////////////////////////////////////////////////////////////////////////////////    
    public class CLine : CShape
    {
        protected int m_iThickness;
        protected double m_dLineLength;

        protected const System.Drawing.Drawing2D.LineCap c_endCap = System.Drawing.Drawing2D.LineCap.Round;

        protected bool m_bPolar = false; // (if false, ignore rotation and line length, and start and end stand)
        protected double m_dRotation = 0; 
        protected int m_iXEnd = 0;
        protected int m_iYEnd = 0;        

        public CLine(int iXStart, int iYStart, int iXEnd, int iYEnd, Color? LineColor = null, int iThickness = 1)
            : base(iXStart, iYStart, LineColor) 
        {
            if (iThickness < 1)
                throw new ArgumentException("CLine:CLine : iThickness(" + iThickness.ToString() + ") must be > 0");
            
            m_iXEnd = iXEnd;
            m_iYEnd = iYEnd;
            m_iThickness = iThickness;
        }
        public CLine(Point StartPos, double dLength, double dRotation = 0, Color? LineColor = null, int iThickness = 1)
            : base(StartPos.X, StartPos.Y, LineColor)
        {
            if (iThickness < 1)
                throw new ArgumentException("CLine:CLine : iThickness(" + iThickness.ToString() + ") must be > 0");
            if (dLength < 0)
                throw new ArgumentException("CLine:CLine : dLength(" + dLength.ToString() + ") must be > 0");

            m_bPolar = true;
            m_dRotation = dRotation; 
            m_dLineLength = dLength;
            m_iThickness = iThickness;
            // x and y ends calculated in render (sad) 'cause scale can change between renders            
        }

        public override void Render(Graphics gr, int iScale)
        {
            using (Pen p = new Pen(m_Color, (float)m_iThickness))
            {
                p.StartCap = c_endCap;
                p.EndCap = c_endCap;
                if (m_bPolar) // use rotation to generate line
                {
                    int iXEnd = (m_iXStart * iScale) + (int)(Math.Sin(-m_dRotation - Math.PI) * m_dLineLength * iScale);
                    int iYEnd = (m_iYStart * iScale) + (int)(Math.Cos(-m_dRotation - Math.PI) * m_dLineLength * iScale);
                    gr.DrawLine(p, m_iXStart * iScale, m_iYStart * iScale, iXEnd, iYEnd);
                }
                else // user start/end pairs
                    gr.DrawLine(p, m_iXStart * iScale, m_iYStart * iScale, m_iXEnd * iScale, m_iYEnd * iScale);
            }
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////////
    // Bezier class, for drawing a bezier spline
    ///////////////////////////////////////////////////////////////////////////////////////    
    public class CBezier : CShape
    {
        protected int m_iThickness;
        protected int m_iCtrlPt1X, m_iCtrlPt1Y, m_iCtrlPt2X, m_iCtrlPt2Y, m_iXEnd, m_iYEnd;

        protected const System.Drawing.Drawing2D.LineCap c_endCap = System.Drawing.Drawing2D.LineCap.Round;

        public CBezier(int iXStart, int iYStart, int iCtrlPt1X, int iCtrlPt1Y, int iCtrlPt2X, int iCtrlPt2Y, int iXEnd, int iYEnd, Color? LineColor = null, int iThickness = 1)
            : base (iXStart, iYStart, LineColor)
        {
            if (iThickness < 1)
                throw new ArgumentException("CBezier : iThickness(" + iThickness.ToString() + ") must be > 0");

            m_iCtrlPt1X = iCtrlPt1X;
            m_iCtrlPt1Y = iCtrlPt1Y;
            m_iCtrlPt2X = iCtrlPt2X;
            m_iCtrlPt2Y = iCtrlPt2Y;
            m_iXEnd = iXEnd;
            m_iYEnd = iYEnd;
            m_iThickness = iThickness;
        }

        public override void Render(Graphics gr, int iScale)
        {
            using (Pen p = new Pen (m_Color, (float)m_iThickness))
            {
                p.StartCap = c_endCap;
                p.EndCap = c_endCap;

                gr.DrawBezier(p, m_iXStart, m_iYStart, m_iCtrlPt1X, m_iCtrlPt1Y, m_iCtrlPt2X, m_iCtrlPt2Y, m_iXEnd, m_iYEnd);
            }
        }
    }    
    
    ///////////////////////////////////////////////////////////////////////////////////////
    // Text Class
    ///////////////////////////////////////////////////////////////////////////////////////    
    public class CText : IRender
    {
        protected string m_sText;
        protected float m_fPointSize;
        protected bool m_bFull;                     // full scale centered
        protected System.Drawing.Rectangle m_rBoundingRect;       // if required
        protected Color m_Color;

        public CText(string sText, float fTextSize, Color ? TextColor = null)
        {
            if (fTextSize < 1)
                throw new ArgumentException("CText: Font Size (" + fTextSize.ToString() + ") must be >= 1.0f");
            m_bFull = true; // full drawer centering
            m_sText = sText;
            m_fPointSize = fTextSize;
            m_Color = TextColor != null ? (Color)TextColor : Color.Blue;
        }
        public CText(string sText, float fTextSize, int iXStart, int iYStart, int iWidth, int iHeight, Color ? TextColor = null)
        {
            if (fTextSize < 1)
                throw new ArgumentException("CText: Font Size (" + fTextSize.ToString() + ") must be >= 1.0f");
            if (iWidth < 0)
                throw new ArgumentException("SText : iWidth(" + iWidth.ToString() + ") must be >= 0");
            if (iHeight < 0)
                throw new ArgumentException("SText : iHeight(" + iHeight.ToString() + ") must be >= 0");

            m_bFull = false; // center in rectangle
            m_sText = sText;
            m_fPointSize = fTextSize;
            m_rBoundingRect = new System.Drawing.Rectangle(iXStart, iYStart, iWidth, iHeight);
 
            m_Color = TextColor != null ? (Color)TextColor : Color.Black;
        }

        public void Render(Graphics gr, int iScale)
        {
            StringFormat drawFormat = new StringFormat();
            drawFormat.FormatFlags = StringFormatFlags.NoWrap;
            drawFormat.Alignment = StringAlignment.Center;
            drawFormat.LineAlignment = StringAlignment.Center;
            drawFormat.Trimming = StringTrimming.EllipsisCharacter;

            int iWidth = m_bFull ? (int)gr.VisibleClipBounds.Width : m_rBoundingRect.Width * iScale;
            int iHeight = m_bFull ? (int)gr.VisibleClipBounds.Height : m_rBoundingRect.Height * iScale;
            gr.DrawString(m_sText, new Font("Trebuchet MS", m_fPointSize), new SolidBrush(m_Color), new RectangleF(m_rBoundingRect.X * iScale, m_rBoundingRect.Y * iScale, iWidth, iHeight), drawFormat);
        }
    }

    static public class RandColor
    {
        static private Random rnd = new Random();
        static KnownColor[] colors = (KnownColor[])Enum.GetValues(typeof(KnownColor));
        //public RandColor() { }
        /// <summary>
        /// Get one of 64 random calculated colors
        /// </summary>
        /// <returns></returns>
        static public Color GetColor()
        {
            switch (rnd.Next(3))
            {
                case 0: return Color.FromArgb(255, 255, rnd.Next(4) * 63, rnd.Next(4) * 63);
                case 1: return Color.FromArgb(255, rnd.Next(4) * 63, 255, rnd.Next(4) * 63);
                case 2: return Color.FromArgb(255, rnd.Next(4) * 63, rnd.Next(4) * 63, 255);
            }
            return Color.FromArgb(255, 125, 125, 125);
        }
        /// <summary>
        /// Get a random Known Color
        /// </summary>
        /// <returns></returns>
        static public Color GetKnownColor()
        {
            return Color.FromKnownColor(colors[rnd.Next(colors.Length)]);
        }
    }
}
