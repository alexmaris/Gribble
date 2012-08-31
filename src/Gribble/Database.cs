﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Gribble.Model;
using Gribble.TransactSql;

namespace Gribble
{
    public class Database : IDatabase
    {
        private readonly IConnectionManager _connectionManager;
        private readonly IProfiler _profiler;

        internal Database(IConnectionManager connectionManager, IProfiler profiler)
        {
            _connectionManager = connectionManager;
            _profiler = profiler;
        }

        public static IDatabase Create(SqlConnection connection, TimeSpan? commandTimeout = null, IProfiler profiler = null)
        {
            return Create(new ConnectionManager(connection, commandTimeout ?? new TimeSpan(0, 5, 0)), profiler);
        }

        public static IDatabase Create(IConnectionManager connectionManager, IProfiler profiler = null)
        {
            return new Database(connectionManager, profiler ?? new ConsoleProfiler());
        }

        public bool TableExists(string tableName)
        { return Command.Create(SchemaWriter.CreateTableExistsStatement(tableName), _profiler).ExecuteScalar<bool>(_connectionManager); }

        public void CreateTable(string tableName, params Column[] columns)
        { Command.Create(SchemaWriter.CreateTableCreateStatement(tableName, columns), _profiler).ExecuteNonQuery(_connectionManager); }

        public void CreateTable(string tableName, string modelTable)
        {
            var columns = GetColumns(modelTable).ToList();
            var indexes = GetIndexes(modelTable).Where(x => !x.Clustered).ToList();
            CreateTable(tableName, columns.ToArray());
            AddNonClusteredIndexes(tableName, indexes.Select(x => new Index.ColumnSet(x.Columns)).ToArray());
        }

        public void DeleteTable(string tableName)
        { Command.Create(SchemaWriter.CreateDeleteTableStatement(tableName), _profiler).ExecuteNonQuery(_connectionManager); }

        
        public IEnumerable<Column> GetColumns(string tableName)
        {
            var statement = SchemaWriter.CreateTableColumnsStatement(tableName);
            var columns = new List<Column>();
            using (var reader = Command.Create(statement, _profiler).ExecuteReader(_connectionManager))
                while (reader.Read()) columns.Add(ColumnFactory(reader)); 
            return columns;
        }

        internal static Column ColumnFactory(IDataReader reader)
        {
            return new Column(
                (string)reader[TransactSql.System.Columns.Name],
                type: ((byte)reader[TransactSql.System.Columns.SystemTypeId]).GetClrType((bool)reader[TransactSql.System.Columns.IsNullable]),
                sqlType: ((byte)reader[TransactSql.System.Columns.SystemTypeId]).GetSqlType(),
                length: (short)reader[TransactSql.System.Columns.MaxLength],
                isNullable: (bool)reader[TransactSql.System.Columns.IsNullable],
                isIdentity: (bool)reader[TransactSql.System.Columns.IsIdentity],
                isAutoGenerated: (bool)reader[SqlWriter.Aliases.IsAutoGenerated],
                key: !(bool)reader[TransactSql.System.Indexes.IsPrimaryKey] ? 
                    Column.KeyType.None : 
                    ((bool)reader[SqlWriter.Aliases.IsPrimaryKeyClustered] ? 
                        Column.KeyType.ClusteredPrimaryKey : 
                        Column.KeyType.PrimaryKey),
                defaultValue: reader[SqlWriter.Aliases.DefaultValue].FromDb<object>(),
                precision: (byte)reader[TransactSql.System.Columns.Precision],
                scale: (byte)reader[TransactSql.System.Columns.Scale],
                computationPersisted: reader[SqlWriter.Aliases.PersistedComputation].FromDb<bool?>(),
                computation: reader[SqlWriter.Aliases.Computation].FromDb<string>());
        }

        public void AddColumn(string tableName, Column column)
        { Command.Create(SchemaWriter.CreateAddColumnStatement(tableName, column), _profiler).ExecuteNonQuery(_connectionManager); }

        public void AddColumns(string tableName, params Column[] columns)
        {
            var existingColumns = GetColumns(tableName);
            columns.Where(x => !existingColumns.Any(y => y.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase))).
                ToList().ForEach(x => AddColumn(tableName, x));
        }

        public void RemoveColumn(string tableName, string columnName)
        { Command.Create(SchemaWriter.CreateRemoveColumnStatement(tableName, columnName), _profiler).ExecuteNonQuery(_connectionManager); }

        public IEnumerable<Index> GetIndexes(string tableName)
        {
            var indexes = new List<Index>();
            using (var reader = Command.Create(SchemaWriter.CreateGetIndexesStatement(tableName), _profiler).ExecuteReader(_connectionManager))
            {
                Index index = null;
                Index.ColumnSet columns = null;
                while (reader.Read())
                {
                    var name = (string)reader[TransactSql.System.Indexes.Name];
                    if (index == null || index.Name != name)
                    {
                        columns = new Index.ColumnSet();
                        index = new Index(name,
                            (byte)reader[TransactSql.System.Indexes.Type] == 1,
                            (bool)reader[TransactSql.System.Indexes.IsUnique],
                            (bool)reader[TransactSql.System.Indexes.IsPrimaryKey], columns);
                        indexes.Add(index);
                    }
                    columns.Add(
                        (string)reader[SqlWriter.Aliases.ColumnName],
                        (bool)reader[TransactSql.System.IndexColumns.IsDescendingKey]);
                }
            }
            return indexes;
        }

        public void AddNonClusteredIndex(string tableName, params Index.Column[] columns)
        { 
            Command.Create(SchemaWriter.CreateAddNonClusteredIndexStatement(tableName, columns), _profiler).ExecuteNonQuery(_connectionManager);
        }

        public void AddNonClusteredIndexes(string tableName, params Index.ColumnSet[] indexColumns)
        {
            var existingIndexes = GetIndexes(tableName).Select(x => x.Columns.Select(y => y.Name).OrderBy(y => y));
            indexColumns.Where(x => !existingIndexes.Any(y => y.SequenceEqual(x.Select(z => z.Name).OrderBy(z => z), StringComparer.OrdinalIgnoreCase))).
                ToList().ForEach(x => AddNonClusteredIndex(tableName, x.ToArray()));
        }

        public void RemoveNonClusteredIndex(string tableName, string indexName)
        { Command.Create(SchemaWriter.CreateRemoveNonClusteredIndexStatement(tableName, indexName), _profiler).ExecuteNonQuery(_connectionManager); }

    }
}
