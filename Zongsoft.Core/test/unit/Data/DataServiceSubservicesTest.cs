using System;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Zongsoft.Data.Tests;

public class DataServiceSubservicesTest
{
	[Fact]
	public void GenericBaseNestedServiceIsDiscovered()
	{
		using var provider = new ServiceCollection().BuildServiceProvider();
		var service = new ConcreteService(provider);
		var subservice = service.GetSubservice<GenericServiceBase<Model>.ChildService>();

		Assert.NotNull(subservice);
		Assert.Same(service, subservice.Service);
		Assert.Same(subservice, service.GetSubservice(typeof(GenericServiceBase<Model>.ChildService)));
	}

	private sealed class Model;
	private sealed class ChildModel;

	private class GenericServiceBase<TModel>(IServiceProvider serviceProvider) : DataServiceBase<TModel>(serviceProvider)
	{
		public class ChildService(GenericServiceBase<TModel> service) : DataServiceBase<ChildModel>(service);
	}

	private sealed class ConcreteService(IServiceProvider serviceProvider) : GenericServiceBase<Model>(serviceProvider);
}
