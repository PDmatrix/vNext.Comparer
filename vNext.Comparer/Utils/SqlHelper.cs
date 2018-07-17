using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace vNext.Comparer.Utils
{
    public static class SqlHelper
    {
        public static async Task<string> GetObjectDefinition(string connectionString, string objName)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand($"select OBJECT_DEFINITION(OBJECT_ID('{objName}'))", connection);
                return (string) await command.ExecuteScalarAsync();
            }
        }

        public static async Task<bool> IsObjectExists(string connectionString, string objName)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand($"select OBJECT_ID('{objName}')", connection);
                var res = await command.ExecuteScalarAsync();
                return res != null && res != DBNull.Value;
            }
        }

        public static async Task ExecuteNonQueryScriptAsync(string connectionString, string script)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    foreach (var splitted in SplitSqlStatements(script))
                    {
                        command.CommandText = splitted;
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        private static IEnumerable<string> SplitSqlStatements(string sqlScript)
        {
            // Split by "GO" statements
            var statements = Regex.Split(
                sqlScript,
                @"^[\t\r\n]*GO[\t\r\n]*\d*[\t\r\n]*(?:--.*)?$",
                RegexOptions.Multiline |
                RegexOptions.IgnorePatternWhitespace |
                RegexOptions.IgnoreCase);

            // Remove empties, trim, and return
            return statements
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim(' ', '\r', '\n'));
        }

        public static async Task<IEnumerable<string>> GetDbObjects(string connectionString, string query)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var list = new List<string>();
                var command = new SqlCommand(query, connection);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(reader.GetString(0));
                    }
                }

                return list;
            }
        }
    }
}