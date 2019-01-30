using System;
using System.Collections.Generic;
using System.Linq;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Pooling;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class BirthContext : IBatchLoader<UnixTime>, ICompresable
    {
        private UnixTime[] _id2time = new UnixTime[DataConfig.MaxId];
        private Dictionary<int, CountSet> _years = new Dictionary<int, CountSet>(100);
        private Dictionary<int, DelaySortedList<int>> _byYear = new Dictionary<int, DelaySortedList<int>>(100);

        public BirthContext()
        {
        }

        public void AddOrUpdate(int id, UnixTime time)
        {
            var oldYear = _id2time[id].Year;
            _id2time[id] = time;

            if (_years.ContainsKey(oldYear))
            {
                _years[oldYear].Remove(id);
                _byYear[oldYear].DelayRemove(id);
            }

            var newYear = time.Year;
            if (!_years.ContainsKey(newYear))
            {
                _years[newYear] = new CountSet();
                var list = _byYear[newYear] = DelaySortedList<int>.CreateDefault();

                _years[newYear].Add(id);
                list.Load(id);
            }
            else
            {
                _years[newYear].Add(id);
                _byYear[newYear].DelayAdd(id);
            }
        }

        public UnixTime Get(int id) => _id2time[id];

        public IIterator Filter(FilterRequest.BirthRequest birth, IdStorage idStorage)
        {
            if (birth.Year.HasValue)
            {
                var list = _byYear.GetValueOrDefault(birth.Year.Value);
                
                if (list == null)
                {
                    return ListHelper.EmptyInt;
                }

                if (!birth.Gt.HasValue && birth.Lt.HasValue)
                {
                    return list.GetIterator();
                }

                IEnumerable<int> result = list;

                if (birth.Gt.HasValue)
                {
                    if (birth.Gt.Value.Year < birth.Year.Value)
                    {
                        return ListHelper.EmptyInt;
                    }

                    result = result.Where(x => _id2time[x] > birth.Gt.Value);
                }

                if (birth.Lt.HasValue)
                {
                    if (birth.Lt.Value.Year > birth.Year.Value)
                    {
                        return ListHelper.EmptyInt;
                    }

                    result = result.Where(x => _id2time[x] < birth.Lt.Value);
                }

                return result.GetIterator();
            }
            else
            {
                List<IIterator> enumerators = new List<IIterator>();

                if (birth.Lt.HasValue && birth.Gt.HasValue)
                {
                    if (birth.Lt <= birth.Gt)
                    {
                        return ListHelper.EmptyInt;
                    }

                    foreach (var pair in _byYear)
                    {
                        if (pair.Key > birth.Gt.Value.Year && pair.Key < birth.Lt.Value.Year)
                        {
                            enumerators.Add(pair.Value.GetIterator());
                        }
                        else if (pair.Key == birth.Gt.Value.Year && pair.Key == birth.Lt.Value.Year)
                        {
                            enumerators.Add(
                                pair.Value
                                    .Where(x => _id2time[x] > birth.Gt.Value && _id2time[x] < birth.Lt.Value)
                                    .GetIterator()
                            );
                        }
                        else if (pair.Key == birth.Gt.Value.Year)
                        {
                            enumerators.Add(
                                pair.Value
                                    .Where(x => _id2time[x] > birth.Gt.Value)
                                    .GetIterator()
                            );
                        }
                        else if (pair.Key == birth.Lt.Value.Year)
                        {
                            enumerators.Add(
                                pair.Value
                                    .Where(x => _id2time[x] < birth.Lt.Value)
                                    .GetIterator()
                            );
                        }
                    }
                }
                else if (birth.Lt.HasValue)
                {
                    foreach (var pair in _byYear)
                    {
                        if (pair.Key < birth.Lt.Value.Year)
                        {
                            enumerators.Add(pair.Value.GetIterator());
                        }
                        else if (pair.Key == birth.Lt.Value.Year)
                        {
                            enumerators.Add(
                                pair.Value
                                    .Where(x => _id2time[x] < birth.Lt.Value)
                                    .GetIterator()
                            );
                        }
                    }
                }
                else
                {
                    foreach (var pair in _byYear)
                    {
                        if (pair.Key > birth.Gt.Value.Year)
                        {
                            enumerators.Add(pair.Value.GetIterator());
                        }
                        else if (pair.Key == birth.Gt.Value.Year)
                        {
                            enumerators.Add(
                                pair.Value
                                    .Where(x => _id2time[x] > birth.Gt.Value)
                                    .GetIterator()
                            );
                        }
                    }
                }

                return enumerators.MergeSort();
            }
        }

        public IFilterSet Filter(GroupRequest.BirthRequest birth)
        {
            if (_years.ContainsKey(birth.Year))
            {
                return _years[birth.Year];
            }
            else
            {
                return FilterSet.Empty;
            }
        }

        public void LoadBatch(int id, UnixTime item)
        {
            var newYear = item.Year;
            if (!_years.ContainsKey(newYear))
            {
                _years[newYear] = new CountSet();
                _byYear[newYear] = DelaySortedList<int>.CreateDefault();
            }

            _years[newYear].Add(id);
            _byYear[newYear].Load(id);
            _id2time[id] = item;
        }

        public void Compress()
        {
            foreach(var list in _byYear.Values)
            {
                list.Flush();
            }
        }

        public void LoadEnded()
        {
            foreach (var list in _byYear.Values)
            {
                list.LoadEnded();
            }
        }
    }
}