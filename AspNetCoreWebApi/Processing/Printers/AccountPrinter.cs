using System;
using System.Collections.Generic;
using System.IO;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Responses;
using AspNetCoreWebApi.Storage;
using AspNetCoreWebApi.Storage.Contexts;

namespace AspNetCoreWebApi.Processing.Printers
{
    public class AccountPrinter
    {
        private readonly MainStorage _storage;
        private readonly MainContext _context;

        public AccountPrinter(
            MainStorage mainStorage,
            MainContext mainContext)
        {
            _storage = mainStorage;
            _context = mainContext;
        }

        private void Write(int id, StreamWriter sw, IEnumerable<Field> fields)
        {
            using (new JsObject(sw))
            {
                sw.Property("id", id);
                sw.Comma();

                Email email = _context.Emails.Get(id);
                sw.PropertyNameWithColon("email");
                sw.Write('\"');
                sw.Write(email.Prefix);
                sw.Write('@');
                sw.Write(_storage.Domains.GetString(email.DomainId));
                sw.Write('\"');

                foreach (var field in fields)
                {
                    switch (field)
                    {
                        case Field.Sex:
                            sw.Comma();
                            sw.Property("sex", _context.Sex.Get(id) ? "m" : "f");
                            break;
                        case Field.Status:
                            sw.Comma();
                            sw.Property("status", _context.Statuses.Get(id).ToStr());
                            break;
                        case Field.FName:
                            string fname;
                            if (_context.FirstNames.TryGet(id, out fname))
                            {
                                sw.Comma();
                                sw.Property("fname", fname);
                            }
                            break;
                        case Field.SName:
                            string sname;
                            if (_context.LastNames.TryGet(id, out sname))
                            {
                                sw.Comma();
                                sw.Property("sname", sname);
                            }
                            break;
                        case Field.Phone:
                            Phone phone;
                            if (_context.Phones.TryGet(id, out phone))
                            {
                                sw.Comma();
                                sw.PropertyNameWithColon("phone");
                                sw.Write('\"');
                                sw.Write(phone.Prefix);
                                sw.Write('(');
                                sw.Write(phone.Code);
                                sw.Write(')');
                                sw.Write(phone.Suffix.ToString("D7"));
                                sw.Write('\"');
                            }
                            break;
                        case Field.Country:
                            short countryId;
                            if (_context.Countries.TryGet(id, out countryId))
                            {
                                sw.Comma();
                                sw.Property("country", _storage.Countries.GetString(countryId));
                            }
                            break;
                        case Field.City:
                            short cityId;
                            if (_context.Cities.TryGet(id, out cityId))
                            {
                                sw.Comma();
                                sw.Property("city", _storage.Cities.GetString(cityId));
                            }
                            break;
                        case Field.Birth:
                            sw.Comma();
                            sw.Property("birth", _context.Birth.Get(id).Seconds);
                            break;
                        case Field.Premium:
                            Premium premium;
                            if (_context.Premiums.TryGet(id, out premium))
                            {
                                sw.Comma();
                                sw.PropertyNameWithColon("premium");
                                using (new JsObject(sw))
                                {
                                    sw.Property("start", premium.Start.Seconds);
                                    sw.Comma();
                                    sw.Property("finish", premium.Finish.Seconds);
                                }
                            }
                            break;
                    }
                }
            }
        }

        public void Write(FilterResponse response, StreamWriter sw, IEnumerable<Field> fields)
        {
            using (new JsObject(sw))
            {
                sw.PropertyNameWithColon("accounts");
                using (new JsArray(sw))
                {
                    var accounts = response.Ids;
                    var limit = Math.Min(accounts.Count, response.Limit);
                    for (int i = 0; i < limit ; i++)
                    {
                        Write(accounts[i], sw, fields);
                        if (i < limit - 1)
                        {
                            sw.Comma();
                        }
                    }
                }
            }
        }
    }
}