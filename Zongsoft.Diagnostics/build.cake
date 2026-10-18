var target = Argument("target", "default");
var edition = Argument("edition", "Debug");

var solutionFile = "Zongsoft.Diagnostics.slnx";

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

	var projects = GetFiles("test/*.csproj").Concat(GetFiles("protocols/**/test/unit/*.csproj"));

	foreach(var project in projects)
	{
		DotNetTest(project.FullPath, settings);
	}
});

Task("integration-test")
	.Description("协议集成测试（本机回环端点）")
	.IsDependentOn("build")
	.Does(() =>
{
	var settings = new DotNetBuildSettings
	{
		Configuration = edition,
		MSBuildSettings = new DotNetMSBuildSettings().WithProperty("GeneratePackageOnBuild", "false"),
	};

	// 集成测试通过 HintPath 引用客户端和样例；客户端已由 build 任务生成。
	DotNetBuild("protocols/server/samples/Zongsoft.Diagnostics.Protocols.Server.Samples.csproj", settings);
	DotNetBuild("protocols/client/test/integration/Zongsoft.Diagnostics.Protocols.Client.Tests.slnx", settings);
	DotNetBuild("protocols/server/test/integration/Zongsoft.Diagnostics.Protocols.Server.Tests.slnx", settings);

	var testSettings = new DotNetTestSettings
	{
		NoBuild = true,
		NoRestore = true,
		Configuration = edition,
	};

	foreach(var project in GetFiles("protocols/**/test/integration/*.csproj"))
	{
		DotNetTest(project.FullPath, testSettings);
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
