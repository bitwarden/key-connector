using System;
using System.ComponentModel.DataAnnotations;

namespace Bit.CryptoAgent.Models
{
    public class UserKeyGetRequestModel
    {
        [Required]
        public string PublicKey { get; set; }
    }
}
