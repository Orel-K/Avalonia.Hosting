using System;
using System.Threading.Tasks;
using Avalonia;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain (builder.Build) is called: things aren't initialized
        // yet and stuff might break.
        public static async Task Main(string[] args)
        {
            var builder = App.CreateBuilder();

            builder.Logging.AddConsole();

            var app = builder.Build();

            await app.RunAsync();
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
