using Avalonia.Collections;
using Nitrox.Launcher.Models.Design;
using Nitrox.Launcher.ViewModels;

namespace Nitrox.Launcher.ViewModels.Designer;

internal class DesignBelowZeroServersViewModel : BelowZeroServersViewModel
{
    public DesignBelowZeroServersViewModel() : base(null!, null!, null!, null!)
    {
        Servers = new AvaloniaList<BelowZeroServerEntry>
        {
            new()
            {
                Name = "Below Zero Demo Server",
                Weather = "Snow",
                Temperature = -15.0f,
                EnableSeatruckFeatures = true,
                EnableWeatherSystem = true,
                EnableIceLayerManagement = true,
                EnableTemperatureSystem = true,
                MaxPlayers = 8,
                IsOnline = true,
                Players = 3
            },
            new()
            {
                Name = "Arctic Base Alpha",
                Weather = "Blizzard",
                Temperature = -25.0f,
                EnableSeatruckFeatures = true,
                EnableWeatherSystem = true,
                EnableIceLayerManagement = false,
                EnableTemperatureSystem = true,
                MaxPlayers = 4,
                IsOnline = false,
                Players = 0
            }
        };
    }
}
