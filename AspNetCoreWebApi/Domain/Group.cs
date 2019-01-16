using System;

namespace AspNetCoreWebApi.Domain
{
    public struct Group
    {
        public Group(
            bool? sex = null,
            Status? status = null,
            short? interestId = null,
            short? countryId = null,
            short? cityId = null)
        {
            Sex = sex;
            Status = status;
            InterestId = interestId;
            CountryId = countryId;
            CityId = cityId;
        }

        public bool? Sex;

        public Status? Status;

        public short? InterestId;

        public short? CountryId;

        public short? CityId;
    }
}