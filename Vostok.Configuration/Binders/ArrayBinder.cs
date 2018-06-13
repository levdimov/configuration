﻿using System;
using System.Linq;
using Vostok.Configuration.SettingsTree;

namespace Vostok.Configuration.Binders
{
    internal class ArrayBinder<T> : ISettingsBinder<T>
    {
        private readonly ISettingsBinderFactory binderFactory;

        public ArrayBinder(ISettingsBinderFactory binderFactory) =>
            this.binderFactory = binderFactory;

        public T Bind(ISettingsNode settings)
        {
            var subType = typeof(T).GetElementType();
            var binder = binderFactory.CreateForType(subType);
            SettingsNode.CheckSettings(settings);

            var i = 0;
            var instance = Array.CreateInstance(subType, settings.Children.Count());
            foreach (var value in settings.Children.Select(n => binder.Bind(n)))
                instance.SetValue(value, i++);

            return (T)(object)instance;
        }
    }
}