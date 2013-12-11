using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace GDIDrawer
{
    class StatusLog
    {
        private string _FileName;
        private Queue<string> _qLogItems = new Queue<string>();
        private Thread _tLogThread = null;

        public StatusLog(string sFileName)
        {
            // attempt to open the log for writing, to ensure it's ok
            _FileName = sFileName;

            // start the log writing thread
            try
            {
                _tLogThread = new Thread(ThreadLog);
                _tLogThread.IsBackground = true;
                _tLogThread.Start();

                // show startup log message
                WriteLine("----------------------------------------------------------------------");
            }
            catch (Exception err)
            {
                Console.WriteLine("Error starting log thread : " + err.Message);
            }
        }

        public void WriteLine(string sText)
        {
            string sLogItem = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToLongTimeString() + " - " + sText;
            lock (_qLogItems)
                _qLogItems.Enqueue(sLogItem);
        }

        private void ThreadLog(object o)
        {
            string sLogItem = "";
            bool bIsItem;

            while (true)
            {
                // grab a log item, if there is one to get...
                bIsItem = false;
                lock (_qLogItems)
                {
                    if (_qLogItems.Count != 0)
                    {
                        sLogItem = _qLogItems.Dequeue();
                        bIsItem = true;
                    }
                }

                if (bIsItem)
                {
                    // log it...
                    try
                    {
                        StreamWriter sw = new StreamWriter(_FileName, true, Encoding.UTF8);
                        sw.WriteLine(sLogItem);
                        sw.Close();
                    }
                    catch (Exception err)
                    {
                        _FileName = "Error : " + err.Message;
                    }
                }

                Thread.Sleep(1);
            }
        }
    }
}
