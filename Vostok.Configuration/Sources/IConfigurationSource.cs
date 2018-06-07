﻿using System;
using Vostok.Configuration.SettingsTree;

namespace Vostok.Configuration.Sources
{
    /// <inheritdoc />
    /// <summary>
    /// File converter to <see cref="ISettingsNode"/> tree
    /// </summary>
    public interface IConfigurationSource: IDisposable
    {
        /// <summary>
        /// Converts file
        /// </summary>
        /// <returns><see cref="ISettingsNode"/> tree</returns>
        ISettingsNode Get();

        /// <summary>
        /// Watches file changes
        /// </summary>
        /// <returns>Event with new <see cref="ISettingsNode"/> tree</returns>
        IObservable<ISettingsNode> Observe();
    }
}