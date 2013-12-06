using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using GDIDrawer;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace DrawerDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            CDrawer dr = new CDrawer();
            dr.BBColour = Color.White;
            Random rnd = new Random();
            dr.Scale = 20; // adjust scale to test ScaledWidth/ScaledHeight

            // Disable continuous update
            dr.ContinuousUpdate = false;
            // perform lengthy/high object count operation
            for (int i = 0; i < 1000; ++i)
            {
              dr.AddEllipse(rnd.Next(dr.ScaledWidth), rnd.Next(dr.ScaledHeight), 1, 1, RandColor.GetColor());
              dr.Render(); // tell drawer to show now, all elements have been added
            }
            

            int iNum = 0;

            iNum++;




Point pCoord;           // coords to accept mouse click pos
int iNumClicks = 0;     // count number of clicks accepted
int iFalseAlarm = 0;    // count the number of poll calls
do
{
    bool bRes = dr.GetLastMouseLeftClick(out pCoord);   // poll
    if (bRes)                                           // new coords?
    {
        ++iNumClicks;
        dr.AddEllipse(pCoord.X - 10, pCoord.Y - 10, 20, 20);

    }
    else
        iFalseAlarm++;                                  // not new coords
}
while (iNumClicks < 10);

Console.WriteLine("Checked for coordinates " + iFalseAlarm.ToString() + " times!");
            

            Console.ReadKey();

            /*
            {
                FileStream foo = new FileStream("snot", FileMode.Create);
                MemoryStream ms = new MemoryStream();
                BinaryFormatter bf = new BinaryFormatter();

                SThing temp = new SThing();
                temp.i = 42;

                bf.Serialize(ms, temp);
                foo.Write(ms.GetBuffer(), 0, (int)ms.Length);
                foo.Close();
            }

            {
                FileStream foo = new FileStream("snot", FileMode.Open);
                BinaryFormatter bf = new BinaryFormatter();

                object o = bf.Deserialize(foo);
                if (o is SThing)
                {
                    SThing temp = (SThing)o;
                    Console.WriteLine (temp.i.ToString());
                }
            }

            Console.ReadKey();
            */
        }

    }

    /*
    [Serializable]
    struct SThing
    {
        public int i;
    }
    */
}
