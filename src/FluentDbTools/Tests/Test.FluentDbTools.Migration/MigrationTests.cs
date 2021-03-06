using System;
using FluentDbTools.Common.Abstractions;
using Example.FluentDbTools.Common;
using Example.FluentDbTools.Config;
using Example.FluentDbTools.Migration;
using FluentAssertions;
using FluentDbTools.DbProviders;
using FluentDbTools.Extensions.DbProvider;
using FluentDbTools.Extensions.Migration;
using FluentDbTools.Extensions.Migration.DefaultConfigs;
using FluentDbTools.Extensions.MSDependencyInjection;
using FluentDbTools.Extensions.MSDependencyInjection.DefaultConfigs;
using FluentDbTools.Extensions.MSDependencyInjection.Oracle;
using FluentDbTools.Migration;
using FluentDbTools.Migration.Abstractions;
using TestUtilities.FluentDbTools;
using FluentMigrator.Runner;
using FluentMigrator.Runner.VersionTableInfo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oracle.ManagedDataAccess.Client;
using Xunit;
// ReSharper disable PossibleNullReferenceException

namespace Test.FluentDbTools.Migration
{
    public class MigrationTests
    {
        [Theory]
        [MemberData(nameof(TestParameters.DbParameters), MemberType = typeof(TestParameters))]
        public void Migration_Success(SupportedDatabaseTypes databaseType)
        {
            var inMemoryOverrideConfig = OverrideConfig.GetInMemoryOverrideConfig(databaseType, OverrideConfig.NewRandomSchema);
            var provider = MigrationBuilder.BuildMigration(databaseType, inMemoryOverrideConfig);

            using (var scope = provider.CreateScope())
            {
                var migrationRunner = scope.ServiceProvider.GetService<IMigrationRunner>();
                var versionTable = scope.ServiceProvider.GetService<IVersionTableMetaData>();

                migrationRunner.MigrateUp();

                migrationRunner.MigrateDown(0);

                migrationRunner.DropSchema(versionTable);
            }
        }

        [Theory]
        [MemberData(nameof(TestParameters.DbParameters), MemberType = typeof(TestParameters))]
        public void MigrationWithExecutorExtension_Success(SupportedDatabaseTypes databaseType)
        {
            var inMemoryOverrideConfig = OverrideConfig.GetInMemoryOverrideConfig(databaseType, OverrideConfig.NewRandomSchema);
            var configuration = new ConfigurationBuilder()
                .AddDbToolsExampleConfiguration(databaseType)
                .AddInMemoryCollection(inMemoryOverrideConfig)
                .Build();

            var serviceProvider = new ServiceCollection()
                .AddScoped<IConfiguration>(provider => configuration)
                .AddDefaultDbMigrationConfig()
                .BuildServiceProvider();

            var dbConfig = serviceProvider.GetService<IDbMigrationConfig>();

            var runner = dbConfig.GetMigrationRunner(MigrationBuilder.MigrationAssemblies);

            runner.MigrateUp();

            dbConfig.DropSchema(MigrationBuilder.MigrationAssemblies);
        }

        [Fact]
        public void OracleMigration_WhenDataSourceIsValidEzConnect_Success()
        {
            var databaseType = SupportedDatabaseTypes.Oracle;
            var defaulDbConfig = MigrationBuilder.BuildMigration(databaseType).GetDbConfig();

            var inMemoryOverrideConfig = OverrideConfig.GetInMemoryOverrideConfig(databaseType, OverrideConfig.NewRandomSchema);
            inMemoryOverrideConfig["database:dataSource"] = $"{defaulDbConfig.Hostname}/{defaulDbConfig.DatabaseName}";
            inMemoryOverrideConfig["database:connectionTimeoutInSecs"] = "5";

            var expectedDataSource = inMemoryOverrideConfig["database:dataSource"];
            var provider = MigrationBuilder.BuildMigration(databaseType, inMemoryOverrideConfig, sp => sp.AddOracleDbProvider());

            using (var scope = provider.CreateScope())
            {
                var migrationRunner = scope.ServiceProvider.GetService<IMigrationRunner>();
                var versionTable = scope.ServiceProvider.GetService<IVersionTableMetaData>();
                var dbconfig = scope.ServiceProvider.GetDbConfig();

                dbconfig.Datasource.Should().Be(inMemoryOverrideConfig["database:dataSource"]);

                dbconfig.GetDbProviderFactory(true)
                    .CreateConnection()
                    .DataSource.Should().Be(expectedDataSource);

                migrationRunner.MigrateUp();

                migrationRunner.MigrateDown(0);

                migrationRunner.DropSchema(versionTable);
            }
        }

        [Fact]
        public void OracleMigration_WhenDatasSourceIsValidTnsAliastName_Success()
        {
            var databaseType = SupportedDatabaseTypes.Oracle;

            var inMemoryOverrideConfig = OverrideConfig.GetInMemoryOverrideConfig(databaseType, OverrideConfig.NewRandomSchema);
            inMemoryOverrideConfig["database:dataSource"] = BaseConfig.InContainer ? "TNSTEST_INDOCKER" : "TNSTEST";
            inMemoryOverrideConfig["database:connectionTimeoutInSecs"] = "5";

            var expectedDataSource = inMemoryOverrideConfig["database:dataSource"];
            var provider = MigrationBuilder.BuildMigration(databaseType, inMemoryOverrideConfig, sp => sp.AddOracleDbProvider());

            using (var scope = provider.CreateScope())
            {
                var migrationRunner = scope.ServiceProvider.GetService<IMigrationRunner>();
                var versionTable = scope.ServiceProvider.GetService<IVersionTableMetaData>();
                var dbconfig = scope.ServiceProvider.GetDbConfig();

                dbconfig.Datasource.Should().Be(inMemoryOverrideConfig["database:dataSource"]);

                dbconfig
                    .GetDbProviderFactory(true).CreateConnection()
                    .DataSource.Should().Be(expectedDataSource);

                migrationRunner.MigrateUp();

                migrationRunner.MigrateDown(0);

                migrationRunner.DropSchema(versionTable);
            }
        }

        [Fact]
        public void OracleMigration_WhenDataSourceIsInvalidTnsAliastName_ShouldFailWithTnsResolvingError()
        {
            var databaseType = SupportedDatabaseTypes.Oracle;

            var inMemoryOverrideConfig = OverrideConfig.GetInMemoryOverrideConfig(databaseType, OverrideConfig.NewRandomSchema);
            inMemoryOverrideConfig["database:dataSource"] = "InvalidTnsAlias";
            inMemoryOverrideConfig["database:connectionTimeoutInSecs"] = "5";

            var expectedDataSource = inMemoryOverrideConfig["database:dataSource"];
            var provider = MigrationBuilder.BuildMigration(databaseType, inMemoryOverrideConfig, sp => sp.AddOracleDbProvider());

            using (var scope = provider.CreateScope())
            {
                var migrationRunner = scope.ServiceProvider.GetService<IMigrationRunner>();
                var versionTable = scope.ServiceProvider.GetService<IVersionTableMetaData>();
                var dbconfig = scope.ServiceProvider.GetDbConfig();

                dbconfig.Datasource.Should().Be(inMemoryOverrideConfig["database:dataSource"]);
                dbconfig
                    .GetDbProviderFactory(true).CreateConnection()
                    .DataSource.Should().Be(expectedDataSource);
                
                Action action = () =>
                {
                    migrationRunner.MigrateUp();

                    migrationRunner.MigrateDown(0);

                    migrationRunner.DropSchema(versionTable);

                };

                // Unable to resolve ORA-12154: TNS:could not resolve the connect identifier specified 
                action.Should().Throw<OracleException>().Which.Number.Should().Be(12154);

            }
        }

    }
}