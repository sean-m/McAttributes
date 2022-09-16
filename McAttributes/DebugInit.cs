using Microsoft.Data.Sqlite;
using System.Linq;

namespace McAttributes {
    public static class DebugInit {
        public static void DbInit(SqliteConnection conn) {
            if (conn == null) throw new ArgumentNullException("Gonna hand me a viable databse connection.");
            if (conn.State != System.Data.ConnectionState.Open) {
                throw new Exception("Database connection is borkened. I need a working/opened one!");
            }

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

            var csvReader = new CsvFileReader(@"C:\Users\smcardle_admin\source\repos\McAttributes\McAttributes\test_azusers.csv", true);
            var _ = csvReader.ReadFileValues().FirstOrDefault();

            var columns = String.Join(", ", csvReader.Header);
            var parameters = String.Join(", ", csvReader.Header.Select(x => $"@{x}"));
            var paramQuery = @$"
insert into azusers ({columns})
values ({parameters});
";

            csvReader = new CsvFileReader(@"C:\Users\smcardle_admin\source\repos\McAttributes\McAttributes\test_azusers.csv");
            foreach (var row in csvReader.ReadFileValues())
            {
                var sqlCmd = new SqliteCommand(paramQuery, conn);
                foreach (var col in row)
                {
                    sqlCmd.Parameters.Add(new SqliteParameter($"@{col.Key}", col.Value));
                }
                sqlCmd.ExecuteReader();
            }

            paramQuery = @"
insert into EmployeeIdRecord (CloudAnchor, UserPrincipalName, EmployeeId)
values (@cloudanchor, @userprincipalname, @employeeid);
";
            csvReader = new CsvFileReader(@"C:\Users\smcardle_admin\source\repos\McAttributes\McAttributes\employeeId_values.csv");
            _ = csvReader.ReadFileValues().FirstOrDefault();

            columns = String.Join(", ", csvReader.Header);
            parameters = String.Join(", ", csvReader.Header.Select(x => $"@{x}"));
            paramQuery = @$"
insert into EmployeeIdRecord ({columns})
values ({parameters});
";

            foreach (var row in csvReader.ReadFileValues())
            {
                var sqlCmd = new SqliteCommand(paramQuery, conn);
                foreach (var col in row)
                {
                    sqlCmd.Parameters.Add(new SqliteParameter($"@{col.Key.ToLower()}", col.Value));
                }
                //sqlCmd.ExecuteReader();
            }
        }
    }
}
