﻿using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Kavalan.Data.Sqlite
{
    public static class MetadataCache
    {
        private static readonly ConcurrentDictionary<Type, TableMetaData> Cache = new();
        public static TableMetaData GetTableMetadata<T>()
        {
            var type = typeof(T);
            if (Cache.TryGetValue(type, out var metadata))
                return metadata;

            string tableName = GetTableName(type);
            List<PropertyInfo> columns = type.GetProperties().Where(prop => prop.GetCustomAttribute<NotMappedAttribute>() == null).ToList();
            List<PropertyInfo> primaryKeyColumns = GetTablePrimaryKey(type, columns);
            string primaryKeyColumnsSql = string.Join(" AND ", primaryKeyColumns.Select(p => $"[{p.Name}] = @{p.Name}"));
            Dictionary<PropertyInfo, bool> autoGeneratedColumns = GetTableAutoGeneratedColumns(type, columns);

            int autoGeneratedPrimaryKeysCount = primaryKeyColumns.Count(pk => autoGeneratedColumns.Any(ac => ac.Key.Name == pk.Name));
            if (autoGeneratedPrimaryKeysCount > 1)
                throw new Exception("Auto-generated compound primary keys are not supported in Sqlite DB");
            bool isPrimaryKeyAutoGenerated = autoGeneratedPrimaryKeysCount == 1;

            string selectQuery = GetTableSelectQuery(tableName, columns);
            string insertQuery = GetTableInsertQuery(tableName, columns, isPrimaryKeyAutoGenerated, autoGeneratedColumns);
            string deleteQuery = GetTableDeleteQuery(tableName, primaryKeyColumnsSql);
            string updateQuery = GetTableUpdateQuery(tableName, primaryKeyColumnsSql, primaryKeyColumns, columns);

            Cache[type] = new(tableName, primaryKeyColumns, isPrimaryKeyAutoGenerated, columns, autoGeneratedColumns, 
                              insertQuery, updateQuery, selectQuery, deleteQuery);
            return Cache[type];
        }
        private static string GetTableName(Type type)
        {
            var tableAttribute = type.GetCustomAttribute<TableAttribute>();
            return tableAttribute != null ? tableAttribute.Name : throw new Exception($"Class {type.Name} must have a declared [Table] attribute");
        }
        private static List<PropertyInfo> GetTablePrimaryKey(Type type, List<PropertyInfo> columns)
        {
            List<PropertyInfo> keyColumns = columns.Where(p => p.GetCustomAttribute<KeyAttribute>() != null).ToList();
            if (keyColumns.Count == 0)
                throw new Exception($"Class {type.Name} must have a declared [Key] attribute");
            else
                return keyColumns;
        }
        private static Dictionary<PropertyInfo, bool> GetTableAutoGeneratedColumns(Type type, List<PropertyInfo> columns)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(columns);

            return columns.Where(prop => prop.GetCustomAttribute<DatabaseGeneratedAttribute>() != null)
                                           .ToDictionary(key => key, value => true);
        }
        private static string GetTableInsertQuery(string tableName, List<PropertyInfo> columns, bool isPrimaryKeyAutoGenerated, Dictionary<PropertyInfo, bool> autoGeneratedColumns)
        {
            //Remove any DB generated fields
            var columnsAndParameters = columns.Where(p => !autoGeneratedColumns.Any(ag => ag.Key.Name == p.Name))
                                              .Select(p => new { p.Name, Parameter = "@" + p.Name })
                                              .ToList();

            var insertColumns = string.Join(", ", columnsAndParameters.Select(cp => cp.Name));
            var parameters = string.Join(", ", columnsAndParameters.Select(cp => cp.Parameter));

            string insertyQuery = $"INSERT INTO {tableName} " +
                                   $"({insertColumns}) VALUES " +
                                   $"({parameters});";

            if (isPrimaryKeyAutoGenerated)
                insertyQuery += "SELECT last_insert_rowid();";

            return insertyQuery;
        }
        private static string GetTableUpdateQuery(string tableName, string primaryKeyColumnsSql, List<PropertyInfo> primaryKeyColumns, List<PropertyInfo> columns)
        {
            //Set values without primary key
            var setValues = string.Join(", ", columns.Where(p => !primaryKeyColumns.Any(pk => pk.Name == p.Name))
                                                     .Select(p => $"[{p.Name}] = @{p.Name}"));

            return $"UPDATE [{tableName}] " +
                   $"SET {setValues} " +
                   $"WHERE {primaryKeyColumnsSql}";
        }
        private static string GetTableSelectQuery(string tableName, List<PropertyInfo> columns)
        {
            var columnsSql = string.Join(", ", columns.Select(p => p.Name));

            string selectQuery = $"SELECT {columnsSql} " +
                                 $"FROM {tableName}";

            return selectQuery;
        }
        private static string GetTableDeleteQuery(string tableName, string primaryKeyColumns)
        {
            string deleteQuery = $"DELETE FROM {tableName} " +
                                 $"WHERE {primaryKeyColumns}";

            return deleteQuery;
        }

    }
}