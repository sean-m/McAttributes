using Microsoft.Data.Sqlite;

namespace McAttributes {
    public static class DebugInit {
        public static void DbInit(SqliteConnection conn) {
            if (conn == null) throw new ArgumentNullException("Gonna hand me a viable databse connection.");
            if (conn.State != System.Data.ConnectionState.Open) {
                throw new Exception("Database connection is borkened. I need a working/opened one!");
            }

            string userTableSchema = @"

-- Make the User table
create table if not exists User (
    Id integer primary key,
    AadId text null,
    Deleted boolean false,
    DisplayName text null,
    EmployeeId text null,
    GivenName text null,
    Surname text null,
    Pronouns text null,
    Suffix text null
);

delete from User;

-- Let's load up some test records
--insert into User (AadId, Deleted, DisplayName, EmployeeId, GivenName, Surname, Pronouns)
--values ();

insert into User (AadId,                        Deleted, DisplayName,    EmployeeId, GivenName, Surname,   Pronouns)
values ('5cb4dc92-1928-4b98-b3a1-7f725442128f', false,   'Sean McArdle', 'E100',     'Sean',    'McArdle', 'He/Him/His');

insert into User (AadId,                        Deleted, DisplayName,         EmployeeId, GivenName, Surname,      Pronouns)
values ('61303c53-5e19-4bac-82d2-62e5f9e81d80', false,   'Zaphod Beablebrox', 'E42',      'Zaphod',  'Beablebrox', 'They/They/Them');

";
            var createCmd = new SqliteCommand(userTableSchema, conn);
            var result = createCmd.ExecuteScalar();
            System.Diagnostics.Debug.WriteLine(result);
        }
    }
}
