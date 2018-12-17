using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AspNetCoreWebApi.Domain.Dto
{
    [JsonObject(MemberSerialization.OptIn)]
    public class AccountDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("fname")]
        public string FirstName { get; set; }

        [JsonProperty("sname")]
        public string Surname { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("birth")]
        public int Birth { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("joined")]
        public int Joined { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("interests")]
        public List<string> Interests { get; set; }

        [JsonProperty("likes")]
        public List<LikeDto> Likes { get; set; }

        [JsonProperty("premium")]
        public PremiumDto Premium { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class PremiumDto
    {
        [JsonProperty("start")]
        public int Start { get; set; }

        [JsonProperty("finish")]
        public int Finish { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class LikeDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("ts")]
        public int Timestamp { get; set; }
    }
}