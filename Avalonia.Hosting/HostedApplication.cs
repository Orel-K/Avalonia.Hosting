using System.Diagnostics;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Avalonia.Hosting;

public abstract class HostedApplication<T> : Application, IHostedService, IDisposable where T : HostedApplication<T>, new()
{
    public sealed class AvaloniaApplicationBuilder<TH> where TH : HostedApplication<TH>, new()
    {
        readonly HostApplicationBuilder _hostBuilder;
        public IServiceCollection Services => _hostBuilder.Services;
        public ILoggingBuilder Logging => _hostBuilder.Logging;
        public IConfigurationBuilder Configuration => _hostBuilder.Configuration;

        readonly AppBuilder _appBuilder;

        public AvaloniaApplicationBuilder()
        {
            _hostBuilder = Host.CreateApplicationBuilder();


            _appBuilder = AppBuilder.Configure<TH>()
              .UsePlatformDetect()
              .WithInterFont()
              .LogToTrace();
        }

        public TH Build()
        {
            _appBuilder.SetupWithClassicDesktopLifetime([], x => x.ShutdownMode = Controls.ShutdownMode.OnMainWindowClose);

            _hostBuilder.Services.AddHostedService<TH>(x => (TH)Application.Current!);

            IHost host = _hostBuilder.Build();

            var app = host.Services.GetRequiredService<TH>();

            app.Services = host.Services;

            return app;
        }
    }
    public IServiceProvider Services { get; private set; } = default!;

    public static AvaloniaApplicationBuilder<T> CreateBuilder()
    {
        Thread.CurrentThread.TrySetApartmentState(ApartmentState.Unknown);
        Thread.CurrentThread.TrySetApartmentState(ApartmentState.STA);

        var builder = new AvaloniaApplicationBuilder<T>();

        builder.Services.AddSingleton<T>(x => (T)Application.Current!);

        return builder;
    }

    public async Task RunAsync()
    {
        Dispatcher.UIThread.VerifyAccess();

        var lifeTime = (ClassicDesktopStyleApplicationLifetime)this.ApplicationLifetime!;

        var hostLifeTime = Services.GetRequiredService<IHostApplicationLifetime>();

        var host = Services.GetRequiredService<IHost>();

        lifeTime.Startup += async delegate
        {
            // cpu bounded, startup will be called from the dispatcher
            await Task.Run(async () =>
            {
                await host.StartAsync(hostLifeTime.ApplicationStopping).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            });
        };

        using var _ = hostLifeTime.ApplicationStopping.Register(() =>
        {
            Dispatcher.UIThread.Invoke(delegate { lifeTime.Shutdown(); });
        });

        lifeTime.ShutdownRequested += async delegate
        {
            await host.StopAsync(CancellationToken.None).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        };

        Environment.ExitCode = lifeTime.Start();

        try
        {
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

    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        Debugger.Break();
        return Task.CompletedTask;
    }

    public virtual void Dispose()
    {
    }

    public virtual Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}


