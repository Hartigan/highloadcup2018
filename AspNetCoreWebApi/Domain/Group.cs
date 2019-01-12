using System;

namespace AspNetCoreWebApi.Domain
{
    public struct Group
    {
        public Group(
            bool? sex = null,
            Status? status = null,
            int? interestId = null,
            int? countryId = null,
            int? cityId = null)
        {
            Sex = sex;
            Status = status;
            InterestId = interestId;
            CountryId = countryId;
            CityId = cityId;
        }

        public bool? Sex;

        public Status? Status;

        public int? InterestId;

        public int? CountryId;

        public int? CityId;

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            Group o = (Group)obj;

            return
                Sex == o.Sex &&
                Status == o.Status &&
                InterestId == o.InterestId &&
                CountryId == o.CountryId &&
                CityId == o.CityId;

        }

        public override int GetHashCode()
        {
            return
                Sex.GetHashCode() ^
                Status.GetHashCode() ^
                InterestId.GetHashCode() ^
                CountryId.GetHashCode() ^
                CityId.GetHashCode();
        }
    }
}