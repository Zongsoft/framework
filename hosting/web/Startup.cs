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
				.AllowAnyOrigin()));

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
					options.ModelBinderProviders.Insert(0, new Zongsoft.Web.Binders.RangeModelBinderProvider());
				});
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if(env.IsDevelopment())
				app.UseDeveloperExceptionPage();

			app.UseCors();
			app.UseRouting();
			app.UseAuthentication();
			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
				endpoints.Map("/", async ctx => ctx.Response.Redirect("/Application"));
			});
		}
	}
}
