using System;
using System.Collections.Generic;
using System.IO;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Responses;
using AspNetCoreWebApi.Storage;
using AspNetCoreWebApi.Storage.Contexts;

namespace AspNetCoreWebApi.Processing.Printers
{
    public class SuggestPrinter
    {
        private readonly MainStorage _storage;
        private readonly MainContext _context;

        public SuggestPrinter(
            MainStorage mainStorage,
            MainContext mainContext)
        {
            _storage = mainStorage;
            _context = mainContext;
        }

        private void Write(int id, Stream sw)
        {
            sw.StartObject();
            sw.Property("id", id);
            sw.Comma();

            Email email = _context.Emails.Get(id);
            sw.PropertyNameWithColon("email");
            sw.WriteDoubleQuote();
            sw.Write(email.Prefix);
            sw.WriteA();
            sw.Write(_storage.Domains.GetString(email.DomainId));
            sw.WriteDoubleQuote();


            sw.Comma();
            sw.Property("status", _context.Statuses.Get(id).ToStr());

            string fname;
            if (_context.FirstNames.TryGet(id, out fname))
            {
                sw.Comma();
                sw.Property("fname", fname);
            }

            string sname;
            if (_context.LastNames.TryGet(id, out sname))
            {
                sw.Comma();
                sw.Property("sname", sname);
            }
            sw.EndObject();
        }

        public void Write(SuggestResponse response, Stream sw)
        {
            sw.StartObject();
            sw.PropertyNameWithColon("accounts");
            sw.StartArray();
            var accounts = response.Ids;
            var limit = Math.Min(accounts.Count, response.Limit);
            for (int i = 0; i < limit; i++)
            {
                Write(accounts[i], sw);
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