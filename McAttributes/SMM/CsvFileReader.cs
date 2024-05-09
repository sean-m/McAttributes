using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;

namespace SMM
{
    public class CsvFileReader {
        static string csv_delimiter_pattern = @"(?:^|,)(?=[^""]|("")?)""?((?(1)(?:[^""]|"""")*|[^,""]*))""?(?=,|$)";
        static Regex csv_delimiter = new Regex(csv_delimiter_pattern, RegexOptions.Compiled);

        string file_path;

        private CsvFileReader(string FilePath) {
            init(FilePath);
        }

        private CsvFileReader(string FilePath, bool EmtyStringsAsNull) {
            this.EmtyStringsAsNull = EmtyStringsAsNull;
            init(FilePath);
        }


        private void init(string FilePath) {
            if (string.IsNullOrEmpty(FilePath)) throw new ArgumentNullException("FilePath needs to have a value.");
            if (!File.Exists(FilePath)) throw new FileNotFoundException(@"Cannot find file: {FilePath}");

            file_path = FilePath;
        }

        public bool EmtyStringsAsNull { get; set; } = false;
        public bool HasHeaderRow { get; set; } = true;
        public List<string>? Header { get; set; }


        private IEnumerable<Dictionary<string, string>> ReadFileValues() {
            using (var parser = new TextFieldParser(file_path)) {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");

                if (!parser.EndOfData && HasHeaderRow) {
                    var headerRow = parser.ReadFields();
                    this.Header = new List<string>(headerRow);
                }

                IEnumerable<string>? header = null;

                while (!parser.EndOfData) {
                    var record = new Dictionary<string, string>();
                    var fields = parser.ReadFields();

                    if (header == null && Header != null)
                        header = Header;
                    else if (header == null && fields != null)
                        header = Enumerable.Range(0, fields.Length).Cast<string>();

                    int i = 0;
                    foreach (var f in fields) {
                        var val = f;
                        var k = header?.Skip(i)?.Take(1)?.First();
                        if (EmtyStringsAsNull && String.IsNullOrEmpty(f)) {
                            val = null;
                        }
                        record.Add(k, val);
                        i++;
                    }
                    yield return record;
                }
            }
        }

        public static IEnumerable<Dictionary<string, string>> GetRecords(string CsvPath) {
            return GetRecords(CsvPath, true);
        }

        public static IEnumerable<Dictionary<string, string>> GetRecords(string CsvPath, bool EmptyStringsAsNull) {
            var csvReader = new CsvFileReader(CsvPath, EmptyStringsAsNull);
            return csvReader.ReadFileValues();
        }

        public static IEnumerable<T> GetRecords<T>(string CsvPath) {
            if (!File.Exists(CsvPath)) throw new ArgumentNullException($"CsvFile path not exists {CsvPath}, do better. CD: {System.Environment.CurrentDirectory}");


            var csvReader = new CsvFileReader(CsvPath, true);
            var _ = csvReader.ReadFileValues().FirstOrDefault();

            var types = new Dictionary<string, PropertyInfo>();
            var modelType = typeof(T);
            if (modelType == null) { throw new Exception($"Can't get type info for {modelType.Name}, you got problems."); }

            Type GetPropType(string name) {
                if (types.ContainsKey(name)) return types[name].PropertyType;

                var prop = modelType.GetProperties().FirstOrDefault(x => x.Name.ToString().Equals(name, StringComparison.CurrentCultureIgnoreCase));
                if (prop == null) {
                    throw new Exception($"Cannot resolve property with name: {name} on class '{modelType.Name}'");
                }

                types.Add(name, prop);
                return prop.PropertyType;
            }

            PropertyInfo GetPropInfo(string name) {
                if (types.ContainsKey(name)) return types[name];

                var prop = modelType.GetProperties().FirstOrDefault(x => x.Name.ToString().Equals(name, StringComparison.CurrentCultureIgnoreCase));
                if (prop == null) {
                    throw new Exception($"Cannot resolve property with name: {name} on class '{modelType.Name}'");
                }

                types.Add(name, prop);
                return prop;
            }

            csvReader = new CsvFileReader(CsvPath);
            var userType = typeof(T);

            foreach (var row in csvReader.ReadFileValues()) {
                var columns = row.Keys.Where(k => !String.IsNullOrEmpty(row.GetValueOrDefault(k)));

                var record = Activator.CreateInstance(modelType);
                foreach (var col in columns) {
                    var prop = GetPropInfo(col);
                    var type = GetPropType(col);
                    object value = GetValueAsType(row[col], type);
                    prop.SetValue(record, value);
                }
                yield return (T)record;
            }
        }

        internal static object? GetValueAsType(object source, Type desiredType) {
            if (source == null) return source;

            string strSrc = source.ToString();
            if (string.IsNullOrEmpty(strSrc)) {
                return null;
            }

            if (desiredType == typeof(Guid)) {
                return Guid.Parse(source.ToString());
            } else if (desiredType == typeof(DateTime?)) {
                return DateTime.Parse(source.ToString());
            } else if (desiredType.IsEnum) {
                dynamic result;
                if (Enum.TryParse(desiredType, strSrc, true, out result)) return result;
                else return null;
            }

            return Convert.ChangeType(source, desiredType);
        }
    }
}
