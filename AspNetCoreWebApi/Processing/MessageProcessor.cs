using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Domain.Dto;
using AspNetCoreWebApi.Processing.Parsers;
using AspNetCoreWebApi.Processing.Requests;
using AspNetCoreWebApi.Processing.Responses;
using AspNetCoreWebApi.Storage;
using AspNetCoreWebApi.Storage.Contexts;
using AspNetCoreWebApi.Storage.StringPools;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreWebApi.Processing
{
    public class MessageProcessor
    {
        private readonly IDisposable _newAccountProcessorSubscription;
        private readonly IDisposable _editAccountProcessorSubscription;
        private readonly IDisposable _newLikesProcessorSubscription;
        private readonly IDisposable _filterProcessorSubscription;
        private readonly IDisposable _groupProcessorSubscription;
        private readonly IDisposable _dataLoaderSubscription;
        private readonly IDisposable _recommendProcessorSubscription;
        private readonly IDisposable _suggestProcessorSubscription;
        private readonly MainStorage _storage;
        private readonly DomainParser _parser;
        private readonly MainContext _context;

        public MessageProcessor(
            MainContext mainContext,
            MainStorage mainStorage,
            DomainParser parser,
            NewAccountProcessor newAccountProcessor,
            EditAccountProcessor editAccountProcessor,
            NewLikesProcessor newLikesProcessor,
            FilterProcessor filterProcessor,
            DataLoader dataLoader,
            GroupProcessor groupProcessor,
            RecommendProcessor recommendProcessor,
            SuggestProcessor suggestProcessor)
        {
            _context = mainContext;
            _storage = mainStorage;
            _parser = parser;

            _newAccountProcessorSubscription = newAccountProcessor
                .DataReceived
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Subscribe(AddNewAccount);

            _editAccountProcessorSubscription = editAccountProcessor
                .DataReceived
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Subscribe(EditAccount);

            _newLikesProcessorSubscription = newLikesProcessor
                .DataReceived
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Subscribe(NewLikes);

            _filterProcessorSubscription = filterProcessor
                .DataRequest
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Subscribe(Filter);

            _groupProcessorSubscription = groupProcessor
                .DataRequest
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Subscribe(Group);

            _dataLoaderSubscription = dataLoader
                .AccountLoaded
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Subscribe(LoadAccount);

            _recommendProcessorSubscription = recommendProcessor
                .DataRequest
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Subscribe(Recommend);

            _suggestProcessorSubscription = suggestProcessor
                .DataRequest
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Subscribe(Suggest);

        }

        private void Suggest(SuggestRequest request)
        {
            IDictionary<int, float> similarity = new Dictionary<int, float>();
            IDictionary<int, IEnumerable<int>> suggested = new Dictionary<int, IEnumerable<int>>();
            _context.Likes.Suggest(request.Id, similarity, suggested);

            IEnumerable<KeyValuePair<int, IEnumerable<int>>> result = suggested;

            if (request.Country.IsActive)
            {
                int countryId = _storage.Countries.Get(request.Country.Country);
                result = result.Where(x => _context.Countries.Contains(countryId, x.Key));
            }

            if (request.City.IsActive)
            {
                int cityId = _storage.Cities.Get(request.City.City);
                result = result.Where(x => _context.Cities.Contains(cityId, x.Key));
            }


            bool sex = _context.Sex.Contains(true, request.Id);
            result = result.Where(x => _context.Sex.Contains(false, x.Key));

            request.TaskCompletionSource.SetResult(
                result
                    .OrderByDescending(x => similarity[x.Key])
                    .SelectMany(x => x.Value)
                    .Take(request.Limit)
                    .ToList());
        }

        private class RecommendComparer : IComparer<int>
        {
            private readonly MainContext _context;
            private readonly IDictionary<int, int> _recommeded;
            private readonly long _birth;
            public RecommendComparer(
                MainContext context,
                IDictionary<int, int> recommeded,
                long birth)
            {
                _context = context;
                _recommeded = recommeded;
                _birth = birth;
            }

            public int Compare(int x, int y)
            {
                bool premiumX = _context.Premiums.IsNow(x);
                bool premiumY = _context.Premiums.IsNow(y);

                if (premiumX != premiumY)
                {
                    return premiumX ? 1 : -1;
                }

                Status statusX = _context.Statuses.Get(x);
                Status statusY = _context.Statuses.Get(y);

                if (statusX != statusY)
                {
                    if (statusX == Status.Free)
                    {
                        return 1;
                    }
                    else if (statusY == Status.Free)
                    {
                        return -1;
                    }
                    else if (statusX == Status.Complicated)
                    {
                        return 1;
                    }
                    else
                    {
                        return -1;
                    }

                }

                int countX = _recommeded[x];
                int countY = _recommeded[y];

                if (countX != countY)
                {
                    return countX - countY;
                }

                long diffX = Math.Abs(_context.Birth.Get(x).ToUnixTimeSeconds() - _birth);
                long diffY = Math.Abs(_context.Birth.Get(y).ToUnixTimeSeconds() - _birth);

                return (int)(diffY - diffX);
            }
        }

        private void Recommend(RecommendRequest request)
        {
            IDictionary<int, int> recomended = new Dictionary<int, int>();
            _context.Interests.Recommend(request.Id, recomended);

            IEnumerable<KeyValuePair<int, int>> result = recomended;

            if (request.Country.IsActive)
            {
                int countryId = _storage.Countries.Get(request.Country.Country);
                result = result.Where(x => _context.Countries.Contains(countryId, x.Key));
            }

            if (request.City.IsActive)
            {
                int cityId = _storage.Cities.Get(request.City.City);
                result = result.Where(x => _context.Cities.Contains(cityId, x.Key));
            }


            bool sex = _context.Sex.Contains(true, request.Id);
            result = result.Where(x => _context.Sex.Contains(false, x.Key));

            var comparer = new RecommendComparer(
                _context,
                recomended,
                _context.Birth.Get(request.Id).ToUnixTimeSeconds());

            result = result.OrderByDescending(x => x.Key, comparer).Take(request.Limit);
            request.TaskCompletionSource.SetResult(result.Select(x => x.Key).ToList());
        }

        private void Group(GroupRequest request)
        {
            HashSet<int> result = null;

            if (request.Sex.IsActive)
            {
                result = Intersect(result, _context.Sex.Filter(request.Sex));
            }

            if (request.Status.IsActive)
            {
                result = Intersect(result, _context.Statuses.Filter(request.Status));
            }

            if (request.Country.IsActive)
            {
                result = Intersect(result, _context.Countries.Filter(request.Country, _storage.Countries));
            }

            if (request.City.IsActive)
            {
                result = Intersect(result, _context.Cities.Filter(request.City, _storage.Cities));
            }

            if (request.Birth.IsActive)
            {
                result = Intersect(result, _context.Birth.Filter(request.Birth));
            }

            if (request.Interest.IsActive)
            {
                result = Intersect(result, _context.Interests.Filter(request.Interest, _storage.Interests));
            }

            if (request.Like.IsActive)
            {
                result = Intersect(result, _context.Likes.Filter(request.Like));
            }

            if (request.Joined.IsActive)
            {
                result = Intersect(result, _context.Joined.Filter(request.Joined));
            }

            if (result == null)
            {
                result = new HashSet<int>(_storage.Ids.Except(Enumerable.Empty<int>()));
            }

            List<Group> groups = new List<Group>();

            foreach (var key in request.Keys)
            {
                switch (key)
                {
                    case GroupKey.City:
                        _context.Cities.FillGroups(groups);
                        break;
                    case GroupKey.Country:
                        _context.Countries.FillGroups(groups);
                        break;
                    case GroupKey.Interest:
                        _context.Interests.FillGroups(groups);
                        break;
                    case GroupKey.Sex:
                        _context.Sex.FillGroups(groups);
                        break;
                    case GroupKey.Status:
                        _context.Statuses.FillGroups(groups);
                        break;
                }
            }

            Dictionary<Group, int> counters = new Dictionary<Group, int>(groups.Count);

            foreach (var id in result)
            {
                foreach (var group in groups)
                {
                    bool contains = true;

                    foreach (var key in request.Keys)
                    {
                        switch (key)
                        {
                            case GroupKey.City:
                                contains = contains && _context.Cities.Contains(group.CityId, id);
                                break;
                            case GroupKey.Country:
                                contains = contains && _context.Countries.Contains(group.CountryId, id);
                                break;
                            case GroupKey.Interest:
                                contains = contains && _context.Interests.Contains(group.InterestId, id);
                                break;
                            case GroupKey.Sex:
                                contains = contains && _context.Sex.Contains(group.Sex.Value, id);
                                break;
                            case GroupKey.Status:
                                contains = contains && _context.Statuses.Contains(group.Status.Value, id);
                                break;
                        }

                        if (!contains)
                        {
                            break;
                        }
                    }

                    if (contains)
                    {
                        counters[group] = counters.GetValueOrDefault(group) + 1;
                    }
                }
            }

            if (request.Order)
            {
                request.TaskCompletionSource.SetResult(
                    new GroupResponse(
                        counters
                            .OrderBy(x => x.Value)
                            .Take(request.Limit)
                            .Select(x => new GroupEntry(x.Key, x.Value))
                            .ToList()));
            }
            else
            {
                request.TaskCompletionSource.SetResult(
                    new GroupResponse(
                        counters
                            .OrderBy(x => -x.Value)
                            .Take(request.Limit)
                            .Select(x => new GroupEntry(x.Key, x.Value))
                            .ToList()));
            }
        }

        private void Filter(FilterRequest request)
        {
            HashSet<int> result = null;

            if (request.Sex.IsActive)
            {
                result = Intersect(result, _context.Sex.Filter(request.Sex));
            }

            if (request.Email.IsActive)
            {
                result = Intersect(result, _context.Emails.Filter(request.Email, _storage.Domains));
            }

            if (request.Status.IsActive)
            {
                result = Intersect(result, _context.Statuses.Filter(request.Status));
            }

            if (request.Fname.IsActive)
            {
                result = Intersect(result, _context.FirstNames.Filter(request.Fname, _storage.Ids));
            }

            if (request.Sname.IsActive)
            {
                result = Intersect(result, _context.LastNames.Filter(request.Sname, _storage.Ids));
            }

            if (request.Phone.IsActive)
            {
                result = Intersect(result, _context.Phones.Filter(request.Phone, _storage.Ids));
            }

            if (request.Country.IsActive)
            {
                result = Intersect(result, _context.Countries.Filter(request.Country, _storage.Ids, _storage.Countries));
            }

            if (request.City.IsActive)
            {
                result = Intersect(result, _context.Cities.Filter(request.City, _storage.Ids, _storage.Cities));
            }

            if (request.Birth.IsActive)
            {
                result = Intersect(result, _context.Birth.Filter(request.Birth));
            }

            if (request.Interests.IsActive)
            {
                result = Intersect(result, _context.Interests.Filter(request.Interests, _storage.Interests));
            }

            if (request.Likes.IsActive)
            {
                result = Intersect(result, _context.Likes.Filter(request.Likes));
            }

            if (request.Premium.IsActive)
            {
                result = Intersect(result, _context.Premiums.Filter(request.Premium, _storage.Ids));
            }

            if (result == null)
            {
                result = new HashSet<int>(_storage.Ids.Except(Enumerable.Empty<int>()));
            }

            request.TaskComletionSource.SetResult(
                result.OrderBy(x => x).Take(request.Limit).ToList());
        }

        private HashSet<int> Intersect(HashSet<int> result, IEnumerable<int> filtered)
        {
            if (result == null)
            {
                result = new HashSet<int>(filtered);
            }
            else
            {
                result.IntersectWith(filtered);
            }
            return result;
        }

        private void EditAccount(AccountDto dto)
        {
            int id = dto.Id.Value;

            if (dto.Email != null)
            {
                Email email = _parser.ParseEmail(dto.Email);
                _context.Emails.Update(id, email);
            }

            if (dto.FirstName != null)
            {
                _context.FirstNames.AddOrUpdate(id, dto.FirstName);
            }

            if (dto.Surname != null)
            {
                _context.LastNames.AddOrUpdate(id, dto.Surname);
            }

            if (dto.Phone != null)
            {
                Phone phone = _parser.ParsePhone(dto.Phone);
                _context.Phones.Update(id, phone);
            }

            if (dto.Birth.HasValue)
            {
                _context.Birth.AddOrUpdate(id, DateTimeOffset.FromUnixTimeSeconds(dto.Birth.Value));
            }

            if (dto.Country != null)
            {
                _context.Countries.AddOrUpdate(id, _storage.Countries.Get(dto.Country));
            }

            if (dto.City != null)
            {
                _context.Cities.AddOrUpdate(id, _storage.Cities.Get(dto.City));
            }

            if (dto.Joined != null)
            {
                _context.Joined.AddOrUpdate(id, DateTimeOffset.FromUnixTimeSeconds(dto.Joined.Value));
            }

            if (dto.Status != null)
            {
                _context.Statuses.Update(id, StatusHelper.Parse(dto.Status));
            }

            if (dto.Interests != null)
            {
                _context.Interests.RemoveAccount(id);
                foreach (var interestStr in dto.Interests)
                {
                    _context.Interests.Add(id, _storage.Interests.Get(interestStr));
                }
            }

            if (dto.Sex != null)
            {
                _context.Sex.Update(id, dto.Sex == "m");
            }

            if (dto.Premium != null)
            {
                _context.Premiums.AddOrUpdate(
                    id,
                    new Premium(
                        DateTimeOffset.FromUnixTimeSeconds(dto.Premium.Start),
                        DateTimeOffset.FromUnixTimeSeconds(dto.Premium.Finish)
                    )
                );
            }
        }

        private void NewLikes(IReadOnlyList<SingleLikeDto> likeDtos)
        {
            foreach (var likeDto in likeDtos)
            {
                _context.Likes.Add(
                    new Like(
                        likeDto.LikeeId,
                        likeDto.LikerId,
                        DateTimeOffset.FromUnixTimeSeconds(likeDto.Timestamp)
                    )
                );
            }
        }

        private void AddNewAccount(AccountDto dto)
        {
            int id = dto.Id.Value;

            Email email = _parser.ParseEmail(dto.Email);
            _context.Emails.Add(id, email);

            if (dto.FirstName != null)
            {
                _context.FirstNames.AddOrUpdate(id, dto.FirstName);
            }

            if (dto.Surname != null)
            {
                _context.LastNames.AddOrUpdate(id, dto.Surname);
            }

            if (dto.Phone != null)
            {
                Phone phone = _parser.ParsePhone(dto.Phone);
                _context.Phones.Add(id, phone);
            }

            if (dto.Birth.HasValue)
            {
                _context.Birth.AddOrUpdate(id, DateTimeOffset.FromUnixTimeSeconds(dto.Birth.Value));
            }

            if (dto.Country != null)
            {
                _context.Countries.Add(id, _storage.Countries.Get(dto.Country));
            }

            if (dto.City != null)
            {
                _context.Cities.Add(id, _storage.Cities.Get(dto.City));
            }

            if (dto.Joined != null)
            {
                _context.Joined.AddOrUpdate(id, DateTimeOffset.FromUnixTimeSeconds(dto.Joined.Value));
            }

            if (dto.Status != null)
            {
                _context.Statuses.Add(id, StatusHelper.Parse(dto.Status));
            }

            if (dto.Interests != null)
            {
                foreach (var interestStr in dto.Interests)
                {
                    _context.Interests.Add(id, _storage.Interests.Get(interestStr));
                }
            }

            if (dto.Sex != null)
            {
                _context.Sex.Add(id, dto.Sex == "m");
            }

            if (dto.Likes != null)
            {
                foreach (var like in dto.Likes)
                {
                    _context.Likes.Add(new Like(like.Id, id, DateTimeOffset.FromUnixTimeSeconds(like.Timestamp)));
                }
            }

            if (dto.Premium != null)
            {
                _context.Premiums.AddOrUpdate(
                    id,
                    new Premium(
                        DateTimeOffset.FromUnixTimeSeconds(dto.Premium.Start),
                        DateTimeOffset.FromUnixTimeSeconds(dto.Premium.Finish)
                    )
                );
            }
        }

        private void LoadAccount(AccountDto dto)
        {
            int id = dto.Id.Value;
            _storage.Ids.Add(id);

            Email email = _parser.ParseEmail(dto.Email);
            _context.Emails.Add(id, email);
            _storage.EmailHashes.Add(dto.Email, id);

            if (dto.FirstName != null)
            {
                _context.FirstNames.AddOrUpdate(id, dto.FirstName);
            }

            if (dto.Surname != null)
            {
                _context.LastNames.AddOrUpdate(id, dto.Surname);
            }

            if (dto.Phone != null)
            {
                Phone phone = _parser.ParsePhone(dto.Phone);
                _storage.PhoneHashes.Add(dto.Phone, id);
                _context.Phones.Add(id, phone);
            }

            if (dto.Birth.HasValue)
            {
                _context.Birth.AddOrUpdate(id, DateTimeOffset.FromUnixTimeSeconds(dto.Birth.Value));
            }

            if (dto.Country != null)
            {
                _context.Countries.Add(id, _storage.Countries.Get(dto.Country));
            }

            if (dto.City != null)
            {
                _context.Cities.Add(id, _storage.Cities.Get(dto.City));
            }

            if (dto.Joined != null)
            {
                _context.Joined.AddOrUpdate(id, DateTimeOffset.FromUnixTimeSeconds(dto.Joined.Value));
            }

            if (dto.Status != null)
            {
                _context.Statuses.Add(id, StatusHelper.Parse(dto.Status));
            }

            if (dto.Interests != null)
            {
                foreach (var interestStr in dto.Interests)
                {
                    _context.Interests.Add(id, _storage.Interests.Get(interestStr));
                }
            }

            if (dto.Sex != null)
            {
                _context.Sex.Add(id, dto.Sex == "m");
            }

            if (dto.Likes != null)
            {
                foreach (var like in dto.Likes)
                {
                    _context.Likes.Add(new Like(like.Id, id, DateTimeOffset.FromUnixTimeSeconds(like.Timestamp)));
                }
            }

            if (dto.Premium != null)
            {
                _context.Premiums.AddOrUpdate(
                    id,
                    new Premium(
                        DateTimeOffset.FromUnixTimeSeconds(dto.Premium.Start),
                        DateTimeOffset.FromUnixTimeSeconds(dto.Premium.Finish)
                    )
                );
            }
        }
    }
}