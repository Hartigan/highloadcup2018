using AspNetCoreWebApi.Processing.Requests;
using System;

namespace AspNetCoreWebApi.Domain
{
    public struct Group
    {
        public Group(
            GroupKey keys,
            bool sex = false,
            Status status = Status.Complicated,
            short interestId = 0,
            short countryId = 0,
            short cityId = 0)
        {
            Keys = keys;
            Sex = sex;
            Status = status;
            InterestId = interestId;
            CountryId = countryId;
            CityId = cityId;
        }

        public GroupKey Keys;

        public bool Sex;

        public Status Status;

        public short InterestId;

        public short CountryId;

        public short CityId;

        public bool Equals(Group y)
        {
            return CityId == y.CityId &&
                CountryId == y.CountryId &&
                InterestId == y.InterestId &&
                Sex == y.Sex &&
                Status == y.Status;
        }
    }
}