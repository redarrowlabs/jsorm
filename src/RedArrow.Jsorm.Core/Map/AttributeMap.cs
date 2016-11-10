﻿using RedArrow.Jsorm.Core.Session;
using System;
using System.Linq.Expressions;

namespace RedArrow.Jsorm.Core.Map
{
    public class AttributeMap<TModel, TProp> : PropertyMap<TModel, TProp>
    {
        public AttributeMap(Expression<Func<TModel, TProp>> property, string attrName = null) : base(property, attrName)
        {
        }

        public override void Configure(ISessionFactory factory)
        {
            throw new NotImplementedException();
        }
    }
}