using System;
using System.Collections.Generic;
using System.IO;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Requests;
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

        private void Write(GroupEntry entry, Stream sw)
        {
            sw.StartObject();
            bool needComma = false;
            if (entry.Group.CityId > 0)
            {
                sw.Property("city", _storage.Cities.GetString(entry.Group.CityId));
                needComma = true;
            }

            if (entry.Group.CountryId > 0)
            {
                if (needComma)
                {
                    sw.Comma();
                }
                sw.Property("country", _storage.Countries.GetString(entry.Group.CountryId));
                needComma = true;
            }

            if (entry.Group.InterestId > 0)
            {
                if (needComma)
                {
                    sw.Comma();
                }
                sw.Property("interests", _storage.Interests.GetString(entry.Group.InterestId));
                needComma = true;
            }

            if (((byte)entry.Group.Keys & (byte)GroupKey.Status) > 0)
            {
                if (needComma)
                {
                    sw.Comma();
                }
                sw.Property("status", StatusHelper.ToStr(entry.Group.Status));
                needComma = true;
            }

            if (((byte)entry.Group.Keys & (byte)GroupKey.Sex) > 0)
            {
                if (needComma)
                {
                    sw.Comma();
                }
                sw.Property("sex", entry.Group.Sex ? "m" : "f");
                needComma = true;
            }

            if (needComma)
            {
                sw.Comma();
            }
            sw.Property("count", entry.Count);
            sw.EndObject();
        }

        public void Write(GroupResponse response, Stream sw)
        {
            sw.StartObject();
            sw.PropertyNameWithColon("groups");
            sw.StartArray();
            var limit = Math.Min(response.Entries.Count, response.Limit);
            for (int i = 0; i < limit; i++)
            {
                Write(response.Entries[i], sw);
                if (i < limit - 1)
                {
                    sw.Comma();
                }
            }
            sw.EndArray();
            sw.EndObject();
        }
    }
}