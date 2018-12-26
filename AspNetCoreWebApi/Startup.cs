using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Storage;
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

            services.AddSingleton<CityStorage>();
            services.AddSingleton<InterestStorage>();
            services.AddSingleton<CountryStorage>();

            services.AddSingleton<IdStorage>();
            services.AddSingleton<PhoneHashStorage>();
            services.AddSingleton<EmailHashStorage>();

            services.AddSingleton<AccountParser>();
            services.AddSingleton<NewAccountProcessor>();
            services.AddSingleton<EditAccountProcessor>();
            services.AddSingleton<NewLikesProcessor>();
            services.AddSingleton<FilterProcessor>();
            services.AddSingleton<MessageProcessor>();

            services.AddDbContextPool<AccountContext>(options => { });
            services.AddScoped<DataLoader>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseResponseBuffering();
            app.UseMvc();
        }
    }
}
