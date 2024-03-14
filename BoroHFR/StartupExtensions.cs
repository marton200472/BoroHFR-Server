using BoroHFR.Controllers;
using BoroHFR.Data;
using BoroHFR.Helpers;
using BoroHFR.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace BoroHFR
{
    public static class StartupExtensions
    {
        public static WebApplicationBuilder ConfigureSetUpMode(this WebApplicationBuilder builder)
        {
            if (!File.Exists("./storage/appsettings.json"))
            {
                Directory.CreateDirectory("storage");
                File.WriteAllText("./storage/appsettings.json", "{}");
            }

            builder.Configuration["Jwt:Key"] ??= RandomNumberGenerator.GetHexString(128);
            builder.Configuration["Jwt:Audience"] ??= "";
            builder.Configuration["Jwt:Issuer"] ??= "";
            builder.Configuration["FileStorageDir"] ??= "./storage/files";

            if (builder.Environment.IsEnvironment("Docker")) {
                builder.Configuration["ConnectionStrings:Default"] = "Server=mariadb-server; Port=3306; User=borohfr; Database=borohfr; Password=borohfr";
            }

            builder.Configuration["Logging:LogLevel:Default"] ??= "Information";
            builder.Configuration["Logging:LogLevel:Microsoft"] ??= "Warning";
            builder.Configuration["Logging:LogLevel:Microsoft.Hosting.Lifetime"] ??= "Information";

            builder.Services.AddControllersWithViews().UseSpecificControllers(typeof(SetupController));


            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, x =>
                {
                    x.LoginPath = "/authentication/login";
                    x.AccessDeniedPath = "/authentication/login";
                    x.Cookie.Name = "ChocolateBiscuit";
                    x.SlidingExpiration = true;
                    x.Cookie.IsEssential = true;
                    x.Cookie.SameSite = SameSiteMode.Strict;
                });

            builder.Services.AddAuthorization(opt =>
            {
                opt.AddPolicy("SysAdmin", p =>
                {
                    p.AddAuthenticationSchemes("Cookies");
                    p.RequireAuthenticatedUser();
                    p.RequireRole("SysAdmin");
                });
            });

            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            return builder;
        }

        public static WebApplicationBuilder ConfigureNormalMode(this WebApplicationBuilder builder)
        {
            builder.Services.AddControllersWithViews().ExcludeSpecificControllers(typeof(SetupController));

            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen(x =>
                {
                    x.DocumentFilter<BoroHFR.Helpers.SwaggerControllerFilter>();
                });
            }


            builder.Services.AddDbContext<BoroHfrDbContext>(c =>
            {
                string connStr =  builder.Configuration.GetConnectionString("Default")!;
                c.UseMySql(connStr, ServerVersion.AutoDetect(connStr)!);
            });


            builder.Services.AddSignalR();

            builder.Services.AddSingleton<EmailService>();
            builder.Services.AddHostedService<EmailSenderBackgroundService>();

            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            builder.Services.AddDataProtection()
                .PersistKeysToDbContext<BoroHfrDbContext>();

            builder.Services.AddDataProtection().UseCryptographicAlgorithms(
                new AuthenticatedEncryptorConfiguration
                {
                    EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
                    ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
                });
            


            //check for attachment storage errors
            string? rootPath = builder.Configuration["FileStorageDir"];
            if (rootPath is null)
            {
                throw new Exception("FileStorageDir is not set in config!");
            }
            else if (!Directory.Exists(rootPath))
            {
                try
                {
                    Directory.CreateDirectory(rootPath);
                }
                catch (Exception)
                {
                    throw new Exception(String.Format("The directory set in config FileStorageDir ({0}) does not exist, and couldn't be created.", rootPath));
                }

            }
            if (!FileStorageHandler.IsDirectoryWritable(rootPath))
            {
                throw new Exception(String.Format("The directory set in config FileStorageDir ({0}) is not writable.", rootPath));
            }
            builder.Services.AddSingleton<FileStorageHandler>();

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, x =>
                {
                    x.LoginPath = "/authentication/login";
                    x.AccessDeniedPath = "/authentication/login";
                    x.Cookie.Name = "ChocolateBiscuit";
                    x.SlidingExpiration = true;
                    x.Cookie.IsEssential = true;
                    x.Cookie.SameSite = SameSiteMode.Strict;
                })
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, x =>
                {
                    x.RequireHttpsMetadata = true;
                    //x.SaveToken = true;
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                    };
                });

            builder.Services.AddAuthorization(opt =>
            {
                opt.AddPolicy("User", p =>
                {
                    p.AddAuthenticationSchemes("Bearer", "Cookies");
                    p.RequireAuthenticatedUser();
                    p.RequireRole("User", "Admin");
                });

                opt.AddPolicy("Admin", p =>
                {
                    p.AddAuthenticationSchemes("Bearer", "Cookies");
                    p.RequireAuthenticatedUser();
                    p.RequireRole("Admin");
                });

                opt.AddPolicy("SysAdmin", p =>
                {
                    p.AddAuthenticationSchemes("Cookies");
                    p.RequireAuthenticatedUser();
                    p.RequireRole("SysAdmin");
                });
            });

            return builder;
        }


        public static WebApplication AddSetupPipeline(this WebApplication app)
        {
            app.UseStaticFiles();

            app.UseRouting();

            app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Setup}/{action=Index}");

            return app;
        }

        public static WebApplication AddNormalPipeline(this WebApplication app)
        {
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }


            app.UseStaticFiles();

            app.UseRouting();


            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}");
            app.MapHub<BoroHFR.Hubs.ChatHub>("/hubs/chat");

            using (IServiceScope scope = app.Services.CreateScope())
            {
                var db = (BoroHfrDbContext)scope.ServiceProvider.GetRequiredService(typeof(BoroHfrDbContext));
                db.Database.Migrate();
            }

            return app;
        }
    }
}
