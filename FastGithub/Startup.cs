using FastGithub.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace FastGithub
{
    /// <summary>
    /// ������
    /// </summary>
    public class Startup
    {
        public IConfiguration Configuration { get; }

        /// <summary>
        /// ������
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        /// <summary>
        /// ���÷���
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<FastGithubOptions>(this.Configuration.GetSection(nameof(FastGithub)));

            services.AddConfiguration();
            services.AddDomainResolve();
            services.AddHttpClient();
            services.AddReverseProxy();
            services.AddHostedService<AppHostedService>();

            if (OperatingSystem.IsWindows())
            {
                services.AddPacketIntercept();
            }
        }

        /// <summary>
        /// �����м��
        /// </summary>
        /// <param name="app"></param>
        public void Configure(IApplicationBuilder app)
        {
            var httpProxyPort = app.ApplicationServices.GetRequiredService<IOptions<FastGithubOptions>>().Value.HttpProxyPort;
            app.MapWhen(context => context.Connection.LocalPort == httpProxyPort, appBuilder =>
            {
                appBuilder.UseHttpProxy();
            });

            app.MapWhen(context => context.Connection.LocalPort != httpProxyPort, appBuilder =>
            {
                appBuilder.UseRequestLogging();
                appBuilder.UseHttpReverseProxy();

                appBuilder.UseRouting();
                appBuilder.UseEndpoints(endpoint => endpoint.MapFallback(context =>
                {
                    context.Response.Redirect("https://github.com/dotnetcore/fastgithub");
                    return Task.CompletedTask;
                }));
            });
        }
    }
}
