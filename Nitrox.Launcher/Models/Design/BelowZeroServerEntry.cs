using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NitroxModel.Logger;
using NitroxModel.Server;

namespace Nitrox.Launcher.Models.Design;

/// <summary>
///     Below Zero服务器条目 - 扩展标准服务器条目以支持Below Zero特有功能
/// </summary>
public partial class BelowZeroServerEntry : ServerEntry
{
    [ObservableProperty]
    private string weather = "Clear";
    
    [ObservableProperty]
    private float temperature = -10.0f;
    
    [ObservableProperty]
    private bool enableSeatruckFeatures = true;
    
    [ObservableProperty]
    private bool enableWeatherSystem = true;
    
    [ObservableProperty]
    private bool enableIceLayerManagement = true;
    
    [ObservableProperty]
    private bool enableTemperatureSystem = true;

    public override string ToString()
    {
        return $"[Below Zero] {Name} - {(IsOnline ? "Online" : "Offline")} - Weather: {Weather}, Temp: {Temperature}°C";
    }
}
