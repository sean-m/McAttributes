using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;


namespace SMM {
    public class CsvFileReader {
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

        public static IEnumerable<dynamic> GetRecordsDynamic(string CsvPath) {
            int index = 0;
            foreach (var r in GetRecords(CsvPath)) {
                index++;
                var record = new System.Dynamic.ExpandoObject();
                try {
                    foreach (var kv in r) {
                        ((ICollection<KeyValuePair<String, Object>>)record).Add(new KeyValuePair<string, object>(kv.Key, kv.Value));
                    }
                } catch (Exception e) {
                    throw new Exception($"Blew up on record {index}", e);
                }
                yield return record;
            }
        }

        public static IEnumerable<T> GetRecords<T>(string CsvPath, bool JustDoWhatYouCan = false) {
            if (!File.Exists(CsvPath)) throw new ArgumentNullException($"CsvFile path not exists {CsvPath}, do better. CD: {System.Environment.CurrentDirectory}");

            var csvReader = new CsvFileReader(CsvPath, true);
            var _ = csvReader.ReadFileValues().FirstOrDefault();

            var types = new Dictionary<string, PropertyInfo>();
            var modelType = typeof(T);
            if (modelType == null) { throw new Exception($"Can't get type info for {modelType.Name}, you got problems."); }

            Type? GetPropType(string name) {
                if (types.ContainsKey(name)) return types[name].PropertyType;

                var prop = modelType.GetProperties().FirstOrDefault(x => x.Name.ToString().Equals(name, StringComparison.CurrentCultureIgnoreCase));
                if (prop == null) {
                    return null;
                }

                types.Add(name, prop);
                return prop.PropertyType;
            }

            PropertyInfo? GetPropInfo(string name) {
                if (types.ContainsKey(name)) return types[name];

                var prop = modelType.GetProperties().FirstOrDefault(x => x.Name.ToString().Equals(name, StringComparison.CurrentCultureIgnoreCase));
                if (prop == null) {
                    return null;
                }

                types.Add(name, prop);
                return prop;
            }

            csvReader = new CsvFileReader(CsvPath);
            var userType = typeof(T);

            int rindex = 0;
            foreach (var row in csvReader.ReadFileValues()) {
                rindex++;
                var columns = row.Keys.Where(k => !String.IsNullOrEmpty(row.GetValueOrDefault(k)));

                int cindex = 1;
                var record = Activator.CreateInstance(modelType);
                try {
                    foreach (var col in columns) {
                        try {
                            var prop = GetPropInfo(col);
                            var type = GetPropType(col);
                            if (prop == null || type == null) { continue; }
                            object value = GetValueAsType(row[col], type);
                            prop.SetValue(record, value);
                        } catch (Exception e) {
                            if (!JustDoWhatYouCan) {
                                throw new Exception($"Encountered error handling property {col}", e);
                            }
                        }
                    }
                    cindex++;
                } catch (Exception e) {
                    throw new Exception($"Blew up on record {rindex} column {cindex}", e);
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