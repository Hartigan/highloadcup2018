using System;
using System.Text.RegularExpressions;

namespace AspNetCoreWebApi.Processing.Parsers
{
    public class Validator
    {
        private readonly Regex _emailEx = new Regex(".+\\@.+\\..+", RegexOptions.Compiled);
        private readonly Regex _phoneEx = new Regex("\\d+\\(\\d+\\)\\d+", RegexOptions.Compiled);

        public Validator()
        {
            
        }

        public bool Email(string email)
        {
            return email.Length <= 50 && _emailEx.IsMatch(email);
        }

        public bool Phone(string phone)
        {
            return phone.Length <= 16 && _phoneEx.IsMatch(phone);
        }

        public bool Sex(string sex)
        {
            return sex == "m" || sex == "f"; 
        }

        public bool Birth(int birth)
        {
            return birth >= -631152000 && birth <= 1104537600;
        }

        public bool Country(string country)
        {
            return country.Length <= 50;
        }

        public bool Surname(string surname)
        {
            return surname.Length <= 50;
        }

        public bool FirstName(string firstName)
        {
            return firstName.Length <= 50;
        }

        public bool City(string city)
        {
            return city.Length <= 50;
        }

        public bool Joined(int joined)
        {
            return joined >= 1293840000 && joined <= 1514764800;
        }

        public bool Status(string status)
        {
            switch (status)
            {
                case "свободны":
                case "заняты":
                case "всё сложно":
                    return true;
                default:
                    return false;
            }
        }

        public bool Interest(string interest)
        {
            return interest.Length <= 100;
        }

        public bool Premium(int start, int finish)
        {
            return finish > start && start >= 1514764800;
        }
    }
}