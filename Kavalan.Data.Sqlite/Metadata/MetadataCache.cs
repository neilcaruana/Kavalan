﻿using System.Collections.Concurrent;
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
            List<PropertyInfo> props = [.. type.GetProperties()];
            PropertyInfo pkProp = GetTablePrimaryKey(type, props);
            Dictionary<PropertyInfo, bool> autoGeneratedColumns = GetTableAutoGeneratedColumns(type, props);
            bool isPrimaryKeyAutoGenerated = autoGeneratedColumns.FirstOrDefault(column => column.Key.Name == pkProp.Name).Value;
            string selectQuery = GetTableSelectQuery(tableName, props);
            string deleteQuery = GetTableDeleteQuery(tableName, pkProp.Name);
            string insertQuery = GetTableInsertQuery(tableName, pkProp.Name, autoGeneratedColumns, isPrimaryKeyAutoGenerated, props);
            string updateQuery = GetTableUpdateQuery(tableName, pkProp.Name, props);

            Cache[type] = new(tableName, pkProp.Name, pkProp, isPrimaryKeyAutoGenerated, insertQuery, updateQuery,
                              selectQuery, deleteQuery, props, autoGeneratedColumns);
            return Cache[type];
        }
        private static string GetTableName(Type type)
        {
            var tableAttribute = type.GetCustomAttribute<TableAttribute>();
            return tableAttribute != null ? tableAttribute.Name : throw new Exception($"Class {type.Name} must have a declared [Table] attribute");
        }
        private static PropertyInfo GetTablePrimaryKey(Type type, List<PropertyInfo> props)
        {
            var keyProperty = props.FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);
            if (keyProperty == null)
                throw new Exception($"Class {type.Name} must have a declared [Key] attribute");
            else
                return keyProperty;
        }
        private static Dictionary<PropertyInfo, bool> GetTableAutoGeneratedColumns(Type type, List<PropertyInfo> props)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(props);

            return props.Where(prop => prop.GetCustomAttribute<DatabaseGeneratedAttribute>() != null)
                                           .ToDictionary(key => key, value => true);
        }
        private static string GetTableInsertQuery(string tableName, string pk, Dictionary<PropertyInfo, bool> autoGeneratedColumns, bool isPrimaryKeyAutoGenerated, List<PropertyInfo> props)
        {
            //Remove any DB generated fields
            var columnsAndParameters = props.Where(p => !autoGeneratedColumns.Any(ag => ag.Key.Name == p.Name))
                                            .Select(p => new { p.Name, Parameter = "@" + p.Name })
                                            .ToList();

            var columns = string.Join(", ", columnsAndParameters.Select(cp => cp.Name));
            var parameters = string.Join(", ", columnsAndParameters.Select(cp => cp.Parameter));

            string insertyQuery = $"INSERT INTO {tableName} " +
                                   $"({columns}) VALUES " +
                                   $"({parameters});";

            if (isPrimaryKeyAutoGenerated)
                insertyQuery += "SELECT last_insert_rowid();";

            return insertyQuery;
        }
        private static string GetTableUpdateQuery(string tableName, string pk, List<PropertyInfo> props)
        {
            //Set values without primary key
            var setValues = string.Join(", ", props.Where(p => p.Name != pk)
                                                   .Select(p => $"[{p.Name}] = @{p.Name}"));

            return $"UPDATE [{tableName}] " +
                   $"SET {setValues} " +
                   $"WHERE {pk} = @{pk}";
        }
        private static string GetTableSelectQuery(string tableName, List<PropertyInfo> props)
        {
            var columns = string.Join(", ", props.Select(p => p.Name));

            string selectQuery = $"SELECT {columns} " +
                                 $"FROM {tableName}";

            return selectQuery;
        }
        private static string GetTableDeleteQuery(string tableName, string pk)
        {
            string deleteQuery = $"DELETE FROM {tableName} " +
                                 $"WHERE {pk} = @{pk}";

            return deleteQuery;
        }

    }
}