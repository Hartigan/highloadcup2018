using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Domain.Dto;
using AspNetCoreWebApi.Processing.Parsers;
using AspNetCoreWebApi.Processing.Pooling;
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
        private struct LoadEvent
        {
            public LoadEvent(AccountDto dto)
            {
                Dto = dto;
                ImportEnded = false;
            }

            public static LoadEvent EndEvent { get; } = new LoadEvent() { Dto = null, ImportEnded = true }; 
            public AccountDto Dto;
            public bool ImportEnded;
        }

        private struct LikeEvent
        {
            public LikeEvent(Like like)
            {
                Like = like;
                ImportEnded = false;
            }

            public static LikeEvent EndEvent { get; } = new LikeEvent() { Like = new Like(), ImportEnded = true }; 
            public Like Like;
            public bool ImportEnded;
        }

        private struct PostEvent
        {
            public AccountDto Account;
            public bool IsAdd;
            public bool Completed;
            public static PostEvent Add(AccountDto dto) => new PostEvent() { Account = dto, IsAdd = true };
            public static PostEvent Edit(AccountDto dto) => new PostEvent() { Account = dto };
            public static PostEvent End() => new PostEvent() { Completed = true };
        }

        private readonly IDisposable _newAccountProcessorSubscription;
        private readonly IDisposable _editAccountProcessorSubscription;
        private readonly IDisposable _newLikesProcessorSubscription;
        private IDisposable _dataLoaderSubscription;
        private readonly IDisposable _secondPhaseEndSubscription;
        private readonly MainStorage _storage;
        private readonly DomainParser _parser;
        private readonly MainContext _context;
        private readonly MainPool _pool;
        private readonly GroupPreprocessor _groupPreprocessor;
        private readonly IComparer<int> _reverseIntComparer = new ReverseComparer<int>(Comparer<int>.Default);
        private readonly SingleThreadWorker<LoadEvent> _loadWorker;
        private readonly SingleThreadWorker<LikeEvent> _likeWorker;
        private readonly SingleThreadWorker<PostEvent> _postWorker;
        private volatile int _editQuery = 0;

        public MessageProcessor(
            MainContext mainContext,
            MainStorage mainStorage,
            DomainParser parser,
            MainPool mainPool,
            GroupPreprocessor groupPreprocessor,
            NewAccountProcessor newAccountProcessor,
            EditAccountProcessor editAccountProcessor,
            NewLikesProcessor newLikesProcessor,
            DataLoader dataLoader)
        {
            _context = mainContext;
            _storage = mainStorage;
            _parser = parser;
            _pool = mainPool;
            _groupPreprocessor = groupPreprocessor;

            var newAccountObservable = newAccountProcessor
                .DataReceived;

            var editAccountObservable = editAccountProcessor
                .DataReceived;

            var newLikesObservable = newLikesProcessor
                .DataReceived;


            _likeWorker = new SingleThreadWorker<LikeEvent>(ProcessLike, "Like thread started");
            _loadWorker = new SingleThreadWorker<LoadEvent>(LoadAccount, "Import thread started");
            _postWorker = new SingleThreadWorker<PostEvent>(PostProcess, "Post thread started");

            _dataLoaderSubscription = dataLoader
                .AccountLoaded
                .Subscribe(
                    item => { _loadWorker.Enqueue(new LoadEvent(item)); },
                     _ => {},
                    () => { _loadWorker.Enqueue(LoadEvent.EndEvent); });

            _newAccountProcessorSubscription = newAccountObservable
                .Subscribe(x => { _postWorker.Enqueue(PostEvent.Add(x)); });

            _editAccountProcessorSubscription = editAccountObservable
                .Subscribe(x => { _postWorker.Enqueue(PostEvent.Edit(x)); });

            _newLikesProcessorSubscription = newLikesObservable
                .Subscribe(NewLikes);

            var updateObservable = newAccountObservable
                .Select(_ => Interlocked.Increment(ref _editQuery))
                .Merge(editAccountObservable.Select(_ => Interlocked.Increment(ref _editQuery)))
                .Merge(newLikesObservable.Select(_ => Interlocked.Increment(ref _editQuery)));

            _secondPhaseEndSubscription = updateObservable
                .Throttle(TimeSpan.FromMilliseconds(1000))
                .Subscribe(_ =>
                    {
                        _postWorker.Enqueue(PostEvent.End());
                        _likeWorker.Enqueue(LikeEvent.EndEvent);
                    });
        }

        private void Collect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForFullGCComplete();
            GC.Collect();
        }

        private void PostProcess(PostEvent e)
        {
            if (e.Completed)
            {
                _context.Compress();
                _context.InitNull(_storage.Ids);
                DataConfig.UpdateInProgress = false;
                Collect();
                return;
            }
            DataConfig.UpdateInProgress = true;

            if (e.IsAdd)
            {
                AddNewAccount(e.Account);
            }
            else
            {
                EditAccount(e.Account);
            }
        }

        public SuggestResponse Suggest(SuggestRequest request)
        {
            Dictionary<int, float> similarity = _pool.DictionaryOfFloatByInt.Get();
            var selfIds = _pool.HashSetOfInts.Get();
            Dictionary<int, IEnumerable<int>> suggested = _pool.DictionaryOfIntsByInt.Get();
            _context.Likes.Suggest(request.Id, similarity, suggested, selfIds);
            

            IEnumerable<int> result = suggested.Keys;

            bool sex = _context.Sex.Contains(true, request.Id);
            result = result.Where(x => _context.Sex.Get(x) == sex);

            if (request.Country.IsActive)
            {
                short countryId = _storage.Countries.Get(request.Country.Country);
                result = result.Where(x => _context.Countries.Get(x) == countryId);
            }

            if (request.City.IsActive)
            {
                short cityId = _storage.Cities.Get(request.City.City);
                result = result.Where(x => _context.Cities.Get(x) == cityId);
            }

            var response = _pool.SuggestResponse.Get();
            var list = _pool.ListOfIntegers.Get();
            var comparer = _pool.SuggestComparer.Get();
            comparer.Init(similarity);
            list.AddRange(result);
            list.Sort(comparer);
            response.Ids.AddRange(list.SelectMany(x => suggested[x]).Take(request.Limit));
            response.Limit = request.Limit;

            _pool.DictionaryOfFloatByInt.Return(similarity);
            _pool.DictionaryOfIntsByInt.Return(suggested);
            _pool.ListOfIntegers.Return(list);
            _pool.SuggestComparer.Return(comparer);
            _pool.HashSetOfInts.Return(selfIds);

            return response;
        }

        public RecommendResponse Recommend(RecommendRequest request)
        {
            Dictionary<int, int> recomended = _pool.DictionaryOfIntByInt.Get();
            _context.Interests.Recommend(
                request.Id,
                recomended,
                _context,
                request.Country.IsActive ? _storage.Countries.Get(request.Country.Country) : (short)0,
                request.City.IsActive ? _storage.Cities.Get(request.City.City) : (short)0);

            var comparer = _pool.RecommendComparer.Get();
            comparer.Init(
                _context,
                recomended,
                _context.Birth.Get(request.Id).Seconds);

            var response = _pool.RecommendResponse.Get();
            response.Limit = request.Limit;
            response.Ids.AddRange(recomended.Keys.TakeMax(comparer, request.Limit));

            _pool.DictionaryOfIntByInt.Return(recomended);
            _pool.RecommendComparer.Return(comparer);

            return response;
        }

        public GroupResponse Group(GroupRequest request)
        {
            var result = _pool.FilterSet.Get();
            var filterList = _pool.ListOfLists.Get();
            bool inited = false;

            if (request.Sex.IsActive)
            {
                Intersect(result, _context.Sex.Filter(request.Sex), ref inited);
            }

            if (request.Status.IsActive)
            {
                Intersect(result, _context.Statuses.Filter(request.Status), ref inited);
            }

            if (request.Country.IsActive)
            {
                var tmp = _pool.FilterSet.Get();
                tmp.Add(_context.Countries.Filter(request.Country, _storage.Countries));
                Intersect(result, tmp, ref inited);
                _pool.FilterSet.Return(tmp);
            }

            if (request.City.IsActive)
            {
                var tmp = _pool.FilterSet.Get();
                tmp.Add(_context.Cities.Filter(request.City, _storage.Cities));
                Intersect(result, tmp, ref inited);
                _pool.FilterSet.Return(tmp);
            }

            if (request.Birth.IsActive)
            {
                Intersect(result, _context.Birth.Filter(request.Birth), ref inited);
            }

            if (request.Interest.IsActive)
            {
                var tmp = _pool.FilterSet.Get();
                tmp.Add(_context.Interests.Filter(request.Interest, _storage.Interests));
                Intersect(result, tmp, ref inited);
                _pool.FilterSet.Return(tmp);
            }

            if (request.Like.IsActive)
            {
                var tmp = _pool.FilterSet.Get();
                tmp.Add(_context.Likes.Filter(request.Like));
                Intersect(result, tmp, ref inited);
                _pool.FilterSet.Return(tmp);
            }

            if (request.Joined.IsActive)
            {
                Intersect(result, _context.Joined.Filter(request.Joined), ref inited);
            }

            GroupResponse response = _pool.GroupResponse.Get();
            response.Limit = request.Limit;

            var entries = _pool.ListOfGroupEntry.Get();

            _groupPreprocessor.FillResponse(entries, inited ? result : null, request.Keys, request.KeyOrder.Count == 1);

            GroupEntryComparer comparer = _pool.GroupEntryComparer.Get();
            comparer.Init(_storage, request.KeyOrder, request.Order);
            response.Entries.AddRange(entries.TakeMax(comparer, request.Limit));

            _pool.ListOfGroupEntry.Return(entries);
            _pool.FilterSet.Return(result);
            _pool.GroupEntryComparer.Return(comparer);
            _pool.ListOfLists.Return(filterList);

            return response;
        }

        public FilterResponse Filter(FilterRequest request)
        {
            FilterSet result = _pool.FilterSet.Get();
            var listFilters = _pool.ListOfLists.Get();
            bool inited = false;

            if (request.Sex.IsActive)
            {
                listFilters.Add(_context.Sex.Filter(request.Sex));
            }

            if (request.Email.IsActive)
            {
                listFilters.Add(_context.Emails.Filter(request.Email, _storage.Domains, _storage.Ids));
            }

            if (request.Status.IsActive)
            {
                listFilters.Add(_context.Statuses.Filter(request.Status));
            }

            if (request.Fname.IsActive)
            {
                listFilters.Add(_context.FirstNames.Filter(request.Fname, _storage.Ids));
            }

            if (request.Sname.IsActive)
            {
                listFilters.Add(_context.LastNames.Filter(request.Sname, _storage.Ids));
            }

            if (request.Phone.IsActive)
            {
                listFilters.Add(_context.Phones.Filter(request.Phone, _storage.Ids));
            }

            if (request.Country.IsActive)
            {
                listFilters.Add(_context.Countries.Filter(request.Country, _storage.Ids, _storage.Countries));
            }

            if (request.City.IsActive)
            {
                listFilters.Add(_context.Cities.Filter(request.City, _storage.Ids, _storage.Cities));
            }

            if (request.Birth.IsActive)
            {
                listFilters.Add(_context.Birth.Filter(request.Birth, _storage.Ids));
            }

            if (request.Interests.IsActive)
            {
                var tmp = _pool.FilterSet.Get();
                _context.Interests.Filter(request.Interests, _storage.Interests, tmp);
                Intersect(result, tmp, ref inited);
                _pool.FilterSet.Return(tmp);
            }

            if (request.Likes.IsActive)
            {
                listFilters.Add(_context.Likes.Filter(request.Likes));
            }

            if (request.Premium.IsActive)
            {
                listFilters.Add(_context.Premiums.Filter(request.Premium, _storage.Ids));
            }

            bool noFiltres = !inited && listFilters.Count == 0;
            bool noLists = listFilters.Count == 0;
            var response = _pool.FilterResponse.Get();

            if (noFiltres)
            {
                response.Ids.AddRange(_storage.Ids.AsEnumerable().Take(request.Limit));
            }
            else
            {
                if (noLists)
                {
                    int count = 0;
                    foreach(var id in _storage.Ids.AsEnumerable())
                    {
                        if (count == request.Limit)
                        {
                            break;
                        }
                        if (result.Contains(id))
                        {
                            response.Ids.Add(id);
                            count++;
                        }
                    }
                }
                else
                {
                    var enumerators = _pool.ListOfEnumerators.Get();
                    enumerators.AddRange(listFilters.Select(x => x.GetEnumerator()));
                    int min = DataConfig.MaxId;

                    for(int i = 0; i < enumerators.Count; i++)
                    {
                        if (!enumerators[i].MoveNext())
                        {
                            _pool.ListOfEnumerators.Return(enumerators);
                            goto Finish;
                        }
                        min = Math.Min(min, enumerators[i].Current);
                    }

                    do
                    {
                        for (int i = 0; i < enumerators.Count; i++)
                        {
                            while (enumerators[i].Current > min)
                            {
                                if (!enumerators[i].MoveNext())
                                {
                                    _pool.ListOfEnumerators.Return(enumerators);
                                    goto Finish;
                                }
                            }
                        }

                        int currentMin = int.MaxValue;
                        for (int i = 0; i < enumerators.Count; i++)
                        {
                            if (enumerators[i].Current < currentMin)
                            {
                                currentMin = enumerators[i].Current;
                            }
                        }

                        if (currentMin == min)
                        {
                            if ((inited && result.Contains(min)) || !inited)
                            {
                                response.Ids.Add(min);
                                if (response.Ids.Count == request.Limit)
                                {
                                    _pool.ListOfEnumerators.Return(enumerators);
                                    goto Finish;
                                }
                            }

                            if (enumerators[0].MoveNext())
                            {
                                min = enumerators[0].Current;
                            }
                            else
                            {
                                _pool.ListOfEnumerators.Return(enumerators);
                                goto Finish;
                            }
                        }
                        else
                        {
                            min = currentMin;
                        }
                    }
                    while (true);
                }
            }
        Finish:
            response.Limit = request.Limit;

            _pool.FilterSet.Return(result);
            _pool.ListOfLists.Return(listFilters);

            return response;
        }

        private void Intersect(FilterSet result, IFilterSet filtered, ref bool inited)
        {
            if (inited)
            {
                result.IntersectWith(filtered);
            }
            else
            {
                result.Add(filtered);
                inited = true;
            }
            
        }

        private void EditAccount(AccountDto dto)
        {
            int id = dto.Id.Value;

            if (dto.Interests != null && dto.Interests.Count > 0)
            {
                _context.Interests.RemoveAccount(id);
                foreach (var interestStr in dto.Interests)
                {
                    _context.Interests.Add(id, _storage.Interests.Get(interestStr));
                }
                dto.Interests.Clear();
            }

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
                _context.Birth.AddOrUpdate(id, new UnixTime(dto.Birth.Value));
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
                _context.Joined.AddOrUpdate(id, new UnixTime(dto.Joined.Value));
            }

            if (dto.Status != null)
            {
                _context.Statuses.Update(id, StatusHelper.Parse(dto.Status));
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
                        new UnixTime(dto.Premium.Start),
                        new UnixTime(dto.Premium.Finish)
                    )
                );
            }

            _groupPreprocessor.Update(dto);
        }

        private void NewLikes(List<SingleLikeDto> likeDtos)
        {
            foreach (var likeDto in likeDtos)
            {
                _likeWorker.Enqueue(
                    new LikeEvent(
                        new Like(
                            likeDto.LikeeId,
                            likeDto.LikerId,
                            new UnixTime(likeDto.Timestamp)
                        )
                    )
                );
                _pool.SingleLikeDto.Return(likeDto);
            }
            _pool.ListOfLikeDto.Return(likeDtos);
        }

        private void AddNewAccount(AccountDto dto)
        {
            int id = dto.Id.Value;

            if (dto.Likes != null)
            {
                foreach(var like in dto.Likes)
                {
                    _likeWorker.Enqueue(new LikeEvent(new Like(like.Id, id, new UnixTime(like.Timestamp))));
                }
                dto.Likes.Clear();
            }

            if (dto.Interests != null)
            {
                foreach (var interestStr in dto.Interests)
                {
                    _context.Interests.Add(id, _storage.Interests.Get(interestStr));
                }
                dto.Interests.Clear();
            }

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
                _context.Birth.AddOrUpdate(id, new UnixTime(dto.Birth.Value));
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
                _context.Joined.AddOrUpdate(id, new UnixTime(dto.Joined.Value));
            }

            if (dto.Status != null)
            {
                _context.Statuses.Add(id, StatusHelper.Parse(dto.Status));
            }

            if (dto.Sex != null)
            {
                _context.Sex.Add(id, dto.Sex == "m");
            }

            if (dto.Premium != null)
            {
                _context.Premiums.AddOrUpdate(
                    id,
                    new Premium(
                        new UnixTime(dto.Premium.Start),
                        new UnixTime(dto.Premium.Finish)
                    )
                );
            }

            _groupPreprocessor.Add(dto);
        }

        private void ProcessLike(LikeEvent e)
        {
            if (e.ImportEnded)
            {
                _context.Likes.Compress();
                Collect();
                Console.WriteLine($"Likes import end {DateTime.Now}");
                return;
            }

            _context.Likes.Add(e.Like);
        }

        private void LoadAccount(LoadEvent e)
        {
            if (e.ImportEnded)
            {
                _likeWorker.Enqueue(LikeEvent.EndEvent);
                _context.LoadEnded();
                _context.InitNull(_storage.Ids);
                _groupPreprocessor.LoadEnd();
                Collect();
                Console.WriteLine($"Import end {DateTime.Now}");
                return;
            }

            var dto = e.Dto;
            int id = dto.Id.Value;
            _storage.Ids.Add(id);

            if (dto.Likes != null)
            {
                foreach(var like in dto.Likes)
                {
                    _likeWorker.Enqueue(new LikeEvent(new Like(like.Id, id, new UnixTime(like.Timestamp))));
                }
                dto.Likes.Clear();
            }

            if (dto.Interests != null)
            {
                _context.Interests.LoadBatch(id, dto.Interests.Select(x => _storage.Interests.Get(x)));
                dto.Interests.Clear();
            }

            Email email = _parser.ParseEmail(dto.Email);
            _context.Emails.LoadBatch(id, email);
            _storage.EmailHashes.Add(dto.Email, id);

            if (dto.FirstName != null)
            {
                _context.FirstNames.LoadBatch(id, dto.FirstName);
            }

            if (dto.Surname != null)
            {
                _context.LastNames.LoadBatch(id, dto.Surname);
            }

            if (dto.Phone != null)
            {
                Phone phone = _parser.ParsePhone(dto.Phone);
                _storage.PhoneHashes.Add(dto.Phone, id);
                _context.Phones.LoadBatch(id, phone);
            }

            if (dto.Birth.HasValue)
            {
                _context.Birth.LoadBatch(id, new UnixTime(dto.Birth.Value));
            }

            if (dto.Country != null)
            {
                _context.Countries.LoadBatch(id, _storage.Countries.Get(dto.Country));
            }

            if (dto.City != null)
            {
                _context.Cities.LoadBatch(id, _storage.Cities.Get(dto.City));
            }

            if (dto.Joined != null)
            {
                _context.Joined.LoadBatch(id, new UnixTime(dto.Joined.Value));
            }

            if (dto.Status != null)
            {
                _context.Statuses.LoadBatch(id, StatusHelper.Parse(dto.Status));
            }

            if (dto.Sex != null)
            {
                _context.Sex.LoadBatch(id, dto.Sex == "m");
            }

            if (dto.Premium != null)
            {
                _context.Premiums.LoadBatch(id, new Premium(
                        new UnixTime(dto.Premium.Start),
                        new UnixTime(dto.Premium.Finish)
                    ));
            }

            _groupPreprocessor.Load(dto);
        }
    }
}