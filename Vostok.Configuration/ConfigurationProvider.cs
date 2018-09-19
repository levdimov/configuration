using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Vostok.Configuration.Abstractions;
using Vostok.Configuration.Abstractions.Attributes;
using Vostok.Configuration.Binders;
using Vostok.Configuration.Extensions;
using Vostok.Configuration.Sources;

namespace Vostok.Configuration
{
    public class ConfigurationProvider : IConfigurationProvider
    {
        private static readonly string UnknownTypeExceptionMsg = $"{nameof(IConfigurationSource)} for specified type \"typeName\" is absent. User {nameof(SetupSourceFor)} to add source.";
        private readonly ConfigurationProviderSettings settings;

        private readonly ConcurrentDictionary<Type, object> typeCache;
        private readonly ConcurrentQueue<Type> typeCacheQueue;
        private readonly ConcurrentDictionary<Type, IConfigurationSource> typeSources;
        private readonly ConcurrentDictionary<Type, IObservable<object>> typeWatchers;
        private readonly TypedTaskSource taskSource;

        private readonly ConcurrentDictionary<IConfigurationSource, object> sourceCache;
        private readonly ConcurrentQueue<IConfigurationSource> sourceCacheQueue;

        /// <summary>
        ///     Creates a <see cref="ConfigurationProvider" /> instance with given settings
        ///     <paramref name="configurationProviderSettings" />
        /// </summary>
        /// <param name="configurationProviderSettings">
        ///     Provider settings. Uses <see cref="DefaultSettingsBinder" /> if
        ///     <see cref="ConfigurationProviderSettings.Binder" /> is null.
        /// </param>
        public ConfigurationProvider(ConfigurationProviderSettings configurationProviderSettings = null)
        {
            settings = configurationProviderSettings ?? new ConfigurationProviderSettings();
            if (settings.Binder == null)
                settings.Binder = new DefaultSettingsBinder().WithDefaultParsers();

            typeSources = new ConcurrentDictionary<Type, IConfigurationSource>();
            typeWatchers = new ConcurrentDictionary<Type, IObservable<object>>();
            typeCache = new ConcurrentDictionary<Type, object>();
            typeCacheQueue = new ConcurrentQueue<Type>();
            sourceCache = new ConcurrentDictionary<IConfigurationSource, object>();
            sourceCacheQueue = new ConcurrentQueue<IConfigurationSource>();
            taskSource = new TypedTaskSource();
        }

        /// <inheritdoc />
        /// <summary>
        ///     <para>Returns value of given type <typeparamref name="TSettings" /> using binder from constructor.</para>
        ///     <para>Uses cache.</para>
        /// </summary>
        public TSettings Get<TSettings>()
        {
            var type = typeof(TSettings);
            if (typeCache.TryGetValue(type, out var item))
                return (TSettings)item;
            if (!typeSources.ContainsKey(type))
                throw new ArgumentException($"{UnknownTypeExceptionMsg.Replace("typeName", type.Name)}");
            return taskSource.Get(Observe<TSettings>());
        }

        /// <inheritdoc />
        /// <summary>
        ///     Returns value of given type <typeparamref name="TSettings" /> from specified <paramref name="source" />.
        /// </summary>
        public TSettings Get<TSettings>(IConfigurationSource source)
        {
            return sourceCache.TryGetValue(source, out var item)
                ? (TSettings)item
                : taskSource.Get(Observe<TSettings>(source));
        }

        /// <inheritdoc />
        /// <summary>
        ///     <para>Subscribtion to see changes in source.</para>
        ///     <para>Returns current value immediately on subscribtion.</para>
        /// </summary>
        /// <returns>Event with new value</returns>
        public IObservable<TSettings> Observe<TSettings>()
        {
            var type = typeof(TSettings);
            if (!typeWatchers.ContainsKey(type) && typeSources.ContainsKey(type))
                typeWatchers[type] = typeSources[type].Observe().Select(p => SubscribeWatcher<TSettings>(p.settings));

            if (typeWatchers.TryGetValue(type, out var watcher))
                return watcher.Select(TypedSubscriptionPrepare<TSettings>);

            throw new NullReferenceException($"{nameof(ConfigurationProvider)}: watcher for type \"{type.Name}\" not found.");
        }

        /// <inheritdoc />
        /// <summary>
        ///     <para>Subscribtion to see changes in specified <paramref name="source" />.</para>
        ///     <para>Returns current value immediately on subscribtion.</para>
        /// </summary>
        /// <returns>Event with new value</returns>
        public IObservable<TSettings> Observe<TSettings>(IConfigurationSource source)
        {
            return source.Observe().Select(s => SourcedSubscriptionPrepare<TSettings>(source, s.settings));
        }

        public IObservable<(TSettings settings, Exception error)> ObserveWithErrors<TSettings>()
        {
            throw new NotImplementedException();
        }

        public IObservable<(TSettings settings, Exception error)> ObserveWithErrors<TSettings>(IConfigurationSource source)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Changes source to combination of source for given type <typeparamref name="TSettings" /> and
        ///     <paramref name="source" />
        /// </summary>
        /// <typeparam name="TSettings">Type of souce to combine with</typeparam>
        /// <param name="source">Second souce to combine with</param>
        public ConfigurationProvider SetupSourceFor<TSettings>(IConfigurationSource source)
        {
            var type = typeof(TSettings);
            var hasWatcher = typeWatchers.ContainsKey(type);
            if (hasWatcher)
                throw new InvalidOperationException($"{nameof(ConfigurationProvider)}: it is not allowed to add sources for \"{type.Name}\" to a {nameof(ConfigurationProvider)} after {nameof(Get)}() or {nameof(Observe)}() was called for this type.");
            if (!hasWatcher && typeCache.ContainsKey(type))
                throw new InvalidOperationException($"{nameof(ConfigurationProvider)}: it is not allowed to add sources for \"{type.Name}\" to a {nameof(ConfigurationProvider)} after {nameof(SetManually)}() was called for this type.");

            if (typeSources.TryGetValue(type, out var existingSource))
                source = existingSource.Combine(source);
            typeSources[type] = source;

            return this;
        }

        public ConfigurationProvider SetManually<TSettings>(TSettings value, bool validate = false)
        {
            if (validate)
                Validate(value);
            AddInCache(value);
            return this;
        }

        private TSettings TypedSubscriptionPrepare<TSettings>(object node)
        {
            try
            {
                var value = (TSettings)node;
                AddInCache(value);
                return value;
            }
            catch (Exception e)
            {
                if (typeCache.TryGetValue(typeof(TSettings), out var val) && val != null)
                {
                    settings.ErrorCallBack?.Invoke(e);
                    return (TSettings)val;
                }

                throw;
            }
        }

        private void AddInCache<TSettings>(TSettings value)
        {
            var type = typeof(TSettings);
            if (!typeCache.ContainsKey(type))
                typeCacheQueue.Enqueue(type);
            typeCache.AddOrUpdate(type, value, (t, o) => value);
            if (typeCache.Count > settings.MaxTypeCacheSize && typeCacheQueue.TryDequeue(out var tp))
                typeCache.TryRemove(tp, out _);
        }

        private TSettings SourcedSubscriptionPrepare<TSettings>(IConfigurationSource source, ISettingsNode node)
        {
            try
            {
                var value = ValidatedBind<TSettings>(node);
                if (!sourceCache.ContainsKey(source))
                    sourceCacheQueue.Enqueue(source);
                sourceCache.AddOrUpdate(source, value, (t, o) => value);
                if (sourceCache.Count > settings.MaxSourceCacheSize && sourceCacheQueue.TryDequeue(out var src))
                    sourceCache.TryRemove(src, out _);
                return value;
            }
            catch (Exception e)
            {
                if (sourceCache.TryGetValue(source, out var val) && val != null)
                {
                    settings.ErrorCallBack?.Invoke(e);
                    return (TSettings)val;
                }

                throw;
            }
        }

        private object SubscribeWatcher<TSettings>(ISettingsNode rs)
        {
            var type = typeof(TSettings);
            if (!typeSources.TryGetValue(type, out _))
                throw new ArgumentException($"{UnknownTypeExceptionMsg.Replace("typeName", type.Name)}");

            try
            {
                return ValidatedBind<TSettings>(rs);
            }
            catch (Exception e)
            {
                if (typeCache.TryGetValue(typeof(TSettings), out var val) && val != null)
                {
                    settings.ErrorCallBack?.Invoke(e);
                    return (TSettings)val;
                }

                throw;
            }
        }

        private TSettings ValidatedBind<TSettings>(ISettingsNode rs)
        {
            var value = settings.Binder.Bind<TSettings>(rs);
            Validate(value);
            return value;
        }

        private void Validate<TSettings>(TSettings value)
        {
            var type = typeof(TSettings);

            var errors = Validate(type, value, "").ToList();
            if (errors.Any())
            {
                throw new SettingsValidationException(string.Join(Environment.NewLine, errors));
            }
        }

        private static IEnumerable<string> Validate(Type type, object value, string prefix)
        {
            if (value == null)
                yield break;
            if (!(type.GetCustomAttributes(typeof(ValidateByAttribute), false).FirstOrDefault() is ValidateByAttribute validAttribute))
                yield break;

            var validator = Activator.CreateInstance(validAttribute.ValidatorType);
            var validateMethod = validator.GetType().GetMethod(nameof(ISettingsValidator<object>.Validate));
            if (validateMethod == null)
                yield break; // TODO(krait): Report error.
            foreach (var error in (IEnumerable<string>)validateMethod.Invoke(validator, new[] { value }))
                yield return prefix + error;

            foreach (var field in type.GetFields())
            {
                foreach (var error in Validate(field.FieldType, field.GetValue(value), field.Name))
                    yield return prefix + error;
            }

            foreach (var prop in type.GetProperties())
            {
                foreach (var error in Validate(prop.PropertyType, prop.GetValue(value), prop.Name))
                    yield return prefix + error;
            }
        }
    }
}