using System.Text.Json;
using LearningAPI.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DataDownloaderController : ControllerBase
{
    private readonly ILogger<DataDownloaderController> _logger;
    private readonly IServiceBusHelper _serviceBusHelper;

    public DataDownloaderController(ILogger<DataDownloaderController> logger, IServiceBusHelper serviceBusHelper)
    {
        _logger = logger;
        _serviceBusHelper = serviceBusHelper;
    }

    [HttpPost("DownloadData")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadData([FromBody] DataDownloadRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.DownloadUrl))
        {
            _logger.LogWarning("Received empty url");
            return BadRequest("Download url can't be empty");
        }
        try
        {
            _logger.LogInformation($"Received URL: {request.DownloadUrl}");
            var message = JsonSerializer.Serialize(request);
            await _serviceBusHelper.SendMessageAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception occurred: {ex}");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
        return Ok($"Request submitted for url: {request.DownloadUrl}");
    }

    [HttpGet("healthCheck")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult DownloadData()
    {
        return Ok("Running fine");
    }
}