﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using RedArrow.Argo.Attributes;
using RedArrow.Argo.Client.Extensions;
using RedArrow.Argo.Client.Query;
using RedArrow.Argo.Client.Session;

namespace RedArrow.Argo.Client.Linq
{
	public abstract class RemoteQueryable<TModel> : IOrderedQueryable<TModel>
	{
        private static readonly ISet<Type> PropAttrTypes = new HashSet<Type>
        {
            typeof(PropertyAttribute),
            typeof(HasOneAttribute),
            typeof(HasManyAttribute)
        };

        protected IQuerySession Session { get; }

		public Type ElementType => typeof (TModel);
        public Expression Expression { get; }

	    public IQueryProvider Provider { get; }

		protected RemoteQueryable(IQuerySession session, IQueryProvider provider)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
			if (provider == null) throw new ArgumentNullException(nameof(provider));

			Expression = Expression.Constant(this);
			Session = session;
			Provider = provider;
        }

		protected RemoteQueryable(IQuerySession session, IQueryProvider provider, Expression expression)
		{
			if (session == null) throw new ArgumentNullException(nameof(session));
			if (provider == null) throw new ArgumentNullException(nameof(provider));
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			if (!typeof(IQueryable<TModel>).GetTypeInfo().IsAssignableFrom(expression.Type.GetTypeInfo()))
				throw new ArgumentOutOfRangeException(nameof(expression));
            
			Expression = expression;
			Session = session;
			Provider = provider;
		}

		public IEnumerator<TModel> GetEnumerator()
		{
			return Session.Query<TModel>(BuildQuery())
				.GetAwaiter()
				.GetResult()
				.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
		    return GetEnumerator();
		}

	    public abstract IQueryContext BuildQuery();
		
		protected string GetJsonName(MemberInfo member)
		{
			return member.GetJsonName(member
				.CustomAttributes
				.Single(a => PropAttrTypes.Contains(a.AttributeType))
				.AttributeType);
		}
	}
}
