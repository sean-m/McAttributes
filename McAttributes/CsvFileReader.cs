using System.Text.RegularExpressions;

namespace McAttributes
{
    public class CsvFileReader
    {
        static string csv_delimiter_pattern = @"(?:^|,)(?=[^""]|("")?)""?((?(1)(?:[^""]|"""")*|[^,""]*))""?(?=,|$)";
        static Regex csv_delimiter = new Regex(csv_delimiter_pattern, RegexOptions.Compiled);

        readonly string file_path;
        StreamReader? file;

        public CsvFileReader(string FilePath)
        {
            if (string.IsNullOrEmpty(FilePath)) throw new ArgumentNullException("FilePath needs to have a value.");
            if (!File.Exists(FilePath)) throw new FileNotFoundException(@"Cannot find file: {FilePath}");

            file_path = FilePath;
        }

        public bool HasHeaderRow { get; set; } = true;
        public List<string>? Header;
        public List<Dictionary<string, string>> Values = new List<Dictionary<string, string>>();

        private IEnumerable<string> GetLines()
        {
            while (file != null && !file.EndOfStream)
            {
                yield return file.ReadLine() ?? string.Empty;
            }

        }

        public void ReadFile()
        {
            using (file = File.OpenText(file_path))
            {
                string header_row = GetLines().First(x => !string.IsNullOrEmpty(x));
                var matches = csv_delimiter.Matches(header_row).Select(
                    x => x.Value
                        .TrimStart(new char[] { ',', '"' })
                        .TrimEnd('"'));

                if (matches != null)
                {
                    Header = new List<string>(matches);

                    foreach (var row in GetLines())
                    {
                        var line = csv_delimiter.Matches(row).Select(
                                    x => x.Value
                                        .TrimStart(new char[] { ',', '"' })
                                        .TrimEnd('"'));

                        var record = new Dictionary<string, string>();
                        for (var i = 0; i < Header.Count; i++)
                        {
                            var key = Header.Skip(i).Take(1).First();
                            var value = line.Skip(i).Take(1).First()?.Replace("&#xa;", Environment.NewLine) ?? string.Empty;
                            record.Add(key, value);
                        }
                        Values.Add(record);
                    }
                }
            }
        }
    }
}
