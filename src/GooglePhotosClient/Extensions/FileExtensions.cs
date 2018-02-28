namespace GooglePhotosClient.Extensions
{
    using Google.Apis.Drive.v3.Data;

    public static class FileExtensions
    {
        public static string GetLocalIdentifierFileName(this File file)
        {
            return string.Concat(file.Id, ".", file.FileExtension);
        }
    }
}
