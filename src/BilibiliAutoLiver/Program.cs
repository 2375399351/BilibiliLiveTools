using System;
using Bilibili.AspNetCore.Apis.DependencyInjection;
using Bilibili.AspNetCore.Apis.Providers;
using BilibiliAutoLiver.Config;
using BilibiliAutoLiver.DependencyInjection;
using BilibiliAutoLiver.Plugin.Base;
using BilibiliAutoLiver.Services;
using BilibiliAutoLiver.Services.Interface;
using BilibiliAutoLiver.Utils;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;

namespace BilibiliAutoLiver
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = $"Bilibili����ֵ��ֱ������ v{VersionHelper.GetVersion()} By withsalt(https://github.com/withsalt)";

            var builder = WebApplication.CreateBuilder(args);

            //Add NLog
            builder.Logging.ClearProviders();
            builder.Logging.AddNLogWeb();

            //���ó�ʼ��
            builder.Services.ConfigureSettings(builder);

            //Db
            builder.Services.AddDatabase();
            builder.Services.AddRepository();

            //Cookie�ִ��ṩ��
            builder.Services.AddSingleton<IBilibiliCookieRepositoryProvider, BilibiliCookieDbRepositoryProvider>();

            //����
            builder.Services.AddMemoryCache();
            //���Bilibili��صķ���
            builder.Services.AddBilibiliApis(false);
            //��ʱ����
            builder.Services.AddQuartz();
            //FFMpeg
            builder.Services.AddFFmpegService();
            //���
            builder.Services.AddPipePlugins();
            //�ʼ�����
            builder.Services.AddTransient<IEmailNoticeService, EmailNoticeService>();

            builder.Services.AddSingleton<IAdvancePushStreamService, AdvancePushStreamService>();
            builder.Services.AddSingleton<INormalPushStreamService, NormalPushStreamService>();
            builder.Services.AddSingleton<IPushStreamProxyService, PushStreamProxyService>();
            builder.Services.AddTransient<IStartupService, StartupService>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(GlobalConfigConstant.DEFAULT_ORIGINS_NAME, policy =>
                {
                    policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
                });
            });

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.ExpireTimeSpan = TimeSpan.FromDays(3650 * 10);
                    options.SlidingExpiration = true;
                    options.AccessDeniedPath = "/Account/Login";
                    options.LogoutPath = "/Account/Login";
                });

            // Add services to the container.
#if DEBUG
            builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
#else
            builder.Services.AddControllersWithViews();
#endif

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //����
            app.UseCors(GlobalConfigConstant.DEFAULT_ORIGINS_NAME);

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            //��ʼ�����ݿ�
            app.InitializeDatabase();

            app.Lifetime.ApplicationStarted.Register((obj, token)
                => app.Services.GetRequiredService<IStartupService>().Start(token), null);

            app.Run();
        }
    }
}
