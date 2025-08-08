// <copyright file="Program.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using Microsoft.Extensions.Configuration;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

var isMultiTenantEditor = builder.Configuration.GetValue<bool?>("MultiTenantEditor") ?? false;

if (isMultiTenantEditor)
{
    System.Console.WriteLine("Starting Cosmos CMS Editor in Multi-Tenant Mode.");
    var app = Cosmos.Editor.Boot.MultiTenant.BuildApp(builder).Result;
    await app.RunAsync();
}
else
{
    System.Console.WriteLine("Starting Cosmos CMS Editor in Single-Tenant Mode.");
    var app = Cosmos.Editor.Boot.SingleTenant.BuildApp(builder).Result;
    await app.RunAsync();
}