using System;
using System.Collections.Generic;
using System.IO;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Responses;
using AspNetCoreWebApi.Storage;
using AspNetCoreWebApi.Storage.Contexts;

namespace AspNetCoreWebApi.Processing.Printers
{
    public class GroupPrinter
    {
        private readonly MainStorage _storage;
        private readonly MainContext _context;

        public GroupPrinter(MainStorage storage, MainContext context)
        {
            _storage = storage;
            _context = context;
        }

        private void Write(GroupEntry entry, StreamWriter sw)
        {
            using (new JsObject(sw))
            {
                bool needComma = false;
                if (entry.Group.CityId.HasValue)
                {
                    sw.Property("city", _storage.Cities.GetString(entry.Group.CityId.Value));
                    needComma = true;
                }

                if (entry.Group.CountryId.HasValue)
                {
                    if (needComma)
                    {
                        sw.Comma();
                    }
                    sw.Property("country", _storage.Countries.GetString(entry.Group.CountryId.Value));
                    needComma = true;
                }

                if (entry.Group.InterestId.HasValue)
                {
                    if (needComma)
                    {
                        sw.Comma();
                    }
                    sw.Property("interests", _storage.Interests.GetString(entry.Group.InterestId.Value));
                    needComma = true;
                }

                if (entry.Group.Status.HasValue)
                {
                    if (needComma)
                    {
                        sw.Comma();
                    }
                    sw.Property("status", StatusHelper.ToStr(entry.Group.Status.Value));
                    needComma = true;
                }

                if (entry.Group.Sex.HasValue)
                {
                    if (needComma)
                    {
                        sw.Comma();
                    }
                    sw.Property("sex", entry.Group.Sex.Value ? "m" : "f");
                    needComma = true;
                }

                if (needComma)
                {
                    sw.Comma();
                }
                sw.Property("count", entry.Count);
            }
        }

        public void Write(GroupResponse response, StreamWriter sw)
        {
            using (new JsObject(sw))
            {
                sw.PropertyNameWithColon("groups");
                using (new JsArray(sw))
                {
                    for (int i = 0; i < response.Entries.Count; i++)
                    {
                        Write(response.Entries[i], sw);
                        if (i < response.Entries.Count - 1)
                        {
                            sw.Comma();
                        }
                    }
                }
            }
        }
    }
}