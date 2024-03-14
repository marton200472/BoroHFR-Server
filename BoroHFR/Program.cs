using BoroHFR;
using BoroHFR.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.ComponentModel;
using System.Reflection;


var builder = WebApplication.CreateBuilder(args);
var jsons = builder.Configuration.Sources.Where(x => x is Microsoft.Extensions.Configuration.Json.JsonConfigurationSource).ToArray();
for (int i = 0; i < jsons.Length; i++)
{
    builder.Configuration.Sources.Remove(jsons[i]);
}



if (System.IO.File.Exists("./storage/appsettings.json"))
{
    builder.Configuration.AddJsonFile("./storage/appsettings.json");
    if (builder.Configuration["ConnectionStrings:Default"] is not null && builder.Configuration["SysAdmin:Email"] is not null && builder.Configuration["Smtp:Server"] is not null)
    {
        builder.ConfigureNormalMode()
        .Build()
        .AddNormalPipeline()
        .Run();
    }
    else
    {
        builder.Configuration.Sources.Add(new BoroHFR.Configuration.AppSettingsJsonConfigurationSource("./storage/appsettings.json"));
        builder.ConfigureSetUpMode()
        .Build()
        .AddSetupPipeline()
        .Run();
    }
    
}
else
{
    builder.Configuration.Sources.Add(new BoroHFR.Configuration.AppSettingsJsonConfigurationSource("./storage/appsettings.json"));
    builder.ConfigureSetUpMode()
        .Build()
        .AddSetupPipeline()
        .Run();
}


