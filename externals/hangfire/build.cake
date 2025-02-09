var target = Argument("target", "default");
var edition = Argument("edition", "Debug");

var solutionFile = "Zongsoft.Externals.Hangfire.sln";
var dependents = new []
{
	"web/Zongsoft.Externals.Hangfire.Web.sln",
	"storages/Zongsoft.Externals.Hangfire.Storages.sln",
	"samples/Zongsoft.Externals.Hangfire.Samples.sln"
};

Task("clean")
	.Description("清理解决方案")
	.Does(() =>
{
	CleanDirectories("**/bin/{edition}");
	CleanDirectories("**/obj/{edition}");
});

Task("restore")
	.Description("还原项目依赖")
	.Does(() =>
{
	DotNetRestore(solutionFile);

	foreach(var dependent in dependents)
	{
		DotNetRestore(dependent);
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
		NoRestore = true,
		Configuration = edition,
	};

	DotNetBuild(solutionFile, settings);

	foreach(var dependent in dependents)
	{
		DotNetBuild(dependent, settings);
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
		DotNetTest(project.FullPath, settings);
	}
});

Task("default")
	.Description("默认")
	.IsDependentOn("test");

RunTarget(target);
