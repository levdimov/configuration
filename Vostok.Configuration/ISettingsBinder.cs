﻿using Vostok.Configuration.SettingsTree;

namespace Vostok.Configuration
{
    /// <summary>
    /// Not static to be configured with custom type parsers.
    /// </summary>
    public interface ISettingsBinder
    {
        /// <summary>
        /// Bindes <paramref name="rawSettings"/> tree to specified type
        /// </summary>
        /// <typeparam name="TSettings">Data type you need to get</typeparam>
        /// <param name="rawSettings"><see cref="ISettingsNode"/> tree</param>
        TSettings Bind<TSettings>(ISettingsNode rawSettings);
    }
}