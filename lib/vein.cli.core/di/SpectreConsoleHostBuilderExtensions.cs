namespace vein.cli;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using ITypeResolver = Spectre.Console.Cli.ITypeResolver;

public static class SpectreConsoleHostBuilderExtensions
{
    public static IHostBuilder UseSpectreConsole(this IHostBuilder builder, Action<IConfigurator> configureCommandApp)
    {
        builder = builder ?? throw new ArgumentNullException(nameof(builder));

        builder.ConfigureServices((_, collection) =>
            {
                var command = new CommandApp(new TypeRegistrar(collection));
                command.Configure(configureCommandApp);
                collection.AddSingleton<ICommandApp>(command);
                collection.AddHostedService<SpectreConsoleWorker>();
            }
        );

        return builder;
    }

    public static IHostBuilder UseSpectreConsole<TDefaultCommand>(this IHostBuilder builder,
        Action<IConfigurator>? configureCommandApp = null)
        where TDefaultCommand : class, ICommand
    {
        builder = builder ?? throw new ArgumentNullException(nameof(builder));

        builder.ConfigureServices((_, collection) =>
            {
                var command = new CommandApp<TDefaultCommand>(new TypeRegistrar(collection));
                if (configureCommandApp != null)
                {
                    command.Configure(configureCommandApp);
                }

                collection.AddSingleton<ICommandApp>(command);
                collection.AddHostedService<SpectreConsoleWorker>();
            }
        );

        return builder;
    }
}
public sealed class TypeResolver(IServiceProvider serviceProvider) : ITypeResolver, IDisposable
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public void Dispose()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public object? Resolve(Type? type)
    {
        if (type == null)
        {
            return null;
        }

        return _serviceProvider.GetService(type) ?? Activator.CreateInstance(type);
    }
}
public sealed class TypeRegistrar(IServiceCollection builder) : ITypeRegistrar
{
    public ITypeResolver Build() => new TypeResolver(builder.BuildServiceProvider());

    public void Register(Type service, Type implementation) => builder.AddSingleton(service, implementation);

    public void RegisterInstance(Type service, object implementation) => builder.AddSingleton(service, implementation);

    public void RegisterLazy(Type service, Func<object> func)
    {
        if (func is null)
            throw new ArgumentNullException(nameof(func));

        builder.AddSingleton(service, _ => func());
    }
}

public class SpectreConsoleWorker(
    ILogger<SpectreConsoleWorker> logger,
    ICommandApp commandApp,
    IHostApplicationLifetime hostLifetime)
    : IHostedService
{
    private int _exitCode;

    public Task StartAsync(CancellationToken cancellationToken) =>
        Task.Factory.StartNew(async () => {
            try
            {
                var args = GetArgs();
                await Task.Delay(100, cancellationToken);
                _exitCode = await commandApp.RunAsync(args);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred");
                _exitCode = 1;
            }
            finally
            {
                hostLifetime.StopApplication();
            }
        }, cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Environment.ExitCode = _exitCode;
        return Task.CompletedTask;
    }

    private static string[] GetArgs() => Environment.GetCommandLineArgs().Skip(1).Where(x => !x.StartsWith("+")).ToArray();
}
