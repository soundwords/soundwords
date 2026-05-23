using Microsoft.AspNetCore.Mvc;
using SoundWords.Models;

namespace SoundWords.Controllers;

public class HomeController : SoundWordsController
{
    private readonly IRecordingRepository _recordingRepository;

    public HomeController(IRecordingRepository recordingRepository)
    {
        _recordingRepository = recordingRepository;
    }

    [HttpGet("/")]
    [HttpGet("/Home")]
    public IActionResult Index()
    {
        return View(new IndexResponse
                    {
                        Speakers = GetSpeakers(IncludeRestricted),
                        LatestAlbums = _recordingRepository.GetLatestAlbums(IncludeRestricted)
                    });
    }

    [HttpGet("/Home/About")]
    public IActionResult About()
    {
        return View(new AboutResponse { Speakers = GetSpeakers(IncludeRestricted) });
    }

    [HttpGet("/Home/Error")]
    public IActionResult Error()
    {
        return View("Error");
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
