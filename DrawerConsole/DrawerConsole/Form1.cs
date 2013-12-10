using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GDIDrawer;

namespace DrawerConsole
{
    public partial class Form1 : Form
    {
        CDrawer _dr = new CDrawer(80 * 11, 24 * 15);
        Random _rnd = new Random();
        ConsoleManager con = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            con = new ConsoleManager(_dr);
        }

        private void UI_TIM_Render_Tick(object sender, EventArgs e)
        {
            con.ForeCol = GDIDrawer.RandColor.GetColor();
            //con.BackCol = GDIDrawer.RandColor.GetColor();
            con.WriteChar(_rnd.Next(0, 80), _rnd.Next(0, 24), (char)_rnd.Next(0, 128));
        }
    }

    public class ConsoleManager
    {
        private SConChar[,] _BuffWorking = new SConChar[24, 80];
        private SConChar[,] _BuffPresented = new SConChar[24, 80];

        public int PosX { get; set; }
        public int PosY { get; set; }
        public Color ForeCol { get; set; }
        public Color BackCol { get; set; }
        public CDrawer _dr { get; private set; }

        public ConsoleManager(CDrawer target)
        {
            _dr = target;
            ForeCol = Color.White;
            BackCol = Color.Black;
        }

        public void WriteChar(int x, int y, char c)
        {
            if (x < 0 || x > 79)
                return;
            if (y < 0 || y > 23)
                return;
            if (c < 0 || c > 127)
                return;

            _BuffWorking[y, x] = new SConChar(c, ForeCol, BackCol);

            Render();
        }

        public void WriteLine(string str)
        {
            foreach (char c in str)
            {
                _BuffWorking[PosY, PosX] = new SConChar(c, ForeCol, BackCol);
                IncCursor();
            }
            CReturn();
            Render();            
        }

        private void CReturn()
        {
            PosX = 0;
            PosY++;
            if (PosY > 23)
                PosY = 0;
        }

        private void IncCursor()
        {
            PosX++;
            if (PosX > 79)
            {
                PosX = 0;
                PosY++;
                if (PosY > 23)
                    PosY = 0;
            }
        }

        public void Render()
        {
            for (int iy = 0; iy < 24; iy++)
            {
                for (int ix = 0; ix < 80; ix++)
                {
                    if (!_BuffPresented[iy, ix].Equals(_BuffWorking[iy, ix]))
                    {                        
                        _BuffWorking[iy, ix].Render(_dr, ix, iy);
                        _BuffPresented[iy, ix] = _BuffWorking[iy, ix];
                    }
                }
            }
        }
    }

    public struct SConChar
    {
        public Color ForeCol;
        public Color BackCol;

        public char Code;

        public SConChar(char letter, Color FCol, Color BCol)
        {
            Code = letter;
            ForeCol = FCol;
            BackCol = BCol;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SConChar))
                return false;

            SConChar other = (SConChar)obj;

            return this.Code.Equals(other.Code) && this.ForeCol.Equals(other.ForeCol) && this.BackCol.Equals (other.BackCol);            
        }

        public override int GetHashCode()
        {
            return 1;
        }

        private static Dictionary<char, bool[,]> CharMap = new Dictionary<char, bool[,]>();

        static SConChar ()
        {
            string map = DrawerConsole.Properties.Resources.CharMap;
            List<char> chars = new List<char>(map);
            chars.RemoveAll((o) => char.IsWhiteSpace(o));
            
            // the first character will be the letter the map represents
            while (chars.Count >= 1 + 5 * 7)
            {
                // process a character
                string s = new string (chars.GetRange(0, 1 + 5 * 7).ToArray());
                chars.RemoveRange(0, 1 + 5 * 7);

                if (!CharMap.ContainsKey (s[0]))
                {
                    CharMap.Add (s[0], new bool [7,5]);
                    int ix = 0;
                    int iy = 0;
                    for (int i = 0; i < 7 * 5; ++i, ++ix)
                    {
                        if (ix > 4)
                        {
                            ix = 0;
                            iy++;
                        }
                        CharMap[s[0]][iy, ix] = s[i + 1] == '*';
                    }
                }
            }
        }

        public void Render(CDrawer dr, int CellX, int CellY)
        {
            if (CharMap.ContainsKey(Code))
            {
                for (int iy = 0; iy < 7; ++iy)
                    for (int ix = 0; ix < 5; ++ix)
                        if (CharMap[Code][iy, ix])
                            SetSub(dr, ix, iy, CellX, CellY, ForeCol);
                        else
                            SetSub(dr, ix, iy, CellX, CellY, BackCol);
            }
        }

        private void SetSub(CDrawer dr, int ix, int iy, int CellX, int CellY, Color Col)
        {
            int iXOffset = CellX * 11;
            int iYOffset = CellY * 15;

            SetGuard(dr, iXOffset + ix * 2, iYOffset + iy * 2, Col);
            SetGuard(dr, iXOffset + ix * 2 + 1, iYOffset + iy * 2, Col);
            SetGuard(dr, iXOffset + ix * 2, iYOffset + iy * 2 + 1, Col);
            SetGuard(dr, iXOffset + ix * 2 + 1, iYOffset + iy * 2 + 1, Col);
        }

        private void SetGuard(CDrawer dr, int ix, int iy, Color col)
        {
            if (ix >= 0 && ix < dr.m_ciWidth && iy >= 0 && iy < dr.m_ciHeight)
                dr.SetBBPixel(ix, iy, col);
        }
    }
}
