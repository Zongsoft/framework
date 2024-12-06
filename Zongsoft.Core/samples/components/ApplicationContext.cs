using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Components.Samples;

public sealed class ApplicationContext : Zongsoft.Services.ApplicationContext
{
	public ApplicationContext() : base((new Zongsoft.Services.ServiceProviderFactory()).CreateServiceProvider(new ServiceCollection()))
	{
		this.Modules.Add(Module.Current);
	}
}