﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ZipMoneySDK.Models
{
    public class ZipConfig
    {
        [JsonProperty(Required = Required.Always)]
        public string redirect_uri { get; set; }
    }
}
