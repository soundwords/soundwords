using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SoundWords.Models;
using SoundWords.Services;

namespace SoundWords.ViewComponents
{
    public class SpeakerListViewComponent : ViewComponentBase
    {
        private readonly IRecordingRepository _recordingRepository;

        public SpeakerListViewComponent(IRecordingRepository recordingRepository)
        {
            _recordingRepository = recordingRepository;
        }

        public IViewComponentResult Invoke()
        {
            var includeRestricted = IsAuthenticated;

            return View(GetSpeakers(includeRestricted));
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
