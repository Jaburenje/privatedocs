using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrivateDocs
{
    public static class OpenFileCS
    {

        public static Process OpenMicrosoftWord(string f)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "WINWORD.EXE";
            startInfo.Arguments = f;
            Process pro = new Process();
            pro.StartInfo = startInfo;
            pro.Start();
            return pro;
        }
        public static Process OpenExcel(string f)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "EXCEL.EXE";
            startInfo.Arguments = f;
            Process pro = new Process();
            pro.StartInfo = startInfo;
            pro.Start();
            return pro;
        }
        public static Process OpenPicView(string f)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "PAINT.EXE";
            startInfo.Arguments = f;
            Process pro = new Process();
            pro.StartInfo = startInfo;
            pro.Start();
            return pro;
        }
    }
}
