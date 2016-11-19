﻿using Newtonsoft.Json.Linq;

namespace RedArrow.Jsorm.JsonModels
{
    internal class ResourceRootCreate : BaseResourceRoot<ResourceCreate>
    {
        public static ResourceRootCreate FromAttributes(string type, JObject attributes)
        {
            return new ResourceRootCreate
            {
                Data = new ResourceCreate
                {
                    Type = type,
                    Attributes = attributes
                }
            };
        }
    }
}