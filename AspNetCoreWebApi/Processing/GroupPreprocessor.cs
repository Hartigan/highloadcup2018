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
        private readonly List<Group> _index = new List<Group>();
        private readonly List<List<int>> _data = new List<List<int>>();
        private readonly List<int>[] _sorted = new List<int>[120];
        private readonly Dictionary<int, int> _pointer = new Dictionary<int, int>();

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
            Dictionary<Group, List<int>> groups = new Dictionary<Group, List<int>>(new GroupEqualityComparer());
            List<short?> interests = new List<short?>();

            _context.Interests.FillGroups(interests);

            foreach(var interestId in interests)
            {
                var ids = _context.Interests.GetByInterestId(interestId);
                foreach(var id in ids)
                {
                    short? cityId = _context.Cities.Get(id);
                    short? countryId = _context.Countries.Get(id);
                    bool sex = _context.Sex.Get(id);
                    Status status = _context.Statuses.Get(id);

                    Group key = new Group(sex, status, interestId, countryId, cityId);
                    var group = groups.GetValueOrDefault(key);
                    if (group == null)
                    {
                        group = new List<int>();
                        groups[key] = group;
                    }
                    group.Add(id);
                }
            }

            foreach(var group in groups)
            {
                group.Value.TrimExcess();
                _index.Add(group.Key);
                _data.Add(group.Value);
            }

            _index.TrimExcess();
            _data.TrimExcess();

            int sortedIndex = 0;
            var comparer = new GroupComparer();
            var keys = new List<GroupKey>(4);

            for (GroupKey index_0 = GroupKey.Sex; index_0 <= GroupKey.City; index_0++)
            {
                keys.Add(index_0);
                for (GroupKey index_1 = GroupKey.Sex; index_1 <= GroupKey.City; index_1++)
                {
                    if (index_1 == index_0) continue;
                    keys.Add(index_1);
                    for (GroupKey index_2 = GroupKey.Sex; index_2 <= GroupKey.City; index_2++)
                    {
                        if (index_2 == index_0 || index_2 == index_1) continue;
                        keys.Add(index_2);
                        for (GroupKey index_3 = GroupKey.Sex; index_3 <= GroupKey.City; index_3++)
                        {
                            if (index_3 == index_0 || index_3 == index_1 || index_3 == index_2) continue;
                            keys.Add(index_3);

                            int keyCode1 = (byte)index_0 + (byte)index_1 * 4 + (byte)index_2 * 16 + (byte)index_3 * 64;
                            int keyCode2 = (byte)index_0 + (byte)index_1 * 4 + (byte)index_2 * 16;
                            int keyCode3 = (byte)index_0 + (byte)index_1 * 4;
                            int keyCode4 = (byte)index_0;

                            _pointer[keyCode1] = sortedIndex;
                            _pointer[keyCode2] = sortedIndex;
                            _pointer[keyCode3] = sortedIndex;
                            _pointer[keyCode4] = sortedIndex;

                            comparer.Init(keys, _index);
                            var list = new List<int>(_index.Count);
                            for (int i = 0; i < _index.Count; i++)
                            {
                                list.Add(i);
                            }
                            list.Sort(comparer);
                            _sorted[sortedIndex] = list;

                            sortedIndex++;
                            keys.RemoveAt(3);
                        }
                        keys.RemoveAt(2);
                    }
                    keys.RemoveAt(1);
                }
                keys.RemoveAt(0);
            }

            Console.WriteLine("End of groups rebuild");
        }

        private Group GetCurrent(Group current, List<GroupKey> keys)
        {
            var newKey = new Group();
            foreach (var key in keys)
            {
                switch (key)
                {
                    case GroupKey.City:
                        newKey.CityId = current.CityId;
                        break;
                    case GroupKey.Country:
                        newKey.CountryId = current.CountryId;
                        break;
                    case GroupKey.Interest:
                        newKey.InterestId = current.InterestId;
                        break;
                    case GroupKey.Sex:
                        newKey.Sex = current.Sex;
                        break;
                    case GroupKey.Status:
                        newKey.Status = current.Status;
                        break;
                }
            }
            return newKey;
        }

        public void FillResponse(
            GroupResponse response,
            FilterSet ids,
            List<GroupKey> keys)
        {
            var currentSet = _pool.CountSet.Get();
            var comparer = _pool.GroupComparer.Get();

            comparer.Init(keys, _index);

            // calculate groups
            List<int> groups = null;
            if (keys.Count == 5)
            {
                groups = _sorted[0];
            }
            else
            {
                int q = 1;
                int sum = 0;
                foreach (var key in keys)
                {
                    sum += (byte)key * q;
                    q *= 4;
                }
                groups = _sorted[_pointer[sum]];
            }

            var currentKey = GetCurrent(_index[groups[0]], keys);
            var lastIndex = 0;

            foreach(var groupKey in groups)
            {
                if (comparer.Compare(lastIndex, groupKey) != 0)
                {
                    response.Entries.Add(new GroupEntry(currentKey, currentSet.Count));
                    currentSet.Clear();
                    currentKey = GetCurrent(_index[groupKey], keys);
                    lastIndex = groupKey;
                }
                currentSet.UnionWith(_data[groupKey]);
            }

            // last group
            response.Entries.Add(new GroupEntry(currentKey, currentSet.Count));

            _pool.CountSet.Return(currentSet);
            _pool.GroupComparer.Return(comparer);
        }
    }
}
