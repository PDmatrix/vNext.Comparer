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
        /// <summary>
        /// Returns the definition of the specified object.
        /// </summary>
        /// <param name="connectionString">Connection string to database</param>
        /// <param name="objectName">Object to get definition</param>
        /// <returns></returns>
        public static async Task<string> GetObjectDefinitionAsync(string connectionString, string objectName)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                var command = new SqlCommand($"SELECT OBJECT_DEFINITION(OBJECT_ID('{objectName}'))", connection);
                return (string) await command.ExecuteScalarAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Determines whether the specified object exists.
        /// </summary>
        /// <param name="connectionString">Connection string to database</param>
        /// <param name="objectName">Object to check</param>
        /// <returns></returns>
        public static async Task<bool> IsObjectExistsAsync(string connectionString, string objectName)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                var command = new SqlCommand($"SELECT OBJECT_ID('{objectName}')", connection);
                var res = await command.ExecuteScalarAsync().ConfigureAwait(false);
                return res != null && res != DBNull.Value;
            }
        }
        /// <summary>
        /// Opens a connection and executes script.
        /// </summary>
        /// <param name="connectionString">Connection string to database</param>
        /// <param name="script">Script to execute</param>
        /// <returns></returns>
        public static async Task ExecuteNonQueryScriptAsync(string connectionString, string script)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    foreach (var splitted in SplitSqlStatements(script))
                    {
                        command.CommandText = splitted;
                        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                    }
                }
            }
        }
        /// <summary>
        /// Returns the array of the script, splitted by GO commands.
        /// </summary>
        /// <param name="sqlScript">SQL script to split</param>
        /// <returns></returns>
        private static IEnumerable<string> SplitSqlStatements(string sqlScript)
        {
            // Split by "GO" statements
            var statements = Regex.Split(
                sqlScript,
                @"^[\s\t]*GO[\s\t]*\d*[\s\t]*(\-\-[^\r]*?)*$",
                RegexOptions.Multiline |
                RegexOptions.IgnorePatternWhitespace |
                RegexOptions.IgnoreCase);

            // Remove empties, trim, and return
            return statements
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim(' ', '\r', '\n'));
        }
        /// <summary>
        /// Returns the array of database objects
        /// </summary>
        /// <param name="connectionString">Connection string to database</param>
        /// <param name="query">Query to get the database objects</param>
        /// <returns></returns>
        public static async Task<IEnumerable<string>> GetDbObjectsAsync(string connectionString, string query)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                var list = new List<string>();
                var command = new SqlCommand(query, connection);
                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        list.Add(reader.GetString(0));
                    }
                }

                return list;
            }
        }
    }
}