using System;
using SharedKernel.Entities;

namespace SocialAPI.Entities
{
    public class Comment : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid ComicId { get; set; }
        public Guid ChapterId { get; set; }
        public Guid? ParentCommentId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int LikeCount { get; set; }

        public virtual Comment? ParentComment { get; set; }
        public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
    }
}
