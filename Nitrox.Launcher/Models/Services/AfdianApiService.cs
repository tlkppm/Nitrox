using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NitroxModel.Logger;

namespace Nitrox.Launcher.Models.Services;

public class AfdianApiService
{
    private static readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri("https://afdian.com/api/open/")
    };

    
    private static readonly byte[] encryptionKey = Convert.FromBase64String("TmlUcm94QWZkaWFuS2V5MjAyNQ==");
    private const string encryptedToken = "M3BUdHdybTdjYjlqTUtYNEhRYWRGUjVZRGZTdm44VUo="; // 加密的Token
    private const string encryptedUserId = "NzVjNmU1ZjYyNWYxMTFlY2I0YzM1MjU0MDAyNWMzNzc="; // 加密的UserId

    private readonly string userId;
    private readonly string token;

    public AfdianApiService()
    {
        userId = DecryptString(encryptedUserId);
        token = DecryptString(encryptedToken);
    }

    /// <summary>
    /// 简单解密字符串（Base64）
    /// 注意：这只是基本混淆，真正的安全应该使用更强的加密
    /// </summary>
    private static string DecryptString(string encrypted)
    {
        try
        {
            byte[] data = Convert.FromBase64String(encrypted);
            return Encoding.UTF8.GetString(data);
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取赞助者列表
    /// </summary>
    public async Task<List<AfdianSponsor>> GetSponsorsAsync()
    {
        try
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string paramString = $"{{\"page\":1}}";
            string sign = GenerateSign(paramString, timestamp);

            var requestData = new
            {
                user_id = userId,
                @params = paramString,
                ts = timestamp,
                sign = sign
            };

            string jsonContent = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("query-sponsor", content);
            
            if (!response.IsSuccessStatusCode)
            {
                Log.Error($"[AfdianAPI] API请求失败: {response.StatusCode}");
                return new List<AfdianSponsor>();
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<AfdianApiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.Ec != 200)
            {
                Log.Error($"[AfdianAPI] API返回错误");
                return new List<AfdianSponsor>();
            }

            var sponsors = apiResponse.Data?.List ?? new List<AfdianSponsor>();
            
            return sponsors;
        }
        catch
        {
            Log.Error($"[AfdianAPI] 获取赞助者失败");
            return new List<AfdianSponsor>();
        }
    }

    /// <summary>
    /// 生成API签名
    /// </summary>
    private string GenerateSign(string params_, long timestamp)
    {
        string signString = $"{token}params{params_}ts{timestamp}user_id{userId}";
        
        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(signString);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            
            StringBuilder sb = new();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}

/// <summary>
/// 爱发电API响应
/// </summary>
public class AfdianApiResponse
{
    public int Ec { get; set; }  // 错误码，200表示成功
    public string? Em { get; set; }  // 错误消息
    public AfdianDataWrapper? Data { get; set; }
}

/// <summary>
/// 爱发电数据包装
/// </summary>
public class AfdianDataWrapper
{
    public List<AfdianSponsor>? List { get; set; }
    public int Total_count { get; set; }
    public int Total_page { get; set; }
}

/// <summary>
/// 爱发电赞助者信息
/// </summary>
public class AfdianSponsor
{
    public AfdianSponsorUser? User { get; set; }
    public List<AfdianPlan>? Sponsor_plans { get; set; }  // 赞助的所有方案
    public AfdianPlan? Current_plan { get; set; }  // 当前方案
    public string? All_sum_amount { get; set; }  // 总赞助金额（字符串格式，单位：元）
    public long Create_time { get; set; }  // 创建时间戳
    public long First_pay_time { get; set; }  // 首次支付时间戳
    public long Last_pay_time { get; set; }  // 最后支付时间戳
    
    // 辅助属性
    public string DisplayName => User?.Name ?? "匿名赞助者";
    public string AvatarUrl => User?.Avatar ?? string.Empty;
    public decimal AmountInYuan 
    {
        get
        {
            if (decimal.TryParse(All_sum_amount, out decimal amount))
                return amount;
            return 0m;
        }
    }
    public DateTime LastPayDate => Last_pay_time > 0 
        ? DateTimeOffset.FromUnixTimeSeconds(Last_pay_time).LocalDateTime 
        : DateTime.MinValue;
    public string PlanNames
    {
        get
        {
            if (Current_plan != null && !string.IsNullOrEmpty(Current_plan.Name))
                return Current_plan.Name;
            if (Sponsor_plans != null && Sponsor_plans.Any())
                return string.Join(", ", Sponsor_plans.Select(p => p.Name ?? ""));
            return "未指定套餐";
        }
    }
}

/// <summary>
/// 爱发电用户信息
/// </summary>
public class AfdianSponsorUser
{
    public string? User_id { get; set; }
    public string? Name { get; set; }
    public string? Avatar { get; set; }
}

/// <summary>
/// 爱发电套餐信息
/// </summary>
public class AfdianPlan
{
    public string? Plan_id { get; set; }
    public int Rank { get; set; }
    public string? User_id { get; set; }
    public int Status { get; set; }
    public string? Name { get; set; }
    public string? Pic { get; set; }
    public string? Desc { get; set; }
    public string? Price { get; set; }  // 字符串格式
    public long Update_time { get; set; }
    public int Pay_month { get; set; }
    public string? Show_price { get; set; }
    public int Independent { get; set; }
    public int Permanent { get; set; }
    public int Can_buy_hide { get; set; }
    public int Need_address { get; set; }
    public int Product_type { get; set; }
    public int Sale_limit_count { get; set; }
    public bool Need_invite_code { get; set; }
    public long Expire_time { get; set; }
    public int RankType { get; set; }
}

