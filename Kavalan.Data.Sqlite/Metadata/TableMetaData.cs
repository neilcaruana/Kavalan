﻿using System.Reflection;
namespace Kavalan.Data.Sqlite.Metadata
{
    public class TableMetaData(string tableName, List<PropertyInfo> primaryKeyColumns, bool isPrimaryKeyAutoGenerated, List<PropertyInfo> columns, Dictionary<PropertyInfo, bool> databaseAutoGeneratedColumns,
                               string insertQuery, string updateQuery, string selectQuery, string deleteQuery)
    {
        public string TableName { get; set; } = tableName;
        public List<PropertyInfo> Columns { get; set; } = columns;
        public List<PropertyInfo> PrimaryKeyColumns { get; set; } = primaryKeyColumns;
        public bool IsPrimaryKeyAutoGenerated { get; set; } = isPrimaryKeyAutoGenerated;
        public Dictionary<PropertyInfo, bool> DatabaseAutoGeneratedColumns { get; set; } = databaseAutoGeneratedColumns;
        public string SelectQuery { get; set; } = selectQuery;
        public string InsertQuery { get; set; } = insertQuery;
        public string UpdateQuery { get; set; } = updateQuery;
        public string DeleteQuery { get; set; } = deleteQuery;
    }
}