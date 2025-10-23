var target = Argument("target", "default");
var edition = Argument("edition", "Debug");
var drivers = Argument("drivers", "*");

var solutionFile = "Zongsoft.Data.sln";
var providerFiles = drivers == "*" ? new List<string>(
[
	@"drivers/mssql/Zongsoft.Data.MsSql.sln",
	@"drivers/mysql/Zongsoft.Data.MySql.sln",
	@"drivers/sqlite/Zongsoft.Data.SQLite.sln",
	@"drivers/influx/Zongsoft.Data.Influx.sln",
	@"drivers/tdengine/Zongsoft.Data.TDengine.sln",
	@"drivers/postgres/Zongsoft.Data.PostgreSql.sln",
	@"drivers/clickhouse/Zongsoft.Data.ClickHouse.sln",
]) : [];

if(providerFiles.Count == 0 && !string.IsNullOrEmpty(drivers) && drivers != "none" && drivers != "empty")
{
	var parts = drivers.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

	for(int i = 0; i < parts.Length; i++)
	{
		switch(parts[i].ToLowerInvariant())
		{
			case "mssql":
				providerFiles.Add(@"drivers/mssql/Zongsoft.Data.MsSql.sln");
				break;
			case "mysql":
				providerFiles.Add(@"drivers/mysql/Zongsoft.Data.MySql.sln");
				break;
			case "sqlite":
				providerFiles.Add(@"drivers/sqlite/Zongsoft.Data.SQLite.sln");
				break;
			case "influx":
				providerFiles.Add(@"drivers/influx/Zongsoft.Data.Influx.sln");
				break;
			case "tdengine":
				providerFiles.Add(@"drivers/tdengine/Zongsoft.Data.TDengine.sln");
				break;
			case "postgres":
				providerFiles.Add(@"drivers/postgres/Zongsoft.Data.PostgreSql.sln");
				break;
			case "clickhouse":
				providerFiles.Add(@"drivers/clickhouse/Zongsoft.Data.ClickHouse.sln");
				break;
		}
	}
}

Task("clean")
	.Description("清理解决方案")
	.Does(() =>
{
	CleanDirectories($"**/bin/{edition}");
	CleanDirectories($"**/obj/{edition}");
});

Task("restore")
	.Description("还原项目依赖")
	.Does(() =>
{
	DotNetRestore(solutionFile);

	foreach(var providerFile in providerFiles)
	{
		DotNetRestore(providerFile);
	}
});

Task("build")
	.Description("编译项目")
	.IsDependentOn("clean")
	.IsDependentOn("restore")
	.Does(() =>
{
	var settings = new DotNetBuildSettings
	{
		Configuration = edition,
	};

	DotNetBuild(solutionFile, settings);

	foreach(var providerFile in providerFiles)
	{
		DotNetBuild(providerFile, settings);
	}
});

Task("test")
	.Description("单元测试")
	.IsDependentOn("build")
	.Does(() =>
{
	var settings = new DotNetTestSettings
	{
		NoBuild = true,
		NoRestore = true,
		Configuration = edition,
	};

	var projects = GetFiles("**/test/*.csproj");

	foreach(var project in projects)
	{
		if(IsIncluded(project.GetDirectory()))
			DotNetTest(project.FullPath, settings);
	}
});

Task("pack")
	.Description("发包(NuGet)")
	.IsDependentOn("build")
	.Does(() =>
{
	var packages = GetFiles($"**/{edition}/*.nupkg");

	foreach(var package in packages)
	{
		if(!IsIncluded(package.GetDirectory()))
			continue;

		DotNetNuGetPush(package.FullPath, new DotNetNuGetPushSettings
		{
			Source = "nuget.org",
			ApiKey = EnvironmentVariable("NUGET_API_KEY"),
			SkipDuplicate = true,
		});
	}
});

Task("default")
	.Description("默认")
	.IsDependentOn("test");

RunTarget(target);

bool IsIncluded(DirectoryPath directory)
{
	foreach(var filePath in providerFiles)
	{
		if(directory.FullPath.Contains(System.IO.Path.GetDirectoryName(filePath).Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
			return true;
	}

	return false;
}
