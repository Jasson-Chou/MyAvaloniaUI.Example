using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Border.ExampleApp.Models;
using Border.ExampleApp.Services;
using Border.ExampleApp.ViewModels;
using Border.ExampleApp.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Border.ExampleApp
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var services = new ServiceCollection();
            services.AddTransient<IBorderSettingService, BorderSettingSerializerModel>();
            services.AddTransient<IDialogService>(provider => new DefaultDialogService(() => GetDesktopWindow()));
            services.AddTransient<IClipboardService>(provider => new ClipboardService(() => GetDesktopWindow()));
            services.AddSingleton<MainViewModel>();

            Ioc.Default.ConfigureServices(services.BuildServiceProvider());
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = Ioc.Default.GetRequiredService<MainViewModel>(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private Window? GetDesktopWindow()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }
            return null;
        }
    }
}