using CsvHelper;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using YWB.SiteTranslator.Helpers;
using YWB.SiteTranslator.Model;

namespace YWB.SiteTranslator
{
    public class CSVProcessor
    {
        private const string FileName = "translation.csv";
        private string _fullPath = PathHelper.GetFullPath(FileName);

        public void Write(List<TextItem> content)
        {
            using (var writer = new StreamWriter(_fullPath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(content);
            }
        }

        public List<TextItem> Read()
        {
            using (var reader = new StreamReader(_fullPath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<TextItem>();
                return records.ToList();
            }

        }
    }
}
