using System.Collections.Generic;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Domain.Dto;
using AspNetCoreWebApi.Processing.Requests;
using AspNetCoreWebApi.Processing.Responses;
using Microsoft.Extensions.ObjectPool;

namespace AspNetCoreWebApi.Processing.Pooling
{
    public class MainPool
    {
        public DefaultObjectPool<AccountDto> AccountDto { get; } = new DefaultObjectPool<AccountDto>(new GenericPolicy<AccountDto>());

        public DefaultObjectPool<SingleLikeDto> SingleLikeDto { get; } = new DefaultObjectPool<SingleLikeDto>(new GenericPolicy<SingleLikeDto>());

        public DefaultObjectPool<FilterRequest> FilterRequest { get; } = new DefaultObjectPool<FilterRequest>(new GenericPolicy<FilterRequest>());

        public DefaultObjectPool<GroupRequest> GroupRequest { get; } = new DefaultObjectPool<GroupRequest>(new GenericPolicy<GroupRequest>());

        public DefaultObjectPool<RecommendRequest> RecommendRequest { get; } = new DefaultObjectPool<RecommendRequest>(new GenericPolicy<RecommendRequest>());
    
        public DefaultObjectPool<SuggestRequest> SuggestRequest { get; } = new DefaultObjectPool<SuggestRequest>(new GenericPolicy<SuggestRequest>());

        public DefaultObjectPool<List<int>> ListOfIntegers { get; } = new DefaultObjectPool<List<int>>(new ListPolicy<int>());

        public DefaultObjectPool<FilterResponse> FilterResponse { get; } = new DefaultObjectPool<FilterResponse>(new GenericPolicy<FilterResponse>());

        public DefaultObjectPool<GroupResponse> GroupResponse { get; } = new DefaultObjectPool<GroupResponse>(new GenericPolicy<GroupResponse>());

        public DefaultObjectPool<GroupEntryComparer> GroupEntryComparer { get; } = new DefaultObjectPool<GroupEntryComparer>(new GenericPolicy<GroupEntryComparer>());

        public DefaultObjectPool<RecommendComparer> RecommendComparer { get; } = new DefaultObjectPool<RecommendComparer>(new GenericPolicy<RecommendComparer>());

        public DefaultObjectPool<SuggestComparer> SuggestComparer { get; } = new DefaultObjectPool<SuggestComparer>(new GenericPolicy<SuggestComparer>());

        public DefaultObjectPool<RecommendResponse> RecommendResponse { get; } = new DefaultObjectPool<RecommendResponse>(new GenericPolicy<RecommendResponse>());

        public DefaultObjectPool<SuggestResponse> SuggestResponse { get; } = new DefaultObjectPool<SuggestResponse>(new GenericPolicy<SuggestResponse>());

        public DefaultObjectPool<Dictionary<int, int>> DictionaryOfIntByInt { get; } = new DefaultObjectPool<Dictionary<int, int>>(new DictionaryPolicy<int, int>());

        public DefaultObjectPool<Dictionary<int, float>> DictionaryOfFloatByInt { get; } = new DefaultObjectPool<Dictionary<int, float>>(new DictionaryPolicy<int, float>());

        public DefaultObjectPool<Dictionary<int, IEnumerable<int>>> DictionaryOfIntsByInt { get; } = new DefaultObjectPool<Dictionary<int, IEnumerable<int>>>(new DictionaryPolicy<int, IEnumerable<int>>());

        public DefaultObjectPool<List<Group>> ListOfGroup { get; } = new DefaultObjectPool<List<Group>>(new ListPolicy<Group>());

        public DefaultObjectPool<List<SingleLikeDto>> ListOfLikeDto { get; } = new DefaultObjectPool<List<SingleLikeDto>>(new ListPolicy<SingleLikeDto>());

        public DefaultObjectPool<FilterSet> FilterSet { get; } = new DefaultObjectPool<FilterSet>(new GenericPolicy<FilterSet>());

        public DefaultObjectPool<List<IEnumerable<int>>> ListOfLists { get; } = new DefaultObjectPool<List<IEnumerable<int>>>(new ListPolicy<IEnumerable<int>>());

        public DefaultObjectPool<byte[]> WriteBuffer { get; } = new DefaultObjectPool<byte[]>(new BufferPolicy());

        public DefaultObjectPool<List<short>> ListOfInt16 { get; } = new DefaultObjectPool<List<short>>(new ListPolicy<short>());

        public DefaultObjectPool<List<GroupEntry>> ListOfGroupEntry { get; } = new DefaultObjectPool<List<GroupEntry>>(new ListPolicy<GroupEntry>());

        public DefaultObjectPool<List<IEnumerator<int>>> ListOfEnumerators { get; } = new DefaultObjectPool<List<IEnumerator<int>>>(new ListPolicy<IEnumerator<int>>());

        public DefaultObjectPool<HashSet<int>> HashSetOfInts { get; } = new DefaultObjectPool<HashSet<int>>(new HashSetPolicy<int>());
    }
}
