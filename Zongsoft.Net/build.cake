var target = Argument("target", "default");
var compilation = Argument("compilation", "Debug");

var solutionFile = "Zongsoft.Net.slnx";

Task("clean")
	.Description("清理解决方案")
	.Does(() =>
{
	CleanDirectories($"**/bin/{compilation}");
	CleanDirectories($"**/obj/{compilation}");
});

Task("restore")
	.Description("还原项目依赖")
	.Does(() =>
{
	DotNetRestore(solutionFile);
});

Task("build")
	.Description("编译项目")
	.IsDependentOn("clean")
	.IsDependentOn("restore")
	.Does(() =>
{
	var settings = new DotNetBuildSettings
	{
		Configuration = compilation,
	};

	DotNetBuild(solutionFile, settings);
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
		Configuration = compilation,
		ResultsDirectory = MakeAbsolute(Directory("./TestResults")),
		Loggers = new[] { "console;verbosity=normal", "trx;LogFilePrefix=Zongsoft.Net.Tests" },
		ArgumentCustomization = arguments => arguments
			.Append("--blame-hang-timeout 2min")
			.Append("--blame-hang-dump-type full"),
	};

	var projects = GetFiles("**/test/*.csproj");

	foreach(var project in projects)
	{
		DotNetTest(project.FullPath, settings);
	}
});

Task("pack")
	.Description("发包(NuGet)")
	.IsDependentOn("build")
	.Does(() =>
{
	var packages = GetFiles($"**/{compilation}/*.nupkg");

	foreach(var package in packages)
	{
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
