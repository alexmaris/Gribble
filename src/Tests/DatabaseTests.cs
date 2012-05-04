﻿using System;
using System.Collections.Generic;
using System.Linq;
using Gribble;
using Gribble.Mapping;
using Gribble.Model;
using NUnit.Framework;
using Should;

namespace Tests
{
    [TestFixture]
    public class DatabaseTests
    {
        private static readonly TestDatabase Database =
            new TestDatabase("[id] [int] IDENTITY(1,1) NOT NULL, [name] [varchar] (500) NULL, [hide] [bit] NULL, [timestamp] [datetime] NULL",
                             10, "name, hide, [timestamp]", "'oh hai', 0, GETDATE()");

        public class Entity
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public Dictionary<string, object> Values { get; set; }
        }

        public class TestClassMap : ClassMap<Entity>
        {
            public TestClassMap()
            {
                Id(x => x.Id).Column("id");
                Map(x => x.Name).Column("name");
                Map(x => x.Values).Dynamic();
            }
        }

        public static EntityMappingCollection MappingCollection = new EntityMappingCollection(new IClassMap[] { new TestClassMap() });
        public static IProfiler Profiler = new ConsoleProfiler();

        [TestFixtureSetUp]
        public void Setup()
        {
            Database.SetUp();
            Database.CreateTables();
            Database.ExecuteNonQuery("CREATE PROCEDURE GetAll AS BEGIN SELECT * FROM {0} END", Database.FirstTable.Name);
            Database.ExecuteNonQuery("CREATE PROCEDURE GetOne @Id int AS BEGIN SELECT TOP 1 * FROM {0} WHERE Id=@Id END", Database.FirstTable.Name);
            Database.ExecuteNonQuery("CREATE PROCEDURE GetCount AS BEGIN SELECT COUNT(*) FROM {0} END", Database.FirstTable.Name);
            Database.ExecuteNonQuery("CREATE PROCEDURE DeleteOne @Id int AS BEGIN DELETE FROM {0} WHERE Id=@Id END", Database.FirstTable.Name);
        }

        [TestFixtureTearDown]
        public void TearDown() { Database.TearDown(); }

        [SetUp]
        public void TestSetup() { Database.CreateTables(); }

        [Test]
        public void Get_All_Test()
        {
            var results = new Database(Database.Connection, TimeSpan.FromMinutes(5), MappingCollection, Profiler).CallProcedureMany<Entity>("GetAll").ToList();
            results.Count().ShouldEqual(10);
            results.All(x => x.Name.Length > 3).ShouldEqual(true);
            results.All(x => x.Id > -1).ShouldEqual(true);
            results.First().Values.Count.ShouldEqual(2);
            ((bool)results.First().Values["hide"]).ShouldEqual(false);
            ((DateTime)results.First().Values["timestamp"]).ShouldBeGreaterThan(DateTime.MinValue);
        }

        [Test]
        public void Get_One_Test()
        {
            var result = new Database(Database.Connection, TimeSpan.FromMinutes(5), MappingCollection, Profiler).CallProcedureSingle<Entity>("GetOne", new Dictionary<string, object> { { "Id", 5 } });
            result.ShouldNotBeNull();
            result.Name.Length.ShouldBeGreaterThan(3);
            result.Id.ShouldEqual(5);
            result.Values.Count.ShouldEqual(2);
            ((bool)result.Values["hide"]).ShouldEqual(false);
            ((DateTime)result.Values["timestamp"]).ShouldBeGreaterThan(DateTime.MinValue);
        }

        [Test]
        public void Get_Scalar_Test()
        {
            var result = new Database(Database.Connection, TimeSpan.FromMinutes(5), MappingCollection, Profiler).CallProcedureScalar<int>("GetCount");
            result.ShouldBeGreaterThan(8);
        }

        [Test]
        public void Get_Non_Query_Test()
        {
            new Database(Database.Connection, TimeSpan.FromMinutes(5), MappingCollection, Profiler).CallProcedure("DeleteOne", new Dictionary<string, object> { { "Id", 6 } });

            var result = new Database(Database.Connection, TimeSpan.FromMinutes(5), MappingCollection, Profiler).CallProcedureScalar<int>("GetCount");
            result.ShouldEqual(9);
        }

        [Test]
        public void Create_Table_Test()
        {
            var tableName = "Temp" + Guid.NewGuid().ToString("N");
            new Database(Database.Connection, TimeSpan.FromMinutes(5), MappingCollection, Profiler).CreateTable(tableName,
                                    new Column("Id", typeof(Guid), isPrimaryKey: true, isAutoGenerated: true),
                                    new Column("Name", typeof(string), isNullable: true, length: 500),
                                    new Column("Active", typeof(bool), isNullable: false, defaultValue: true),
                                    new Column("Created", typeof(DateTime), isNullable: false, isAutoGenerated: true));

            Database.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.tables WHERE name='{0}'", tableName).ShouldEqual(1);
        }

        [Test]
        public void Create_Table_Identity_Test()
        {
            var tableName = "Temp" + Guid.NewGuid().ToString("N");
            new Database(Database.Connection, TimeSpan.FromMinutes(5), MappingCollection, Profiler).CreateTable(tableName,
                new Column("Id", typeof(int), isIdentity: true, isPrimaryKey: true),
                new Column("Name", typeof(string), isNullable: true, length: 500),
                new Column("Created", typeof(DateTime), isNullable: false, isAutoGenerated: true));

            Database.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.tables WHERE name='{0}'", tableName).ShouldEqual(1);
        }

        [Test]
        public void Drop_Table_Test()
        {
            var tableName = "Temp" + Guid.NewGuid().ToString("N");
            new Database(Database.Connection, TimeSpan.FromMinutes(5), MappingCollection, Profiler).CreateTable(tableName,
                                    new Column("Id", typeof(int), isIdentity: true, isPrimaryKey: true),
                                    new Column("Name", typeof(string), isNullable: true, length: 500),
                                    new Column("Created", typeof(DateTime), isNullable: false, isAutoGenerated: true));

            Database.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.tables WHERE name='{0}'", tableName).ShouldEqual(1);

            new Database(Database.Connection, TimeSpan.FromMinutes(5), MappingCollection, Profiler).DeleteTable(tableName);

            Database.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.tables WHERE name='{0}'", tableName).ShouldEqual(0);
        }

        [Test]
        public void Add_Column_Test()
        {
            var tableName = "Temp" + Guid.NewGuid().ToString("N");
            new Database(Database.Connection, TimeSpan.FromMinutes(5), MappingCollection, Profiler).CreateTable(tableName,
                                    new Column("Id", typeof(int), isIdentity: true, isPrimaryKey: true),
                                    new Column("Created", typeof(DateTime), isNullable: false, isAutoGenerated: true));

            Database.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.columns WHERE name='Name' and object_id('{0}') = object_id", tableName).ShouldEqual(0);

            new Database(Database.Connection, TimeSpan.FromMinutes(5), MappingCollection, Profiler).AddColumn(tableName, 
                                    new Column("Name", typeof(string), isNullable: true, length: 500));

            Database.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.columns WHERE name='Name' and object_id('{0}') = object_id", tableName).ShouldEqual(1);
        }

        [Test]
        public void Drop_Column_Test()
        {
            var tableName = "Temp" + Guid.NewGuid().ToString("N");
            new Database(Database.Connection, TimeSpan.FromMinutes(5), MappingCollection, Profiler).CreateTable(tableName,
                                    new Column("Id", typeof(int), isIdentity: true, isPrimaryKey: true),
                                    new Column("Name", typeof(string), isNullable: true, length: 500),
                                    new Column("Created", typeof(DateTime), isNullable: false, isAutoGenerated: true));

            Database.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.columns WHERE name='Name' and object_id('{0}') = object_id", tableName).ShouldEqual(1);

            new Database(Database.Connection, TimeSpan.FromMinutes(5), MappingCollection, Profiler).RemoveColumn(tableName, "Name");

            Database.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.columns WHERE name='Name' and object_id('{0}') = object_id", tableName).ShouldEqual(0);
        }

        [Test]
        public void Add_Index_Test()
        {
            var tableName = "Temp" + Guid.NewGuid().ToString("N");
            new Database(Database.Connection, TimeSpan.FromMinutes(5), MappingCollection, Profiler).CreateTable(tableName,
                                    new Column("Id", typeof(int), isIdentity: true, isPrimaryKey: true),
                                    new Column("Name", typeof(string), isNullable: true, length: 500),
                                    new Column("Created", typeof(DateTime), isNullable: false, isAutoGenerated: true));

            Database.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.indexes WHERE name='IX_{0}_Name_Created' and object_id('{0}') = object_id", tableName).ShouldEqual(0);

            new Database(Database.Connection, TimeSpan.FromMinutes(5), MappingCollection, Profiler).AddNonClusteredIndex(tableName, "Name", "Created");

            Database.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.indexes WHERE name='IX_{0}_Name_Created' and object_id('{0}') = object_id", tableName).ShouldEqual(1);
        }

        [Test]
        public void Drop_Index_Test()
        {
            var tableName = "Temp" + Guid.NewGuid().ToString("N");
            new Database(Database.Connection, TimeSpan.FromMinutes(5), MappingCollection, Profiler).CreateTable(tableName,
                                    new Column("Id", typeof(int), isIdentity: true, isPrimaryKey: true),
                                    new Column("Name", typeof(string), isNullable: true, length: 500),
                                    new Column("Created", typeof(DateTime), isNullable: false, isAutoGenerated: true));

            Database.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.indexes WHERE name='IX_{0}_Name_Created' and object_id('{0}') = object_id", tableName).ShouldEqual(0);

            new Database(Database.Connection, TimeSpan.FromMinutes(5), MappingCollection, Profiler).AddNonClusteredIndex(tableName, "Name", "Created");

            Database.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.indexes WHERE name='IX_{0}_Name_Created' and object_id('{0}') = object_id", tableName).ShouldEqual(1);

            new Database(Database.Connection, TimeSpan.FromMinutes(5), MappingCollection, Profiler).RemoveNonClusteredIndex(tableName, string.Format("IX_{0}_Name_Created", tableName));

            Database.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.indexes WHERE name='IX_{0}_Name_Created' and object_id('{0}') = object_id", tableName).ShouldEqual(0);
        }
    }
}
