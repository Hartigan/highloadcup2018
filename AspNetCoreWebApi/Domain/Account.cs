using System;
using System.Collections.Generic;
using System.Linq;

namespace AspNetCoreWebApi.Domain
{
    class Account
    {
        // unique
        public int Id { get; set; }

        // unique, 100
        public String Email { get; set; }

        // optional, 50
        public String FirstName { get; set; }
        public String LastName { get; set; }

        // optional, unique, 16
        public String Phone { get; set; }

        public bool Sex { get; set; }

        public long Birth { get; set; }

        public Country Country { get; set; }
        public City City { get; set; }

        public long Joined { get; set; }

        public Status Status { get; set; }

        public IList<Interest> Interests { get; } = new List<Interest>();

        public long PremiumStart { get; set; }
        public long PremiumEnd { get; set; }
    }
}