using System.Collections.Generic;
using System.Linq;
using SoundWords.Models;
using ServiceStack;

namespace SoundWords.Services
{
    [Route("/Home")]
    public class Index : IReturn<IndexResponse> {}

    public class IndexResponse
    {
        public List<AlbumWithSpeakers> LatestAlbums { get; set; }
        public List<SpeakerInfo> Speakers { get; set; }
    }

    [Route("/Home/About")]
    public class About : IReturn<AboutResponse> { }

    public class AboutResponse
    {
        public List<SpeakerInfo> Speakers { get; set; }
    }
    
    public class HomeService : ServiceBase   
    {
        private readonly IRecordingRepository _recordingRepository;

        public HomeService(IRecordingRepository recordingRepository)
        {
            _recordingRepository = recordingRepository;          
        }

        public IndexResponse Get(Index index)
        {
            var includeRestricted = UserSession.IsAuthenticated;

            var indexResponse = new IndexResponse
            {
                Speakers = GetSpeakers(includeRestricted),
                LatestAlbums = _recordingRepository.GetLatestAlbums(includeRestricted)
            };
            return indexResponse;
        }

        public AboutResponse Get(About about)
        {
            var includeRestricted = UserSession.IsAuthenticated;

            return new AboutResponse { Speakers = GetSpeakers(includeRestricted)};
        }

        private List<SpeakerInfo> GetSpeakers(bool includeRestricted)
        {
            List<SpeakerInfo> speakers = _recordingRepository.GetSpeakers(includeRestricted)
                .ConvertAll(s => s.ToSpeakerInfo())
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToList();
            return speakers;
        }
    }
}
