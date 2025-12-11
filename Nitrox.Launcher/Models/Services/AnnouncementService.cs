using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Nitrox.Launcher.Models.Design;
using NitroxModel.Logger;

namespace Nitrox.Launcher.Models.Services;

public class AnnouncementService
{
    private readonly List<AnnouncementItem> announcements = [];

    public AnnouncementService()
    {
        LoadDefaultAnnouncements();
    }

    public List<AnnouncementItem> GetAnnouncements() => announcements;

    private void LoadDefaultAnnouncements()
    {
        try
        {
            // v2.4.8.0 生物生成限制与清除指令
            announcements.Add(new AnnouncementItem
            {
                Id = "v2480_creature_spawn_control",
                Title = "Nitrox v2.4.8.0 生物生成控制系统",
                Content = "本次更新完善了生物生成管理和控制台禁用功能：【生物生成限制修复】修复了之前基于ClassId的生物检测无法识别GUID格式ClassId的问题，现在改用TechType进行精确识别，覆盖所有鱼类、利维坦等生物 【新增清除指令】添加clearCreatures服务器指令，可清除指定范围内的生物实体，支持按物种过滤，格式：clearCreatures [范围] [物种名]，用于清理已存在的过量生物 【控制台禁用修复】新增DevConsole.SetState拦截补丁，彻底阻止控制台通过任何方式打开，确保禁用游戏指令框选项生效 【完善的物种列表】扩展生物识别列表，包含深海迷航和零度之下的所有生物类型，支持School群体生物识别。建议在新存档中启用生物生成限制以获得最佳效果，已有存档可使用clearCreatures指令清理过量生物。",
                Type = AnnouncementType.Feature,
                Priority = AnnouncementPriority.High,
                CreatedAt = DateTime.Now,
                IsActive = true
            });
            
            // v2.4.7.0 BepInEx模组检测
            announcements.Add(new AnnouncementItem
            {
                Id = "v2470_bepinex_detection",
                Title = "Nitrox v2.4.7.0 BepInEx模组检测",
                Content = "本次更新同步官方PR#2569并进行优化，新增BepInEx模组检测功能：【启动检测】在单人和多人模式启动前自动扫描BepInEx/plugins和BepInEx/patchers目录 【详细日志】检测到模组时会在日志中记录完整的模组名称列表，便于排查问题 【友好警告】显示中文警告提示，告知玩家Nitrox多人游戏不支持模组可能导致不稳定 【代码优化】相比原PR合并检测方法为单一方法，代码更简洁，日志更详细。如果您使用了BepInEx模组，建议在多人游戏前暂时移除以获得最佳稳定性。",
                Type = AnnouncementType.Feature,
                Priority = AnnouncementPriority.Normal,
                CreatedAt = DateTime.Now,
                IsActive = true
            });
            
            // v2.4.6.1 优化更新
            announcements.Add(new AnnouncementItem
            {
                Id = "v2461_optimization",
                Title = "Nitrox v2.4.6.1 优化更新",
                Content = "本次更新进行了两项重要优化：【默认引擎】Generic Host现在作为默认服务器引擎，创建新服务器时自动启用通用主机模式，享受更好的性能和架构优势 【文档修正】修正了游戏控制台快捷键说明，正确按键为（· ` ~）而非（F3/Enter），现在服务器管理页面显示正确的快捷键说明。如果您已创建的服务器想要使用Generic Host，请在服务器管理页面勾选使用新服务器引擎（通用主机）选项。",
                Type = AnnouncementType.Improvement,
                Priority = AnnouncementPriority.Normal,
                CreatedAt = DateTime.Now,
                IsActive = true
            });
            
            // v2.4.6.0 Generic Host API 集成
            announcements.Add(new AnnouncementItem
            {
                Id = "v2460_generic_host_api",
                Title = "Nitrox v2.4.6.0 通用主机API集成",
                Content = "为.NET Generic Host服务端搭建了完整的Web API系统，实现真实玩家信息获取！【核心突破】在Generic Host模式下启动了独立的Web API服务（端口=游戏端口+1000），提供RESTful接口供启动器查询玩家信息 【真实数据】外部模式现在能通过HTTP API获取真实玩家名称、ID、权限等信息，不再显示玩家1或玩家2占位符 【智能降级】如果API不可用，自动回退到占位符模式，确保功能稳定性 【技术架构】使用ASP.NET Core构建API服务，包含PlayersController控制器，支持/api/players和/api/players/status两个端点 【完美兼容】嵌入式模式继续使用IPC通信，外部Generic Host模式使用Web API，旧版服务端使用占位符，三种模式和谐共存 【日志优化】API启动信息清晰显示在服务器控制台，便于调试和监控。这是Nitrox服务端架构的重大升级，为未来的远程管理功能奠定了基础！",
                Type = AnnouncementType.Feature,
                Priority = AnnouncementPriority.High,
                CreatedAt = DateTime.Now,
                IsActive = true
            });
            
            // v2.4.5.0 玩家列表功能
            announcements.Add(new AnnouncementItem
            {
                Id = "v2450_player_list",
                Title = "Nitrox v2.4.5.0 玩家列表功能",
                Content = "新增玩家列表功能，完美支持嵌入式和外部服务器两种模式！【双模式支持】无论使用嵌入式还是外部服务器，都可以查看玩家列表。嵌入式模式显示真实玩家名称，外部模式显示玩家数量统计 【智能识别】系统自动识别服务器模式，嵌入式模式通过服务器命令获取真实玩家名称并实时显示，外部模式显示当前在线玩家数量，清晰标注服务器类型 【实时更新】玩家上线下线时自动同步更新列表，支持手动点击刷新按钮获取最新数据 【界面优化】在服务器列表页面的控制台按钮旁新增玩家列表按钮，仅在服务器在线时显示，响应式卡片布局，完整中文界面。同时修复了IDE项目加载问题，提供完整的解决方案文档IDE_LOADING_FIX.md。无论您偏好哪种服务器模式，现在都能轻松查看在线玩家信息！",
                Type = AnnouncementType.Feature,
                Priority = AnnouncementPriority.Normal,
                CreatedAt = DateTime.Now,
                IsActive = true
            });
            
            // v2.4.4.0 高级设置汉化
            announcements.Add(new AnnouncementItem
            {
                Id = "v2440_advanced_settings_localization",
                Title = "Nitrox v2.4.4.0 高级设置汉化",
                Content = "完成服务器高级设置的完整汉化，包括配置编辑器所有选项、说明文本、提示信息等20+项配置的中文化，让服务器管理更加直观易用。",
                Type = AnnouncementType.Feature,
                Priority = AnnouncementPriority.Normal,
                CreatedAt = DateTime.Now,
                IsActive = true
            });
            
            // v2.4.3.4 权限和状态汉化
            announcements.Add(new AnnouncementItem
            {
                Id = "v2434_permissions_and_status_localization",
                Title = "Nitrox v2.4.3.4 权限和状态汉化",
                Content = "本次更新完成了权限系统和服务器状态的全面汉化，提升服务器管理体验！【权限系统汉化】将所有玩家权限等级替换为中文显示，None无权限、Player玩家、Moderator管理员、Admin超级管理员、Console控制台，在服务器管理页面的默认权限下拉框中统一显示中文，让权限层级一目了然 【服务器状态汉化】修复服务器列表中的状态显示，Online在线、Offline离线，玩家数量显示从playing改为玩家，完整格式为离线生存模式0/100玩家，所有状态信息都使用中文，符合国内用户使用习惯 【基于特性的本地化】使用DescriptionAttribute为Perms权限枚举添加中文描述，通过ToStringConverter自动转换显示，保持与游戏模式汉化相同的技术方案，不使用硬编码 【统一的显示效果】在启动器的所有位置包括服务器列表、服务器管理页面、创建服务器对话框等，权限和状态都显示为统一的中文，提供完整的中文化体验 【保持兼容性】所有枚举的底层值保持不变，仅改变显示文本，不影响服务器配置兼容性和网络通信协议。让服务器管理界面完全中文化，更容易理解和操作！",
                Type = AnnouncementType.Feature,
                Priority = AnnouncementPriority.Normal,
                CreatedAt = DateTime.Now,
                IsActive = true
            });
            
            // v2.4.3.3 游戏模式汉化
            announcements.Add(new AnnouncementItem
            {
                Id = "v2433_gamemode_localization",
                Title = "Nitrox v2.4.3.3 游戏模式汉化",
                Content = "本次更新完成了游戏模式的中文本地化，提升用户体验！【完整的游戏模式汉化】将所有游戏模式名称替换为中文显示，Survival生存模式、Freedom自由模式、Hardcore极限模式、Creative创造模式，无论是服务器管理页面还是创建服务器对话框，所有游戏模式都以中文显示，符合国内玩家使用习惯 【基于特性的本地化系统】使用DescriptionAttribute特性为NitroxGameMode枚举添加中文描述，通过ToStringConverter自动获取并显示中文名称，这是标准的.NET本地化实践，不使用硬编码，保持代码的可维护性和扩展性 【统一的显示效果】在启动器的所有位置包括服务器列表、服务器管理页面、创建服务器对话框、游戏模式选择器等，游戏模式都显示为统一的中文名称，提供一致的用户体验 【保持原有功能】所有游戏模式的底层逻辑和数值保持不变，仅改变显示文本，不影响游戏存档兼容性和服务器通信，老玩家可以无缝升级。让中文玩家更轻松地选择和管理游戏模式，享受更友好的界面体验！",
                Type = AnnouncementType.Feature,
                Priority = AnnouncementPriority.Normal,
                CreatedAt = DateTime.Now,
                IsActive = true
            });
            
            // v2.4.3.2 赞助卡片样式系统修复
            announcements.Add(new AnnouncementItem
            {
                Id = "v2432_sponsor_card_style_fix",
                Title = "Nitrox v2.4.3.2 赞助卡片样式系统修复",
                Content = "本次更新紧急修复了v2.4.3.1版本中的严重布局问题，完全重构了样式应用机制！【样式系统重构】从嵌套的FittingWrapPanel样式选择器改为独立的Border.sponsor-card类选择器，彻底解决ItemsControl中样式无法应用的问题，确保每张卡片都能正确获得边框、圆角、内边距等样式 【边框完全可见】修复了边框不显示的严重问题，现在每张赞助者卡片都有清晰的2px半透明白色边框#50FFFFFF，悬停时边框变为蓝色#4B9FE1并显示阴影效果，卡片之间有明确的视觉边界 【布局混乱修复】修复了后续赞助者卡片布局完全错乱的问题，现在所有赞助者都采用统一的垂直StackPanel布局，头像-姓名-分隔线-标签-日期-留言按正确顺序排列，不再出现内容混乱交叠 【内容完整显示】移除了留言的MaxWidth 380限制改为自动适配卡片宽度，姓名区域设置MaxWidth 270确保换行显示，配合卡片最小宽度380px和内边距24px，所有内容都能在卡片内完整显示不会溢出 【统一样式应用】统计信息卡片和赞助者卡片现在都使用明确的样式类而非嵌套选择器，确保样式在任何场景下都能正确应用，不再出现样式失效导致的布局问题 【响应式布局优化】FittingWrapPanel添加统一的Margin负值抵消卡片边距，确保在各种窗口尺寸下都能正确排列，自动计算最优列数，内容永不溢出边界。此次修复解决了上个版本引入的所有布局和样式问题，带来稳定可靠的赞助页面显示效果！",
                Type = AnnouncementType.Bugfix,
                Priority = AnnouncementPriority.Critical,
                CreatedAt = DateTime.Now,
                IsActive = true
            });
            
            // v2.4.3.1 赞助卡片布局优化
            announcements.Add(new AnnouncementItem
            {
                Id = "v2431_sponsor_card_layout_optimization",
                Title = "Nitrox v2.4.3.1 赞助卡片布局优化",
                Content = "本次更新针对用户反馈对赞助者卡片进行了深度优化，彻底解决内容显示不全的问题！【更宽的卡片宽度】将最小卡片宽度从300px增加到380px，为内容提供更充足的展示空间，长留言内容不再被截断，所有信息都能完整显示 【更大的头像尺寸】头像从50px升级到64px，视觉效果更突出，赞助者形象更加醒目，圆角从25px增加到32px更加圆润自然 【更清晰的视觉边界】边框粗细从1.5px增加到2px，透明度从#40提升到#50更加明显，圆角从12px增加到16px更加圆润柔和，每张卡片都有明确的视觉边界 【更舒适的内边距】卡片内边距从20px增加到24px，外边距从4px增加到6px，整体间距从12px增加到16px，视觉呼吸感更强，内容排布更加舒适 【精美的分隔线】在头像区域和详细信息之间添加了细腻的半透明分隔线，视觉层次更加清晰，信息区块划分更加明确 【优化的标签样式】来源标签和套餐名称都改用圆角徽章设计，套餐名称使用金色背景#FFD700配黑色文字，视觉冲击力更强，信息传达更加直观 【增强的文字层次】标题字号从24px增加到28px更加醒目，姓名字号从16px增加到18px更加突出，留言字号从13px增加到14px提升可读性，行高从18增加到20提供更舒适的阅读体验 【完美的留言显示】留言内容添加MaxWidth 380限制确保自动换行，配合LineHeight 20和Opacity 0.8，无论多长的留言都能完美显示不被截断。此次优化让每张赞助者卡片都更加精美大气，所有信息都能完整清晰地展示！",
                Type = AnnouncementType.Feature,
                Priority = AnnouncementPriority.High,
                CreatedAt = DateTime.Now,
                IsActive = true
            });
            
            // v2.4.3.0 赞助支持页面完全重写
            announcements.Add(new AnnouncementItem
            {
                Id = "v2430_sponsor_page_redesign",
                Title = "Nitrox v2.4.3.0 赞助支持页面完全重写",
                Content = "本次更新对赞助支持页面进行了彻底的架构重构和视觉重设计，带来全新的用户体验！【清晰可见的边框】采用#40FFFFFF半透明白色边框搭配1.5px粗细，确保每张卡片都有清晰的视觉边界，边框在任何主题下都清晰可见，鼠标悬停时边框变为蓝色#4B9FE1提供即时反馈 【紧凑高效的布局】完全重新设计卡片内部结构，头像缩小至50px提供更紧凑的视觉效果，所有文字信息采用层级化排列，充分利用卡片空间，信息密度更高但不显拥挤 【智能文本控制】所有文本元素都有明确的换行和截断规则，姓名使用省略号截断避免过长，留言内容自动换行配合LineHeight 18提供最佳可读性，无论内容多长都不会溢出卡片边界 【精简的视觉风格】去除冗余的视觉装饰，专注于内容本身，统一的字号体系16/14/13/12/11确保视觉层次清晰，透明度控制0.75/0.6/0.5营造舒适的信息层次 【完美的响应式设计】最小卡片宽度300px确保在任何窗口尺寸下都有最佳显示效果，从最小窗口到全屏都能完美适配，FittingWrapPanel自动计算最优列数，内容永不溢出 【优化的信息层次】头像+姓名+金额作为主要信息突出显示，来源标签和套餐名称作为次要信息横向排列，日期和留言作为辅助信息使用较低透明度，所有信息一目了然易于浏览。此次完全重写从根本上解决了之前版本的所有布局问题，带来真正专业级的赞助页面体验！",
                Type = AnnouncementType.Feature,
                Priority = AnnouncementPriority.Critical,
                CreatedAt = DateTime.Now,
                IsActive = true
            });
            
            // v2.4.2.4 赞助卡片溢出修复
            announcements.Add(new AnnouncementItem
            {
                Id = "v2424_sponsor_card_overflow_fix",
                Title = "Nitrox v2.4.2.4 赞助卡片溢出修复",
                Content = "本次更新修复了赞助支持页面的关键显示问题，确保所有内容都能完美展示！【边框可见性修复】修复了卡片边框不显示的问题，现在使用明确的半透明白色边框#33FFFFFF，确保在任何主题下都清晰可见，卡片之间有明显的视觉分隔 【文本溢出修复】修复了长留言文本溢出卡片边界的问题，为留言内容添加MaxWidth 260px限制，配合TextWrapping自动换行，再长的留言也能完美显示 【姓名截断优化】为赞助者姓名添加MaxWidth 170px限制和省略号截断，超长姓名会显示为省略号，避免破坏布局 【套餐名称换行】套餐信息也添加了MaxWidth 260px和自动换行，即使是超长的套餐名称也能正确显示 【窗口缩放适配】修复了窗口缩小到极限时的显示问题，所有内容都会正确换行或截断，不再溢出卡片边界 【视觉一致性】确保在各种窗口尺寸下，从最小到最大，所有赞助者卡片都能保持完美的视觉效果。此次修复彻底解决了内容溢出问题，每张赞助者卡片都有清晰的边框，内容整洁有序！",
                Type = AnnouncementType.Bugfix,
                Priority = AnnouncementPriority.Critical,
                CreatedAt = DateTime.Now,
                IsActive = true
            });
            
            // v2.4.2.3 赞助卡片边框优化
            announcements.Add(new AnnouncementItem
            {
                Id = "v2423_sponsor_card_border",
                Title = "Nitrox v2.4.2.3 赞助卡片边框优化",
                Content = "本次更新针对赞助支持页面进行了视觉优化，解决卡片拥挤问题！【清晰边界】为所有赞助者卡片添加了细腻的边框线条，使用系统主题边框色确保与整体风格协调一致，每张卡片都有明确的视觉边界 【悬停高亮】鼠标悬停时边框会变为蓝色高亮，提供即时的交互反馈，让用户清楚知道当前焦点位置 【视觉层次】通过1px的精细边框和12px的圆角设计，在保持简洁美观的同时，让卡片之间有明显的区分度，不再拥挤 【统一风格】所有赞助者卡片采用完全统一的样式处理，无论赞助金额大小都享受同等的视觉呈现，体现对每一位支持者的尊重 【布局优化】配合自适应布局系统，卡片间距和边框完美配合，在各种窗口尺寸下都有最佳的显示效果。立即体验焕然一新的赞助支持页面，每一位赞助者都能被清晰完美地展示！",
                Type = AnnouncementType.Feature,
                Priority = AnnouncementPriority.High,
                CreatedAt = DateTime.Now,
                IsActive = true
            });
            
            // v2.4.2.2 赞助页面UI优化
            announcements.Add(new AnnouncementItem
            {
                Id = "v2422_sponsor_ui_redesign",
                Title = "Nitrox v2.4.2.2 赞助页面UI全面优化",
                Content = "本次更新全面重构了赞助支持页面的UI布局，带来更美观、更实用的体验！【响应式布局】采用与社区页面一致的FittingWrapPanel自适应布局系统，赞助者卡片会根据窗口大小自动调整排列，完美支持各种分辨率和窗口尺寸 【视觉优化】统一使用系统主题配色，与启动器整体风格保持一致，卡片圆角、间距、阴影效果全面优化，视觉更加清爽舒适 【信息层次】重新设计信息展示层次，头像尺寸优化为60x60，姓名、金额、套餐信息、留言按重要性合理排布，一目了然 【交互改进】按钮样式统一，悬停效果更流畅，加载状态更清晰，整体交互体验大幅提升 【空间利用】卡片最小宽度优化为320px，配合MaxWidth 2500px的容器设置，确保在小窗口和大屏幕上都有最佳显示效果 【功能保留】完整保留爱发电API集成、本地赞助者管理、实时刷新等所有功能，只优化了展示层。立即体验全新的赞助支持页面，流畅自适应的布局让每一位赞助者都能被完美展示！",
                Type = AnnouncementType.Feature,
                Priority = AnnouncementPriority.High,
                CreatedAt = DateTime.Now,
                IsActive = true
            });
            

            // v2.4.2.1 赞助支持功能上线
            announcements.Add(new AnnouncementItem
            {
                Id = "v2421_sponsor_integration",
                Title = "Nitrox v2.4.2.1 赞助支持功能上线",
                Content = "本次更新为Nitrox带来全新的赞助支持体系！【赞助者展示】启动器新增赞助支持页面，实时展示所有爱发电平台赞助者信息，包括赞助者头像、套餐名称、赞助金额等完整信息 【实时同步】通过爱发电开放API自动获取赞助者数据，支持一键刷新更新列表，让每一份支持都能被及时看到 【来源标识】清晰区分爱发电赞助者和本地永久贡献者，感谢所有支持者的慷慨付出 【安全保障】采用加密存储和安全传输机制，确保赞助者信息安全 【多语言支持】完整的中文界面，友好的用户体验。感谢每一位赞助者对Nitrox项目的支持，您的赞助将帮助项目持续发展，为所有玩家带来更好的多人游戏体验！现在前往赞助支持页面，查看完整的赞助者名单，或成为我们的赞助者。",
                Type = AnnouncementType.Feature,
                Priority = AnnouncementPriority.High,
                CreatedAt = DateTime.Now,
                IsActive = true
            });
            
            // v2.4.1.0 关键Bug修复与功能增强
            announcements.Add(new AnnouncementItem
            {
                Id = "v2410_critical_bugfix",
                Title = "Nitrox v2.4.1.0 关键Bug修复与功能增强",
                Content = "紧急修复更新！本次更新同步官方PR #2535和#2541，并修复多个关键问题：【建造系统修复】修复建造物销毁包不广播的bug，彻底解决存储柜/模块重复出现的异常问题，每次进入存档都会看到重复建筑物的情况已完全修复 【实体生成修复】修复空单元格信息不发送的问题，解决泡泡鱼等生物异常聚集刷新的bug，实体生成现在更加稳定可靠 【载具物理修复】修复外骨骼和海蛾穿地掉落的严重问题，添加constructionFallOverride保护，载具生成后不再异常坠落穿过地形 【生物同步修复】修复生物死亡后尸体位置不同步问题，改用世界坐标确保所有客户端看到的尸体位置一致 【同步优化】改进建造物销毁时的包发送逻辑，确保所有玩家都能正确看到建筑物的移除 【地形加载】优化单元格加载机制，即使是空单元格也会正确标记为已加载，防止生物重复生成 【新功能】在服务器管理页面新增禁用游戏指令框选项，可独立控制游戏内置控制台的启用状态，提供更灵活的服务器管理。强烈建议所有玩家立即更新，这些修复大幅提升多人游戏的稳定性和一致性！",
                Type = AnnouncementType.Bugfix,
                Priority = AnnouncementPriority.Critical,
                CreatedAt = DateTime.Now,
                IsActive = true
            });
            
            // v2.4.0.0 官方1.8.0.0完全同步
            announcements.Add(new AnnouncementItem
            {
                Id = "v2400_official_180_sync",
                Title = "Nitrox v2.4.0.0 官方1.8.0.0完全同步",
                Content = "史诗级更新！完全同步官方Nitrox 1.8.0.0所有功能：【世界特性】天空盒云同步、生物重生、果实生长收获、辐射持久化、Reefback产卵、喷泉喷发、生物死亡、照明弹、潜行者牙齿掉落、时间胶囊同步 【利维坦】收割者/幽灵/海踏浪者/海龙完整行为同步 【武器系统】静止步枪、载具鱼雷、刀具PvP 【载具】Cyclops残骸、灭火器、载具传送命令支持 【基地系统】家具同步、船体洞修复、水上乐园生物繁殖、农作物持久化、垃圾桶/咖啡机、长凳防拆解（坐人时）、扫描室建造警告、建筑不同步智能检测 【生活质量】RadminVPN IP显示、游戏模式持久化、脚步声/感染动画同步、库存重连保护、聊天改进、工艺台持久化、载具自定义同步、PDA扫描改进、远程玩家生命值视觉同步、Aurora/Sunbeam故事同步、故事目标持久化 【声音修复】距离音量计算、载具引擎/海蛾/Cyclops/激光切割器全局声音修复 【Bug修复】蟹蛇体型、公共IP回退、LAN Discovery崩溃、碎片生成、Cyclops声纳、载具健康、前体传送器、蓝图解锁、Aurora模型、按键绑定重置、Discord活动、水中容器、推进炮、断开连接提示 【本地化】完全同步官方最新39种语言翻译文件，包含简体中文103个最新翻译键 【额外修复】故事PDA终端同步、Discord跨平台支持、Cyclops健康同步优化。100%功能覆盖率，103项语言本地化，为您带来最完整的多人游戏体验！",
                Type = AnnouncementType.Feature,
                Priority = AnnouncementPriority.Critical,
                CreatedAt = DateTime.Now.AddHours(-1),
                IsActive = true
            });
            
            // v2.3.6.7 成就系统上线
            announcements.Add(new AnnouncementItem
            {
                Id = "v2367_achievement_system",
                Title = "Nitrox v2.3.6.7 成就系统正式上线",
                Content = "版本号已更新至2.3.6.7。本次重磅更新：1.全新成就系统，追踪您的游戏进度和里程碑 2.成就解锁实时通知 3.完整的成就持久化存储 4.优化公告系统，移除表情符号确保跨平台兼容性。前往成就页面查看所有可解锁的成就，开始您的收集之旅！",
                Type = AnnouncementType.Feature,
                Priority = AnnouncementPriority.High,
                CreatedAt = DateTime.Now.AddHours(-1),
                IsActive = true
            });
            
            // v2.3.6.5 社区链接完善更新
            announcements.Add(new AnnouncementItem
            {
                Id = "v2365_community_links_expansion",
                Title = "Nitrox v2.3.6.5 社区链接完善",
                Content = "版本号已更新至2.3.6.5。本次更新内容：完善社区贡献者B站链接，为逃避现实の幻想乡、提纯源岩添加社交平台跳转功能。现在您可以在贡献者页面直接访问所有主要贡献者的B站主页，更便捷地了解和支持社区创作者。持续优化用户体验，感谢中文社区全体成员的辛勤付出。",
                Type = AnnouncementType.Feature,
                Priority = AnnouncementPriority.Normal,
                CreatedAt = DateTime.Now.AddMinutes(-30),
                IsActive = true
            });
            
            // v2.3.6.4 更新公告
            announcements.Add(new AnnouncementItem
            {
                Id = "v2364_version_update",
                Title = "Nitrox v2.3.6.4 版本发布",
                Content = "版本号已更新至2.3.6.4。本次更新：1.同步官方PR#2469修复fragments位置错误问题 2.为贡献者页面添加B站社交链接 3.新增公告详情弹窗功能 4.界面布局持续优化 感谢社区贡献者们的支持！",
                Type = AnnouncementType.Feature,
                Priority = AnnouncementPriority.Normal,
                CreatedAt = DateTime.Now.AddMinutes(-30),
                IsActive = true
            });
            
            // v2.3.5.5 公告系统上线
            announcements.Add(new AnnouncementItem
            {
                Id = "v2355_enhanced_announcement",
                Title = "全新公告系统上线",
                Content = "全新设计的公告系统已上线！新增优先级颜色标识、公告类型图标、内容预览等功能。公告现在更易于阅读和区分重要程度。",
                Type = AnnouncementType.Feature,
                Priority = AnnouncementPriority.Normal,
                CreatedAt = DateTime.Now.AddHours(-1),
                IsActive = true
            });
            
            // v2.3.5.5 界面优化公告
            announcements.Add(new AnnouncementItem
            {
                Id = "v2355_ui_update",
                Title = "Nitrox v2.3.5.5 界面优化更新",
                Content = "本次更新内容：1. 重构社区贡献者页面，添加新贡献者（面包没睡醒、无尽夏、面包的游戏群），优化UI布局 2. 统一所有版本号显示 3. 优化开始游戏页面布局 4. 完全汉化服务器管理页面 5. 感谢所有社区贡献者的支持！",
                Type = AnnouncementType.Feature,
                Priority = AnnouncementPriority.Low,
                CreatedAt = DateTime.Now.AddHours(-2),
                IsActive = true
            });
            
            // v2.3.5.4 更新公告
            announcements.Add(new AnnouncementItem
            {
                Id = "v2354_major_update",
                Title = "Nitrox v2.3.5.4 全新架构发布",
                Content = "史上最大更新！全新 .NET Generic Host 服务端架构、双模式服务端支持（新版/传统）、增强的Steam验证系统、完整中文汉化、资源冲突修复、显著提升性能和稳定性！",
                Type = AnnouncementType.Feature,
                Priority = AnnouncementPriority.Normal,
                CreatedAt = DateTime.Now.AddDays(-1),
                IsActive = true
            });
            
            // v2.3.5.0 重大更新公告（保留为历史记录）
            announcements.Add(new AnnouncementItem
            {
                Id = "v235_major_update", 
                Title = "Nitrox v2.3.5.0 重大更新",
                Content = "重要同步修复：修复联机睡觉卡死问题、新增咖啡机多人同步功能、优化网络同步稳定性。",
                Type = AnnouncementType.Feature,
                Priority = AnnouncementPriority.Low,
                CreatedAt = DateTime.Now.AddDays(-7),
                IsActive = false
            });

            announcements.Add(new AnnouncementItem
            {
                Id = "sleep_freeze_fixed",
                Title = "重大修复：睡觉卡死问题",
                Content = "完全解决了多人游戏中使用床铺睡觉时的卡死问题！现在时间跳过功能在多人模式下正常工作，不再需要重新连接服务器。",
                Type = AnnouncementType.Bugfix,
                Priority = AnnouncementPriority.Normal,
                CreatedAt = DateTime.Now.AddDays(-2),
                IsActive = true
            });

            announcements.Add(new AnnouncementItem
            {
                Id = "new_server_engine_guide",
                Title = "新服务端引擎使用指南",
                Content = "如何启用新服务端模式：1. 在服务器管理页面勾选\"使用新服务端引擎\" 2. 保存设置并重启服务器 3. 享受更强性能和Mod支持！传统模式仍可用，确保兼容性。新版本支持现代异步架构，为未来扩展奠定基础！",
                Type = AnnouncementType.Tips,
                Priority = AnnouncementPriority.Normal,
                CreatedAt = DateTime.Now.AddDays(-1),
                IsActive = true
            });

            announcements.Add(new AnnouncementItem
            {
                Id = "coffee_machine_sync",
                Title = "新功能：咖啡机多人同步",
                Content = "同步官方最新功能！现在其他玩家使用咖啡机时，你也能听到音效并看到视觉效果。真正的多人协作体验，每一个细节都不会错过！",
                Type = AnnouncementType.Feature,
                Priority = AnnouncementPriority.Low,
                CreatedAt = DateTime.Now.AddDays(-3),
                IsActive = true
            });

            announcements.Add(new AnnouncementItem
            {
                Id = "sponsor_thanks",
                Title = "感谢赞助者支持",
                Content = "特别感谢 Volt_伏特 的慷慨赞助！您的支持让 Nitrox 项目得以持续发展。查看赞助支持页面了解更多信息。",
                Type = AnnouncementType.Info,
                Priority = AnnouncementPriority.Low,
                CreatedAt = DateTime.Now.AddDays(-4),
                IsActive = true
            });

            announcements.Add(new AnnouncementItem
            {
                Id = "below_zero_support",
                Title = "全新功能：Below Zero 独立服务器",
                Content = "现在您可以创建和管理 Below Zero 专用服务器，享受独立的游戏体验！前往服务器管理页面开始使用。",
                Type = AnnouncementType.Feature,
                Priority = AnnouncementPriority.Medium,
                CreatedAt = DateTime.Now.AddHours(-1),
                IsActive = true
            });

            announcements.Add(new AnnouncementItem
            {
                Id = "performance_tips",
                Title = "性能优化建议",
                Content = "为了获得最佳游戏体验，建议关闭其他占用内存的程序，并确保游戏安装在 SSD 硬盘上。",
                Type = AnnouncementType.Tips,
                Priority = AnnouncementPriority.Low,
                CreatedAt = DateTime.Now.AddDays(-1),
                IsActive = true
            });

            Log.Info($"已加载 {announcements.Count} 条公告");
        }
        catch (Exception ex)
        {
            Log.Error($"加载公告失败: {ex.Message}");
        }
    }

    public void AddAnnouncement(AnnouncementItem announcement)
    {
        announcements.Insert(0, announcement); // 新公告插入到顶部
    }

}
