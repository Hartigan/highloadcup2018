using System;
using System.Collections.Generic;
using System.IO;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Responses;
using AspNetCoreWebApi.Storage;
using AspNetCoreWebApi.Storage.Contexts;

namespace AspNetCoreWebApi.Processing.Printers
{
    public class RecommendPrinter
    {
        private readonly MainStorage _storage;
        private readonly MainContext _context;

        public RecommendPrinter(
            MainStorage mainStorage,
            MainContext mainContext)
        {
            _storage = mainStorage;
            _context = mainContext;
        }

        private void Write(int id, Stream sw)
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

                sw.Comma();
                sw.Property("birth", _context.Birth.Get(id).Seconds);

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
            }
        }

        public void Write(RecommendResponse response, Stream sw)
        {
            using (new JsObject(sw))
            {
                sw.PropertyNameWithColon("accounts");
                using (new JsArray(sw))
                {
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
                }
            }
        }
    }
}