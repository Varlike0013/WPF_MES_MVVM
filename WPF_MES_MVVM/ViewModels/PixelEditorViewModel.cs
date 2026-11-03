using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WPF_MES.Shared.Models;
using WPF_MES_MVVM.Models;

namespace WPF_MES_MVVM.ViewModels;

public partial class PixelEditorViewModel : ObservableObject
{
    // ========== 核心数据 ==========
    public ObservableCollection<PixelItem> Pixels { get; } = new();
    public ObservableCollection<Color> UniqueColors { get; } = new();
    public ObservableCollection<ColorGroup> ColorGroups { get; } = new();
    private bool _isUpdatingHex;
    private string _hexInput = "#000000";
    [ObservableProperty] private string _imageName = "未加载图片";
    [ObservableProperty] private string _replaceColorsText = "{[#FFFFFF,#000000],[#FF0000,#00FF00]}";

    [ObservableProperty] private int _gridRows;
    [ObservableProperty] private int _gridCols;

    // ========== 选中与颜色控制 ==========
    [ObservableProperty] private PixelItem? _selectedPixel;
    [ObservableProperty] private bool _syncSameColor = true; // 同步修改所有同色像素开关

    [ObservableProperty] private byte _red = 0;
    [ObservableProperty] private byte _green = 0;
    [ObservableProperty] private byte _blue = 0;
    [ObservableProperty] private byte _alpha = 255;

    public SolidColorBrush CurrentColorBrush { get; } = new SolidColorBrush(Colors.White);

    // 防抖定时器：拖动滑块时不立刻更新，停下来 30ms 后再更新画布
    private readonly DispatcherTimer _updateTimer;
    private Color _editingOldColor; // 记录当前正在修改的原本颜色

    public PixelEditorViewModel()
    {
        _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        _updateTimer.Tick += (_, _) =>
        {
            _updateTimer.Stop();
            ApplyColorToCanvas();
        };
    }

    // 当滑块变化时
    partial void OnRedChanged(byte value) => TriggerColorChange();
    partial void OnGreenChanged(byte value) => TriggerColorChange();
    partial void OnBlueChanged(byte value) => TriggerColorChange();
    partial void OnAlphaChanged(byte value) => TriggerColorChange();

    private void TriggerColorChange()
    {
        CurrentColorBrush.Color = Color.FromArgb(Alpha, Red, Green, Blue);

        // 同步更新文本输入框
        _isUpdatingHex = true;
        HexInput = Alpha == 255
            ? $"#{Red:X2}{Green:X2}{Blue:X2}"
            : $"#{Alpha:X2}{Red:X2}{Green:X2}{Blue:X2}";
        _isUpdatingHex = false;

        _updateTimer.Stop();
        _updateTimer.Start();
    }


    // ========== 交互命令 ==========

    /// <summary>
    /// 点击右侧像素格子
    /// </summary>
    [RelayCommand]
    private void SelectPixel(PixelItem? pixel)
    {
        if (pixel == null) return;
        SelectedPixel = pixel;

        // 带出颜色到滑块
        _editingOldColor = pixel.Color;
        SetSliders(pixel.Color);
    }

    /// <summary>
    /// 点击左侧颜色列表
    /// </summary>
    [RelayCommand]
    private void SelectColor(Color color)
    {
        _editingOldColor = color;
        SetSliders(color);
        // 不清除 SelectedPixel，允许用户在选中画笔颜色的同时，依然保留对某个格子的焦点
    }

    private void SetSliders(Color color)
    {
        _updateTimer.Stop();
        Red = color.R;
        Green = color.G;
        Blue = color.B;
        Alpha = color.A;
        CurrentColorBrush.Color = color;

        _isUpdatingHex = true;
        HexInput = color.A == 255
            ? $"#{color.R:X2}{color.G:X2}{color.B:X2}"
            : $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        _isUpdatingHex = false;
    }

    /// <summary>
    /// 将颜色应用到画布（根据开关决定替换范围）
    /// </summary>
    private void ApplyColorToCanvas()
    {
        Color newColor = Color.FromArgb(Alpha, Red, Green, Blue);

        if (SyncSameColor)
        {
            bool changed = false;
            foreach (var p in Pixels)
            {
                if (p.Color == _editingOldColor)
                {
                    p.Color = newColor;
                    changed = true;
                }
            }
            if (changed)
            {
                _editingOldColor = newColor;
                RefreshColorGroups(); // ★ 刷新分类列表
            }
        }
        else
        {
            if (SelectedPixel != null)
            {
                SelectedPixel.Color = newColor;
                _editingOldColor = newColor;
                RefreshColorGroups(); // ★ 刷新分类列表
            }
        }
    }

    /// <summary>
    /// 重新统计画布颜色
    /// </summary>
    private void RefreshColorGroups()
    {
        var stats = Pixels.GroupBy(p => p.Color)
                          .Select(g => new { Color = g.Key, Count = g.Count() })
                          .OrderByDescending(x => x.Count)
                          .ToList();

        // 如果数据没有变化（例如只滑动了但没有改颜色），则不刷新防止闪烁
        if (stats.Count == ColorGroups.Count)
        {
            bool isSame = true;
            for (int i = 0; i < stats.Count; i++)
            {
                if (stats[i].Color != ColorGroups[i].Color || stats[i].Count != ColorGroups[i].Count)
                {
                    isSame = false; break;
                }
            }
            if (isSame) return;
        }

        ColorGroups.Clear();
        foreach (var item in stats)
        {
            ColorGroups.Add(new ColorGroup(item.Color, item.Count));
        }
    }

    private void RefreshUniqueColors()
    {
        // 提取画布上所有出现过的颜色，去重并排序
        var colors = Pixels.Select(p => p.Color).Distinct().OrderByDescending(c => c.A).ToList();

        // 简单比对，如果没变就不刷新集合，防止闪烁
        if (colors.Count == UniqueColors.Count && !colors.Where((t, i) => t != UniqueColors[i]).Any())
            return;

        UniqueColors.Clear();
        foreach (var c in colors)
        {
            UniqueColors.Add(c);
        }
    }
    public string HexInput
    {
        get => _hexInput;
        set
        {
            if (_hexInput == value) return;
            _hexInput = value;
            OnPropertyChanged(nameof(HexInput));

            if (_isUpdatingHex) return; // 防止死循环

            // 解析输入的 Hex
            if (!string.IsNullOrWhiteSpace(value))
            {
                string hex = value.TrimStart('#');
                try
                {
                    if (hex.Length == 6) // RRGGBB
                    {
                        Red = Convert.ToByte(hex.Substring(0, 2), 16);
                        Green = Convert.ToByte(hex.Substring(2, 2), 16);
                        Blue = Convert.ToByte(hex.Substring(4, 2), 16);
                        Alpha = 255; // 没写透明度默认 255
                    }
                    else if (hex.Length == 8) // AARRGGBB
                    {
                        Alpha = Convert.ToByte(hex.Substring(0, 2), 16);
                        Red = Convert.ToByte(hex.Substring(2, 2), 16);
                        Green = Convert.ToByte(hex.Substring(4, 2), 16);
                        Blue = Convert.ToByte(hex.Substring(6, 2), 16);
                    }
                }
                catch
                {
                    // 输入了非法的 Hex，忽略
                }
            }
        }
    }

    // ========== 文件操作 ==========

    [RelayCommand]
    private void LoadImage()
    {
        var dlg = new OpenFileDialog { Filter = "图片|*.png;*.jpg;*.bmp" };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var bitmap = new BitmapImage(new Uri(dlg.FileName));
            if (bitmap.PixelWidth > 128 || bitmap.PixelHeight > 128)
            {
                MessageBox.Show("图片过大，请选择宽高不超过 128 像素的图片。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ImageName = Path.GetFileName(dlg.FileName);

            GridRows = bitmap.PixelHeight;
            GridCols = bitmap.PixelWidth;

            var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
            int stride = converted.PixelWidth * 4;
            byte[] pixels = new byte[converted.PixelHeight * stride];
            converted.CopyPixels(pixels, stride, 0);

            Pixels.Clear();
            for (int y = 0; y < converted.PixelHeight; y++)
            {
                for (int x = 0; x < converted.PixelWidth; x++)
                {
                    int index = (y * stride) + (x * 4);
                    var color = Color.FromArgb(pixels[index + 3], pixels[index + 2], pixels[index + 1], pixels[index]);
                    Pixels.Add(new PixelItem(x, y, color));
                }
            }
            RefreshUniqueColors();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private void SaveImage()
    {
        if (Pixels.Count == 0) return;

        var dlg = new SaveFileDialog { Filter = "PNG 图片|*.png", DefaultExt = ".png", FileName = "NewPixelArt" };
        if (dlg.ShowDialog() != true) return;

        try
        {
            byte[] pixels = new byte[GridCols * GridRows * 4];
            for (int i = 0; i < Pixels.Count; i++)
            {
                var color = Pixels[i].Color;
                pixels[i * 4 + 0] = color.B;
                pixels[i * 4 + 1] = color.G;
                pixels[i * 4 + 2] = color.R;
                pixels[i * 4 + 3] = color.A;
            }

            var writeableBitmap = new WriteableBitmap(GridCols, GridRows, 96, 96, PixelFormats.Bgra32, null);
            writeableBitmap.WritePixels(new Int32Rect(0, 0, GridCols, GridRows), pixels, GridCols * 4, 0);

            using var fs = new FileStream(dlg.FileName, FileMode.Create);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(writeableBitmap));
            encoder.Save(fs);

            MessageBox.Show("保存成功！");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败: {ex.Message}");
        }
    }
    [RelayCommand]
    private void ExportColorTxt()
    {
        if (Pixels.Count == 0)
        {
            MessageBox.Show("画布为空，没有可导出的颜色信息。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dlg = new SaveFileDialog
        {
            Filter = "文本文档|*.txt",
            DefaultExt = ".txt",
            FileName = "ColorPalette_Export"
        };

        if (dlg.ShowDialog() != true) return;

        try
        {
            // 1. 统计颜色信息 (按数量降序排列)
            var colorStats = Pixels
                .GroupBy(p => p.Color)
                .Select(g => new { Color = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            // 2. 构建文本内容
            StringBuilder sb = new StringBuilder();

            // 头部信息
            sb.AppendLine("================= 颜色导出报告 =================");
            sb.AppendLine($"ImageName={ImageName}");
            sb.AppendLine($"图片分辨率: {GridCols} x {GridRows}");
            sb.AppendLine($"总像素数量: {Pixels.Count}");
            sb.AppendLine($"不同颜色数: {colorStats.Count}");
            sb.AppendLine($"导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("------------------------------------------------");
            sb.AppendLine(string.Format("{0,-15} {1,-25} {2,-10}", "HEX 颜色值", "RGBA 值", "像素数量"));
            sb.AppendLine("------------------------------------------------");

            // 颜色明细
            foreach (var stat in colorStats)
            {
                Color c = stat.Color;
                string hex = $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}"; // #AARRGGBB
                string rgba = $"({c.R}, {c.G}, {c.B}, {c.A})";

                // 如果透明度是 255（完全不透明），可以给个更友好的 Hex 标识
                if (c.A == 255)
                {
                    hex = $"#{c.R:X2}{c.G:X2}{c.B:X2} (不透明)";
                }

                sb.AppendLine(string.Format("{0,-15} {1,-25} {2,-10}", hex, rgba, stat.Count));
            }
            sb.AppendLine("================= 导出结束 =================");

            // 3. 写入文件 (使用 UTF-8 编码防止中文乱码)
            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);

            MessageBox.Show($"颜色信息已成功导出！\n共统计 {colorStats.Count} 种颜色。", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    // ========== 1. 导出点阵 TXT ==========
    [RelayCommand]
    private void ExportMatrixTxt()
    {
        if (Pixels.Count == 0) return;

        var dlg = new SaveFileDialog { Filter = "点阵文件|*.txt", DefaultExt = ".txt", FileName = "PixelMatrix" };
        if (dlg.ShowDialog() != true) return;

        try
        {
            // 1. 统计颜色并按出现次数排序（出现最多的分配 '.'，依次分配 1-9, A-Z, a-z）
            var colorStats = Pixels.GroupBy(p => p.Color)
                                   .Select(g => new { Color = g.Key, Count = g.Count() })
                                   .OrderByDescending(x => x.Count)
                                   .ToList();

            string charPool = ".123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            if (colorStats.Count > charPool.Length)
            {
                MessageBox.Show($"颜色种类超过了 {charPool.Length} 种，无法生成点阵字符！", "错误");
                return;
            }

            var colorToChar = new Dictionary<Color, char>();
            var charToColor = new Dictionary<char, Color>();
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("[ImageInfo]");
            sb.AppendLine($"InputName={ImageName}");
            sb.AppendLine($"OutputName={Path.GetFileName(dlg.FileName)}");
            sb.AppendLine($"Width={GridCols}");
            sb.AppendLine($"Height={GridRows}");
            sb.AppendLine();

            sb.AppendLine("[Colors]");
            for (int i = 0; i < colorStats.Count; i++)
            {
                var c = colorStats[i].Color;
                char ch = charPool[i];
                colorToChar[c] = ch;
                charToColor[ch] = c;
                sb.AppendLine($"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}={ch}");
            }
            sb.AppendLine();

            sb.AppendLine("[Matrix]");
            for (int y = 0; y < GridRows; y++)
            {
                for (int x = 0; x < GridCols; x++)
                {
                    var pixel = Pixels[y * GridCols + x];
                    sb.Append(colorToChar[pixel.Color]);
                }
                sb.AppendLine();
            }

            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
            MessageBox.Show("点阵文件导出成功！", "成功");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败: {ex.Message}");
        }
    }

    // ========== 2. 导入点阵 TXT ==========
    [RelayCommand]
    private void ImportMatrixTxt()
    {
        var dlg = new OpenFileDialog { Filter = "点阵文件|*.txt" };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var lines = File.ReadAllLines(dlg.FileName);
            var charToColor = new Dictionary<char, Color>();
            List<string> matrixLines = new List<string>();
            int width = 0, height = 0;

            string section = "";
            foreach (var line in lines)
            {
                string trim = line.Trim();
                if (string.IsNullOrEmpty(trim)) continue;

                if (trim.StartsWith("[") && trim.EndsWith("]"))
                {
                    section = trim;
                    continue;
                }

                if (section == "[ImageInfo]")
                {
                    if (trim.StartsWith("Width=")) width = int.Parse(trim.Substring(6));
                    if (trim.StartsWith("Height=")) height = int.Parse(trim.Substring(7));
                }
                else if (section == "[Colors]")
                {
                    var parts = trim.Split('=');
                    if (parts.Length == 2 && parts[0].StartsWith("#") && parts[1].Length == 1)
                    {
                        string hex = parts[0].TrimStart('#');
                        byte a = 255, r = 0, g = 0, b = 0;
                        if (hex.Length == 6)
                        {
                            r = Convert.ToByte(hex.Substring(0, 2), 16);
                            g = Convert.ToByte(hex.Substring(2, 2), 16);
                            b = Convert.ToByte(hex.Substring(4, 2), 16);
                        }
                        else if (hex.Length == 8)
                        {
                            a = Convert.ToByte(hex.Substring(0, 2), 16);
                            r = Convert.ToByte(hex.Substring(2, 2), 16);
                            g = Convert.ToByte(hex.Substring(4, 2), 16);
                            b = Convert.ToByte(hex.Substring(6, 2), 16);
                        }
                        charToColor[parts[1][0]] = Color.FromArgb(a, r, g, b);
                    }
                }
                else if (section == "[Matrix]")
                {
                    matrixLines.Add(trim);
                }
            }

            if (width <= 0 || height <= 0 || matrixLines.Count != height)
            {
                MessageBox.Show("文件格式不正确或尺寸不匹配！", "解析失败");
                return;
            }

            // 重置画布
            Pixels.Clear();
            GridCols = width;
            GridRows = height;

            foreach (var line in matrixLines)
            {
                foreach (var ch in line)
                {
                    if (charToColor.TryGetValue(ch, out Color c))
                    {
                        Pixels.Add(new PixelItem(0, 0, c)); // X,Y 这里不影响显示，只是为了占位
                    }
                    else
                    {
                        Pixels.Add(new PixelItem(0, 0, Colors.Transparent)); // 找不到的字符用透明替代
                    }
                }
            }
            RefreshColorGroups();
            MessageBox.Show("点阵文件导入成功！", "成功");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导入失败: {ex.Message}");
        }
    }

    // ========== 3. 一键批量替换颜色 ==========
    [RelayCommand]
    private void ExecuteColorReplace()
    {
        if (string.IsNullOrWhiteSpace(ReplaceColorsText) || Pixels.Count == 0) return;

        try
        {
            // 使用正则匹配 [{#111111,#222222},{#333333,#444444}] 或 [[#111111,#222222],[#333333,#444444]]
            var regex = new Regex(@"\[?(#[0-9A-Fa-f]{6,8})\s*,\s*(#[0-9A-Fa-f]{6,8})\]?");
            var matches = regex.Matches(ReplaceColorsText);

            if (matches.Count == 0)
            {
                MessageBox.Show("替换格式不正确，请使用 {[#FFFFFF,#000000]} 格式", "提示");
                return;
            }

            var replaceDict = new Dictionary<Color, Color>();
            foreach (Match m in matches)
            {
                Color oldC = (Color)ColorConverter.ConvertFromString(m.Groups[1].Value);
                Color newC = (Color)ColorConverter.ConvertFromString(m.Groups[2].Value);
                replaceDict[oldC] = newC;
            }

            int replaceCount = 0;
            foreach (var p in Pixels)
            {
                if (replaceDict.TryGetValue(p.Color, out Color newC))
                {
                    p.Color = newC;
                    replaceCount++;
                }
            }

            if (replaceCount > 0)
            {
                RefreshColorGroups();
                MessageBox.Show($"成功替换了 {replaceCount} 个像素的颜色！", "完成");
            }
            else
            {
                MessageBox.Show("画布上没有找到需要替换的旧颜色。", "提示");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"替换失败，请检查颜色格式。错误: {ex.Message}");
        }
    }
}