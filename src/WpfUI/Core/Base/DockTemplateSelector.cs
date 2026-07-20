// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using AvalonDock.Controls;
using Microsoft.Extensions.DependencyInjection;
// ---.
using WpfUI.Features.Shell;

namespace WpfUI.Core.Base;

public class DockTemplateSelector : DataTemplateSelector
{
    // 内部キャッシュ（動的Template生成のオーバーヘッドを削減）
    private readonly Dictionary<Type, DataTemplate> _cachedTemplates = new();
    private DataTemplate _emptyPlaceholderTemplate;

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (item == null || container is not FrameworkElement element)
        {
            return base.SelectTemplate(item, container);
        }

        var vmType = item.GetType();
        // キャッシュにあれば即返却
        if (_cachedTemplates.TryGetValue(vmType, out var cached)) return cached;

        var navDataList = App.Current is App app && app.MainWindow?.DataContext is MainViewModel mainVm
            ? mainVm.Navigation.Items
            : null;
        if (navDataList == null) return base.SelectTemplate(item, container);


        // 対応する設定を型ベースで検索
        var config = navDataList.FirstOrDefault(x => x.DocumentViewModelType == vmType);
        if (config == null) return base.SelectTemplate(item, container);

        // ケース①：受け入れ専用（ViewTypeがnull）の場合のプレースホルダー
        if (config.DocumentViewType == null)
        {
            return _emptyPlaceholderTemplate ??= CreateEmptyPlaceholderTemplate();
        }

        // ケース②：正規のViewが存在する場合（動的DataTemplateビルド）
        var template = CreateDynamicDataTemplate(config.DocumentViewType);
        _cachedTemplates[vmType] = template;
        return template;
    }

    /// <summary>
    /// 型情報から完全に動的な DataTemplate を XAMLReader 経由で生成する（OCP準拠）
    /// </summary>
    private DataTemplate CreateDynamicDataTemplate(Type viewType)
    {
        var xaml = $@"
            <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                          xmlns:view='clr-namespace:{viewType.Namespace};assembly={viewType.Assembly.GetName().Name}'>
                <view:{viewType.Name} />
            </DataTemplate>";

        return (DataTemplate)XamlReader.Parse(xaml);
    }

    /// <summary>
    /// 他の機能のドッキングビューを受け入れるための、視覚的領域を残した空テンプレート
    /// </summary>
    private DataTemplate CreateEmptyPlaceholderTemplate()
    {
        // var xaml = @"
        //     <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
        //         <Grid Background='#F5F5F5'>
        //             <Border BorderBrush='#CCCCCC' BorderThickness='2' BorderDashArray='4 4' Margin='10' CornerRadius='4'>
        //                 <TextBlock Text='ここにドメインペインをドロップして並べて比較できます' 
        //                            HorizontalAlignment='Center' 
        //                            VerticalAlignment='Center' 
        //                            Foreground='#888888' 
        //                            FontSize='12'/>
        //             </Border>
        //         </Grid>
        //     </DataTemplate>";

        var xaml = @"
            <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                <Grid Background='#F9FAFB'>
                    <Border BorderBrush='#D1D5DB' BorderThickness='2' BorderDashArray='4 4' Margin='12' CornerRadius='6'>
                        <StackPanel HorizontalAlignment='Center' VerticalAlignment='Center'>
                            <TextBlock Text='📊 ドッキング可能領域' 
                                       HorizontalAlignment='Center' Foreground='#4B5563' FontSize='13' FontWeight='Bold' Margin='0,0,0,6'/>
                            <TextBlock Text='ここに他のフローティングペインをドロップして配置できます' 
                                       HorizontalAlignment='Center' Foreground='#9CA3AF' FontSize='11'/>
                        </StackPanel>
                    </Border>
                </Grid>
            </DataTemplate>";

        return (DataTemplate)XamlReader.Parse(xaml);
    }
}
