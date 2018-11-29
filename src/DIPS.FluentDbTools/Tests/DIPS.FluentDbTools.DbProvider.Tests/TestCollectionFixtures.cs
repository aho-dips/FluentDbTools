﻿using DIPS.FluentDbTools.TestUtilities;
using Xunit;

namespace DIPS.FluentDbTools.DbProvider.Tests
{
    [CollectionDefinition(CollectionTag)]
    public class TestCollectionFixtures : ICollectionFixture<DatabaseFixture>
    {
        public const string CollectionTag = "Database collection";
    }
}