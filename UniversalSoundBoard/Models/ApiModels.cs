using System.Collections.Generic;

namespace UniversalSoundboard.Models
{
    public class RetrieveUserResponse
    {
        public UserResponse RetrieveUser { get; set; }
    }

    public class RetrieveSoundResponse
    {
        public SoundResponse RetrieveSound { get; set; }
    }

    public class ListSoundsResponse
    {
        public ListResponse<SoundResponse> ListSounds { get; set; }
    }

    public class CreateSoundResponse
    {
        public SoundResponse CreateSound { get; set; }
    }

    public class UpdateSoundResponse
    {
        public SoundResponse UpdateSound { get; set; }
    }

    public class DeleteSoundResponse
    {
        public SoundResponse DeleteSound { get; set; }
    }

    public class CreateSoundPromotionResponse
    {
        public SoundPromotionResponse CreateSoundPromotion { get; set; }
    }

    public class ListTagsResponse
    {
        public ListResponse<TagResponse> ListTags { get; set; }
    }

    public class UserResponse
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string ProfileImage { get; set; }
    }

    public class SoundResponse
    {
        public string Uuid { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string AudioFileUrl { get; set; }
        public string Type { get; set; }
        public string Source { get; set; }
        public List<string> Tags { get; set; }
        public UserResponse User { get; set; }
    }

    public class SoundPromotionResponse
    {
        public string SessionUrl { get; set; }
    }

    public class TagResponse
    {
        public string Uuid { get; set; }
        public string Name { get; set; }
    }

    public class ListResponse<T>
    {
        public int Total { get; set; }
        public List<T> Items { get; set; }
    }
}
