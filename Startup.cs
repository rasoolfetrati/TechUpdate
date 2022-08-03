using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DomainClasses.Operator;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PersianTranslation.Identity;
using reCAPTCHA.AspNetCore;
using SendEmail;
using Services.ContactUs;
using TechUpdate.Core.Services.Comment;
using TechUpdate.Core.Services.ContactUs;
using TechUpdate.Core.Services.Groups;
using TechUpdate.Core.Services.News;
using TechUpdate.Core.Services.User;
using TechUpdate.DataLayer.Context;
using TechUpdate.DataLayer.Entities.Operator;

namespace TechUpdate
{
    public class Startup
    {
        public IConfiguration Configuration { get; set; }
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }
        public void ConfigureServices(IServiceCollection services)
        {
            #region IoC

            services.AddDbContext<TechUpdateContext>(options => options.UseSqlServer(Configuration["Data:TechUpdate_DB:ConnectionString"]));
            services.AddScoped<IGroupRepository, GroupRepository>();
            services.AddScoped<INewsRepository, NewsRepository>();
            services.AddScoped<IContactRepository, ContactRepository>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IViewRenderService, RenderViewToString>();
            services.AddScoped<ICommentRepository, CommentRepository>();
            services.AddTransient<IRecaptchaService, RecaptchaService>();
            #endregion

            #region Identity

            services.AddIdentity<Operator, IdentityRole>(option =>
            {
                option.SignIn.RequireConfirmedEmail = true;
                option.User.RequireUniqueEmail = true;
            })
                .AddEntityFrameworkStores<TechUpdateContext>()
                .AddDefaultTokenProviders()
                .AddErrorDescriber<PersianIdentityErrorDescriber>();

            #endregion

            #region Authentication

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
              .AddCookie(option =>
              {
                  option.AccessDeniedPath = "/AccessDenied";
                  option.LoginPath = "/Account/Login";
                  option.LogoutPath = "/Account/Logout";
                  option.Cookie.Name = "Cookie";
                  option.ExpireTimeSpan = TimeSpan.FromDays(10);
                  option.Cookie.HttpOnly = true;
                  _ = option.Cookie.SecurePolicy == CookieSecurePolicy.Always;
              }).AddGoogle(option =>
              {
                  option.ClientId = "771168227112-56uivi771l92nic08ikdf80b390gqieg.apps.googleusercontent.com";
                  option.ClientSecret = "wWdEnbjNYUOgVYIbir_ntrcE";
              });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireClaim("Admins"));
            });
            services.AddRecaptcha(options =>
            {
                options.SiteKey = "6LdgOqAbAAAAAGOmjSQzjMDlgTISsTW2S8SFCoXL";
                options.SecretKey = "6LdgOqAbAAAAAJ_4zk6LyFvCRA0xxRxT9MShrQXC";
            });
            #endregion
            services.AddControllersWithViews();
        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseStatusCodePages();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "areas",
                    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
                );
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            try
            {
                TechUpdateContext.CreateAdminAccount(app.ApplicationServices, Configuration).Wait();
            }
            catch (AggregateException e)
            {
                foreach (var errInner in e.InnerExceptions)
                {
                    Debug.WriteLine(errInner);
                }
            }
        }
    }
}
