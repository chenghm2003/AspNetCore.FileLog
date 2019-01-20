using AspNetCore.FileLog;
using AspNetCore.FileLog.Factory;
using AspNetCore.FileLog.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HostingEnvironmentExtensions = Microsoft.AspNetCore.Hosting.Internal.HostingEnvironmentExtensions;
using HtmlDirectoryFormatter = AspNetCore.FileLog.HtmlDirectoryFormatter;
using LOG = AspNetCore.FileLog;
using LoggerFactory = AspNetCore.FileLog.LoggerFactory;
using LoggerFilterOptions = AspNetCore.FileLog.LoggerFilterOptions;

namespace Microsoft.Extensions.DependencyInjection
{

    public static class LoggerExtensions
    {
        /// <summary>
        /// set file format for logs
        /// </summary>
        /// <param name="services"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static IServiceCollection SetFileLogFormat(this IServiceCollection services, Format format)
        {
            LoggerSettings.Format = format;
            return services;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="services"></param>
        /// <param name="logAction">logAction</param>
        /// <para>e.g. .Logs or wwwroor/logs or C:/wwwroot/logs</para>
        public static IServiceCollection AddFileLog(this IServiceCollection services, Action<LogOptions> logAction = null)
        {
            bool flag = !services.Any((ServiceDescriptor x) => x.ImplementationType == typeof(LoggerFactory));
            if (flag)
            {
                LogOptions logOptions = new LogOptions();
                if (logAction != null)
                {
                    logAction(logOptions);
                }
                services.Replace(ServiceDescriptor.Transient<IApplicationBuilderFactory, DefaultApplicationBuilderFactory>());
                LoggerSettings.Format = logOptions.Format;
                LoggerFactory.ServiceCollection = services;
                bool flag2 = string.IsNullOrEmpty(logOptions.LogDirectory);
                if (flag2)
                {
                    logOptions.LogDirectory = ".Logs";
                }
                services.AddHttpContextAccessor();
                services.AddSingleton<ILogAdapter, FileLogAdapter>();
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                Console.OutputEncoding = Encoding.UTF8;
                ServiceDescriptor serviceDescriptor = services.FirstOrDefault((ServiceDescriptor x) => x.ServiceType == typeof(IConfiguration));
                IConfiguration _config = ((serviceDescriptor != null) ? serviceDescriptor.ImplementationInstance : null) as IConfiguration;
                bool flag3 = _config == null;
                if (flag3)
                {
                    _config = FileConfigurationExtensions.SetBasePath(EnvironmentVariablesExtensions.AddEnvironmentVariables(new ConfigurationBuilder(), "ASPNETCORE_"), AppContext.BaseDirectory).Build();
                    _config[WebHostDefaults.ContentRootKey] = AppContext.BaseDirectory;
                    services.AddSingleton(_config);
                }
                bool flag4 = string.IsNullOrEmpty(_config[WebHostDefaults.ContentRootKey]);
                if (flag4)
                {
                    _config[WebHostDefaults.ContentRootKey] = AppContext.BaseDirectory;
                }
                ServiceDescriptor serviceDescriptor2 = services.FirstOrDefault((ServiceDescriptor x) => x.ServiceType == typeof(IHostingEnvironment));
                IHostingEnvironment environment = (IHostingEnvironment)((serviceDescriptor2 != null) ? serviceDescriptor2.ImplementationInstance : null);
                bool flag5 = environment == null;
                if (flag5)
                {
                    WebHostOptions options = new WebHostOptions(_config, Assembly.GetEntryAssembly().GetName().Name);
                    environment = new HostingEnvironment();
                    HostingEnvironmentExtensions.Initialize(environment, AppContext.BaseDirectory, options);
                    services.TryAddSingleton(environment);
                }
                bool flag6 = string.IsNullOrEmpty(environment.WebRootPath);
                if (flag6)
                {
                    string _contentPath = _config[WebHostDefaults.ContentRootKey];
                    int binIndex = _contentPath.LastIndexOf("\\bin\\");
                    bool flag7 = binIndex > -1;
                    if (flag7)
                    {
                        string contentPath = _contentPath.Substring(0, binIndex);
                        bool flag8 = contentPath.IndexOf(environment.ApplicationName) > -1;
                        if (flag8)
                        {
                            _config[WebHostDefaults.ContentRootKey] = contentPath;
                            environment.ContentRootPath = contentPath;
                            environment.WebRootPath = Path.Combine(contentPath, "wwwroot");
                        }
                        else
                        {
                            environment.WebRootPath = _contentPath;
                        }
                    }
                    else
                    {
                        environment.WebRootPath = Path.Combine(_config["ContentRoot"], "wwwroot");
                    }
                }
                bool flag9 = Path.IsPathRooted(logOptions.LogDirectory);
                if (flag9)
                {
                    LoggerSettings.LogDirectory = logOptions.LogDirectory;
                }
                else
                {
                    LoggerSettings.LogDirectory = Path.Combine(_config["ContentRoot"], logOptions.LogDirectory);
                }
                bool flag10 = !Directory.Exists(LoggerSettings.LogDirectory);
                if (flag10)
                {
                    Directory.CreateDirectory(LoggerSettings.LogDirectory);
                }
                string path = Path.Combine(LoggerSettings.LogDirectory, "_logging.json");
                bool flag11 = !File.Exists(path);
                if (flag11)
                {
                    File.AppendAllText(path, "{\"Rules\":[{\"CategoryName\":\"Default\",\"LogLevel\":\"Information\",\"LogType\":\"All\",\"TraceCount\":5}]}");
                }
                ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
                JsonConfigurationExtensions.AddJsonFile(FileConfigurationExtensions.SetBasePath(configurationBuilder, environment.ContentRootPath), path, true, true);
                IConfigurationRoot configuration = configurationBuilder.Build();
                services.RemoveAll<ILoggerProviderConfigurationFactory>();
                services.RemoveAll(typeof(ILoggerProviderConfiguration<>));
                TypeInfo type = typeof(ILoggerProviderConfigurationFactory).Assembly.DefinedTypes.SingleOrDefault((TypeInfo t) => t.Name == "LoggingConfiguration");
                services.RemoveAll(type);                

                LoggingServiceCollectionExtensions.AddLogging(services, delegate (ILoggingBuilder x)
                {
                    x.AddConfiguration(configuration);
                    //bool flag13 = !x.Services.Any((ServiceDescriptor t) => t.ServiceType == typeof(ILoggerProvider));
                    //if (flag13)
                    //{
                    //    ConsoleLoggerExtensions.AddConsole(x);
                    //}
                    x.Services.RemoveAll<IConfigureOptions<LoggerFilterOptions>>();
                    x.Services.AddSingleton(new LoggerFilterConfigureOptions(configuration));
                });
                services.Replace(ServiceDescriptor.Singleton<ILoggerFactory, LoggerFactory>());
                services.Replace(ServiceDescriptor.Singleton<DiagnosticSource>(new DefaultDiagnosticListener()));
                bool flag12 = services.IsHttpRequest();
                if (flag12)
                {
                    MarkdownFileMiddleware.SaveResourceFiles(environment.WebRootPath);
                }
                DefaultApplicationBuilderFactory.OnCreateBuilder(new Action<IApplicationBuilder, object>(LoggerExtensions.UseFileLog), logOptions);
            }
            return services;
        }
        
        private static void UseFileLog(IApplicationBuilder app, object state)
        {
            LogOptions logOptions = state as LogOptions;
            LoggerFactory.ServiceProvider = app.ApplicationServices;
            bool flag = app.ApplicationServices.GetService<ILoggerFactory>().GetType() != typeof(LoggerFactory);
            if (flag)
            {
                throw new NotImplementedException("Please use IServiceCollection.AddFileLog first.");
            }
            LoggerSettings.LogRequestPath = logOptions.LogRequestPath;
            LoggerSettings.SettingsPath = logOptions.SettingsPath;
            bool flag2 = string.IsNullOrEmpty(LoggerSettings.LogRequestPath);
            if (flag2)
            {
                LoggerSettings.LogRequestPath = "/_Logs_/";
            }
            bool flag3 = string.IsNullOrEmpty(LoggerSettings.SettingsPath);
            if (flag3)
            {
                LoggerSettings.SettingsPath = "/_Settings_";
            }
            FileServerOptions fileOption = new FileServerOptions
            {
                EnableDirectoryBrowsing = true,
                RequestPath = LoggerSettings.LogRequestPath,
                FileProvider = new PhysicalFileProvider(LoggerSettings.LogDirectory)
            };
            fileOption.StaticFileOptions.OnPrepareResponse = new Action<StaticFileResponseContext>(LoggerExtensions.PrepareResponse);
            fileOption.DirectoryBrowserOptions.Formatter = new HtmlDirectoryFormatter();
            FileServerExtensions.UseFileServer(app, fileOption);
           
        }
        
        private static bool IsHttpRequest(this IServiceCollection services)
        {
            return services.Any((ServiceDescriptor x) => x.ServiceType == typeof(IServer));
        }
        
        private static void PrepareResponse(StaticFileResponseContext context)
        {
            bool flag = LoggerExtensions.ContentTypes.Contains(context.Context.Response.ContentType);
            if (flag)
            {
                HttpResponse response = context.Context.Response;
                response.ContentType += "; charset=utf-8";
            }
        }

        internal static readonly List<string> ContentTypes = new List<string>
        {
            "text/plain",
            "text/css",
            "text/javascript",
            "text/json",
            "text/html"
        };
    }
}
