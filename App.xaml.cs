using System;
using System.Windows;
using System.Windows.Threading;

namespace ColumnFinder;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show(args.Exception.ToString(), "Dispatcher Exception");
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            MessageBox.Show(args.ExceptionObject?.ToString() ?? "(null)", "Unhandled Exception");
        };
        base.OnStartup(e);
    }
}
