using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Web;
using Zongsoft.Web.Security;
using Zongsoft.Plugins;
using Zongsoft.Security;

namespace Zongsoft.Hosting.Web
{
	internal class Program
	{
		public static void Main(string[] args)
		{
			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args)
		{
			return Host.CreateDefaultBuilder(args)
				.ConfigurePlugins<WebApplicationContext>(builder =>
				{
					builder.ConfigureServices(services =>
					{
						services.AddHttpContextAccessor();
						services.AddSingleton<IControllerActivator, ControllerActivator>();
						services.AddAuthentication(CredentialPrincipal.Scheme).AddCredentials();
					});
				})
				.ConfigureWebHostDefaults(builder =>
				{
					builder.UseStartup<Startup>();
				});
		}
	}
}
