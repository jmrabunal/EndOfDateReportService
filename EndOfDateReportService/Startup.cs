using EndOfDateReportService.DataAccess;
using EndOfDateReportService.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

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
            
            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = long.MaxValue; 
            });

            services.AddCors();
            services.AddScoped<BranchService>();
            
            services.AddHttpClient();
            services.AddControllers();
            services.AddSwaggerGen();
            services.AddDbContextPool<ReportContext>((provider, options) =>
            {
                options.UseNpgsql(Configuration.GetConnectionString("LocalConnection"));
            });
            services.AddScoped<Repository>();
            services.AddAutoMapper(typeof(AutoMapper));
            services.AddScoped<PdfService>();

            services.AddScoped<ExcelService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.RoutePrefix = "api"; c.SwaggerEndpoint("/swagger/v1/swagger.json", "API de BridgetIt V1"); });
            }

            app.UseCors(builder =>
            {
                builder.WithOrigins(Configuration.GetValue<string>("ip"))
                       .AllowAnyMethod()
                       .AllowAnyHeader().AllowCredentials();

            });

            app.UseCors("AllowSpecificOrigin");

            app.UseRouting();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}