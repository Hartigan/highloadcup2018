using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspNetCoreWebApi.Domain
{

    class Like
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int LikerId { get; set; }

        [Required]
        [Column(TypeName = "datetime")]
        public DateTimeOffset Timestamp { get; set; }
    }
}