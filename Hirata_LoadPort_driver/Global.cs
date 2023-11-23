using System;
using System.IO;
using System.Linq;

namespace Hirata_LoadPort_driver
{
    class Global
    {
        public static string BasePath = Path.GetFullPath(Path.Combine(System.AppContext.BaseDirectory, @"..\..\"));
        public static string logfilePath = Path.GetFullPath(Path.Combine(System.AppContext.BaseDirectory, @"..\..\Hirata_LoadPort_Log\"));        
        
        public static void EventLog(string Msg)
        {
            string sDate = DateTime.Today.ToShortDateString();
            string sTime = DateTime.Now.ToString("HH:mm:ss:fff");
            string sDateTime;
            sDateTime = "[" + sDate + ", " + sTime + "] ";

            WriteFile(sDateTime + Msg);
        }

        private static void WriteFile(string Msg)
        {
            string sDate = DateTime.Today.ToShortDateString();
            string FileName = sDate + ".txt";

            if (File.Exists(logfilePath + FileName))
            {
                StreamWriter writer;
                writer = File.AppendText(logfilePath + FileName);
                writer.WriteLine(Msg);
                writer.Close();
            }
            else
            {
                CreateFile(Msg);
            }
        }

        private static void CreateFile(string Msg)
        {
            string sDate = DateTime.Today.ToShortDateString();
            string FileName = sDate + ".txt";

            if (!File.Exists(logfilePath + FileName))
            {
                using (File.Create(logfilePath + FileName)) ;
            }

            StreamWriter writer;
            writer = File.AppendText(logfilePath + FileName);
            writer.WriteLine(Msg);
            writer.Close();
        }

        public static string Checksum(string Data)
        {
            try
            {
                string hexstring = ConvertToHex(Data);                
                return Csum(hexstring);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static string ConvertToHex(string asciiString)
        {
            string hex = "";
            foreach (char c in asciiString)
            {
                int tmp = c;
                hex += string.Format("{0:X2}", Convert.ToUInt32(tmp.ToString()));
            }
            return hex;
        }

        private static string Csum(string data)
        {
            int total = StringToByteArray(data).Sum(x => x);
            total %= 0x100;
            string totalStr = string.Format("{0:X2}", total);
            
            return totalStr;
        }

        private static byte[] StringToByteArray(string text)
        {
            return Enumerable.Range(0, text.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(text.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}
