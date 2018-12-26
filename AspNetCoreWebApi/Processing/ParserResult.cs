using System;
using System.Collections.Generic;
using AspNetCoreWebApi.Domain;

namespace AspNetCoreWebApi.Processing
{
    public class ParserResult
    {
        public ParserResult(
            Account account,
            IEnumerable<Like> likes,
            IEnumerable<Interest> interests)
        {
            Account = account;
            Likes = likes;
            Interests = interests;
        }

        public Account Account { get; }
        public IEnumerable<Like> Likes { get; }
        public IEnumerable<Interest> Interests { get; }
    }
}