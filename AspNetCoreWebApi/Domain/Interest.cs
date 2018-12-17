using System;
using System.ComponentModel.DataAnnotations;

namespace AspNetCoreWebApi.Domain
{

    class Interest
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int StringId { get; set; }
    }
}