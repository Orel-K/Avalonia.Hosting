using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Hosting;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SampleApp;

public partial class App : HostedApplication<App>
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    // any non dispatcher related async init stuff
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var logger = Services.GetRequiredService<ILogger<App>>();

        logger.LogInformation("StartAsync");

        return Task.CompletedTask;
    }

    // any non dispatcher related async fini stuff
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        var logger = Services.GetRequiredService<ILogger<App>>();

        logger.LogInformation("StopAsync, press any key to stop the application");

        return Task.CompletedTask;
    }
}