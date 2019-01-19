using AspNetCoreWebApi.Domain;
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
        private readonly MainContext _context;
        private readonly MainStorage _storage;
        private readonly MainPool _pool;

        private Dictionary<GroupKey, Dictionary<Group, List<int>>> _data = new Dictionary<GroupKey, Dictionary<Group, List<int>>>(); 

        public GroupPreprocessor(
            MainContext mainContext,
            MainStorage mainStorage,
            MainPool mainPool)
        {
            _context = mainContext;
            _storage = mainStorage;
            _pool = mainPool;
        }

        public void Rebuild()
        {
            Console.WriteLine("Rebuild");
            List<short?> interests = new List<short?>();
            var comparer = new GroupEqualityComparer();
            _context.Interests.FillGroups(interests);

            for(int i = 1; i < 32; i++)
            {
                GroupKey keys = (GroupKey)i;
                _data[keys] = new Dictionary<Group, List<int>>(comparer);
            }

            foreach(var interestId in interests)
            {
                var ids = _context.Interests.GetByInterestId(interestId);
                foreach(var id in ids)
                {
                    short? cityId = _context.Cities.Get(id);
                    short? countryId = _context.Countries.Get(id);
                    bool sex = _context.Sex.Get(id);
                    Status status = _context.Statuses.Get(id);

                    foreach(var section in _data)
                    {
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
                            group.InterestId = interestId;
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
                            !group.InterestId.HasValue &&
                            !group.Sex.HasValue &&
                            !group.Status.HasValue &&
                            i > 1)
                        {
                            continue;
                        }

                        if (!group.InterestId.HasValue && section.Key == GroupKey.Interest && i == 1)
                        {
                            continue;
                        }

                        List<int> groupIds;
                        if (!section.Value.TryGetValue(group, out groupIds))
                        {
                            groupIds = new List<int>();
                            section.Value[group] = groupIds;
                        }
                        var index = groupIds.BinarySearch(id);
                        if (index < 0)
                        {
                            groupIds.Insert(~index, id);
                        }
                    }
                }
            }

            _data.TrimExcess();
            foreach(var dict in _data.Values)
            {
                dict.TrimExcess();
                foreach(var list in dict.Values)
                {
                    list.TrimExcess();
                }
            }

            Console.WriteLine("End of groups rebuild");
        }

        public void FillResponse(
            GroupResponse response,
            FilterSet ids,
            GroupKey keys)
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
}
