namespace BoroHFR.Services
{
    public class FileStorageHandler
    {
        private readonly string _storageRoot;
        public FileStorageHandler(IConfiguration configuration)
        {
            _storageRoot = configuration["FileStorageDir"]!;
        }

        public static bool IsDirectoryWritable(string dirPath)
        {
            try
            {
                using (File.Create(
                           Path.Combine(
                               dirPath,
                               Path.GetRandomFileName()
                           ),
                           1,
                           FileOptions.DeleteOnClose))
                { }

                return true;
            }
            catch
            {
                    return false;
            }
        }

        public async Task<Guid> WriteToFileStorageAsync(Stream file)
        {
            Guid name = Guid.NewGuid();
            var locStream = File.OpenWrite(Path.Combine(_storageRoot, name.ToString()));
            await file.CopyToAsync(locStream);
            await locStream.DisposeAsync();
            return name;
        }

        public Stream ReadFileFromStorage(string name)
        {
            string path = Path.Combine(_storageRoot, name);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            return File.OpenRead(path);
        }

        public void DeleteFileFromStorage(Guid name)
        {
            string path = Path.Combine(_storageRoot, name.ToString());
            if(File.Exists(path))
                File.Delete(path);
        }
    }
}
