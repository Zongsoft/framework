var target = Argument("target", "default");
var compilation = Argument("compilation", "Debug");

var solutionFile = "Zongsoft.Diagnostics.Web.slnx";

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
	};

	var projects = GetFiles("test/unit/*.csproj");

	foreach(var project in projects)
	{
		DotNetTest(project.FullPath, settings);
	}
});

Task("integration-test")
	.Description("Prometheus 集成测试（本机 HTTP/HTTPS 端点）")
	.IsDependentOn("build")
	.Does(() =>
{
	DotNetBuild("test/integration/Zongsoft.Diagnostics.Web.Tests.slnx", new DotNetBuildSettings
	{
		Configuration = compilation,
		MSBuildSettings = new DotNetMSBuildSettings().WithProperty("GeneratePackageOnBuild", "false"),
	});

	DotNetTest("test/integration/Zongsoft.Diagnostics.Web.Tests.csproj", new DotNetTestSettings
	{
		NoBuild = true,
		NoRestore = true,
		Configuration = compilation,
	});
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
