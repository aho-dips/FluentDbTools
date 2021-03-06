﻿using System.Collections.Generic;
using System.Reflection;
using FluentDbTools.Extensions.DbProvider;
using FluentDbTools.Common.Abstractions;
using FluentDbTools.Migration.Abstractions;
using FluentDbTools.Migration.Common;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.Processors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FluentDbTools.Migration
{
    internal static class FluentMigrationConfigurationExtensions
    {
        public static IServiceCollection ConfigureFluentMigrationWithDatabaseType(this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddScoped<IOptionsSnapshot<ProcessorOptions>>(sp =>
                {
                    var factory = sp.GetService<IOptionsFactory<ProcessorOptions>>();
                    var manager = new OptionsManager<ProcessorOptions>(factory);
                    manager.Value.ConnectionString = sp.GetService<IDbMigrationConfig>().ConnectionString;
                    return manager;
                })
                .AddScoped<IOptionsSnapshot<SelectingProcessorAccessorOptions>>(sp =>
                {
                    var factory = sp.GetService<IOptionsFactory<SelectingProcessorAccessorOptions>>();
                    var optionsManager = new OptionsManager<SelectingProcessorAccessorOptions>(factory);
                    optionsManager.Value.ProcessorId = sp.GetService<IDbMigrationConfig>().ProcessorId;
                    return optionsManager;
                });
                //.AddScoped<IOptions<ProcessorOptions>>(sp =>
                //{
                //    var options = new OptionsWrapper<ProcessorOptions>(new ProcessorOptions
                //    {
                //        ConnectionString =
                //            sp.GetService<IDbConfig>().GetAdminConnectionString()
                //    });
                //    return options;
                //})
                //.AddScoped<IOptions<SelectingProcessorAccessorOptions>>(sp =>
                //{
                //    var options = new OptionsWrapper<SelectingProcessorAccessorOptions>(
                //        new SelectingProcessorAccessorOptions()
                //        {
                //            ProcessorId = sp.GetService<IDbConfig>().DbType.GetProcessorId()
                //        });
                //    return options;
                //});
        }

        public static IScanInBuilder AssembliesForMigrations(this IScanInBuilder scanInBuilder, IEnumerable<Assembly> assembliesWithMigrationModels)
        {
            foreach (var assemblyWithMigrationModels in assembliesWithMigrationModels)
            {
                scanInBuilder.ScanIn(assemblyWithMigrationModels).For.Migrations();
            }
            return scanInBuilder;
        }
    }
}