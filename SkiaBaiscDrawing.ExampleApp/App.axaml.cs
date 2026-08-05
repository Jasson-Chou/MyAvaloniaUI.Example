using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using SkiaBasicDrawing.ExampleApp.Models;
using SkiaBasicDrawing.ExampleApp.ViewModels;
using SkiaBasicDrawing.ExampleApp.Views;

namespace SkiaBasicDrawing.ExampleApp
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
            services.AddTransient<IUiTimer, DefaultUiTimer>();
            services.AddTransient<IWaveformRunService, WaveformRunService>();
            services.AddTransient<IWaveformGenFactory, WaveformGenFactory>();
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
    }
}