using McAttributes.Data;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Npgsql;
using SMM;
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
