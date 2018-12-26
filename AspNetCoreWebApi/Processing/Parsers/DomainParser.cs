using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Storage;

namespace AspNetCoreWebApi.Processing.Parsers
{
    public class DomainParser
    {
        private readonly MainStorage _storage;

        public DomainParser(
            MainStorage mainStorage
        )
        {
            _storage = mainStorage;
        }

        public Email ParseEmail(string str)
        {
            var splited = str.Split('@');
            return new Email(splited[0], _storage.Domains.Get(splited[1]));
        }

        public Phone ParsePhone(string str)
        {
            var splited = str.Split('(', ')');
            return new Phone(
                    short.Parse(splited[0]),
                    short.Parse(splited[1]),
                    int.Parse(splited[2])
                );
        }
    }
}