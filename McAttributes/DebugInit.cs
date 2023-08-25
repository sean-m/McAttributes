using McAttributes.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Npgsql;
using SMM;
using System.Linq;
using System.Reflection;

namespace McAttributes {
    public static class DebugInit {
        public async static void DbInit(NpgsqlConnection conn) {
            if (conn == null) throw new ArgumentNullException("Gonna hand me a viable databse connection.");
            if (conn.State != System.Data.ConnectionState.Open) {
                throw new Exception("Database connection is borkened. I need a working/opened one!");
            }

            /*
            string userTableSchema = @"

-- Make the User table
create table if not exists azusers (
    id integer PRIMARY KEY,
    merged TIMESTAMP default null,
    modified TIMESTAMP default null,
    lastFetched TIMESTAMP default null,
    created TIMESTAMP default null,
    enabled BOOLEAN,
    tenant TEXT,
    aadId UUID UNIQUE,
    upn TEXT,
    mail TEXT,
    employeeId TEXT,
    adEmployeeId TEXT,
    hrEmployeeId TEXT,
    wid TEXT,
    creationType TEXT,
    company TEXT,
    displayName TEXT,
    preferredGivenName TEXT,
    preferredSurname TEXT,
    articles TEXT,
    pronouns TEXT
);

delete from azusers;



create table if not exists EmployeeIdRecord (
    id integer PRIMARY KEY,
    partitionKey text,
    rowKey text,
    cloudAnchor text not null,
    userPrincipalName text not null,
    employeeId text null,
    adEmployeeId text null
);

delete from EmployeeIdRecord;



--drop table if exists IssueLogEntry;
create table if not exists IssueLogEntry (
    id integer primary key,
    created TIMESTAMP default CURRENT_TIMESTAMP,
    targetUserId integer,
    level integer default 1,
    entryId integer,
    message text, 
    acknowledged boolean,
    resolved boolean,

    FOREIGN KEY(TargetUserId) REFERENCES azusers(id)
);

";
            
            
            var userTableCmd = new SqliteCommand(userTableSchema, conn);
            userTableCmd.ExecuteNonQuery();
            */

            var csvReader = new CsvFileReader(@"./test_values.csv", true);
            var _ = csvReader.ReadFileValues().FirstOrDefault();
                        
            var types = new Dictionary<string, Type>();
            var azuser = System.Reflection.TypeInfo.GetType("McAttributes.Models.User");


            Type GetParamType(string name) {
                if (types.ContainsKey(name)) return types[name];

                var prop = azuser?.GetProperties().FirstOrDefault(x => x.Name.ToString().Equals(name, StringComparison.CurrentCultureIgnoreCase));
                if (prop == null) {
                    throw new Exception($"Cannot resolve property with name: {name} on class 'Models.User'");
                }

                types.Add(name, prop.PropertyType);
                return prop.PropertyType;
            }

            var inserts = new List<Task>();

            csvReader = new CsvFileReader(@"./test_values.csv");
            foreach (var row in csvReader.ReadFileValues())
            {
                var columns = row.Keys.Where(k => !String.IsNullOrEmpty(row.GetValueOrDefault(k)));

                var paramQuery = @$"
                insert into azusers ({String.Join(',', columns)})
                values ({String.Join(',', columns.Select(x => $"@{x}"))})
                on conflict(aadid)
                do nothing;
                ";
                var sqlCmd = new NpgsqlCommand(paramQuery, conn);
                foreach (var col in columns)
                {
                    var type = GetParamType(col);
                    object value = GetAsType(row[col], type);
                    sqlCmd.Parameters.Add(new NpgsqlParameter($"@{col}", value));
                }
                inserts.Add(sqlCmd.ExecuteNonQueryAsync());
            }

            await Task.WhenAll(inserts.ToArray());
        }

        public static void DbInit(IdDbContext context) {
            if (context == null) throw new ArgumentNullException("Gonna hand me a viable databse connection.");

            UglyDbInitHelper.DbInit<Models.User>(context, @"./test_values.csv");
        }

        public static void DbInit<T>(IdDbContext context) {
            if (context == null) throw new ArgumentNullException("Gonna hand me a viable databse connection.");

            UglyDbInitHelper.DbInit<T>(context, @"./test_values.csv");
        }

        static object? GetAsType(object source, Type desiredType) {
            if (source == null) return source;

            string strSrc = source.ToString();
            if (string.IsNullOrEmpty(strSrc)) {
                return null;
            }

            if (desiredType == typeof(Guid)) {
                return Guid.Parse(source.ToString());
            } else if (desiredType == typeof(DateTime?)) {
                return DateTime.Parse(source.ToString());
            }

            return Convert.ChangeType(source, desiredType);
        }
    }
}
