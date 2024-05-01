using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Events;

namespace TripPin.CLI;

class Program
{
    private static readonly List<string> MenuOptions =
    [
        "1. Search People",
        "2. Get User Details",
        "3. Modify User Details",
        "4. Quit CLI"
    ];

    private static IPeopleService _peopleService;

    static async Task Main(string[] args)
    {
        var host = CreateDefaultApp();

        _peopleService = host.Services.GetService<IPeopleService>()!;

        Console.WriteLine("==================================");
        Console.WriteLine("  Welcome to TripPin CLI Client  ");
        Console.WriteLine(" (a Jibble Group tech assessment) ");
        Console.WriteLine("==================================");
        Console.WriteLine();
        
        var shouldContinue = await RenderMainMenu();
        while (shouldContinue)
        {
            shouldContinue = await RenderMainMenu();
        }
    }

    private static async Task<bool> RenderMainMenu()
    {
        Console.WriteLine("\nPlease select one of the following options:");
        foreach (var option in MenuOptions)
        {
            Console.WriteLine(option);
        }

        var keyValue = 0;

        Console.Write("Your action:");
        var key = Console.ReadKey(true);
        while (!int.TryParse(key.KeyChar.ToString(), out keyValue) || keyValue == 0 || keyValue > MenuOptions.Count)
        {
            key = Console.ReadKey(true);
        }

        Console.WriteLine(key.KeyChar);

        try
        {
            Console.WriteLine(MenuOptions[keyValue - 1]);
            Console.WriteLine("-------------\n");

            switch (keyValue)
            {
                case 1:
                    var filter = ReadValue("Filter in OData format (Enter to proceed without one):", false);
                    var users = await _peopleService.Search(filter);
                    Console.WriteLine($"{(users.Any() ? "No" : users.Count)} users found with these parameters.\n");
                    for (int i = 0; i < users.Count; i++)
                    {
                        Console.WriteLine($"{i+1}. {users[i].UserName} - {users[i].FirstName} {users[i].LastName}");
                    }

                    break;
                case 2:
                    var userName = ReadValue("Username:", true)!;
                    var user = await _peopleService.GetByUserName(userName);
                    Console.Write(JToken.Parse(JsonConvert.SerializeObject(user)).ToString(Formatting.Indented));
                    break;
                case 3:
                    userName = ReadValue("Username:", true)!;
                    var fieldName = ReadValue("Field name (eg. FirstName):", true)!;
                    var newValue = ReadValue("New value:", false)!;
                    await _peopleService.UpdateUserField(userName, new Dictionary<string, string>
                    {
                        { fieldName, newValue }
                    });
                    break;
                case 4:
                    return false;
            }
            
            Console.ReadKey();
            return true;
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"Error: {e.Message}");
            Console.WriteLine("See the logs for more details.\n");
            Console.ResetColor();

            Console.ReadKey();
            return true;
        }
    }

    private static string? ReadValue(string label, bool mandatory)
    {
        Console.Write(label);
        var value = Console.ReadLine();
        while (mandatory && string.IsNullOrEmpty(value))
        {
            value = Console.ReadLine();
        }

        return value;
    }

    private static IHost CreateDefaultApp()
    {
        var builder = Host.CreateDefaultBuilder().ConfigureHostConfiguration(options =>
        {
            options.AddJsonFile($"appsettings.json", false, true);
        });
        
        builder.ConfigureServices(services =>
        {
            services.AddHttpClient();
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.SetMinimumLevel(LogLevel.Warning);
                loggingBuilder.AddSerilog();
            });

            services.AddScoped<IPeopleService, PeopleService>();
        });

        builder.UseConsoleLifetime();
        var host = builder.Build();

        var configuration = host.Services.GetService<IConfiguration>()!;
        var logDictionary = configuration.GetValue<string>("LogPath") ?? "./log";
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Error()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .WriteTo.File(Path.Combine(logDictionary, "log-error-.log"), LogEventLevel.Error, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 100)
            .CreateLogger();

        return host;
    }
}