﻿using System;
using RedArrow.Argo.Attributes;

namespace RedArrow.Argo.Client.Tests.Session.Registry
{
    [Model("customized-model-name")]
    public class TestCustomWovenModel
    {
        [Id]
        public Guid Id { get; set; }
    }
}
