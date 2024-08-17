﻿using Kavalan.Data.Sqlite;
using Microsoft.Data.Sqlite;
using System.Reflection;

namespace Kavalan.Data.Sqlite.Repositories
{
    public class GenericSqliteRepository<T> : SqlLiteDataLayer, IGenericRepository<T> where T : new()
    {
        public GenericSqliteRepository(string databasePath) : base(databasePath)
        {
        }

        public async Task<T?> SelectByPrimaryKeyAsync(object[] primaryKeyValues)
        {
            if (primaryKeyValues == null || primaryKeyValues.Length == 0)
                throw new Exception("Primary key value(s) must be provided");

            TableMetaData meta = MetadataCache.GetTableMetadata<T>();
            List<T> data = await getListDataByFieldAsync(meta.PrimaryKeyColumns.Select(column => column.Name).ToList(), primaryKeyValues);
            return data.FirstOrDefault();
        }
        public async Task<T?> SelectByFieldValueAsync(string fieldName, object fieldValue)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new Exception("Field name must be specified");

            if (fieldValue == null)
                throw new Exception("Field value must be specified");

            List<T> data = await getListDataByFieldAsync([fieldName], [fieldValue]);
            return data.FirstOrDefault();
        }
        public async Task<List<T>> SelectDataByFieldValueAsync(string fieldName = "", object? fieldValue = null)
        {
            return await getListDataByFieldAsync([fieldName], [fieldValue]);
        }
        public async Task<T> InsertAsync(T entity)
        {
            using SqliteConnection connection = await base.GetOpenConnection();
            using SqliteTransaction transaction = connection.BeginTransaction();

            TableMetaData meta = MetadataCache.GetTableMetadata<T>();
            using SqliteCommand command = new(meta.InsertQuery, connection, transaction);

            base.InjectCommandWithParameters(command, meta.InsertQuery, meta.Columns, entity);
            object? dbEntity = await command.ExecuteScalarAsync();

            //Update of database Identity primary key to entity
            if (meta.IsPrimaryKeyAutoGenerated)
            {
                //TODO: Maybe improve this as here im assuming that when the primary key is auto generated it is not compound (Since its not supported by Sqlite DB)
                if (meta.PrimaryKeyColumns.First().CanWrite)
                    meta.PrimaryKeyColumns.First().SetValue(entity, Convert.ChangeType(dbEntity, meta.PrimaryKeyColumns[0].PropertyType));
                else
                    throw new Exception($"Entity [{meta.TableName}] primary key property [{meta.PrimaryKeyColumns.First().Name}] is read only and cannot be updated");
            }

            //Update entity with other database generated columns that are not a primary key
            var nonPkAutoGeneratedColumns = meta.DatabaseAutoGeneratedColumns.Where(c => !meta.PrimaryKeyColumns.Any(pk => pk.Name == c.Key.Name));
            if (nonPkAutoGeneratedColumns.Any())
            {
                //Read primary key value from DB if auto generated identity or from entity if not
                object?[] pkValues = new object[meta.PrimaryKeyColumns.Count];
                pkValues[0] = dbEntity != null ? meta.PrimaryKeyColumns.First().GetValue(dbEntity) : meta.PrimaryKeyColumns.First().GetValue(entity);
                if (meta.PrimaryKeyColumns.Count > 1) //Compound primary keys
                {
                    for (int i = 1; i > meta.PrimaryKeyColumns.Count; i++) //Skip first column of primary keys (already populated)
                        pkValues[i] = meta.PrimaryKeyColumns[i].GetValue(entity);
                }

                T? updateEntity = (await getListDataByFieldAsync(meta.PrimaryKeyColumns.Select(column => column.Name).ToList(), pkValues, connection, transaction)).FirstOrDefault();
                //Loop only db generated columns and update entity from DB
                foreach (PropertyInfo autoGenProperty in nonPkAutoGeneratedColumns.Select(c => c.Key))
                {
                    if (autoGenProperty != null && autoGenProperty.CanWrite)
                        autoGenProperty.SetValue(entity, autoGenProperty.GetValue(updateEntity));
                    else
                        throw new Exception($"Entity [{meta.TableName}] property [{autoGenProperty?.Name}] is read only/not found and cannot be updated");
                }
            }

            await transaction.CommitAsync();
            return entity;
        }
        public async Task<T> UpsertAsync(T entity)
        {
            int rowsAffected = await this.UpdateAsync(entity);
            if (rowsAffected == 0)
                return await this.InsertAsync(entity);

            TableMetaData meta = MetadataCache.GetTableMetadata<T>();
            object[] pkValues = new object[meta.PrimaryKeyColumns.Count];
            for (int i = 1; i > meta.PrimaryKeyColumns.Count; i++) //Skip first column of primary keys (already populated)
                pkValues[i] = meta.PrimaryKeyColumns[i].GetValue(entity);

            //Record already existed in DB return updated record
            return await this.SelectByPrimaryKeyAsync(pkValues) ?? throw new Exception("Record not found after upsert!");
        }
        public async Task<int> UpdateAsync(T entity)    
        {
            TableMetaData meta = MetadataCache.GetTableMetadata<T>();
            using SqliteCommand command = new(meta.UpdateQuery, await base.GetOpenConnection());
            base.InjectCommandWithParameters(command, meta.UpdateQuery, meta.Columns, entity);

            return await command.ExecuteNonQueryAsync();
        }
        public async Task<int> DeleteAsync(T entity)
        {
            TableMetaData meta = MetadataCache.GetTableMetadata<T>();
            using SqliteCommand command = new(meta.DeleteQuery, await base.GetOpenConnection());
            base.InjectCommandWithParameters(command, meta.DeleteQuery, meta.Columns, entity);

            return await command.ExecuteNonQueryAsync();
        }
        public async Task<long> CountAsync()
        {
            TableMetaData meta = MetadataCache.GetTableMetadata<T>();
            using SqliteCommand command = new($"SELECT Count(*) From {meta.TableName}", await base.GetOpenConnection());

            return Convert.ToInt64(await command.ExecuteScalarAsync());
        }
        public async Task<bool> AnyAsync()
        {
            TableMetaData meta = MetadataCache.GetTableMetadata<T>();
            using SqliteCommand command = new($"SELECT {meta.PrimaryKeyColumns.First().Name} From {meta.TableName} LIMIT 1", await base.GetOpenConnection());

            return await command.ExecuteScalarAsync() != null;
        }

        private async Task<List<T>> getListDataByFieldAsync(List<string> fields, object[] fieldValues, SqliteConnection? externalConnection = null, SqliteTransaction? externalTransaction = null)
        {
            if (fields.Count != 0 && fieldValues.Length == 0)
                throw new Exception("Both fields and values must be specified");

            if (fields.Count != fieldValues.Length)
                throw new Exception("Same number of fields and values must be provided");

            TableMetaData meta = MetadataCache.GetTableMetadata<T>();
            string whereFields = string.Join(" AND ", fields.Select(field => $"[{field}] = @{field}"));

            SqliteConnection connection = externalConnection ?? await GetOpenConnection();
            
            string selectQuery = meta.SelectQuery + (fields.Count > 0 ? $" WHERE {whereFields}" : "");
            using SqliteCommand command = new(selectQuery, connection, externalTransaction);

            for (int i = 0; i < fields.Count; i++)
                command.Parameters.AddWithValue($"@{fields[i]}", fieldValues[i]);

            using SqliteDataReader reader = await command.ExecuteReaderAsync();
            if (!reader.Read())
                return [];

            List<T> data = [];
            do
            {//First record already read map data and read next
                data.Add(base.MapSqliteReaderToEntity<T>(reader, meta.Columns));

            } while (reader.Read());

            if (externalConnection == null)
                connection.Dispose(); //Only dispose if connection is not external

            return data;
        }
    }
}
