using System.Diagnostics;
using System.Globalization;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Avalonia.Hosting;

public abstract class HostedApplication : Application
{
    public new static HostedApplication Current => (HostedApplication)Application.Current!;
    // App Services cannot be called before `Build` was called
    public IServiceProvider Services { get; protected set; } = null!;
}

public abstract class HostedApplication<T> : HostedApplication, IHostedService
    where T : HostedApplication<T>, new()
{
    public sealed class AvaloniaApplicationBuilder<TH> where TH : HostedApplication<TH>, new()
    {
        public IServiceCollection Services => _hostBuilder.Services;
        public ILoggingBuilder Logging => _hostBuilder.Logging;
        public IConfigurationManager Configuration => _hostBuilder.Configuration;

        readonly AppBuilder _appBuilder;
        readonly HostApplicationBuilder _hostBuilder;
        readonly Action<IClassicDesktopStyleApplicationLifetime> _lifeTimeConfigure;
        readonly string[] _args;

        public AvaloniaApplicationBuilder(string[] args,
            Action<IClassicDesktopStyleApplicationLifetime> lifeTimeConfigure)
        {
            _args = args;
            _lifeTimeConfigure = lifeTimeConfigure;
            _hostBuilder = Host.CreateApplicationBuilder();

            _appBuilder = AppBuilder.Configure<TH>()
                .UsePlatformDetect();
        }

        public TH Build(ServiceProviderOptions? serviceProviderOptions = null)
        {
            serviceProviderOptions ??= new ServiceProviderOptions();

            _appBuilder.SetupWithClassicDesktopLifetime(_args, _lifeTimeConfigure);

            _hostBuilder.ConfigureContainer(new DefaultServiceProviderFactory(serviceProviderOptions));

            _hostBuilder.Services.AddHostedService<TH>(x => (TH)Application.Current!);

            IHost host = _hostBuilder.Build();

            var app = host.Services.GetRequiredService<TH>();

            app.Services = host.Services;

            return app;
        }
    }

    public new static T Current => (T)HostedApplication.Current;

    public static AvaloniaApplicationBuilder<T> CreateBuilder(string[]? args = null,
        Action<IClassicDesktopStyleApplicationLifetime>? lifeTimeConfigure = null)
    {

        args ??= Environment.GetCommandLineArgs().Skip(1).ToArray();

        lifeTimeConfigure ??= (x) => { };

        var builder = new AvaloniaApplicationBuilder<T>(args, lifeTimeConfigure);

        builder.Services.AddSingleton<T>(x => (T)Application.Current!);

        return builder;
    }

    async Task HostMain()
    {
        var hostLifeTime = Services.GetRequiredService<IHostApplicationLifetime>();

        var host = Services.GetRequiredService<IHost>();

        try
        {
            await host.StartAsync(hostLifeTime.ApplicationStopping);

            await host.WaitForShutdownAsync(hostLifeTime.ApplicationStopping).ConfigureAwait(false);
        }
        finally
        {
            if (host is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                host.Dispose();
            }
        }
    }

    public int Run()
    {
        Dispatcher.UIThread.VerifyAccess();

        var logger = Services.GetRequiredService<ILogger<HostedApplication<T>>>();

        var appLifeTime = (ClassicDesktopStyleApplicationLifetime)this.ApplicationLifetime!;

        var hostLifeTime = Services.GetRequiredService<IHostApplicationLifetime>();

        uint sessionId = (uint)Process.GetCurrentProcess().SessionId;

        var host = Services.GetRequiredService<IHost>();

        appLifeTime.Startup += async delegate
        {
            await HostMain().ConfigureAwait(false);

            // `Shutdown` and not `TryShutdown`, not cancelable
            Dispatcher.UIThread.Invoke(delegate { appLifeTime.Shutdown(); });
        };

        appLifeTime.ShutdownRequested += (a, b) =>
        {
            b.Cancel = true;
            hostLifeTime.StopApplication();
        };

        return Environment.ExitCode = appLifeTime.Start();
    }

    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public virtual Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
