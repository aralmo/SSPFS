namespace SSPFS
{
    public class RemoteFile
    {
        public string Filename { get; private set; }
        public string Path { get; private set; }

        public RemoteFile(string path)
        {
            Path = path;
            Filename = System.IO.Path.GetFileName(path);
        }
    }
}