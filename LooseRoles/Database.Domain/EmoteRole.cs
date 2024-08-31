using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TLC.LooseRoles.Database.Domain
{
    public class EmoteRole
    {
        [Key]
        public required string EmoteName { get; set; }

        // [MaxLength(100)]
        public ulong RoleID { get; set; }
    }
}
