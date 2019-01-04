using System;
using System.Collections.Generic;
using System.IO;
using AspNetCoreWebApi.Domain;
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

        private void Write(int id, StreamWriter sw)
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
            }
        }

        public void Write(IReadOnlyList<int> accounts, StreamWriter sw)
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