using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace DataAccess.Entities
{
    public class CollaboratorApplication
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(50, MinimumLength = 5)]
        public string FullName { get; set; }

        public string Avatar { get; set; }

        public string FrontOfIdCard { get; set; }

        public string BackOfIdCard { get; set; }

        public DateTime DateOfBirth { get; set; }

        public Gender Gender { get; set; }

        public DateTime CreatedDate { get; set; }

        public CollaboratorStatus Status { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        public User User { get; set; }
    }
}
