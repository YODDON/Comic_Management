using System;
using System.Collections.Generic;

namespace SocialAPI.DTOs
{
    public class CommentDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid ComicId { get; set; }
        public string Content { get; set; } = string.Empty;
        public Guid? ParentCommentId { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public List<CommentDto> Replies { get; set; } = new();
    }
}
