﻿using System.Collections.Generic;

namespace Vostok.Configuration.ClusterConfig
{
    public interface IClusterConfigClientProxy
    {
        Dictionary<string, List<string>> GetAll();
        List<string> GetByKey(string key);
        Dictionary<string, List<string>> GetByPrefix(string prefix);
    }
}