using Nitrox.Launcher.ViewModels;
using Nitrox.Launcher.Views.Abstract;
using NitroxModel.Logger;

namespace Nitrox.Launcher.Views;

internal partial class SponsorView : RoutableViewBase<SponsorViewModel>
{
    public SponsorView()
    {
        Log.Info("[SponsorView] 初始化 SponsorView...");
        InitializeComponent();
        Log.Info("[SponsorView] SponsorView 初始化完成");
    }
}
