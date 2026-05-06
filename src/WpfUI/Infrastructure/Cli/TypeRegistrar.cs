using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace WpfUI.Infrastructure.Cli;

//public sealed class TypeRegistrar(IServiceCollection services) : ITypeRegistrar
//{
//    public void Register(Type service, Type implementation) => services.AddSingleton(service, implementation);
//    public void RegisterInstance(Type service, object implementation) => services.AddSingleton(service, implementation);
//    public void RegisterLazy(Type service, Func<object> factory) => services.AddSingleton(service, _ => factory());
//    public ITypeResolver Build() => new TypeResolver(services.BuildServiceProvider());
//}

//public sealed class TypeResolver(IServiceProvider provider) : ITypeResolver
//{
//    public object? Resolve(Type? type) => type != null ? provider.GetService(type) : null;
//}


public sealed class TypeRegistrar(IServiceCollection services) : ITypeRegistrar
{
    public ITypeResolver Build() => new TypeResolver(services.BuildServiceProvider());
    public void Register(Type service, Type implementation) => services.AddSingleton(service, implementation);
    public void RegisterInstance(Type service, object implementation) => services.AddSingleton(service, implementation);
    public void RegisterLazy(Type service, Func<object> factory) => services.AddSingleton(service, _ => factory());

    private sealed class TypeResolver(IServiceProvider provider) : ITypeResolver, IDisposable
    {
        public object? Resolve(Type? type) => type != null ? provider.GetService(type) : null;
        public void Dispose() => (provider as IDisposable)?.Dispose();
    }
}