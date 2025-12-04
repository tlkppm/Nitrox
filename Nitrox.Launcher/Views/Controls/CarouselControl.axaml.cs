using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

namespace Nitrox.Launcher.Views.Controls;

public partial class CarouselControl : UserControl
{
    private readonly List<string> _imageUrls = new();
    private int _currentIndex = 0;
    private Timer? _autoPlayTimer;
    private readonly List<Image> _images = new();
    private readonly List<Border> _dots = new();
    
    // 图片资源路径
    private readonly string[] _carouselImages = 
    {
        "/Assets/Images/banners/home.png",
        "/Assets/Images/banners/Subnautica.jpg"
    };

    public CarouselControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        InitializeCarousel();
        StartAutoPlay();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        StopAutoPlay();
    }

    private void InitializeCarousel()
    {
        var imagesContainer = this.FindControl<Grid>("ImagesContainer");
        var dotsContainer = this.FindControl<StackPanel>("DotsContainer");
        var prevButton = this.FindControl<Button>("PrevButton");
        var nextButton = this.FindControl<Button>("NextButton");

        if (imagesContainer == null || dotsContainer == null || prevButton == null || nextButton == null)
            return;

        // 添加图片
        for (int i = 0; i < _carouselImages.Length; i++)
        {
            var image = new Image
            {
                Classes = { "carousel-image" },
                Source = GetBitmapFromAsset(_carouselImages[i])
            };
            
            if (i == 0)
            {
                image.Classes.Add("active");
            }
            
            _images.Add(image);
            imagesContainer.Children.Add(image);
        }

        // 添加指示器圆点
        for (int i = 0; i < _carouselImages.Length; i++)
        {
            var dot = new Border
            {
                Classes = { "carousel-dot" }
            };
            
            if (i == 0)
            {
                dot.Classes.Add("active");
            }

            var index = i; // 捕获循环变量
            dot.PointerPressed += (_, _) => GoToSlide(index);
            
            _dots.Add(dot);
            dotsContainer.Children.Add(dot);
        }

        // 绑定按钮事件
        prevButton.Click += (_, _) => PreviousSlide();
        nextButton.Click += (_, _) => NextSlide();
    }

    private Bitmap? GetBitmapFromAsset(string assetPath)
    {
        try
        {
            // 方法1：尝试使用avares协议
            var uri = new Uri($"avares://Nitrox.Launcher{assetPath}");
            if (AssetLoader.Exists(uri))
            {
                using var stream = AssetLoader.Open(uri);
                return new Bitmap(stream);
            }
            
            // 方法2：尝试相对路径
            var relativeUri = new Uri(assetPath, UriKind.Relative);
            if (AssetLoader.Exists(relativeUri))
            {
                using var stream = AssetLoader.Open(relativeUri);
                return new Bitmap(stream);
            }
            
            System.Diagnostics.Debug.WriteLine($"Asset not found: {assetPath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load image: {assetPath}, Error: {ex.Message}");
        }
        return null;
    }

    private void GoToSlide(int index)
    {
        if (index < 0 || index >= _images.Count || index == _currentIndex)
            return;

        // 更新图片
        _images[_currentIndex].Classes.Remove("active");
        _images[index].Classes.Add("active");

        // 更新圆点
        _dots[_currentIndex].Classes.Remove("active");
        _dots[index].Classes.Add("active");

        _currentIndex = index;
        
        // 重启自动播放定时器
        RestartAutoPlay();
    }

    private void NextSlide()
    {
        var nextIndex = (_currentIndex + 1) % _images.Count;
        GoToSlide(nextIndex);
    }

    private void PreviousSlide()
    {
        var prevIndex = (_currentIndex - 1 + _images.Count) % _images.Count;
        GoToSlide(prevIndex);
    }

    private void StartAutoPlay()
    {
        _autoPlayTimer = new Timer((_) => 
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => 
            {
                NextSlide();
            });
        }, null, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(4));
    }

    private void StopAutoPlay()
    {
        _autoPlayTimer?.Dispose();
        _autoPlayTimer = null;
    }

    private void RestartAutoPlay()
    {
        StopAutoPlay();
        StartAutoPlay();
    }
}
