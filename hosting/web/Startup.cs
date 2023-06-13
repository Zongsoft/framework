using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Hosting.Web
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			this.Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddCors(options => options.AddDefaultPolicy(builder =>
				builder
				.AllowAnyHeader()
				.AllowAnyMethod()
				.AllowCredentials()
				.SetIsOriginAllowed(origin => true)));

			services
				.AddSignalR(options => { });

			services
				.AddControllers(options =>
				{
					options.InputFormatters.RemoveType<SystemTextJsonInputFormatter>();
					options.OutputFormatters.RemoveType<SystemTextJsonOutputFormatter>();

					options.InputFormatters.Add(new Zongsoft.Web.Formatters.JsonInputFormatter());
					options.OutputFormatters.Add(new Zongsoft.Web.Formatters.JsonOutputFormatter());

					options.Filters.Add(new Zongsoft.Web.Filters.ExceptionFilter());
					options.Conventions.Add(new Zongsoft.Web.Filters.GlobalFilterConvention());
					options.ModelBinderProviders.Insert(0, new Zongsoft.Web.Binders.RangeModelBinderProvider());
					options.ModelBinderProviders.Insert(0, new Zongsoft.Web.Binders.ComplexModelBinderProvider());
				});
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if(env.IsDevelopment())
				app.UseDeveloperExceptionPage();

			app.UseCors();
			app.UseHttpMethodOverride();
			app.UseRouting();
			app.UseAuthentication();
			app.UseAuthorization();
			app.UseStaticFiles();

			var initializers = app.ApplicationServices.GetServices<Zongsoft.Services.IApplicationInitializer<IApplicationBuilder>>();

			foreach(var initializer in initializers)
				initializer.Initialize(app);

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
				endpoints.Map("/", async ctx => ctx.Response.Redirect("/Application"));
			});
		}
	}
}
