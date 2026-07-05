// ────────────────────────────────
//
// ────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace WpfUI.Core.Base;

public record NavigationData(
    string Title, string IconKey,
    Type ViewType,
    Type ViewModelType,
    Type? ExplorerViewType = null,
    Type? ExplorerViewModelType = null,
    Type? DocumentViewType = null,
    Type? DocumentViewModelType = null);

public static class NavigationExtensions
{
    public static IServiceCollection AddNavigation(this IServiceCollection services, Action<Builder> configure)
    {
        var builder = new Builder(services);
        configure(builder);

        // 登録されたデータを IEnumerable<NavigationData> として確定
        services.AddSingleton<IEnumerable<NavigationData>>(builder.NavigationList);
        return services;
    }

    public class Builder(IServiceCollection services)
    {
        public List<NavigationData> NavigationList { get; } = [];

        public Builder Add<TView, TViewModel>(string title, string iconkey)
            where TView : class
            where TViewModel : FeatureViewModelBase
        {
            services.AddTransient<TView>();
            services.AddTransient<TViewModel>();
            NavigationList.Add(new NavigationData(
                title, iconkey,
                typeof(TView),
                typeof(TViewModel)));
            return this;
        }

        public Builder Add<TView, TViewModel, TExtraView, TExtraViewModel>(string title, string iconkey)
            where TView : class
            where TViewModel : FeatureViewModelBase
            where TExtraView : class
            where TExtraViewModel : class
        {
            services.AddTransient<TView>();
            services.AddTransient<TViewModel>();
            services.AddTransient<TExtraView>();
            services.AddTransient<TExtraViewModel>();

            if (typeof(ExplorerViewModelBase).IsAssignableFrom(typeof(TExtraViewModel)))
            {
                NavigationList.Add(new NavigationData(
                    title, iconkey,
                    typeof(TView), typeof(TViewModel),
                    typeof(TExtraView), typeof(TExtraViewModel)));
            }
            else if (typeof(DocumentViewModelBase).IsAssignableFrom(typeof(TExtraViewModel)))
            {
                NavigationList.Add(new NavigationData(
                    title, iconkey,
                    typeof(TView), typeof(TViewModel),
                    null, null,
                    typeof(TExtraView), typeof(TExtraViewModel)));
            }
            return this;
        }

        public Builder Add<TView, TViewModel, TExplorerView, TExplorerViewModel, TDocumentView, TDocumentViewModel>(string title, string iconkey)
            where TView : class
            where TViewModel : FeatureViewModelBase
            where TExplorerView : class
            where TExplorerViewModel : ExplorerViewModelBase
            where TDocumentView : class
            where TDocumentViewModel : DocumentViewModelBase
        {
            services.AddTransient<TView>();
            services.AddTransient<TViewModel>();
            services.AddTransient<TExplorerView>();
            services.AddTransient<TExplorerViewModel>();
            services.AddTransient<TDocumentView>();
            services.AddTransient<TDocumentViewModel>();
            NavigationList.Add(new NavigationData(
                title, iconkey, typeof(TView), typeof(TViewModel),
                typeof(TExplorerView), typeof(TExplorerViewModel),
                typeof(TDocumentView), typeof(TDocumentViewModel)));
            return this;
        }
    }
}
