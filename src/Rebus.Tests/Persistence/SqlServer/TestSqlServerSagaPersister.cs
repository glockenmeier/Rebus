﻿using System;
using NUnit.Framework;
using Ponder;
using Rebus.Persistence.SqlServer;
using Shouldly;

namespace Rebus.Tests.Persistence.SqlServer
{
    [TestFixture, Category(TestCategories.MsSql)]
    public class TestSqlServerSagaPersister : SqlServerFixtureBase
    {
        SqlServerSagaPersister persister;

        protected override void DoSetUp()
        {
            DropSagaTables();
            persister = new SqlServerSagaPersister(ConnectionStrings.SqlServer, SagaIndexTableName, SagaTableName);
        }

        [Test]
        public void CanUpdateMultipleSagaDatasAtomically()
        {
            Assert.That(persister is ICanUpdateMultipleSagaDatasAtomically, Is.True);
        }

        [Test]
        public void WhenIgnoringNullProperties_DoesNotSaveNullPropertiesOnUpdate()
        {
            persister.EnsureTablesAreCreated()
                .DoNotIndexNullProperties();

            const string correlationProperty1 = "correlation property 1";
            const string correlationProperty2 = "correlation property 2";
            var correlationPropertyPaths = new[]
            {
                Reflect.Path<PieceOfSagaData>(s => s.SomeProperty),
                Reflect.Path<PieceOfSagaData>(s => s.AnotherProperty)
            };

            var firstPieceOfSagaDataWithNullValueOnProperty = new PieceOfSagaData { SomeProperty = correlationProperty1, AnotherProperty="random12423" };
            var nextPieceOfSagaDataWithNullValueOnProperty = new PieceOfSagaData { SomeProperty = correlationProperty2, AnotherProperty="random38791387" };

            persister.Insert(firstPieceOfSagaDataWithNullValueOnProperty, correlationPropertyPaths);
            persister.Insert(nextPieceOfSagaDataWithNullValueOnProperty, correlationPropertyPaths);

            var firstPiece = persister.Find<PieceOfSagaData>(Reflect.Path<PieceOfSagaData>(s => s.SomeProperty), correlationProperty1);
            firstPiece.AnotherProperty = null;
            persister.Update(firstPiece, correlationPropertyPaths);

            var nextPiece = persister.Find<PieceOfSagaData>(Reflect.Path<PieceOfSagaData>(s => s.SomeProperty), correlationProperty2);
            nextPiece.AnotherProperty = null;
            persister.Update(nextPiece, correlationPropertyPaths);
        }

        [Test]
        public void WhenIgnoringNullProperties_DoesNotSaveNullPropertiesOnInsert()
        {
            persister.EnsureTablesAreCreated()
                .DoNotIndexNullProperties();

            const string correlationProperty1 = "correlation property 1";
            const string correlationProperty2 = "correlation property 2";
            var correlationPropertyPaths = new[]
            {
                Reflect.Path<PieceOfSagaData>(s => s.SomeProperty),
                Reflect.Path<PieceOfSagaData>(s => s.AnotherProperty)
            };

            var firstPieceOfSagaDataWithNullValueOnProperty = new PieceOfSagaData
            {
                SomeProperty = correlationProperty1
            };

            var nextPieceOfSagaDataWithNullValueOnProperty = new PieceOfSagaData
            {
                SomeProperty = correlationProperty2
            };

            var firstId = firstPieceOfSagaDataWithNullValueOnProperty.Id;
            var nextId = nextPieceOfSagaDataWithNullValueOnProperty.Id;

            persister.Insert(firstPieceOfSagaDataWithNullValueOnProperty, correlationPropertyPaths);

            // must not throw:
            persister.Insert(nextPieceOfSagaDataWithNullValueOnProperty, correlationPropertyPaths);

            var firstPiece = persister.Find<PieceOfSagaData>(Reflect.Path<PieceOfSagaData>(s => s.SomeProperty), correlationProperty1);
            var nextPiece = persister.Find<PieceOfSagaData>(Reflect.Path<PieceOfSagaData>(s => s.SomeProperty), correlationProperty2);

            Assert.That(firstPiece.Id, Is.EqualTo(firstId));
            Assert.That(nextPiece.Id, Is.EqualTo(nextId));
        }

        class PieceOfSagaData : ISagaData
        {
            public PieceOfSagaData()
            {
                Id = Guid.NewGuid();
            }
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public string SomeProperty { get; set; }
            public string AnotherProperty { get; set; }
        }


        [Test]
        public void CanCreateSagaTablesAutomatically()
        {
            // arrange

            // act
            persister.EnsureTablesAreCreated();

            // assert
            var existingTables = GetTableNames();
            existingTables.ShouldContain(SagaIndexTableName);
            existingTables.ShouldContain(SagaTableName);
        }

        [Test]
        public void DoesntDoAnythingIfTheTablesAreAlreadyThere()
        {
            // arrange
            ExecuteCommand("create table " + SagaTableName + "(id int not null)");
            ExecuteCommand("create table " + SagaIndexTableName + "(id int not null)");

            // act
            // assert
            persister.EnsureTablesAreCreated();
            persister.EnsureTablesAreCreated();
            persister.EnsureTablesAreCreated();
        }
    }
}