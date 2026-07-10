using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace PcMonitorUI.Converters;

public static class AppConverters
{
    /// <summary>bool IsConnected -> green/red dot</summary>
    public static readonly IValueConverter ConnectedToBrush =
        new FuncValueConverter<bool, IBrush>(connected =>
            connected ? new SolidColorBrush(Color.Parse("#3DDC97")) : new SolidColorBrush(Color.Parse("#E5484D")));

    /// <summary>bool IsMonitoring -> button label</summary>
    public static readonly IValueConverter MonitoringToLabel =
        new FuncValueConverter<bool, string>(monitoring => monitoring ? "Stop" : "Start");
}
