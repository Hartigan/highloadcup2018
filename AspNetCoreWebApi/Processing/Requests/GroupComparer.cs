using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Pooling;
using System.Collections.Generic;

namespace AspNetCoreWebApi.Processing.Requests
{
    public class GroupComparer : IComparer<int>, IClearable
    {
        private List<GroupKey> _keys;
        private List<Group> _index;

        public void Init(List<GroupKey> keys, List<Group> index)
        {
            _keys = keys;
            _index = index;
        }

        public int Compare(int indexX, int indexY)
        {
            var x = _index[indexX];
            var y = _index[indexY];

            foreach (var key in _keys)
            {
                switch (key)
                {
                    case GroupKey.Sex:
                        if (x.Sex != y.Sex)
                        {
                            if (x.Sex.HasValue != y.Sex.HasValue)
                            {
                                return x.Sex.HasValue ? 1 : -1;
                            }
                            else
                            {
                                return x.Sex.Value ? 1 : -1;
                            }
                        }
                        break;
                    case GroupKey.Status:
                        if (x.Status != y.Status)
                        {
                            if (x.Status.HasValue != y.Status.HasValue)
                            {
                                return x.Status.HasValue ? 1 : -1;
                            }
                            else
                            {
                                return StatusHelper.CompareString(x.Status.Value, y.Status.Value);
                            }
                        }
                        break;
                    case GroupKey.Interest:
                        if (x.InterestId != y.InterestId)
                        {
                            if (x.InterestId.HasValue != y.InterestId.HasValue)
                            {
                                return x.InterestId.HasValue ? 1 : -1;
                            }
                            else
                            {
                                return x.InterestId.Value - y.InterestId.Value;
                            }
                        }
                        break;
                    case GroupKey.Country:
                        if (x.CountryId != y.CountryId)
                        {
                            if (x.CountryId.HasValue != y.CountryId.HasValue)
                            {
                                return x.CountryId.HasValue ? 1 : -1;
                            }
                            else
                            {
                                return x.CountryId.Value - y.CountryId.Value;
                            }
                        }
                        break;
                    case GroupKey.City:
                        if (x.CityId != y.CityId)
                        {
                            if (x.CityId.HasValue != y.CityId.HasValue)
                            {
                                return x.CityId.HasValue ? 1 : -1;
                            }
                            else
                            {
                                return x.CityId.Value - y.CityId.Value;
                            }
                        }
                        break;
                }
            }

            return 0;
        }

        public void Clear()
        {
            _keys = null;
            _index = null;
        }
    }
}
