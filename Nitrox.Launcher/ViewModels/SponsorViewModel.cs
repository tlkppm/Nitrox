using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nitrox.Launcher.Models.Services;
using Nitrox.Launcher.ViewModels.Abstract;
using NitroxModel.Logger;
using Avalonia.Platform;
using Avalonia.Media.Imaging;

namespace Nitrox.Launcher.ViewModels;

internal partial class SponsorViewModel : RoutableViewModelBase
{
    private readonly AfdianApiService afdianApi = new();
    private static readonly HttpClient httpClient = new();

    /// <summary>
    /// 赞助者信息（合并本地+API）
    /// </summary>
    [ObservableProperty]
    private List<SponsorInfo> sponsors = [];

    /// <summary>
    /// 本地已知赞助者（保留）
    /// </summary>
    [ObservableProperty]
    private List<SponsorInfo> localSponsors = [];

    /// <summary>
    /// API赞助者
    /// </summary>
    [ObservableProperty]
    private List<SponsorInfo> apiSponsors = [];

    /// <summary>
    /// 赞助总金额
    /// </summary>
    [ObservableProperty]
    private decimal totalSponsorAmount;

    /// <summary>
    /// 赞助者数量
    /// </summary>
    [ObservableProperty]
    private int sponsorCount;

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [ObservableProperty]
    private bool isLoading;

    public SponsorViewModel()
    {
        _ = LoadSponsorsAsync();
    }

    /// <summary>
    /// 打开赞助链接命令
    /// </summary>
    [RelayCommand]
    private async Task OpenSponsorLink(string? url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Log.Error($"打开赞助链接失败: {ex.Message}");
            }
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// 成为赞助者命令
    /// </summary>
    [RelayCommand]
    private async Task BecomeSponsor()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://afdian.com/a/TFUY-LFUY",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Log.Error($"打开赞助页面失败: {ex.Message}");
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// 从资源加载Bitmap（复制自CarouselControl）
    /// </summary>
    private Bitmap? GetBitmapFromAsset(string assetPath)
    {
        try
        {
            Log.Info($"[SponsorViewModel] 尝试加载图片: {assetPath}");
            
            // 方法1：尝试使用avares协议
            var uri = new Uri($"avares://Nitrox.Launcher{assetPath}");
            Log.Info($"[SponsorViewModel] 尝试URI: {uri}");
            
            if (AssetLoader.Exists(uri))
            {
                Log.Info($"[SponsorViewModel] 使用avares协议加载图片成功");
                using var stream = AssetLoader.Open(uri);
                return new Bitmap(stream);
            }
            else
            {
                Log.Error($"[SponsorViewModel] avares协议资源不存在: {uri}");
            }
            
            // 方法2：尝试相对路径
            var relativeUri = new Uri(assetPath, UriKind.Relative);
            Log.Info($"[SponsorViewModel] 尝试相对URI: {relativeUri}");
            
            if (AssetLoader.Exists(relativeUri))
            {
                Log.Info($"[SponsorViewModel] 使用相对路径加载图片成功");
                using var stream = AssetLoader.Open(relativeUri);
                return new Bitmap(stream);
            }
            else
            {
                Log.Error($"[SponsorViewModel] 相对路径资源不存在: {relativeUri}");
            }
            
            Log.Error($"[SponsorViewModel] 所有方法都无法加载图片: {assetPath}");
            return null;
        }
        catch (Exception ex)
        {
            Log.Error($"[SponsorViewModel] 加载图片失败 {assetPath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 加载本地已知赞助者
    /// </summary>
    private void LoadLocalSponsors()
    {
        try
        {
            Log.Info("[SponsorViewModel] 加载本地已知赞助者...");
            
            // 加载头像图片
            var avatarPath = "/Assets/Images/avatars/odif.jpg";
            var avatarBitmap = GetBitmapFromAsset(avatarPath);
            
            LocalSponsors = new List<SponsorInfo>
            {
                new SponsorInfo
                {
                    Name = "Volt_伏特",
                    Amount = 200,
                    Date = DateTime.Now.AddMonths(-2),
                    Message = "感谢 Nitrox 团队的出色工作！希望项目能持续发展。",
                    Avatar = avatarPath,
                    AvatarBitmap = avatarBitmap,
                    IsHighlighted = true,
                    IsFromApi = false,
                    PlanName = "早期支持者"
                }
            };

            Log.Info($"[SponsorViewModel] 加载了 {LocalSponsors.Count} 位本地赞助者");
        }
        catch (Exception ex)
        {
            Log.Error($"[SponsorViewModel] 加载本地赞助者失败: {ex.Message}");
            LocalSponsors = [];
        }
    }

    /// <summary>
    /// 从API加载赞助者
    /// </summary>
    private async Task LoadApiSponsorsAsync()
    {
        try
        {
            Log.Info("[SponsorViewModel] 开始从爱发电API获取赞助者...");
            
            var afdianSponsors = await afdianApi.GetSponsorsAsync();
            
            if (afdianSponsors == null || !afdianSponsors.Any())
            {
                Log.Warn("[SponsorViewModel] API未返回赞助者数据");
                ApiSponsors = [];
                return;
            }

            Log.Info($"[SponsorViewModel] API返回 {afdianSponsors.Count} 位赞助者");

            var sponsorTasks = afdianSponsors.Select(async sponsor =>
            {
                Bitmap? avatarBitmap = null;
                
                // 尝试从URL加载头像
                if (!string.IsNullOrEmpty(sponsor.AvatarUrl))
                {
                    try
                    {
                        var imageBytes = await httpClient.GetByteArrayAsync(sponsor.AvatarUrl);
                        using (var ms = new System.IO.MemoryStream(imageBytes))
                        {
                            avatarBitmap = new Bitmap(ms);
                        }
                        Log.Info($"[SponsorViewModel] 成功加载 {sponsor.DisplayName} 的头像");
                    }
                    catch (Exception ex)
                    {
                        Log.Warn($"[SponsorViewModel] 加载 {sponsor.DisplayName} 头像失败: {ex.Message}");
                    }
                }

                return new SponsorInfo
                {
                    Name = sponsor.DisplayName,
                    Amount = sponsor.AmountInYuan,
                    Date = sponsor.LastPayDate,
                    Message = $"已支持 {sponsor.PlanNames}",
                    Avatar = sponsor.AvatarUrl ?? string.Empty,
                    AvatarBitmap = avatarBitmap,
                    IsHighlighted = sponsor.AmountInYuan >= 100, // 100元以上高亮显示
                    IsFromApi = true,
                    PlanName = sponsor.PlanNames,
                    AfdianUserId = sponsor.User?.User_id
                };
            });

            ApiSponsors = (await Task.WhenAll(sponsorTasks)).ToList();
            
            Log.Info($"[SponsorViewModel] 成功处理 {ApiSponsors.Count} 位API赞助者");
        }
        catch (Exception ex)
        {
            Log.Error($"[SponsorViewModel] 从API加载赞助者失败: {ex.Message}");
            Log.Error($"[SponsorViewModel] 异常详情: {ex}");
            ApiSponsors = [];
        }
    }

    /// <summary>
    /// 合并并更新赞助者列表
    /// </summary>
    private void MergeSponsors()
    {
        try
        {
            Log.Info("[SponsorViewModel] 合并赞助者列表...");
            
            // 合并本地和API赞助者
            var allSponsors = new List<SponsorInfo>();
            
            // 先添加本地赞助者
            allSponsors.AddRange(LocalSponsors);
            Log.Info($"[SponsorViewModel] 添加了 {LocalSponsors.Count} 位本地赞助者");
            
            // 添加API赞助者（按金额降序排序）
            allSponsors.AddRange(ApiSponsors.OrderByDescending(s => s.Amount));
            Log.Info($"[SponsorViewModel] 添加了 {ApiSponsors.Count} 位API赞助者");
            
            Sponsors = allSponsors;
            
            // 计算统计信息
            TotalSponsorAmount = allSponsors.Sum(s => s.Amount);
            SponsorCount = allSponsors.Count;
            
            Log.Info($"[SponsorViewModel] 合并完成: {SponsorCount} 位赞助者，总金额 ¥{TotalSponsorAmount:F0}");
        }
        catch (Exception ex)
        {
            Log.Error($"[SponsorViewModel] 合并赞助者列表失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 异步加载所有赞助者数据
    /// </summary>
    private async Task LoadSponsorsAsync()
    {
        try
        {
            IsLoading = true;
            Log.Info("[SponsorViewModel] 开始加载赞助者数据...");
            
            // 加载本地赞助者
            LoadLocalSponsors();
            
            // 从API加载赞助者
            await LoadApiSponsorsAsync();
            
            // 合并两个列表
            MergeSponsors();
            
            Log.Info("[SponsorViewModel] 赞助者数据加载完成");
        }
        catch (Exception ex)
        {
            Log.Error($"[SponsorViewModel] 加载赞助者数据失败: {ex.Message}");
            Sponsors = LocalSponsors; // 失败时至少显示本地赞助者
            TotalSponsorAmount = LocalSponsors.Sum(s => s.Amount);
            SponsorCount = LocalSponsors.Count;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 刷新赞助者数据
    /// </summary>
    [RelayCommand]
    private async Task RefreshSponsors()
    {
        await LoadSponsorsAsync();
    }

    internal override async Task ViewContentLoadAsync(CancellationToken cancellationToken = default)
    {
        Log.Info("[SponsorViewModel] ViewContentLoadAsync 开始执行...");
        await LoadSponsorsAsync();
        Log.Info("[SponsorViewModel] ViewContentLoadAsync 完成");
    }
}

/// <summary>
/// 赞助者信息
/// </summary>
public class SponsorInfo
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public Bitmap? AvatarBitmap { get; set; }
    public bool IsHighlighted { get; set; }
    public string? Website { get; set; }
    public string? Contact { get; set; }
    
    // 新增：API相关属性
    public bool IsFromApi { get; set; } // 是否来自API
    public string? PlanName { get; set; } // 套餐名称
    public string? AfdianUserId { get; set; } // 爱发电用户ID

    public string FormattedAmount => $"¥{Amount:F0}";
    public string FormattedDate => Date.ToString("yyyy年MM月dd日");
    public string AvatarText => string.IsNullOrEmpty(Name) ? "?" : Name.Substring(0, 1).ToUpper();
    public bool HasAvatarImage => AvatarBitmap != null;
    public string AvatarImagePath => HasAvatarImage ? Avatar : string.Empty;
    public string SourceBadge => IsFromApi ? "爱发电" : "本地";
}
