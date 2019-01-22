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
using System.Reactive.Concurrency;
using System.Threading;

namespace AspNetCoreWebApi.Processing
{
    public class GroupEqualityComparer : IEqualityComparer<Group>
    {
        public bool Equals(Group x, Group y)
        {
            return x.CityId == y.CityId &&
                x.CountryId == y.CountryId &&
                x.InterestId == y.InterestId &&
                x.Sex == y.Sex &&
                x.Status == y.Status;
        }

        public int GetHashCode(Group obj)
        {
            return
                obj.Sex.GetHashCode() ^
                obj.Status.GetHashCode() ^
                obj.InterestId.GetHashCode() ^
                obj.CountryId.GetHashCode() ^
                obj.CityId.GetHashCode();
        }
    }

    public class GroupPreprocessor
    {
        private struct Request
        {
            public Request(bool isAdd, AccountDto dto)
            {
                IsAdd = isAdd;
                Dto = dto;
            }
            public bool IsAdd;
            public AccountDto Dto; 
        }

        private readonly MainContext _context;
        private readonly MainStorage _storage;
        private readonly MainPool _pool;
        private readonly SingleThreadWorker<Request> _worker;
        private Dictionary<GroupKey, Dictionary<Group, List<int>>> _data = new Dictionary<GroupKey, Dictionary<Group, List<int>>>(); 

        public GroupPreprocessor(
            MainContext mainContext,
            MainStorage mainStorage,
            MainPool mainPool)
        {
            _context = mainContext;
            _storage = mainStorage;
            _pool = mainPool;

            _worker = new SingleThreadWorker<Request>(r => {
                if (r.IsAdd)
                {
                    AddImpl(r.Dto);
                }
                else
                {
                    UpdateImpl(r.Dto);
                }
                _pool.AccountDto.Return(r.Dto);
            }, "Group thread started");

            var comparer = new GroupEqualityComparer();
            for(int i = 1; i < 32; i++)
            {
                GroupKey keys = (GroupKey)i;
                _data[keys] = new Dictionary<Group, List<int>>(comparer);
            }
        }

        public void FillResponse(
            GroupResponse response,
            FilterSet ids,
            GroupKey keys)
        {
            if (ids == null)
            {
                foreach(var group in _data[keys])
                {
                    int count = group.Value.Count;
                    if (count > 0)
                    {
                        response.Entries.Add(new GroupEntry(group.Key, count));
                    }
                }
            }
            else
            {
                foreach(var group in _data[keys])
                {
                    int count = group.Value.Count(x => ids.Contains(x));
                    if (count > 0)
                    {
                        response.Entries.Add(new GroupEntry(group.Key, count));
                    }
                }
            }
        }

        public void Add(AccountDto dto)
        {
            _worker.Enqueue(new Request(true, dto));
        }

        private void UpdateGroups(
            int id,
            bool sex,
            Status status,
            short? cityId,
            short? countryId,
            List<short> interestIds)
        {
            foreach(var section in _data)
            {
                if (interestIds.Count == 0 && (section.Key ^ GroupKey.Interest) == GroupKey.Empty)
                {
                    continue;
                }

                Group group = new Group();
                int i = 0;
                if (section.Key.HasFlag(GroupKey.City))
                {
                    group.CityId = cityId;
                    i++;
                }
                if (section.Key.HasFlag(GroupKey.Country))
                {
                    group.CountryId = countryId;
                    i++;
                }
                if (section.Key.HasFlag(GroupKey.Interest))
                {
                    i++;
                }
                if (section.Key.HasFlag(GroupKey.Sex))
                {
                    group.Sex = sex;
                    i++;
                }
                if (section.Key.HasFlag(GroupKey.Status))
                {
                    group.Status = status;
                    i++;
                }
                if (!group.CityId.HasValue &&
                    !group.CountryId.HasValue &&
                    interestIds.Count == 0 &&
                    !group.Sex.HasValue &&
                    !group.Status.HasValue &&
                    i > 1)
                {
                    continue;
                }

                if (section.Key.HasFlag(GroupKey.Interest))
                {
                    for(int index = 0; index < interestIds.Count; index++)
                    {
                        group.InterestId = interestIds[index];
                        List<int> groupIds;
                        if (!section.Value.TryGetValue(group, out groupIds))
                        {
                            groupIds = new List<int>();
                            section.Value[group] = groupIds;
                        }
                        groupIds.Add(id);
                    }
                }
                else
                {
                    List<int> groupIds;
                    if (!section.Value.TryGetValue(group, out groupIds))
                    {
                        groupIds = new List<int>();
                        section.Value[group] = groupIds;
                    }
                    groupIds.Add(id);
                }
            }
        }

        private void AddImpl(AccountDto dto)
        {
            var interestIds = _pool.ListOfInt16.Get();
            int id = dto.Id.Value;
            bool sex = dto.Sex == "m";
            Status status = StatusHelper.Parse(dto.Status);
            short? cityId = dto.City == null ? (short?)null : _storage.Cities.Get(dto.City);
            short? countryId = dto.Country == null ? (short?)null : _storage.Countries.Get(dto.Country);
            if (dto.Interests != null)
            {
                interestIds.AddRange(dto.Interests.Select(x => _storage.Interests.Get(x)));
            }

            UpdateGroups(id, sex, status, cityId, countryId, interestIds);

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

            _worker.Enqueue(new Request(false, dto));
        }

        private void UpdateImpl(AccountDto dto)
        {
            int id = dto.Id.Value;
            bool sex = false;
            short? cityId = null;
            short? countryId = null;
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

            foreach(var list in _data.Values.SelectMany(x => x.Values))
            {
                list.Remove(id);
            }

            UpdateGroups(id, sex, status, cityId, countryId, interestIds);

            _pool.ListOfInt16.Return(interestIds);
        }
    }
}
