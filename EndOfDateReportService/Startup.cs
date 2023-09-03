using EndOfDateReportService.DataAccess;
using EndOfDateReportService.Services;
using Microsoft.EntityFrameworkCore;

namespace EndOfDateReportService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<BranchService>();
            
            
            services.AddHttpClient();
            services.AddControllers();
            services.AddSwaggerGen();
            services.AddDbContextPool<ReportContext>((provider, options) =>
            {
                options.UseNpgsql(Configuration.GetConnectionString("LocalConnection"));
            });
            
            services.AddScoped<Repository>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "API de BridgetIt V1"); });
            }


            app.UseRouting();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}