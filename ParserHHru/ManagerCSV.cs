using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserHHru
{
    public static class ManagerCSV
    {
        public static bool Write(List<Summary> summaries)
        {
            try
            {
                using (var writer = new StreamWriter("file.csv", false, Encoding.UTF8))
                using (var csv = new CsvWriter(writer))
                {
                    csv.WriteRecords(summaries);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }

        public static bool Write(List<Summary> summaries, string path)
        {
            try
            {
                using (var writer = new StreamWriter(path + "/file.csv", false, Encoding.UTF8))
                using (var csv = new CsvWriter(writer))
                {
                    csv.WriteRecords(summaries);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }

        public static bool Write(List<Summary> summaries, string path, bool isAppend)
        {
            try
            {
                using (var writer = new StreamWriter(path + "/file.csv", isAppend, Encoding.UTF8))
                using (var csv = new CsvWriter(writer))
                {
                    csv.WriteRecords(summaries);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }
    }
}
