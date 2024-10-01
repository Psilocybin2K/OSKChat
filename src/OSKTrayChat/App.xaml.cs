using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OSKTrayChat.Agents;
using OSKTrayChat.Services;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;

namespace OSKTrayChat
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    [Experimental("SKEXP0010")]
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }
        public IConfiguration Configuration { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            //mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Add logging
            services.AddLogging(configure => configure.AddConsole());

            // Add configuration
            services.AddSingleton(Configuration);
            services.AddSingleton<IConfigurationService, ConfigurationService>();

            // Add application services
            services.AddSingleton<ITestCaseWritingAgent, TestCaseWritingAgent>();
            services.AddSingleton<TestCaseWritingFunctions>();
            services.AddSingleton<TestCaseInvocationFilter>();

            // Add main window
            services.AddTransient<MainWindow>();
        }
    }

}
