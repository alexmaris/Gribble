﻿using System;
using System.Collections.Generic;
using System.Linq;
using Gribble.Model;

namespace Gribble.TransactSql
{
    public static class SchemaWriter
    {
        public static Statement CreateTableCreateStatement(string tableName, params Column[] columns)
        {
            var writer = SqlWriter.CreateWriter().Create.Table.QuotedName(tableName).OpenBlock.Trim();
            var first = true;
            foreach (var column in columns)
            {
                if (!first) writer.Trim().Comma.Flush();
                writer.ColumnDefinition(column.Name, column.Type, column.Length, column.IsPrimaryKey, column.IsIdentity, column.IsNullable, column.IsAutoGenerated, column.DefaultValue);
                first = false;
            }
            var primaryKey = columns.FirstOrDefault(x => x.IsPrimaryKey || x.IsClusteredPrimaryKey);
            if (primaryKey != null) writer.Trim().Comma.PrimaryKeyConstraint(tableName, primaryKey.Name, primaryKey.IsClusteredPrimaryKey);
            writer.Trim().CloseBlock.Flush();
            return new Statement(writer.ToString(), Statement.StatementType.Text, Statement.ResultType.None);
        }

        public static Statement CreateUnionColumnsStatement(Select select)
        {
            return CreateColumnsStatement(GetUnionTables(select).Select(x => x.Source.Table.Name));
        }

        public static Statement CreateSelectIntoColumnsStatement(Select select)
        {
            var sql = new SqlWriter();
            var sourceTables = GetUnionTables(select).Select(x => x.Source.Table.Name);
            var statement = CreateColumnsStatement(sourceTables.Concat(new[] { select.Target.Table.Name }));

            sql.Select.SubQueryAlias_Name.Trim().Comma.
                    Cast(z => z.Case.When.SystemColumns_Type.LessThan.OpenBlock.Trim().
                        Select.Max(x => x.SystemTypeColumn.Flush()).From.SystemColumns.Where.
                        SystemNameColumn.Equal.SubQueryAlias_Name.And.
                        SystemTypeColumn.In.OpenBlock.Trim().ExpressionList(x => x.Comma.Flush(), x => x.DataTypeId(DataTypes.SqlTypeId.Char),
                                                                                            x => x.DataTypeId(DataTypes.SqlTypeId.VarChar),
                                                                                            x => x.DataTypeId(DataTypes.SqlTypeId.Text),
                                                                                            x => x.DataTypeId(DataTypes.SqlTypeId.NChar),
                                                                                            x => x.DataTypeId(DataTypes.SqlTypeId.NVarChar),
                                                                                            x => x.DataTypeId(DataTypes.SqlTypeId.NText)).Trim().CloseBlock.And.
                        SystemObjectIdColumn.In.OpenBlock.Trim().ExpressionList(x => x.Comma.Flush(), sourceTables.Select<string, Action<SqlWriter>>(x => y => y.ObjectId(x))).Trim().CloseBlock.Trim().CloseBlock.
                        Then.True.Else.False.End.Flush(), typeof(bool)).As.QuotedName("NarrowingConversion").
                    From.OpenBlock.Trim().Write(statement.Text).Trim().CloseBlock.SubQueryAlias.
                    Join.SystemColumns.On.
                    SubQueryAlias_Name.Equal.SystemColumns_Name.And.
                    SystemObjectIdColumn.Equal.ObjectId(select.Target.Table.Name);
            statement.Text = sql.ToString();
            return statement;
        }

        public static Statement CreateCreateTableColumnsStatement(Select select)
        {
            var sourceTables = GetUnionTables(select).Select(x => x.Source.Table.Name);
            var statement = CreateColumnsStatement(sourceTables);

            var writer = SqlWriter.CreateWriter().
                Select.SystemColumnsAlias_Name.Trim().Comma.
                       SystemColumnsAlias_Type.Trim().Comma.
                       SystemMaxLengthColumn.Trim().Comma.
                       SystemIsNullableColumn.Trim().Comma.
                       SystemIsIdentityColumn.Trim().Comma.
                       Cast(x => x.Trim().OpenBlock.Trim().
                            Case.ObjectDefinition(y => y.SystemDefaultObjectIdColumn.Flush()).
                                When.GetDateColumnDefault.Then.True.
                                When.NewIdColumnDefault.Then.True.
                                When.NewSequentialIdColumnDefault.Then.True.
                                Else.False.End.Trim().
                            CloseBlock.Flush(), typeof(bool)).As.SystemIsAutoGeneratedAlias.Trim().Comma.
                       Case.ObjectDefinition(y => y.SystemDefaultObjectIdColumn.Flush()).
                            When.GetDateColumnDefault.Then.Null.
                            When.NewIdColumnDefault.Then.Null.
                            When.NewSequentialIdColumnDefault.Then.Null.
                            Else.Replace(x => x.Replace(y => y.ObjectDefinition(z => z.SystemDefaultObjectIdColumn.Flush()),
                                                        y => y.QuotedString("("),
                                                        y => y.QuotedString(string.Empty)),
                                         x => x.QuotedString(")"),
                                         x => x.QuotedString(string.Empty)).End.
                            As.SystemDefaultValueAlias.Trim().Comma.
                       IsNull(x => x.Trim().OpenBlock.Trim().
                           Select.Top(1).SystemIsPrimaryKeyColumn.
                           From.OpenBlock.Trim().SystemColumns.
                                Join.SystemIndexColumns.On.
                                    SystemColumns_ObjectId.Equal.SystemIndexColumns_ObjectId.And.
                                    SystemColumns_ColumnId.Equal.SystemIndexColumns_ColumnId.Trim().CloseBlock.
                                Join.SystemIndexes.On.
                                    SystemIndexes_IndexId.Equal.SystemIndexColumns_IndexId.And.
                                    SystemIndexes_ObjectId.Equal.SystemIndexColumns_ObjectId.
                           Where.SystemColumns_ObjectId.Equal.SystemColumnsAlias_ObjectId.And.
                                 SystemColumns_ColumnId.Equal.SystemColumnsAlias_ColumnId.And.
                                 SystemIsPrimaryKeyColumn.Equal.True.Trim().
                           CloseBlock.Flush(), x => x.False.Flush()).As.SystemIsPrimaryKeyColumn.
                    From.OpenBlock.Trim().Write(statement.Text).Trim().CloseBlock.SubQueryAlias.
                    Join.SystemColumns.SystemColumnsAlias.On.
                    SubQueryAlias_Name.Equal.SystemColumnsAlias_Name.And.
                    SystemObjectIdColumn.Equal.ObjectId(sourceTables.First());
            return new Statement(writer.ToString(), Statement.StatementType.Text, Statement.ResultType.Multiple);
        }

        public static Statement CreateColumnsStatement(IEnumerable<string> tables)
        {
            var sql = new SqlWriter();

            foreach (var table in tables.Select((x, i) => new { First = i == 0, Name = x }))
            {
                if (!table.First) sql.Intersect.Flush();
                sql.Select.SystemNameColumn.Trim().Comma.
                    Case.SystemTypeColumn.When.DataTypeId(DataTypes.SqlTypeId.VarChar).Then.DataTypeId(DataTypes.SqlTypeId.NVarChar).
                                            When.DataTypeId(DataTypes.SqlTypeId.Char).Then.DataTypeId(DataTypes.SqlTypeId.NChar).
                                            When.DataTypeId(DataTypes.SqlTypeId.Text).Then.DataTypeId(DataTypes.SqlTypeId.NText).
                                            Else.SystemTypeColumn.End.As.SystemTypeColumn.Trim().Comma.
                    Case.SystemUserTypeColumn.When.DataTypeId(DataTypes.SqlTypeId.VarChar).Then.DataTypeId(DataTypes.SqlTypeId.NVarChar).
                                                When.DataTypeId(DataTypes.SqlTypeId.Char).Then.DataTypeId(DataTypes.SqlTypeId.NChar).
                                                When.DataTypeId(DataTypes.SqlTypeId.Text).Then.DataTypeId(DataTypes.SqlTypeId.NText).
                                                Else.SystemUserTypeColumn.End.As.SystemUserTypeColumn.
                    From.SystemColumns.
                    Where.SystemObjectIdColumn.Equal.ObjectId(table.Name);
            }

            return new Statement(sql.ToString(), Statement.StatementType.Text, Statement.ResultType.Multiple);
        }

        private static IEnumerable<Select> GetUnionTables(Select select)
        {
            var tables = new List<Select>();
            if (select.Source.Type == Data.DataType.Table) tables.Add(select);
            else if (select.Source.HasQueries) tables.AddRange(select.Source.Queries.SelectMany(GetUnionTables));
            return tables;
        }

        public static Statement CreateTableExistsStatement(string tableName)
        {
            var writer = SqlWriter.CreateWriter().Select.TableExistsValue(tableName);
            return new Statement(writer.ToString(), Statement.StatementType.Text, Statement.ResultType.Scalar);
        }

        public static Statement CreateDeleteTableStatement(string tableName)
        {
            var writer = SqlWriter.CreateWriter().If.TableExists(tableName).Drop.Table.QuotedName(tableName);
            return new Statement(writer.ToString(), Statement.StatementType.Text, Statement.ResultType.None);
        }

        public static Statement CreateAddColumnStatement(string tableName, Column column)
        {
            var writer = new SqlWriter();
            writer.Alter.Table.QuotedName(tableName).Add.Flush();
            writer.ColumnDefinition(column.Name, column.Type, column.Length, column.IsPrimaryKey, column.IsIdentity, column.IsNullable, column.IsAutoGenerated, column.DefaultValue);
            return new Statement(writer.ToString(), Statement.StatementType.Text, Statement.ResultType.None);
        }

        public static Statement CreateRemoveColumnStatement(string tableName, string columnName)
        {
            var writer = SqlWriter.CreateWriter().IfColumnExists(tableName, columnName).Alter.Table.QuotedName(tableName).Drop.Column.QuotedName(columnName);
            return new Statement(writer.ToString(), Statement.StatementType.Text, Statement.ResultType.None);
        }

        public static Statement CreateAddNonClusteredIndexStatement(string tableName, params string[] columnNames)
        {
            var indexName = string.Format("IX_{0}_{1}", tableName, string.Join("_", columnNames));
            var writer = SqlWriter.CreateWriter().Create.NonClustered.Index.QuotedName(indexName).On.QuotedName(tableName).OpenBlock.Trim();
            var first = true;
            foreach (var columnName in columnNames)
            {
                if (!first) writer.Trim().Comma.Flush();
                writer.QuotedName(columnName).Ascending.Flush();
                first = false;
            }
            writer.Trim().CloseBlock.Flush();
            return new Statement(writer.ToString(), Statement.StatementType.Text, Statement.ResultType.None);
        }

        public static Statement CreateRemoveNonClusteredIndexStatement(string tableName, string indexName)
        {
            var writer = SqlWriter.CreateWriter().IfIndexExists(tableName, indexName).Drop.Index.QuotedName(indexName).On.QuotedName(tableName);
            return new Statement(writer.ToString(), Statement.StatementType.Text, Statement.ResultType.None);
        }
    }
}
