namespace SSPFS
{
    public class RemoteFile
    {
        public string Path { get; private set; }

        public RemoteFile(string path)
        {
            Path = path;
            Name = System.IO.Path.GetFileName(path);
        }
        public virtual string Name { get; set; }
    }
}