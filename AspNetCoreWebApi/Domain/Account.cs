using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace AspNetCoreWebApi.Domain
{
    class Account
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public String Email { get; set; }

        [MaxLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public String FirstName { get; set; }
        [MaxLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public String LastName { get; set; }

        [MaxLength(16)]
        [Column(TypeName = "nvarchar(16)")]
        public String Phone { get; set; }

        public bool Sex { get; set; }

        public int? CountryId { get; set; }
        public int? CityId { get; set; }

        public Status Status { get; set; }

        public ICollection<Interest> Interests { get; set; } = new List<Interest>();
        public ICollection<Like> Likes {get;set;} = new List<Like>();

        [Column(TypeName = "datetime")]
        public DateTimeOffset? PremiumStart { get; set; }
        [Column(TypeName = "datetime")]
        public DateTimeOffset? PremiumEnd { get; set; }
        [Column(TypeName = "datetime")]
        public DateTimeOffset Joined { get; set; }
        [Column(TypeName = "datetime")]
        public DateTimeOffset Birth { get; set; }
    }
}