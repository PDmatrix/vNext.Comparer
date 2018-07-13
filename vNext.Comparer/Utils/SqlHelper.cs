using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace vNext.Comparer.Utils
{
    public static class SqlHelper
    {
        private static async Task<int> GetProcedureId(string connectionString, string procedureName)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command =
                    new SqlCommand(
                        $"select object_id from sys.procedures where name = '{procedureName}' and type = 'P'",
                        connection);
                return (int) await command.ExecuteScalarAsync();
            }
        }

        public static async Task<string> GetObjectDefinition(string connectionString, string objName)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand($"select OBJECT_DEFINITION(OBJECT_ID('{objName}'))", connection);
                try
                {
                    return (string) await command.ExecuteScalarAsync();
                }
                catch (Exception ex)
                {
                    command = new SqlCommand(
                        $"select OBJECT_DEFINITION({await GetProcedureId(connectionString, objName)})", connection);
                    return (string) await command.ExecuteScalarAsync();
                }
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

        public static async Task AlterProcedure(string connectionString, string script)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var server = new Server(new ServerConnection(connection));
                server.ConnectionContext.ExecuteNonQuery(script);
            }
        }

        public static async Task<IEnumerable<string>> GetProcedures(string connectionString, string query)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var list = new List<string>();
                var command = new SqlCommand(query, connection);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync()) list.Add(reader.GetString(0));
                }

                return list;
            }
        }
    }
}