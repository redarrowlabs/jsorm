﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using RedArrow.Jsorm.Client.JsonModels;

namespace RedArrow.Jsorm.Client.Http
{
    internal class RequestContext
    {
        public HttpRequestMessage Request { get; set; }

        public Guid ResourceId { get; set; }
        public string ResourceType { get; set; }
        public JObject Attributes { get; set; }
        public IDictionary<string, Relationship> Relationships { get; set; }

        internal RequestContext() { }
    }
}
