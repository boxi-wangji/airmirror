using Velopack;

namespace AirMirror;

internal static class Program
{
    [STAThread]
    private static void Main(string[] arguments)
    {
        VelopackApp.Build().Run();

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        var startReceiverImmediately = arguments.Any(argument =>
            string.Equals(argument, "--start", StringComparison.OrdinalIgnoreCase));
        Application.Run(new MainForm(startReceiverImmediately));
    }
}
