using System;

namespace Vostok.Configuration.CurrentValueProvider
{
    internal interface ICurrentValueProviderFactory
    {
        ICurrentValueProvider<T> Create<T>(Func<IObservable<T>> observableProvider);
    }
}