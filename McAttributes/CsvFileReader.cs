using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;

namespace McAttributes
{
    public class CsvFileReader
    {
        static string csv_delimiter_pattern = @"(?:^|,)(?=[^""]|("")?)""?((?(1)(?:[^""]|"""")*|[^,""]*))""?(?=,|$)";
        static Regex csv_delimiter = new Regex(csv_delimiter_pattern, RegexOptions.Compiled);

        string file_path;
        StreamReader? file;
        

        public CsvFileReader(string FilePath)
        {
            init(FilePath);
        }


        public CsvFileReader(string FilePath, bool EmtyStringsAsNull)
        {
            this.EmtyStringsAsNull = EmtyStringsAsNull;
            init(FilePath);
        }


        private void init(string FilePath)
        {
            if (string.IsNullOrEmpty(FilePath)) throw new ArgumentNullException("FilePath needs to have a value.");
            if (!File.Exists(FilePath)) throw new FileNotFoundException(@"Cannot find file: {FilePath}");

            file_path = FilePath;
        }

        public bool EmtyStringsAsNull { get; set; } = false;
        public bool HasHeaderRow { get; set; } = true;
        public List<string>? Header;
        private IEnumerable<string> GetLines()
        {
            while (file != null && !file.EndOfStream)
            {
                yield return file.ReadLine() ?? string.Empty;
            }

        }


        public IEnumerable<Dictionary<string, string>> ReadFileValues()
        {
            using (var parser = new TextFieldParser(file_path))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");

                if (!parser.EndOfData)
                {
                    var header = parser.ReadFields();
                    this.Header = new List<string>(header);
                }


                while (!parser.EndOfData)
                {
                    var record = new Dictionary<string, string>();
                    var fields = parser.ReadFields();
                    foreach (var i in Enumerable.Range(0, Header?.Count ?? 0))
                    {
                        var k = Header.Skip(i)?.Take(1)?.First();
                        var f = fields.Skip(i)?.Take(1)?.First();
                        if (EmtyStringsAsNull && String.IsNullOrEmpty(f))
                        {
                            f = null;
                        }
                        record.Add(k, f);
                    }
                    yield return record;
                }
            }
        }
    }
}
