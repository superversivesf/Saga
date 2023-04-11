using System.Collections.Generic;
using System.IO;
using System.Linq;
using File = TagLib.File;

namespace SagaImporter;

internal class FileFetcher
{
    public File GetMetaData(string path)
    {
        return File.Create(path);
    }

    public List<string> GetFiles(string path, string filter = "*.*")
    {
        var _result = Directory.EnumerateFiles(path, filter, SearchOption.AllDirectories).ToList();
        return _result;
    }
}