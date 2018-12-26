using System;
using System.Collections.Generic;
using System.IO;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Storage;

namespace AspNetCoreWebApi.Processing
{
    public class AccountPrinter
    {
        private readonly IReadOnlyList<Field> _fields;
        private readonly CountryStorage _countryStorage;
        private readonly CityStorage _cityStorage;

        public AccountPrinter(
            IReadOnlyList<Field> fields,
            CountryStorage countryStorage,
            CityStorage cityStorage)
        {
            _fields = fields;
            _countryStorage = countryStorage;
            _cityStorage = cityStorage;
        }

        private void Write(Account account, StreamWriter sw)
        {
            using (new JsObject(sw))
            {
                sw.Property("id", account.Id);
                sw.Comma();
                sw.Property("email", account.Email);

                foreach (var field in _fields)
                {
                    sw.Comma();
                    switch (field)
                    {
                        case Field.Sex:
                            sw.Property("sex", account.Sex ? "m" : "f");
                            break;
                        case Field.Status:
                            sw.Property("status", account.Status.ToStr());
                            break;
                        case Field.FName:
                            if (account.FirstName != null)
                            {
                                sw.Property("fname", account.FirstName);
                            }
                            break;
                        case Field.SName:
                            if (account.LastName != null)
                            {
                                sw.Property("sname", account.LastName);
                            }
                            break;
                        case Field.Phone:
                            if (account.Phone != null)
                            {
                                sw.Property("phone", account.Phone);
                            }
                            break;
                        case Field.Country:
                            if (account.CountryId.HasValue)
                            {
                                sw.Property("country", _countryStorage.GetString(account.CountryId.Value));
                            }
                            break;
                        case Field.City:
                            if (account.CityId.HasValue)
                            {
                                sw.Property("city", _cityStorage.GetString(account.CityId.Value));
                            }
                            break;
                        case Field.Birth:
                            sw.Property("birth", account.Birth.ToUnixTimeSeconds());
                            break;
                        case Field.Premium:
                            if (account.PremiumStart == null)
                            {
                                break;
                            }
                            sw.PropertyNameWithColon("premium");
                            using (new JsObject(sw))
                            {
                                sw.Property("start", account.PremiumStart.Value.ToUnixTimeSeconds());
                                sw.Comma();
                                sw.Property("finish", account.PremiumEnd.Value.ToUnixTimeSeconds());
                            }
                            break;
                    }
                }
            }
        }

        public void WriteFilterResponse(IReadOnlyList<Account> accounts, StreamWriter sw)
        {
            using (new JsObject(sw))
            {
                sw.PropertyNameWithColon("accounts");
                using (new JsArray(sw))
                {
                    for (int i = 0; i < accounts.Count; i++)
                    {
                        Write(accounts[i], sw);
                        if (i < accounts.Count - 1)
                        {
                            sw.Comma();
                        }
                    }
                }
            }
        }
    }
}