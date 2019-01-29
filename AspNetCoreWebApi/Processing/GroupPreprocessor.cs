using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Domain.Dto;
using AspNetCoreWebApi.Processing.Pooling;
using AspNetCoreWebApi.Processing.Requests;
using AspNetCoreWebApi.Processing.Responses;
using AspNetCoreWebApi.Storage;
using AspNetCoreWebApi.Storage.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AspNetCoreWebApi.Processing
{
    public class GroupPreprocessor
    {
        private struct Request
        {
            public static Request Add(AccountDto dto) => new Request() { IsAdd = true, Dto = dto };
            public static Request Load(AccountDto dto) => new Request() { IsLoad = true, Dto = dto };
            public static Request Edit(AccountDto dto) => new Request() { Dto = dto };
            public static Request ImportCompleted() => new Request() { ImportEnded = true };
            public static Request PostCompleted() => new Request() { PostEnded = true };

            public bool IsLoad;
            public bool IsAdd;
            public AccountDto Dto;
            public bool PostEnded;
            public bool ImportEnded;
        }

        private struct GroupBucket
        {
            public GroupBucket(Group group)
            {
                Key = group;
                Ids = null;
            }

            public GroupBucket(Group group, int id)
            {
                Key = group;
                Ids = DelaySortedList<int>.CreateDefault();
                Ids.Load(id);
            }

            public Group Key;
            public DelaySortedList<int> Ids;
        }

        private class GroupBucketComparer : IComparer<GroupBucket>
        {
            public static GroupBucketComparer Default { get; } = new GroupBucketComparer();

            public int Compare(GroupBucket x, GroupBucket y)
            {
                return x.Key.CompareTo(y.Key);
            }
        }

        private readonly MainContext _context;
        private readonly MainStorage _storage;
        private readonly MainPool _pool;
        private readonly SingleThreadWorker<Request> _worker;
        private Dictionary<GroupKey, SortedSet<GroupBucket>> _data = new Dictionary<GroupKey, SortedSet<GroupBucket>>(); 

        public GroupPreprocessor(
            MainContext mainContext,
            MainStorage mainStorage,
            MainPool mainPool)
        {
            _context = mainContext;
            _storage = mainStorage;
            _pool = mainPool;

            _worker = new SingleThreadWorker<Request>(r => {
                if (r.PostEnded)
                {
                    CompressImpl();
                    DataConfig.UpdateInProgress = false;
                    return;
                }

                if (r.ImportEnded)
                {
                    LoadEndedImpl();
                    return;
                }

                if (r.IsLoad)
                {
                    AddImpl(r.Dto, true);
                    return;
                }

                if (r.IsAdd)
                {
                    AddImpl(r.Dto, false);
                }
                else
                {
                    UpdateImpl(r.Dto);
                }
            }, "Group thread started");

            for(int i = 1; i < 32; i++)
            {
                GroupKey keys = (GroupKey)i;

                int initialCapacity = 1;
                if (keys.HasFlag(GroupKey.Sex))
                {
                    initialCapacity *= 2;
                }

                if (keys.HasFlag(GroupKey.Status))
                {
                    initialCapacity *= 3;
                }

                if (keys.HasFlag(GroupKey.City))
                {
                    initialCapacity *= 700;
                }

                if (keys.HasFlag(GroupKey.Country))
                {
                    initialCapacity *= 100;
                }

                if (keys.HasFlag(GroupKey.Interest))
                {
                    initialCapacity *= 160;
                }

                initialCapacity = Math.Min(initialCapacity, DataConfig.MaxId);

                _data[keys] = new SortedSet<GroupBucket>(GroupBucketComparer.Default);
            }
        }

        private void LoadEndedImpl()
        {
            _data.TrimExcess();
            foreach (var buckets in _data.Values)
            {
                foreach (var bucket in buckets)
                {
                    bucket.Ids.LoadEnded();
                }
            }
        }

        public void Compress()
        {
            _worker.Enqueue(Request.PostCompleted());
        }

        private void CompressImpl()
        {
            _data.TrimExcess();
            foreach(var buckets in _data.Values)
            {
                foreach(var bucket in buckets)
                {
                    bucket.Ids.Flush();
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForFullGCComplete();
            GC.Collect();
        }

        public void FillResponse(
            List<GroupEntry> entries,
            FilterSet ids,
            GroupKey keys,
            bool singleKey)
        {
            if (singleKey)
            {
                if (ids == null)
                {
                    switch(keys)
                    {
                        case GroupKey.City:
                            entries.AddRange(
                                _context.Cities
                                    .GetGroups()
                                    .Select(x => new GroupEntry(new Group(GroupKey.City, cityId: x.Key), x.Count)));
                            return;
                        case GroupKey.Country:
                            entries.AddRange(
                                _context.Countries
                                    .GetGroups()
                                    .Select(x => new GroupEntry(new Group(GroupKey.Country, countryId: x.Key), x.Count)));
                            return;
                        case GroupKey.Interest:
                            entries.AddRange(
                                _context.Interests
                                    .GetGroups()
                                    .Select(x => new GroupEntry(new Group(GroupKey.Interest, interestId: x.Key), x.Count)));
                            return;
                        case GroupKey.Sex:
                            entries.AddRange(
                                _context.Sex
                                    .GetGroups()
                                    .Select(x => new GroupEntry(new Group(GroupKey.Sex, sex: x.Key), x.Count)));
                            return;
                        case GroupKey.Status:
                            entries.AddRange(
                                _context.Statuses
                                    .GetGroups()
                                    .Select(x => new GroupEntry(new Group(GroupKey.Status, status: x.Key), x.Count)));
                            return;
                        default:
                            return;
                    }
                }
                else
                {
                    switch(keys)
                    {
                        case GroupKey.City:
                            foreach (var item in _context.Cities.GetGroups())
                            {
                                int count = item.Ids.Count(x => ids.Contains(x));
                                if (count > 0)
                                {
                                    entries.Add(new GroupEntry(new Group(GroupKey.City, cityId: item.Key), count));
                                }
                            }
                            return;
                        case GroupKey.Country:
                            foreach (var item in _context.Countries.GetGroups())
                            {
                                int count = item.Ids.Count(x => ids.Contains(x));
                                if (count > 0)
                                {
                                    entries.Add(new GroupEntry(new Group(GroupKey.Country, countryId: item.Key), count));
                                }
                            }
                            return;
                        case GroupKey.Interest:
                            foreach (var item in _context.Interests.GetGroups())
                            {
                                int count = item.Ids.Count(x => ids.Contains(x));
                                if (count > 0)
                                {
                                    entries.Add(new GroupEntry(new Group(GroupKey.Interest, interestId: item.Key), count));
                                }
                            }
                            return;
                        case GroupKey.Sex:
                            foreach (var item in _context.Sex.GetGroups())
                            {
                                int count = item.Ids.Count(x => ids.Contains(x));
                                if (count > 0)
                                {
                                    entries.Add(new GroupEntry(new Group(GroupKey.Sex, sex: item.Key), count));
                                }
                            }
                            return;
                        case GroupKey.Status:
                            foreach (var item in _context.Statuses.GetGroups())
                            {
                                int count = item.Ids.Count(x => ids.Contains(x));
                                if (count > 0)
                                {
                                    entries.Add(new GroupEntry(new Group(GroupKey.Status, status: item.Key), count));
                                }
                            }
                            return;
                        default:
                            return;
                    }
                }
            }


            if (ids == null)
            {
                foreach(var group in _data[keys])
                {
                    int count = group.Ids.Count;
                    if (count > 0)
                    {
                        entries.Add(new GroupEntry(group.Key, count));
                    }
                }
            }
            else
            {
                foreach(var group in _data[keys])
                {
                    int count = group.Ids.GetList().Count(x => ids.Contains(x));
                    if (count > 0)
                    {
                        entries.Add(new GroupEntry(group.Key, count));
                    }
                }
            }
        }

        public void Add(AccountDto dto)
        {
            _worker.Enqueue(Request.Add(dto));
        }

        public void Load(AccountDto dto)
        {
            _worker.Enqueue(Request.Load(dto));
        }

        public void LoadEnd()
        {
            _worker.Enqueue(Request.ImportCompleted());
        }

        private void UpdateGroups(
            int id,
            bool sex,
            Status status,
            short cityId,
            short countryId,
            List<short> interestIds,
            bool isImport)
        {
            foreach(var section in _data)
            {
                if ((section.Key ^ GroupKey.City) == GroupKey.Empty &&
                    (section.Key ^ GroupKey.Country) == GroupKey.Empty &&
                    (section.Key ^ GroupKey.Interest) == GroupKey.Empty &&
                    (section.Key ^ GroupKey.Status) == GroupKey.Empty &&
                    (section.Key ^ GroupKey.Sex) == GroupKey.Empty) 
                {
                    continue;
                }

                if (interestIds.Count == 0 && (section.Key ^ GroupKey.Interest) == GroupKey.Empty)
                {
                    continue;
                }

                Group group = new Group();
                group.Keys = section.Key;
                int i = 0;
                byte key = (byte)section.Key;
                if ((key & (byte)GroupKey.City) > 0)
                {
                    group.CityId = cityId;
                    i++;
                }
                if ((key & (byte)GroupKey.Country) > 0)
                {
                    group.CountryId = countryId;
                    i++;
                }
                if ((key & (byte)GroupKey.Interest) > 0)
                {
                    i++;
                }
                if ((key & (byte)GroupKey.Sex) > 0)
                {
                    group.Sex = sex;
                    i++;
                }
                if ((key & (byte)GroupKey.Status) > 0)
                {
                    group.Status = status;
                    i++;
                }

                if ((key & (byte)GroupKey.Interest) > 0)
                {
                    for(int index = 0; index < interestIds.Count; index++)
                    {
                        group.InterestId = interestIds[index];
                        AddAccountToBuckets(section.Value, id, group, isImport);
                    }
                }
                else
                {
                    AddAccountToBuckets(section.Value, id, group, isImport);
                }
            }
        }

        private void AddAccountToBuckets(SortedSet<GroupBucket> buckets, int id, Group group, bool isImport)
        {
            GroupBucket currentBucket = new GroupBucket(group);

            if (buckets.TryGetValue(currentBucket, out currentBucket))
            {
                if (isImport)
                {
                    currentBucket.Ids.Load(id);
                }
                else
                {
                    currentBucket.Ids.DelayAdd(id);
                }
            }
            else
            {
                currentBucket.Key = group;
                currentBucket.Ids = DelaySortedList<int>.CreateDefault();
                currentBucket.Ids.Load(id);
                buckets.Add(currentBucket);
            }
        }

        private void AddImpl(AccountDto dto, bool isImport)
        {
            var interestIds = _pool.ListOfInt16.Get();
            int id = dto.Id.Value;
            bool sex = dto.Sex == "m";
            Status status = StatusHelper.Parse(dto.Status);
            short cityId = dto.City == null ? (short)0 : _storage.Cities.Get(dto.City);
            short countryId = dto.Country == null ? (short)0 : _storage.Countries.Get(dto.Country);
            if (dto.Interests != null)
            {
                interestIds.AddRange(dto.Interests.Select(x => _storage.Interests.Get(x)));
            }
            _pool.AccountDto.Return(dto);

            UpdateGroups(id, sex, status, cityId, countryId, interestIds, isImport);

            _pool.ListOfInt16.Return(interestIds);
        }

        public void Update(AccountDto dto)
        {
            if (dto.City == null &&
                dto.Country == null &&
                (dto.Interests == null || dto.Interests.Count == 0) &&
                dto.Sex == null &&
                dto.Status == null)
            {
                _pool.AccountDto.Return(dto);
                return;
            }

            _worker.Enqueue(Request.Edit(dto));
        }

        private void UpdateImpl(AccountDto dto)
        {
            int id = dto.Id.Value;
            bool sex = false;
            short cityId = 0;
            short countryId = 0;
            Status status = default(Status);
            var interestIds = _pool.ListOfInt16.Get();

            if (dto.Sex == null)
            {
                sex = _context.Sex.Get(id);
            }
            else
            {
                sex = dto.Sex == "m";
            }

            if (dto.Status == null)
            {
                status = _context.Statuses.Get(id);
            }
            else
            {
                status = StatusHelper.Parse(dto.Status);
            }

            if (dto.City == null)
            {
                cityId = _context.Cities.Get(id);
            }
            else
            {
                cityId = _storage.Cities.Get(dto.City);
            }

            if (dto.Country == null)
            {
                countryId = _context.Countries.Get(id);
            }
            else
            {
                countryId = _storage.Countries.Get(dto.Country);
            }

            if (dto.Interests == null || dto.Interests.Count == 0)
            {
                interestIds.AddRange(_context.Interests.GetAccountInterests(id));
            }
            else
            {
                interestIds.AddRange(dto.Interests.Select(x => _storage.Interests.Get(x)));
            }

            foreach(var buckets in _data.Values)
            {
                foreach (var bucket in buckets)
                {
                    bucket.Ids.DelayRemove(id);
                }
            }

            _pool.AccountDto.Return(dto);

            UpdateGroups(id, sex, status, cityId, countryId, interestIds, false);

            _pool.ListOfInt16.Return(interestIds);
        }
    }
}
