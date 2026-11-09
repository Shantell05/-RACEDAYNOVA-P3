using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayNova.Api.Data;
using RaceDayNova.Api.Models;
using RaceDayNova.Api.Security;

namespace RaceDayNova.Api.Controllers;

/// <summary>Image uploads are handled by the API and stored in Azure Blob Storage. The MVC frontend never connects to Azure directly.</summary>
[Route("api")]
public class UploadsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IConfiguration _configuration;
    public UploadsController(RaceDayDbContext db, IConfiguration configuration) { _db = db; _configuration = configuration; }

    [HttpPost("events/{id:int}/banner")]
    [SessionAuthorize(Roles.Organiser)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file, CancellationToken ct)
    {
        if (!ValidImage(file)) return Fail(StatusCodes.Status400BadRequest, "Invalid image", "Upload a PNG, JPEG or WebP image up to 5 MB.");
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id, ct);
        if (ev is null) return Fail(StatusCodes.Status404NotFound, "Event not found");
        if (ev.OrganiserId != CurrentUserId) return Fail(StatusCodes.Status403Forbidden, "Forbidden", "You can only upload banners for your own events.");
        var url = await SaveToBlob(file, $"events/{id}/banner-{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}", ct);
        if (url is null) return Fail(StatusCodes.Status503ServiceUnavailable, "Image storage unavailable", "Configure AzureBlob:ConnectionString to enable uploads.");
        ev.BannerImageUrl = url; await _db.SaveChangesAsync(ct); return Ok(new { url });
    }

    [HttpPost("users/me/profile-picture")]
    [SessionAuthorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file, CancellationToken ct)
    {
        if (!ValidImage(file)) return Fail(StatusCodes.Status400BadRequest, "Invalid image", "Upload a PNG, JPEG or WebP image up to 5 MB.");
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == CurrentUserId, ct);
        if (user is null) return Fail(StatusCodes.Status404NotFound, "User not found");
        var url = await SaveToBlob(file, $"profiles/{user.UserId}/{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}", ct);
        if (url is null) return Fail(StatusCodes.Status503ServiceUnavailable, "Image storage unavailable", "Configure AzureBlob:ConnectionString to enable uploads.");
        user.ProfileImageUrl = url; await _db.SaveChangesAsync(ct); return Ok(new { url });
    }

    private static bool ValidImage(IFormFile? file) => file is { Length: > 0 and <= 5_242_880 } && new[] { "image/jpeg", "image/png", "image/webp" }.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase);
    private async Task<string?> SaveToBlob(IFormFile file, string name, CancellationToken ct)
    {
        var connection = _configuration["AzureBlob:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connection)) return null;
        try
        {
            var containerName = _configuration["AzureBlob:Container"] ?? "raceday-media";
            var service = new BlobServiceClient(connection); var container = service.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);
            var blob = container.GetBlobClient(name);
            await using var stream = file.OpenReadStream();
            await blob.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType } }, ct);
            return blob.Uri.ToString();
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            // Do not expose storage credentials or provider internals to the client.
            return null;
        }
    }
}
