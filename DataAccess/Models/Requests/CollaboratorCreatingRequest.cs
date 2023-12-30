using DataAccess.EntityEnums;
using Microsoft.AspNetCore.Http;

namespace DataAccess.Models.Requests
{
    public class CollaboratorCreatingRequest
    {
        public string FullName { get; set; }

        public IFormFile Avatar { get; set; }
        public DateTime DateOfBirth { get; set; }

        public Gender Gender { get; set; }

        public IFormFile FrontOfIdCard { get; set; }

        public IFormFile BackOfIdCard { get; set; }

        public string? Note { get; set; }
    }
}
