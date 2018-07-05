using System;
using System.Data.SqlClient;
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
                var command = new SqlCommand($"select OBJECT_DEFINITION(object_id('{objName}'))", connection);
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
    }
}