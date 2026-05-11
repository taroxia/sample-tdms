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

public record NavigationData(Type ViewType, Type ViewModelType, string Title, string IconKey);

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
            where TViewModel : class
        {
            services.AddTransient<TView>();
            services.AddTransient<TViewModel>();
            NavigationList.Add(new NavigationData(typeof(TView), typeof(TViewModel), title, iconkey));
            return this;
        }
    }
}
