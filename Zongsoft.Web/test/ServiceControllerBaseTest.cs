using System;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

using Zongsoft.Data;

namespace Zongsoft.Web.Tests;

public class ServiceControllerBaseTest
{
	[Fact]
	public void NestedServiceInGenericBaseResolvesFromClosedContract()
	{
		using var provider = new ServiceCollection()
			.AddSingleton<GenericServiceBase<Model>>(services => new ConcreteService(services))
			.BuildServiceProvider();

		var controller = new ChildController
		{
			ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext { RequestServices = provider },
			},
		};

		var service = provider.GetRequiredService<GenericServiceBase<Model>>();
		var expected = service.GetSubservice<GenericServiceBase<Model>.ChildService>();
		Assert.NotNull(expected);

		var nestedType = typeof(GenericServiceBase<Model>.ChildService);
		var declaringType = nestedType.DeclaringType.MakeGenericType(nestedType.GenericTypeArguments);

		Assert.Equal(typeof(GenericServiceBase<Model>), declaringType);
		Assert.Same(service, provider.GetService(declaringType));
		Assert.Same(expected, controller.Resolve());
	}

}

internal sealed class Model;
internal sealed class ChildModel;

internal class GenericServiceBase<TModel>(IServiceProvider serviceProvider) : DataServiceBase<TModel>(serviceProvider)
{
	public class ChildService(GenericServiceBase<TModel> service) : DataServiceBase<ChildModel>(service);
}

internal sealed class ConcreteService(IServiceProvider serviceProvider) : GenericServiceBase<Model>(serviceProvider);
internal sealed class ChildController : ServiceControllerBase<ChildModel, GenericServiceBase<Model>.ChildService>
{
	public GenericServiceBase<Model>.ChildService Resolve() => this.DataService;
}
