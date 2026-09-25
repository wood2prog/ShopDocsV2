using Microsoft.Extensions.DependencyInjection;
using ShopDocsV2.Application;
using ShopDocsV2.Infrastructure.Apis;
using ShopDocsV2.Infrastructure.Files;
using ShopDocsV2.Infrastructure.Sqlite;

namespace ShopDocsV2.WinForms;

static class Program
{
    [STAThread]
    static void Main()
    {
        System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

        var services = new ServiceCollection();
        services.AddSingleton<SqliteConnectionFactory>();
        services.AddSingleton<SchemaInitializer>();
        services.AddSingleton<IJobRepository, JobRepository>();
        services.AddSingleton<IQuestionSetProvider, QuestionSetFileProvider>();
        services.AddSingleton<ICatalogRepository, CatalogRepository>();
        services.AddSingleton<ISpecFormattingService, SpecFormattingService>();
        services.AddSingleton<IJobPrintContentBuilder, JobPrintContentBuilder>();
        services.AddSingleton(new HttpClient());
        services.AddSingleton<IPaintColorLookupService, CompositePaintColorClient>();
        services.AddSingleton<IOrdxExportService, OrdxExportService>();
        services.AddSingleton<IKeyBindingStore, KeyBindingSettingsStore>();
        services.AddTransient<MainForm>();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<SchemaInitializer>().Initialize();

        System.Windows.Forms.Application.Run(provider.GetRequiredService<MainForm>());
    }
}
