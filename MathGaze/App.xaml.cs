using MathGaze.Core;
using MathGaze.Services;
using MathGaze.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace MathGaze;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                // Services
                services.AddSingleton<IPdfService, DocnetPdfService>();
                services.AddSingleton<IFileDialogService, FileDialogService>();
                services.AddSingleton<IGeometryService, GeometryService>();
                services.AddSingleton<UndoService>();  // UndoService is internal to GeometryService but registered for future injection if needed

                services.AddSingleton<IExportService, PdfExportService>();

                // Session persistence (SYS-02, SYS-03)
                // Func<int> breaks the circular dependency: SessionService needs CurrentPage from
                // MainViewModel, but MainViewModel depends on ISessionService. The lambda resolves
                // MainViewModel lazily (after all singletons are registered), avoiding a constructor
                // dependency cycle.
                services.AddSingleton<ISessionService>(sp =>
                    new SessionService(
                        sp.GetRequiredService<IGeometryService>(),
                        () => sp.GetRequiredService<MainViewModel>().CurrentPage));

                // ViewModels
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<GeometryLayerViewModel>();
                services.AddSingleton<PdfCanvasViewModel>();
                services.AddSingleton<ToolViewModel>();
                services.AddSingleton<RightRailViewModel>();
                services.AddSingleton<SnapEngine>();

                // Windows — resolved from DI so they can receive injected ViewModels
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        // Break circular constructor dependency: MainViewModel ↔ PdfCanvasViewModel.
        // Both are singletons so resolving them here and cross-wiring is safe.
        var mainVm      = _host.Services.GetRequiredService<MainViewModel>();
        var pdfCanvasVm = _host.Services.GetRequiredService<PdfCanvasViewModel>();
        mainVm.SetPdfCanvasViewModel(pdfCanvasVm);

        var window = _host.Services.GetRequiredService<MainWindow>();
        window.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }
}
