using System;
using System.Collections.Generic;
using Nitrox.Launcher.ViewModels;

namespace Nitrox.Launcher.ViewModels.Designer;

internal class DesignSponsorViewModel : SponsorViewModel
{
    public DesignSponsorViewModel()
    {
        // 设计时数据
        Sponsors = new List<SponsorInfo>
        {
            new()
            {
                Name = "Volt_伏特",
                Amount = 200,
                Date = DateTime.Now.AddDays(-1),
                Message = "感谢 Nitrox 团队的出色工作！希望项目能持续发展。",
                Avatar = "V",
                IsHighlighted = true
            },
            new()
            {
                Name = "海洋探索者",
                Amount = 100,
                Date = DateTime.Now.AddDays(-7),
                Message = "深海迷航联机版太棒了！",
                Avatar = "海",
                IsHighlighted = false
            },
            new()
            {
                Name = "Anonymous",
                Amount = 50,
                Date = DateTime.Now.AddDays(-14),
                Message = "匿名支持，继续加油！",
                Avatar = "A",
                IsHighlighted = false
            }
        };

        TotalSponsorAmount = 350;
        SponsorCount = 3;
    }
}
