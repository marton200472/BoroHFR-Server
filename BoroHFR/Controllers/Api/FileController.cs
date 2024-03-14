using BoroHFR.Controllers.Helpers;
using BoroHFR.Data;
using BoroHFR.Models;
using BoroHFR.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoroHFR.Controllers.Api
{
    [Route("api/file")]
    [ApiController]
    [Authorize("User")]
    public class FileController : ControllerBase
    {
        private readonly FileStorageHandler _fileStore;
        private readonly BoroHfrDbContext _dbContext;

        public FileController(FileStorageHandler fileStore, BoroHfrDbContext dbContext)
        {
            _fileStore = fileStore;
            _dbContext = dbContext;
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetFile(FileId id)
        {
            var user = await _dbContext.Users.FindAsync(this.GetUserId());
            var file = _dbContext.Files.Where(x=>x.ClassId == user!.ClassId).FirstOrDefault(x => x.Id == id);
            if (file is null)
                return NotFound();

            var fileHandle = _fileStore.ReadFileFromStorage(file.Id.value.ToString());
            return File(fileHandle, file.ContentType, file.DownloadName);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteFile(FileId id)
        {
            var user = await _dbContext.Users.FirstAsync(x=>x.Id == this.GetUserId());
            var file = _dbContext.Files.Include(x=>x.Owner).Where(x => x.ClassId == user!.ClassId).FirstOrDefault(x => x.Id == id);
            if (file is null || (file.Owner != user && user.Role != UserRole.Admin))
                return NotFound();
            _fileStore.DeleteFileFromStorage(id.value);
            _dbContext.Files.Remove(file);
            await _dbContext.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("")]
        public async Task<IActionResult> UploadFiles(IFormCollection form)
        {
            var user = (await _dbContext.Users.FindAsync(this.GetUserId()))!;
            List<Models.File> files = new(form.Files.Count);
            foreach (var file in form.Files)
            {
                try
                {

                    Guid id = await _fileStore.WriteToFileStorageAsync(file.OpenReadStream());
                    Models.File a = new()
                    {
                        Id = new FileId(id),
                        ContentType = file.ContentType,
                        DownloadName = SafeFileName(file.FileName),
                        ClassId = user.ClassId,
                        Size = file.Length,
                        Owner = user
                    };
                    files.Add(a);
                }
                catch
                {
                    // ignored
                }
            }
            await _dbContext.Files.AddRangeAsync(files);
            await _dbContext.SaveChangesAsync();
            return Ok(files.Select(x=>x.Id.value));
        }

        private string SafeFileName(string name)
        {
            var safe = new string(name.Where(x=>char.IsLetterOrDigit(x) || x == '.' || x == '_').ToArray());
            if (safe.Length < 4)
            {
                safe = DateTime.Today.ToShortDateString() + safe;
            }
            return safe;
        }



    }
}
