using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Parsers;
using AspNetCoreWebApi.Processing.Pooling;
using AspNetCoreWebApi.Processing.Printers;
using AspNetCoreWebApi.Storage;
using AspNetCoreWebApi.Storage.Contexts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetCoreWebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Latest);

            services.AddSingleton<RecommendProcessor>();
            services.AddSingleton<SuggestProcessor>();
            services.AddSingleton<NewAccountProcessor>();
            services.AddSingleton<EditAccountProcessor>();
            services.AddSingleton<NewLikesProcessor>();
            services.AddSingleton<FilterProcessor>();
            services.AddSingleton<GroupProcessor>();
            services.AddSingleton<MessageProcessor>();

            services.AddSingleton<DomainParser>();
            services.AddSingleton<Validator>();

            services.AddSingleton<MainContext>();
            services.AddSingleton<MainStorage>();

            services.AddSingleton<DataLoader>();
            services.AddSingleton<MainPool>();

            services.AddSingleton<AccountPrinter>();
            services.AddSingleton<GroupPrinter>();
            services.AddSingleton<RecommendPrinter>();
            services.AddSingleton<SuggestPrinter>();
            services.AddSingleton<GroupPreprocessor>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.Use(next => context =>
            {
                context.Response.OnStarting(() =>
                {
                    if (context.Response.StatusCode == 405)
                    {
                        context.Response.StatusCode = 404;
                    }

                    return Task.CompletedTask;
                });

                return next(context);
            });

            app.UseResponseBuffering();
            app.UseMvc();
        }
    }
}
