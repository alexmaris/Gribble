﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Gribble.Extensions;
using Gribble.Mapping;
using Gribble.TransactSql;

namespace Gribble
{
    public interface ISqlStatement
    {
        int Execute(string commandText, IDictionary<string, object> parameters = null);
        T ExecuteScalar<T>(string commandText, IDictionary<string, object> parameters = null);
        TEntity ExecuteSingle<TEntity>(string commandText, 
            IDictionary<string, object> parameters = null) where TEntity : class;
        TEntity ExecuteSingleOrNone<TEntity>(string commandText, 
            IDictionary<string, object> parameters = null) where TEntity : class;
        IEnumerable<TEntity> ExecuteMany<TEntity>(string commandText, 
            IDictionary<string, object> parameters = null) where TEntity : class;
        DataSet ExecuteDataSet(string commandText,
            IDictionary<string, object> parameters = null);
        DataTable ExecuteDataTable(string tableName, string commandText,
            IDictionary<string, object> parameters = null);
    }

    public class SqlStatement : ISqlStatement
    {
        private readonly IConnectionManager _connectionManager;
        private readonly IProfiler _profiler;
        private readonly EntityMappingCollection _map;

        public SqlStatement(IConnectionManager connectionManager, 
            EntityMappingCollection map, IProfiler profiler)
        {
            _connectionManager = connectionManager;
            _profiler = profiler;
            _map = map;
        }

        public static ISqlStatement Create(SqlConnection connection, 
            TimeSpan? commandTimeout = null, IProfiler profiler = null)
        {
            return Create(new ConnectionManager(connection, commandTimeout ?? 
                new TimeSpan(0, 5, 0)), profiler);
        }

        public static ISqlStatement Create(IConnectionManager connectionManager, IProfiler profiler = null)
        {
            return new SqlStatement(connectionManager, new EntityMappingCollection(
                Enumerable.Empty<IClassMap>()), profiler ?? new ConsoleProfiler());
        }

        public static ISqlStatement Create(SqlConnection connection, string keyColumn, 
            TimeSpan? commandTimeout = null, IProfiler profiler = null)
        {
            return Create(new ConnectionManager(connection, commandTimeout ?? 
                new TimeSpan(0, 5, 0)), keyColumn, profiler);
        }

        public static ISqlStatement Create(IConnectionManager connectionManager, 
            string keyColumn, IProfiler profiler = null)
        {
            return new SqlStatement(connectionManager, new EntityMappingCollection(
                new IClassMap[] { new GuidKeyEntityMap(keyColumn), new IntKeyEntityMap(keyColumn) }), 
                profiler ?? new ConsoleProfiler());
        }

        public static ISqlStatement Create(SqlConnection connection, 
            EntityMappingCollection mappingCollection, 
            TimeSpan? commandTimeout = null, IProfiler profiler = null)
        {
            return Create(new ConnectionManager(connection, commandTimeout ?? 
                new TimeSpan(0, 5, 0)), mappingCollection, profiler);
        }

        public static ISqlStatement Create(IConnectionManager connectionManager, 
            EntityMappingCollection mappingCollection, IProfiler profiler = null)
        {
            return new SqlStatement(connectionManager, mappingCollection, profiler ?? new ConsoleProfiler());
        }

        public int Execute(string commandText, IDictionary<string, object> parameters = null)
        {
            return ExecuteBatches(commandText, parameters, x => ExecuteNonQuery(x, parameters));
        }

        public T ExecuteScalar<T>(string commandText, IDictionary<string, object> parameters = null)
        {
            return ExecuteBatches(commandText, parameters, x =>
                Command.Create(StatementWriter.CreateStatement(x,
                        parameters, Statement.ResultType.Scalar), _profiler)
                    .ExecuteScalar<T>(_connectionManager));
        }

        public TEntity ExecuteSingle<TEntity>(string commandText,
            IDictionary<string, object> parameters = null) where TEntity : class
        {
            return ExecuteBatches(commandText, parameters, x =>
                Load<TEntity, TEntity>(Command.Create(StatementWriter.CreateStatement(x,
                    parameters, Statement.ResultType.Single), _profiler)));
        }

        public TEntity ExecuteSingleOrNone<TEntity>(string commandText,
            IDictionary<string, object> parameters = null) where TEntity : class
        {
            return ExecuteBatches(commandText, parameters, x =>
                Load<TEntity, TEntity>(Command.Create(StatementWriter.CreateStatement(x,
                    parameters, Statement.ResultType.SingleOrNone), _profiler)));
        }

        public IEnumerable<TEntity> ExecuteMany<TEntity>(string commandText,
            IDictionary<string, object> parameters = null) where TEntity : class
        {
            return ExecuteBatches(commandText, parameters, x =>
                Load<TEntity, IEnumerable<TEntity>>(
                    Command.Create(StatementWriter.CreateStatement(x, parameters,
                        Statement.ResultType.Multiple), _profiler)));
        }

        public DataSet ExecuteDataSet(string commandText,
            IDictionary<string, object> parameters = null)
        {
            return ExecuteBatches(commandText, parameters, x =>
                Command.Create(StatementWriter.CreateStatement(x, parameters,
                        Statement.ResultType.Multiple), _profiler)
                    .ExecuteDataSet(_connectionManager));
        }

        public DataTable ExecuteDataTable(string tableName, string commandText,
            IDictionary<string, object> parameters = null)
        {
            return ExecuteBatches(commandText, parameters, x =>
                Command.Create(StatementWriter.CreateStatement(x, parameters,
                    Statement.ResultType.Multiple), _profiler)
                        .ExecuteDataTable(tableName, _connectionManager));
        }

        private TResult Load<TEntity, TResult>(Command command) where TEntity : class 
        {
            return (TResult)new Loader<TEntity>(command, _map.GetEntityMapping<TEntity>())
                .Execute(_connectionManager);
        }

        private T ExecuteBatches<T>(string commandText, IDictionary<string, object> parameters, 
            Func<string, T> command)
        {
            var commands = SplitBatches(commandText);
            if (commands.Length > 1)
                commands.Take(commands.Length - 1).ToList()
                    .ForEach(x => ExecuteNonQuery(x, parameters));
            return command(commands.Last());
        }

        private int ExecuteNonQuery(string commandText, IDictionary<string, object> parameters = null)
        {
            return Command.Create(StatementWriter.CreateStatement(commandText,
                    parameters, Statement.ResultType.None), _profiler)
                    .ExecuteNonQuery(_connectionManager);
        }

        private static string[] SplitBatches(string commandText)
        {
            return commandText.Split(new[] { "\r\nGO\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        }
    }

    public static class ISqlStatementExtensions
    {
        public static int Execute(this ISqlStatement sqlStatement, 
            string commandText, object parameters)
        {
            return sqlStatement.Execute(commandText, parameters.AsDictionary());
        }

        public static T ExecuteScalar<T>(this ISqlStatement sqlStatement,
            string commandText, object parameters)
        {
            return sqlStatement.ExecuteScalar<T>(commandText, parameters.AsDictionary());
        }

        public static TEntity ExecuteSingle<TEntity>(this ISqlStatement sqlStatement,
            string commandText, object parameters) where TEntity : class
        {
            return sqlStatement.ExecuteSingle<TEntity>(commandText, parameters.AsDictionary());
        }

        public static TEntity ExecuteSingleOrNone<TEntity>(this ISqlStatement sqlStatement,
            string commandText, object parameters) where TEntity : class
        {
            return sqlStatement.ExecuteSingleOrNone<TEntity>(commandText, parameters.AsDictionary());
        }

        public static IEnumerable<TEntity> ExecuteMany<TEntity>(this ISqlStatement sqlStatement,
            string commandText, object parameters) where TEntity : class
        {
            return sqlStatement.ExecuteMany<TEntity>(commandText, parameters.AsDictionary());
        }

        public static DataSet ExecuteDataSet(this ISqlStatement sqlStatement,
            string commandText, object parameters)
        {
            return sqlStatement.ExecuteDataSet(commandText, parameters.AsDictionary());
        }

        public static DataTable ExecuteDataTable(this ISqlStatement sqlStatement,
            string tableName, string commandText, object parameters)
        {
            return sqlStatement.ExecuteDataTable(tableName, commandText, parameters.AsDictionary());
        }
    }
}
