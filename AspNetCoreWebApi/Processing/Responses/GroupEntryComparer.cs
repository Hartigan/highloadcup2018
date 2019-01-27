using System;
using System.Collections.Generic;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Pooling;
using AspNetCoreWebApi.Processing.Requests;
using AspNetCoreWebApi.Storage;

namespace AspNetCoreWebApi.Processing.Responses
{
    public class GroupEntryComparer : IComparer<GroupEntry>, IClearable
    {
        private MainStorage _storage;
        private List<GroupKey> _keys;
        private bool _order;

        public void Init(
            MainStorage mainStorage,
            List<GroupKey> keys,
            bool order)
        {
            _storage = mainStorage;
            _keys = keys;
            _order = order;
        }

        public void Clear()
        {
            _storage = null;
            _keys = null;
            _order = false;
        }

        public int Compare(GroupEntry x, GroupEntry y)
        {
            if (_order)
            {
                return CompareImpl(x, y);
            }
            else
            {
                return CompareImpl(y, x);
            }
        }

        private int CompareImpl(GroupEntry x, GroupEntry y)
        {
            if (x.Count != y.Count)
            {
                return x.Count - y.Count;
            }

            for (int i = 0; i < _keys.Count; i++)
            {
                switch (_keys[i])
                {
                    case GroupKey.City:
                        if (x.Group.CityId != y.Group.CityId)
                        {
                            if (y.Group.CityId == 0)
                            {
                                return 1;
                            }
                            if (x.Group.CityId == 0)
                            {
                                return -1;
                            }

                            return string.Compare(
                                _storage.Cities.GetString(x.Group.CityId),
                                _storage.Cities.GetString(y.Group.CityId),
                                StringComparison.Ordinal
                            );
                        }
                        break;
                    case GroupKey.Country:
                        if (x.Group.CountryId != y.Group.CountryId)
                        {
                            if (y.Group.CountryId == 0)
                            {
                                return 1;
                            }
                            if (x.Group.CountryId == 0)
                            {
                                return -1;
                            }
                            return string.Compare(
                                _storage.Countries.GetString(x.Group.CountryId),
                                _storage.Countries.GetString(y.Group.CountryId),
                                StringComparison.Ordinal
                            );
                        }
                        break;
                    case GroupKey.Interest:
                        if (x.Group.InterestId != y.Group.InterestId)
                        {
                            if (y.Group.InterestId == 0)
                            {
                                return 1;
                            }
                            if (x.Group.InterestId == 0)
                            {
                                return -1;
                            }
                            return string.Compare(
                                _storage.Interests.GetString(x.Group.InterestId),
                                _storage.Interests.GetString(y.Group.InterestId),
                                StringComparison.Ordinal
                            );
                        }
                        break;
                    case GroupKey.Sex:
                        if (x.Group.Sex != y.Group.Sex)
                        {
                            return x.Group.Sex ? 1 : -1;
                        }
                        break;
                    case GroupKey.Status:
                        if (x.Group.Status != y.Group.Status)
                        {
                            return StatusHelper.CompareString(x.Group.Status, y.Group.Status);
                        }
                        break;
                }
            }

            return 0;
        }
    }
}