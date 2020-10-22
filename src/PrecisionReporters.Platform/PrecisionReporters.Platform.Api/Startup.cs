using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Api.Mappers;
using PrecisionReporters.Platform.Data;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;

namespace PrecisionReporters.Platform.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        readonly string AllowedOrigins = "_AllowedOrigins";

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            //TODO get origins from config file
            services.AddCors(options =>
            {
                options.AddPolicy(name: AllowedOrigins,
                builder =>
                    {
                        builder.WithOrigins("http://localhost:3000");
                        builder.AllowAnyHeader();
                    });
            });

            services.AddControllers();

            // Register the Swagger generator, defining our Swagger documents
            services.AddSwaggerGen();

            services.AddHealthChecks();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySQL($"{Configuration.GetConnectionString("MySqlConnection")}"));

            // Mappers
            services.AddSingleton<IMapper<Case, CaseDto, CreateCaseDto>, CaseMapper>();
            services.AddSingleton<IMapper<Room, RoomDto, CreateRoomDto>, RoomMapper>();

            // Services
            services.AddScoped<ICaseService, CaseService>();
            services.Configure<TwilioAccountConfiguration>(Configuration.GetSection(TwilioAccountConfiguration.SectionName));
            services.AddScoped<ITwilioService, TwilioService>();
            services.AddScoped<IRoomService, RoomService>();

            // Repositories
            services.AddScoped<ICaseRepository, CaseRepository>();
            services.AddScoped<IRoomRepository, RoomRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // TODO: use configuration flag instead
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui, specifying the Swagger JSON endpoint.
            // TODO: use configuration flag for enabling/disabling swagger UI
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Precision Reporters API v1");
            });

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors(AllowedOrigins);

            app.UseAuthorization();

            app.UseHealthChecks("/healthcheck");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
