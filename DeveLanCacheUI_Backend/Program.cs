
using DeveLanCacheUI_Backend.Db;
using DeveLanCacheUI_Backend.Hubs;
using DeveLanCacheUI_Backend.LogReading;
using DeveLanCacheUI_Backend.Steam;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace DeveLanCacheUI_Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddDbContext<DeveLanCacheUIDbContext>(options =>
            {
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            builder.Services.AddControllers().AddJsonOptions(x =>
                 x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //I don't get why but this is not working
            //builder.Services.AddHttpClient<SteamDepotDownloaderHostedService>(client =>
            //{
            //    client.DefaultRequestHeaders.Add("User-Agent", "request");
            //});

            //builder.Services.AddHttpClient<SteamDepotDownloaderHostedService>();
            builder.Services.AddHostedService<LanCacheLogReaderHostedService>();
            builder.Services.AddHostedService<SteamDepotEnricherHostedService>();
            builder.Services.AddHostedService<SteamDepotDownloaderHostedService>();

            builder.Services.AddSignalR();
            builder.Services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                   new[] { "application/octet-stream" });
            });

            // Configure CORS policy
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                    });
            });

            var app = builder.Build();

            app.UseResponseCompression();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();
                Console.WriteLine("Migrating DB...");
                dbContext.Database.Migrate();
                Console.WriteLine("DB migration completed");
            }


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // Use the CORS policy
            app.UseCors();

            app.UseAuthorization();

            app.MapControllers();

            app.MapHub<ChatHub>("/chathub");
            app.MapHub<LanCacheHub>("/lancachehub");

            app.Run();
        }
    }
}