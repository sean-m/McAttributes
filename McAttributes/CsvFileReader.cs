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
        public List<string>? Header { get; set; }

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

                if (!parser.EndOfData && HasHeaderRow)
                {
                    var headerRow = parser.ReadFields();
                    this.Header = new List<string>(headerRow);
                }

                IEnumerable<string>? header = null;

                while (!parser.EndOfData)
                {
                    var record = new Dictionary<string, string>();
                    var fields = parser.ReadFields();

                    if (header == null && Header != null)
                        header = Header;
                    else if (header == null && fields != null)
                        header = Enumerable.Range(0, fields.Length).Cast<string>();

                    int i = 0;
                    foreach (var f in fields)
                    {
                        var val = f;
                        var k = header.Skip(i)?.Take(1)?.First();
                        if (EmtyStringsAsNull && String.IsNullOrEmpty(f))
                        {
                            val = null;
                        }
                        record.Add(k, val);
                        i++;
                    }
                    yield return record;
                }
            }
        }
    }
}
