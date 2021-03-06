﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace tterm
{
    public class Profile
    {
        [JsonProperty(PropertyName = "command")]
        public string Command { get; set; }

        [JsonProperty(PropertyName = "args")]
        public string[] Arguments { get; set; }

        [JsonProperty(PropertyName = "cwd")]
        public string CurrentWorkingDirectory { get; set; }

        [JsonProperty(PropertyName = "env")]
        public IDictionary<string, string> EnvironmentVariables { get; set; }
    }
}
