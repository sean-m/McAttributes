
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace SMM {

    public static class UglyDbInitHelper {

        public static void DbInit<T>(DbContext context, string CsvPath) {
            if (context == null) throw new ArgumentNullException("Gonna hand me a viable databse connection.");
            if (!File.Exists(CsvPath)) throw new ArgumentNullException($"CsvFile path not exists {CsvPath}, do better.");


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
                    object value = CsvFileReader.GetValueAsType(row[col], type);
                    prop.SetValue(record, value);
                }
                if (record != null) context.Add(Convert.ChangeType(record, modelType));
            }

            context.SaveChanges();
        }
    }
}

