using McAttributes.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Npgsql;
using System.Linq;
using System.Reflection;

namespace McAttributes {
    public static class DebugInit {
        public static void DbInit(IdDbContext context) {
            if (context == null) throw new ArgumentNullException("Gonna hand me a viable databse connection.");

            UglyDbInitHelper.DbInit<Models.User>(context, @"./test_values.csv");
        }

        public static void DbInit<T>(IdDbContext context) {
            if (context == null) throw new ArgumentNullException("Gonna hand me a viable databse connection.");

            UglyDbInitHelper.DbInit<T>(context, @"./test_values.csv");
        }

    }
}
