using System;

namespace SocialAPI.DTOs
{
    public class FavoriteDto
    {
        public Guid Id { get; set; }
        public Guid ComicId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
