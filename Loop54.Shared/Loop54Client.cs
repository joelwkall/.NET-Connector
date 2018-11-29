using Loop54.AspNet;
using Loop54.Http;
using Loop54.Model.Request;
using Loop54.Model.Response;
using Loop54.User;
using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Loop54
{
    /// <summary>
    /// Default implementation of the ILoop54Client interface.
    /// 
    /// When issuing a request the user data will be used in this order of priority:
    /// 1. The <see cref="UserMetaData"/> provided in <see cref="RequestContainer{T}"/> in the method call.
    /// 2. The <see cref="IRemoteClientInfo"/> gotten from the <see cref="IRemoteClientInfoProvider"/>.
    /// </summary>
    public class Loop54Client : ILoop54Client
    {
        private const string SearchRequestName = "search";
        private const string AutoCompleteRequestName = "autoComplete";
        private const string GetEntitiesRequestName = "getEntities";
        private const string GetEntitiesByAttributeRequestName = "getEntitiesByAttribute";
        private const string GetRelatedEntitiesRequestName = "getRelatedEntities";
        private const string CreateEventsRequestName = "createEvents";
        private const string SyncRequestName = "sync";

        private readonly IRequestManager _requestManager;
        private readonly IRemoteClientInfoProvider _remoteClientInfoProvider;

        /// <summary>
        /// IRequestManager used by this client.
        /// </summary>
        internal IRequestManager RequestManager => _requestManager;

        /// <summary>
        /// Creates an instance of Loop54Client using the specified <see cref="IRequestManager"/> and <see cref="IRemoteClientInfoProvider"/>.
        /// </summary>
        /// <param name="requestManager">The <see cref="IRequestManager"/> to use when making calls to the api. Must not be null.</param>
        /// <param name="remoteClientInfoProvider">The <see cref="IRemoteClientInfoProvider"/></param>
        public Loop54Client(IRequestManager requestManager, IRemoteClientInfoProvider remoteClientInfoProvider)
        {
            _requestManager = requestManager ?? throw new ArgumentNullException(nameof(requestManager));
            _remoteClientInfoProvider = remoteClientInfoProvider ?? throw new ArgumentNullException(nameof(remoteClientInfoProvider));
        }

        public SearchResponse Search(SearchRequest request) => Search(request.Wrap());
        public SearchResponse Search(RequestContainer<SearchRequest> request) 
            => CallEngineWithinClientContext<SearchRequest, SearchResponse>(SearchRequestName, request);
        public async Task<SearchResponse> SearchAsync(SearchRequest request) => await SearchAsync(request.Wrap());
        public async Task<SearchResponse> SearchAsync(RequestContainer<SearchRequest> request) 
            => await CallEngineWithinClientContextAsync<SearchRequest, SearchResponse>(SearchRequestName, request);

        public AutoCompleteResponse AutoComplete(AutoCompleteRequest request) => AutoComplete(request.Wrap());
        public AutoCompleteResponse AutoComplete(RequestContainer<AutoCompleteRequest> request)
            => CallEngineWithinClientContext<AutoCompleteRequest, AutoCompleteResponse>(AutoCompleteRequestName, request);
        public async Task<AutoCompleteResponse> AutoCompleteAsync(AutoCompleteRequest request) => await AutoCompleteAsync(request.Wrap());
        public async Task<AutoCompleteResponse> AutoCompleteAsync(RequestContainer<AutoCompleteRequest> request) 
            => await CallEngineWithinClientContextAsync<AutoCompleteRequest, AutoCompleteResponse>(AutoCompleteRequestName, request);

        public GetEntitiesResponse GetEntities(GetEntitiesRequest request) => GetEntities(request.Wrap());
        public GetEntitiesResponse GetEntities(RequestContainer<GetEntitiesRequest> request) 
            => CallEngineWithinClientContext<GetEntitiesRequest, GetEntitiesResponse>(GetEntitiesRequestName, request);
        public async Task<GetEntitiesResponse> GetEntitiesAsync(GetEntitiesRequest request) => await GetEntitiesAsync(request.Wrap());
        public async Task<GetEntitiesResponse> GetEntitiesAsync(RequestContainer<GetEntitiesRequest> request) 
            => await CallEngineWithinClientContextAsync<GetEntitiesRequest, GetEntitiesResponse>(GetEntitiesRequestName, request);

        public GetEntitiesByAttributeResponse GetEntitiesByAttribute(GetEntitiesByAttributeRequest request) => GetEntitiesByAttribute(request.Wrap());
        public GetEntitiesByAttributeResponse GetEntitiesByAttribute(RequestContainer<GetEntitiesByAttributeRequest> request) 
            => CallEngineWithinClientContext<GetEntitiesByAttributeRequest, GetEntitiesByAttributeResponse>(GetEntitiesByAttributeRequestName, request);
        public async Task<GetEntitiesByAttributeResponse> GetEntitiesByAttributeAsync(GetEntitiesByAttributeRequest request) => await GetEntitiesByAttributeAsync(request.Wrap());
        public async Task<GetEntitiesByAttributeResponse> GetEntitiesByAttributeAsync(RequestContainer<GetEntitiesByAttributeRequest> request) 
            => await CallEngineWithinClientContextAsync<GetEntitiesByAttributeRequest, GetEntitiesByAttributeResponse>(GetEntitiesByAttributeRequestName, request);

        public GetRelatedEntitiesResponse GetRelatedEntities(GetRelatedEntitiesRequest request) => GetRelatedEntities(request.Wrap());
        public GetRelatedEntitiesResponse GetRelatedEntities(RequestContainer<GetRelatedEntitiesRequest> request) 
            => CallEngineWithinClientContext<GetRelatedEntitiesRequest, GetRelatedEntitiesResponse>(GetRelatedEntitiesRequestName, request);
        public async Task<GetRelatedEntitiesResponse> GetRelatedEntitiesAsync(GetRelatedEntitiesRequest request) => await GetRelatedEntitiesAsync(request.Wrap());
        public async Task<GetRelatedEntitiesResponse> GetRelatedEntitiesAsync(RequestContainer<GetRelatedEntitiesRequest> request) 
            => await CallEngineWithinClientContextAsync<GetRelatedEntitiesRequest, GetRelatedEntitiesResponse>(GetRelatedEntitiesRequestName, request);

        public Response CreateEvents(CreateEventsRequest request) => CreateEvents(request.Wrap());
        public Response CreateEvents(RequestContainer<CreateEventsRequest> request) 
            => CallEngineWithinClientContext<CreateEventsRequest, Response>(CreateEventsRequestName, request);
        public async Task<Response> CreateEventsAsync(CreateEventsRequest request) => await CreateEventsAsync(request.Wrap());
        public async Task<Response> CreateEventsAsync(RequestContainer<CreateEventsRequest> request) 
            => await CallEngineWithinClientContextAsync<CreateEventsRequest, Response>(CreateEventsRequestName, request);

        public Response Sync(Request request = null) => Sync(request.Wrap());
        public Response Sync(RequestContainer<Request> request)
            => CallEngineWithinClientContext<Request, Response>(SyncRequestName, request, true);
        public async Task<Response> SyncAsync(Request request = null) => await SyncAsync(request.Wrap());
        public async Task<Response> SyncAsync(RequestContainer<Request> request)
            => await CallEngineWithinClientContextAsync<Request, Response>(SyncRequestName, request, true);

        public Response CustomCall(string name, Request request) => CustomCall(name, request.Wrap());
        public Response CustomCall(string name, RequestContainer<Request> request) => CallEngineWithinClientContext<Request, Response>(name, request);
        public async Task<Response> CustomCallAsync(string name, Request request) => await CustomCallAsync(name, request.Wrap());
        public async Task<Response> CustomCallAsync(string name, RequestContainer<Request> request) => await CallEngineWithinClientContextAsync<Request, Response>(name, request);

        private TResponse CallEngineWithinClientContext<TRequest, TResponse>(string requestName, RequestContainer<TRequest> request, bool allowNullRequest = false) where TResponse : Response where TRequest : Request
        {
            request = ValidateRequest(requestName, request, allowNullRequest);
            UserMetaData metaData = PrepareRequest(requestName, request);

            try
            {
                //To not run in to deadlocks in some milieus we need to wrap the call in a Task.Run
                return Task.Run(() => _requestManager.CallEngineAsync<TRequest, TResponse>(requestName, request.Request, metaData)).Result;
            }
            catch (AggregateException ae)
            {
                ExceptionDispatchInfo.Capture(ae.InnerException).Throw();
                throw;
            }
        }

        private async Task<TResponse> CallEngineWithinClientContextAsync<TRequest, TResponse>(string requestName, RequestContainer<TRequest> request, bool allowNullRequest = false) where TResponse : Response where TRequest : Request
        {
            request = ValidateRequest(requestName, request, allowNullRequest);
            UserMetaData metaData = PrepareRequest(requestName, request);
            return await _requestManager.CallEngineAsync<TRequest, TResponse>(requestName, request.Request, metaData);
        }

        private RequestContainer<TRequest> ValidateRequest<TRequest>(string requestName, RequestContainer<TRequest> request, bool allowNullRequest) where TRequest : Request
        {
            if (requestName == null)
                throw new ArgumentNullException(nameof(requestName));

            if (requestName.Length == 0 || char.IsUpper(requestName[0]))
                throw new ArgumentException($"The {nameof(requestName)} must be in lower camel case with a length over 0.", nameof(requestName));

            //null handling
            if ((request == null || request.Request == null))
            {
                if (allowNullRequest)
                {
                    if (typeof(TRequest) != typeof(Request))
                        throw new ArgumentNullException(nameof(request), $"{nameof(request)} can not be null when the type is not {typeof(Request)}.");

                    if (request == null)
                        request = new RequestContainer<TRequest>((TRequest)new Request());
                    else if (request.Request == null)
                        request.Request = (TRequest)new Request();
                }
                else
                {
                    throw new ArgumentNullException(nameof(request), $"{nameof(request)} can not be null for the engine operation {requestName}.");
                }
            }

            return request;
        }

        private UserMetaData PrepareRequest<TRequest>(string requestName, RequestContainer<TRequest> request) where TRequest : Request
        {
            return GetOrCreateMetaData(request);
        }

        private UserMetaData GetOrCreateMetaData<TRequest>(RequestContainer<TRequest> request) where TRequest : Request
        {
            UserMetaData metaData = request.MetaDataOverrides ?? new UserMetaData();
            
            IRemoteClientInfo remoteClientInfo = _remoteClientInfoProvider.GetRemoteClientInfo();

            if (remoteClientInfo == null)
                throw new ClientInfoException($"The {nameof(IRemoteClientInfoProvider)} returned a null {nameof(IRemoteClientInfo)}. Make sure you've implemented it correctly!");
            
            metaData.SetFromClientInfo(remoteClientInfo);
            return metaData;
        }
    }
}
