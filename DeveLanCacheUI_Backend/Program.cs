namespace DeveLanCacheUI_Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var deveLanCacheConfiguration = builder.Configuration.Get<DeveLanCacheConfiguration>()!;
            builder.Services.AddSingleton<DeveLanCacheConfiguration>(deveLanCacheConfiguration);

            // Add services to the container.
            var deveLanCacheUIDataDirectory = deveLanCacheConfiguration.DeveLanCacheUIDataDirectory;
            if (deveLanCacheUIDataDirectory != null)
            {
                Directory.CreateDirectory(deveLanCacheUIDataDirectory);
            }

            var conString = builder.Configuration.GetConnectionString("DefaultConnection");
            var conStringReplaced = conString?.Replace("{DeveLanCacheUIDataDirectory}", deveLanCacheUIDataDirectory ?? "");

            try
            {
                var sqliteFileName = SqliteFolderCreator.GetFileNameFromSqliteConnectionString(conStringReplaced);

                var invalidPathChars = Path.GetInvalidPathChars();
                if (!string.IsNullOrWhiteSpace(sqliteFileName) && sqliteFileName.All(t => !invalidPathChars.Any(z => t == z)))
                {
                    var parent = Path.GetDirectoryName(sqliteFileName);
                    if (!string.IsNullOrWhiteSpace(parent) && !Directory.Exists(parent))
                    {
                        Directory.CreateDirectory(parent);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while creating subfolder for database, exception: {ex}");
            }

            builder.Services.AddDbContext<DeveLanCacheUIDbContext>(options =>
            {
                options.UseSqlite(conStringReplaced);
            });
            //TODO make everythging use this
            //builder.Services.AddSingleton<DbContextFactory>();

            builder.Services.AddControllers(options =>
            {
                options.CacheProfiles.Add("ForeverCache",
                    new CacheProfile()
                    {
                        Duration = 31536000,
                        Location = ResponseCacheLocation.Any
                    });
            }).AddJsonOptions(x =>
                 x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            // Swagger
            builder.Services.AddControllers().AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //TODO this guy is using up a ton of ram at idle.  ~600mb
            builder.Services.AddHostedService<LanCacheLogReaderHostedService>();

            builder.Services.AddHttpClient();

            builder.Services.AddSingleton<RoboHashCache>();
            builder.Services.AddSingleton<SteamManifestService>();

            if (deveLanCacheConfiguration.UseDirectSteamIntegrationForDepots)
            {
                builder.Services.AddHostedService<SteamAppInfoService>();

                //TODO should probably initialize the Steam session here rather than inside the service
                builder.Services.AddSingleton<Steam3Session>();
                builder.Services.AddSingleton<AppInfoHandler>();
            }
            else
            {
                builder.Services.AddHostedService<SteamDepotEnricherHostedService>();
                builder.Services.AddHostedService<SteamDepotDownloaderHostedService>();
            }

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

            // Applying migrations
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();
                Console.WriteLine("Migrating DB (ensure the database folder from the query string exists)...");
                dbContext.Database.Migrate();
                Console.WriteLine("DB migration completed");
            }

            //Redirect to /swagger
            var option = new RewriteOptions();
            option.AddRedirect("^$", "swagger");
            app.UseRewriter(option);

            // Configure the HTTP request pipeline.
            app.UseSwagger();
            app.UseSwaggerUI();

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