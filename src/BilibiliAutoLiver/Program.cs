using System;
using System.Reflection;
using Bilibili.AspNetCore.Apis.DependencyInjection;
using BilibiliAutoLiver.Config;
using BilibiliAutoLiver.DependencyInjection;
using BilibiliAutoLiver.Services;
using BilibiliAutoLiver.Services.Interface;
using BilibiliAutoLiver.Plugin.Base;
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
            Console.Title = $"Bilibili����ֵ��ֱ������ v{Assembly.GetExecutingAssembly().GetName().Version} By withsalt(https://github.com/withsalt)";

            var builder = WebApplication.CreateBuilder(args);

            //Add NLog
            builder.Logging.ClearProviders();
            builder.Logging.AddNLogWeb();

            //���ó�ʼ��
            builder.Services.ConfigureSettings(builder);
            //����
            builder.Services.AddMemoryCache();
            //���Bilibili��صķ���
            builder.Services.AddBilibiliApis();
            //��ʱ����
            builder.Services.AddQuartz();
            //FFMpeg
            builder.Services.AddFFmpegService();
            //���
            builder.Services.AddPipePlugins();

            builder.Services.AddSingleton<IPushStreamServiceV1, PushStreamServiceV1>();
            builder.Services.AddSingleton<IPushStreamServiceV2, PushStreamServiceV2>();
            builder.Services.AddTransient<IStartupService, StartupService>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(GlobalConfigConstant.DefaultOriginsName, policy =>
                {
                    policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
                });
            });

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //����
            app.UseCors(GlobalConfigConstant.DefaultOriginsName);

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            IStartupService startService = app.Services.GetRequiredService<IStartupService>();
            app.Lifetime.ApplicationStarted.Register(() =>
            {
                startService.Start();
            });
            //��ʱ����
            app.Run();
        }
    }
}
