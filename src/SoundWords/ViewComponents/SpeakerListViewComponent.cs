using Microsoft.AspNetCore.Mvc;
using SoundWords.Models;

namespace SoundWords.ViewComponents;

public class SpeakerListViewComponent : ViewComponentBase
{
    private readonly IRecordingRepository _recordingRepository;

    public SpeakerListViewComponent(IRecordingRepository recordingRepository)
    {
        _recordingRepository = recordingRepository;
    }

    public IViewComponentResult Invoke()
    {
        bool includeRestricted = IsAuthenticated;
        return View(GetSpeakers(includeRestricted));
    }

    private List<SpeakerInfo> GetSpeakers(bool includeRestricted)
    {
        return _recordingRepository.GetSpeakers(includeRestricted)
                                   .ConvertAll(s => s.ToSpeakerInfo())
                                   .OrderBy(s => s.LastName)
                                   .ThenBy(s => s.FirstName)
                                   .ToList();
    }
}
