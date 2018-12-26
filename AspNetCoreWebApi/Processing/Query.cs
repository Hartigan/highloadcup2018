using System;
using System.Linq;
using AspNetCoreWebApi.Domain;

namespace AspNetCoreWebApi.Processing
{
    public static class QueryExtenstions
    {
        public static Query Create(this Query query, IQueryable<Account> accounts)
        {
            return new Query(accounts, query.Likes, query.Interests);
        }
    }

    public class Query
    {
        public Query(
            IQueryable<Account> accounts,
            IQueryable<Like> likes,
            IQueryable<Interest> interests
        )
        {
            Accounts = accounts;
            Likes = likes;
            Interests = interests;
        }

        public IQueryable<Account> Accounts { get; }
        public IQueryable<Like> Likes { get; }
        public IQueryable<Interest> Interests { get; }
    }
}