﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using RedArrow.Argo.Client.Model;
using RedArrow.Argo.Client.Services.Relationships;
using RedArrow.Argo.Client.Services.SparseFieldsets;
using RedArrow.Argo.Client.Session.Patch;
using RedArrow.Argo.Client.Session.Registry;

namespace RedArrow.Argo.Client.Http
{
    internal class HttpRequestBuilder : IHttpRequestBuilder
    {
        private const string JsonApiHeader = "application/vnd.api+json";

        private IModelRegistry ModelRegistry { get; }
        private IResourceRegistry ResourceRegistry { get; }

        //private ISparseFieldsetService SparseFieldsetService { get; }
        //private IIncludeResourcesService IncludeResources { get; }
        //private IRelateResources RelateResources { get; }

        internal HttpRequestBuilder(IModelRegistry modelRegistry, IResourceRegistry resourceRegistry)
        {
            ModelRegistry = modelRegistry;
            ResourceRegistry = resourceRegistry;

            //SparseFieldsetService = new SparseFieldsetService(ModelRegistry);
            //IncludeResources = new IncludeResourcesService(ModelRegistry);
            //RelateResources = new RelateResources(ModelRegistry);
        }

        public RequestContext GetResource(Guid id, Type modelType)
        {
            //var resourceType = ModelRegistry.GetResourceType(modelType);
            ////var url = $"{resourceType}/{id}";

            ////url = SparseFieldsetService.BuildSparseFieldsetUrl(modelType, url).Result;
            ////url = IncludeResources.BuildIncludesUrl(modelType, url);

            //return new RequestContext
            //{
            //    Request = new HttpRequestMessage(HttpMethod.Get, url),
            //    ResourceId = id,
            //    ResourceType = resourceType
            //};
            return null;
        }

        public RequestContext GetRelated(object owner, string rltnName)
        {
            var id = ModelRegistry.GetId(owner);
            var resourceType = ModelRegistry.GetResourceType(owner.GetType());

            return new RequestContext
            {
                Request = new HttpRequestMessage(HttpMethod.Get, $"{resourceType}/{id}/{rltnName}"),

                ResourceId = id,
                ResourceType = resourceType,
            };
        }

        public HttpRequestMessage CreateResource(Resource resource, IEnumerable<Resource> include)
        {
            var root = ResourceRootCreate.FromResource(resource, include);

            return new HttpRequestMessage(HttpMethod.Post, resource.Type)
            {
                Content = BuildHttpContent(root.ToJson()),
            };
        }

        public RequestContext UpdateResource(Guid id, object model)
        {
            //if (model == null)
            //{
            //    throw new ArgumentNullException(nameof(model));
            //}

            //var resourceType = ModelRegistry.GetResourceType(model.GetType());

            //    var included = model != null ? IncludeResources.Process(model.GetType(), model, resourceState) : new List<Resource>();
            //    var relationships = model != null ? RelateResources.Process(model.GetType(), model) : new Dictionary<string, Relationship>();

            //    patchContext.Resource.Relationships = relationships;
            //    var root = ResourceRootSingle.FromResource(patchContext.Resource, included);

            //    return new RequestContext
            //    {
            //        Request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{resourceType}/{id}")
            //        {
            //            Content = BuildHttpContent(root.ToJson())
            //        },

            //        ResourceId = id,
            //        ResourceType = resourceType,
            //        Attributes = patchContext.Resource?.Attributes,
            //        Relationships = patchContext.Resource?.Relationships,
            //        Included = included
            //    };
            return null;
        }

        private static HttpContent BuildHttpContent(string content)
        {
            return new StringContent(content, Encoding.UTF8, JsonApiHeader);
        }
    }
}
