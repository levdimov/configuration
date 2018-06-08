﻿using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using JetBrains.Annotations;

namespace Vostok.Configuration.Tests.Helper
{
    internal class SingleFileWatcherSubstitute : IObservable<string>
    {
        private const string DefaultSettingsValue = "\u0001";
        private readonly List<IObserver<string>> observers;
        private string currentValue;
        private readonly object locker;

        public SingleFileWatcherSubstitute([NotNull] string filePath)
        {
            observers = new List<IObserver<string>>();
            currentValue = DefaultSettingsValue;
            locker = new object();
        }

        public IDisposable Subscribe(IObserver<string> observer)
        {
            if (!observers.Contains(observer))
                observers.Add(observer);
            lock (locker)
                if (currentValue != DefaultSettingsValue)
                    observer.OnNext(currentValue);

            return Disposable.Create(
                () =>
                {
                    lock (locker)
                        if (observers.Contains(observer))
                            observers.Remove(observer);
                });
        }

        /// <summary>
        /// Imitates file creating/updating. Do not send OnNext if new and old values are equal.
        /// </summary>
        /// <param name="newValue">File content</param>
        /// <param name="ignoreIfEquals">Ignore if old and new values are equal. Always send OnNext for observers</param>
        public void GetUpdate(string newValue, bool ignoreIfEquals = false)
        {
            var isNew = newValue != currentValue;
            currentValue = newValue;
            if (isNew || ignoreIfEquals)
                foreach (var observer in observers.ToArray())
                    observer.OnNext(currentValue);
        }

        /// <summary>
        /// Imitates throwing exeptions on reading file
        /// </summary>
        /// <param name="e">Some exception</param>
        public void ThrowException(Exception e)
        {
            foreach (var observer in observers.ToArray())
                observer.OnError(e);
        }
    }
}