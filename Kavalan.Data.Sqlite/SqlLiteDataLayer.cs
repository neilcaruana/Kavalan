using Microsoft.Data.Sqlite;
using System.Reflection;

namespace Kavalan.Data.Sqlite
{
    public class SqlLiteDataLayer
    {
        private string connectionString = "Data Source=[[databasePath]];";
        public SqlLiteDataLayer(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
                throw new Exception("Database path cannot be blank");

            connectionString = connectionString.Replace("[[databasePath]]", databasePath);
        }
        public async Task ExecuteScript(string script)
        {
            if (string.IsNullOrWhiteSpace(script))
                throw new Exception("Script cannot be blank");

            using SqliteConnection connection = new(connectionString);
            await connection.OpenAsync();

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = script;
            int result = command.ExecuteNonQuery();
        }
        public async Task<SqliteConnection> GetOpenConnection()
        {
            var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            return connection;
        }
        protected SqliteCommand InjectCommandWithParameters(SqliteCommand command, string query, List<PropertyInfo> properties, object? entity)
        {
            if (entity == null)
                throw new Exception("Entity cannot be null when updating DB");

            foreach (var property in properties)
            {
                command.Parameters.AddWithValue("@" + property.Name, property.GetValue(entity) ?? DBNull.Value);
            }
            return command;
        }
        protected T MapSqliteReaderToEntity<T>(SqliteDataReader dataReader, List<PropertyInfo> properties) where T : new()
        {
            var entity = new T();
            foreach (var property in properties)
            {
                object databaseValue = dataReader[property.Name];
                if (databaseValue != DBNull.Value) //Only set value if not null in DB
                {
                    //Handle special types that do not auto convert
                    if (property.PropertyType == typeof(DateTime))
                        property.SetValue(entity, Convert.ToDateTime(databaseValue));
                    else if (property.PropertyType == typeof(Boolean))
                        property.SetValue(entity, Convert.ToBoolean(databaseValue));
                    else
                        property.SetValue(entity, databaseValue); //Auto converted types
                }
            }
            return entity;
        }
    }
}
