﻿using Newtonsoft.Json.Linq;
using RedArrow.Argo.Client.Cache;
using RedArrow.Argo.Client.Extensions;
using RedArrow.Argo.Client.Session.Registry;
using RedArrow.Argo.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RedArrow.Argo.Client.Collections;
using RedArrow.Argo.Client.Collections.Generic;
using RedArrow.Argo.Client.Exceptions;
using RedArrow.Argo.Client.Http;
using RedArrow.Argo.Client.Logging;
using RedArrow.Argo.Client.Model;
using RedArrow.Argo.Model;

namespace RedArrow.Argo.Client.Session
{
    public class Session : IModelSession, ISession, ICollectionSession
    {
        private static readonly ILog Log = LogProvider.For<Session>();

        private HttpClient HttpClient { get; }

        private IHttpRequestBuilder HttpRequestBuilder { get; }

        internal ICacheProvider Cache { get; }

        internal IModelRegistry ModelRegistry { get; }

        public bool Disposed { get; set; }

        internal Session(
            Func<HttpClient> httpClientFactory,
            IHttpRequestBuilder httpRequestBuilder,
            ICacheProvider cache,
            IModelRegistry modelRegistry)
        {
            HttpClient = httpClientFactory();
            HttpRequestBuilder = httpRequestBuilder;
            Cache = cache;

            ModelRegistry = modelRegistry;
        }

        public void Dispose()
        {
            HttpClient.Dispose();
            Disposed = true;
        }

        public async Task<TModel> Create<TModel>()
            where TModel : class
        {
            return (TModel) await Create(typeof(TModel), null);
        }

        public async Task<TModel> Create<TModel>(TModel model)
            where TModel : class
        {
            return (TModel) await Create(typeof(TModel), model);
        }

        private async Task<object> Create(Type rootModelType, object rootModel)
        {
            ThrowIfDisposed();
			
			// set Id if unset
	        var rootModelId = rootModel == null
				? Guid.NewGuid()
				: ModelRegistry.GetId(rootModel);
	        if (rootModelId == Guid.Empty)
	        {
				rootModelId = Guid.NewGuid();
				ModelRegistry.SetId(rootModel, rootModelId);
	        }

	        IDictionary<Guid, Resource> resourceIndex;

            if (rootModel != null) // map model to resource
            {
				// all unmanaged models in the object graph, including root
				resourceIndex = ModelRegistry.GetIncludedModels(rootModel)
		            .Select(model =>
		            {
			            var modelType = model.GetType();
			            var resourceType = ModelRegistry.GetResourceType(modelType);

			            JObject attrs = null;
			            IDictionary<string, Relationship> rltns = null;

			            // attribute bag
			            var modelAttributeBag = ModelRegistry.GetAttributeBag(model);
			            if (modelAttributeBag != null)
			            {
				            attrs = modelAttributeBag;
			            }

			            // attributes
			            var modelAttributes = ModelRegistry.GetAttributeValues(model);
			            if (modelAttributes != null)
			            {
				            if (attrs == null)
				            {
					            attrs = modelAttributes;
				            }
				            else // occurs when we already set from AttrBag
				            {
								// mapped attrs override anything also in the AttrBag
					            attrs.Merge(modelAttributes, new JsonMergeSettings
					            {
						            MergeNullValueHandling = MergeNullValueHandling.Ignore,
						            MergeArrayHandling = MergeArrayHandling.Replace
					            });
				            }
			            }

			            // relationships
			            // Note: this process sets unset model Ids in order to create relationships
			            var relationships = ModelRegistry.GetRelationshipValues(model);
			            if (!relationships.IsNullOrEmpty())
			            {
				            rltns = relationships;
			            }

			            return new Resource
			            {
				            Id = ModelRegistry.GetId(model),
				            Type = resourceType,
				            Attributes = attrs,
				            Relationships = rltns
			            };
		            })
					.ToDictionary(x => x.Id);
            }
            else // model is null - all we know is resource type
            {
	            resourceIndex = new Dictionary<Guid, Resource>
	            {
		            {
			            rootModelId, new Resource
			            {
				            Id = rootModelId,
				            Type = ModelRegistry.GetResourceType(rootModelType)
			            }
		            }
	            };
            }

	        var rootResource = resourceIndex[rootModelId];
	        var includes = resourceIndex.Values.Where(x => x.Id != rootModelId).ToArray();

            var request = HttpRequestBuilder.CreateResource(rootResource, includes);

            if (Log.IsDebugEnabled())
            {
                Log.Debug(() => $"preparing to POST {rootResource.Type}:{{{rootResource.Id}}}");
                foreach (var include in includes)
                {
                    Log.Debug(() => $"preparing to POST included {include.Type}:{{{include.Id}}}");
                }
            }

            var response = await HttpClient.SendAsync(request);
			//TODO: callback to inspect copy of response
            response.EnsureSuccessStatusCode();

			// create and cache models
            await Task.WhenAll(resourceIndex.Values.Select(x => Task.Run(() => 
			{
                var model = CreateModel(ModelRegistry.GetModelType(x.Type), x);
                Cache.Update(x.Id, model);
            })));
	        return Cache.Retrieve(rootModelId);
        }

        public async Task<TModel> Get<TModel>(Guid id)
            where TModel : class
        {
            ThrowIfDisposed();

            var model = Cache.Retrieve(id);
            if (model != null)
            {
                return (TModel) model;
            }

            var resourceType = ModelRegistry.GetResourceType<TModel>();
            var request = HttpRequestBuilder.GetResource(id, resourceType);
            var response = await HttpClient.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return default(TModel); // null
            }
            response.EnsureSuccessStatusCode();

            var contentString = await response.Content.ReadAsStringAsync();
            var root = JsonConvert.DeserializeObject<ResourceRootSingle>(contentString);

            model = CreateModel(typeof(TModel), root.Data);
            Cache.Update(id, model);

            return (TModel) model;
        }

        public async Task Update<TModel>(TModel model)
            where TModel : class
        {
            ThrowIfDisposed();

            var id = ModelRegistry.GetId(model);
            var resourceType = ModelRegistry.GetResourceType(typeof(TModel));

            var patch = ModelRegistry.GetPatch(model);
            if (patch == null) return;

            //TODO: includes

            var request = HttpRequestBuilder.UpdateResource(patch, null);
            
            //var requestContext = HttpRequestBuilder.UpdateResource(id, model, context, ResourceState);
            //// nothing to update?  don't hit the server
            //if (requestContext.Attributes.IsNullOrEmpty() && requestContext.Relationships.IsNullOrEmpty())
            //{
            //    return;
            //}
            //var response = await HttpClient.SendAsync(requestContext.Request);
            //response.EnsureSuccessStatusCode();

            //Resource resource;
            //if (ResourceState.TryGetValue(id, out resource))
            //{
            //    // this updates the locally-cached resource
            //    // TODO: we need a better solution here
            //    if (requestContext.Attributes != null)
            //    {
            //        resource.GetAttributes().Merge(requestContext.Attributes, new JsonMergeSettings
            //        {
            //            MergeNullValueHandling = MergeNullValueHandling.Merge,
            //            MergeArrayHandling = MergeArrayHandling.Replace
            //        });
            //    }
            //    requestContext.Relationships?.Each(kvp => resource.GetRelationships()[kvp.Key] = kvp.Value);
            //}
            //PatchContexts.Remove(id);
            //dirtyCollections.Each(x => x.Clean());
        }

        public Task Delete<TModel>(TModel model)
            where TModel : class
        {
            ThrowIfDisposed();

            var id = ModelRegistry.GetId(model);
            return Delete<TModel>(id);
        }

        public async Task Delete<TModel>(Guid id)
            where TModel : class
        {
            ThrowIfDisposed();

            var resourceType = ModelRegistry.GetResourceType<TModel>();
            var response = await HttpClient.DeleteAsync($"{resourceType}/{id}");
            response.EnsureSuccessStatusCode();

            Cache.Remove(id);
            // TODO update model to indicate it is no longer managed
        }

        #region IModelSession

        public Guid GetId<TModel>(TModel model)
        {
            return ModelRegistry.GetResource(model).Id;
        }

        public TAttr GetAttribute<TModel, TAttr>(TModel model, string attrName)
            where TModel : class
        {
            ThrowIfDisposed();

            return ModelRegistry.GetAttributeValue<TAttr>(model, attrName);
        }

        public void SetAttribute<TModel, TAttr>(TModel model, string attrName, TAttr value)
            where TModel : class
        {
            ThrowIfDisposed();

            ModelRegistry.SetAttributeValue(model, attrName, value);
        }

        public TRltn GetReference<TModel, TRltn>(TModel model, string rltnName)
            where TModel : class
            where TRltn : class
        {
            ThrowIfDisposed();
			
			// check patch first, fall back on resource if rltnName not found
	        Relationship rltn;
			var relationships = ModelRegistry.GetPatch(model)?.Relationships;
	        if (relationships == null || !relationships.TryGetValue(rltnName, out rltn))
	        {
		        relationships = ModelRegistry.GetResource(model).Relationships;
		        if (relationships == null || !relationships.TryGetValue(rltnName, out rltn))
		        {
					// the rltnName was not found in the patch or resource
			        return default(TRltn); // TODO: return GetRelated<TModel>(modelId, rltnName);
		        }
	        }
			// if we make it to here, 'rltn' has been set
	        var rltnData = rltn.Data;
	        if (rltnData?.Type != JTokenType.Object)
	        {
				//TODO: what if type is array? throw execption? log warning? get first element?
		        return default(TRltn);
	        }
	        var rltnIdentifier = rltnData.ToObject<ResourceIdentifier>();
			// calling Get<> here will check the cache first, then go remote if necessary
	        return Task.Run(async () => await Get<TRltn>(rltnIdentifier.Id)).Result;
        }

        public void SetReference<TModel, TRltn>(TModel model, string rltnName, TRltn rltn)
            where TModel : class
            where TRltn : class
        {
            ThrowIfDisposed();

	        var patch = ModelRegistry.GetOrCreatePatch(model);
	        var relationship = new Relationship();
	        if (rltn != null)
	        {
		        var rltnType = ModelRegistry.GetResourceType<TRltn>();
		        var rltnId = ModelRegistry.GetId(rltn);
		        if (rltnId == Guid.Empty)
		        {
			        rltnId = Guid.NewGuid();
					ModelRegistry.SetId(rltn, rltnId);
		        }
		        relationship.Data = JObject.FromObject(new ResourceIdentifier {Id = rltnId, Type = rltnType});
	        }
	        else
	        {
		        relationship.Data = JValue.CreateNull();
	        }
	        patch.GetRelationships()[rltnName] = relationship;
        }

        public void InitializeCollection(IRemoteCollection collection)
        {
            // TODO: determine if {id/type} from owner relationship are cached already
            // TODO: abstract this into a collection loader/initializer

            // TODO: brute force this for now

            // TODO: don't run this task if the resource collection is empty/null!

            //Task.Run(async () =>
            //{
            //    var requestContext = HttpRequestBuilder.GetRelated(collection.Owner, collection.Name);
            //    var response = await HttpClient.SendAsync(requestContext.Request);
            //    if (response.StatusCode == HttpStatusCode.NotFound)
            //    {
            //        return;
            //    }
            //    response.EnsureSuccessStatusCode();

            //    var contentJson = await response.Content.ReadAsStringAsync();
            //    var root = JsonConvert.DeserializeObject<ResourceRootCollection>(contentJson);

            //    if (root.Data == null)
            //    {
            //        return;
            //    }

            //    var items = root.Data.Select(x =>
            //    {
            //        ResourceState[x.Id] = x;
            //        var modelType = ModelRegistry.GetModelType(x.Type);
            //        if (modelType == null)
            //        {
            //            // TODO: ModelNotRegisteredException
            //            throw new Exception("TODO");
            //        }
            //        var model = CreateModel(modelType, x.Id);
            //        Cache.Update(x.Id, model);
            //        return model;
            //    });
            //    collection.SetItems(items);
            //}).Wait();
        }

        public IEnumerable<TElmnt> GetGenericEnumerable<TModel, TElmnt>(TModel model, string rltnName)
            where TModel : class
            where TElmnt : class
        {
            return GetRemoteCollection<TModel, TElmnt>(model, rltnName);
        }

        public IEnumerable<TElmnt> SetGenericEnumerable<TModel, TElmnt>(TModel model, string attrName,
            IEnumerable<TElmnt> value)
            where TModel : class
            where TElmnt : class
        {
            return SetRemoteCollection<TModel, TElmnt>(model, attrName, value);
        }

        public ICollection<TElmnt> GetGenericCollection<TModel, TElmnt>(TModel model, string rltnName)
            where TModel : class
            where TElmnt : class
        {
            return GetRemoteCollection<TModel, TElmnt>(model, rltnName);
        }

        public ICollection<TElmnt> SetGenericCollection<TModel, TElmnt>(TModel model, string attrName,
            IEnumerable<TElmnt> value)
            where TModel : class
            where TElmnt : class
        {
            return SetRemoteCollection<TModel, TElmnt>(model, attrName, value);
        }

        private RemoteGenericBag<TElmnt> GetRemoteCollection<TModel, TElmnt>(TModel model, string rltnName)
            where TModel : class
            where TElmnt : class
        {
            var rltnConfig = ModelRegistry.GetHasManyConfig<TModel>(rltnName);

            // TODO: configure collection based on rltnConfig

            return new RemoteGenericBag<TElmnt>(this)
            {
                Name = rltnName,
                Owner = model
            };
        }

        private RemoteGenericBag<TElmnt> SetRemoteCollection<TModel, TElmnt>(TModel model, string rltnName,
            IEnumerable<TElmnt> value)
            where TModel : class
            where TElmnt : class
        {
            var rltnConfig = ModelRegistry.GetHasManyConfig<TModel>(rltnName);

            // TODO: configure collection based on rltnConfig

            return new RemoteGenericBag<TElmnt>(this, value)
            {
                Name = rltnName,
                Owner = model
            };
        }

        #endregion IModelSession

        private object CreateModel(Type type, IResourceIdentifier resource)
        {
            Log.Debug(() => $"activating new session-managed instance of {type}:{{{resource.Id}}}");
            return Activator.CreateInstance(type, resource, this);
        }

        private void ThrowIfDisposed()
        {
            if (Disposed)
            {
                throw new Exception("Session disposed");
            }
        }
    }
}