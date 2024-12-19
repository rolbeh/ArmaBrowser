using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ArmaBrowser.Data.DefaultImpl.Helper
{
    internal class PathHelper
    {
        private const int FileAttributeDirectory = 0x10;
        private const int FileAttributeNormal = 0x80;

        public static string GetRelativePath(string fromPath, string toPath)
        {
            var fromAttr = GetPathAttribute(fromPath);
            var toAttr = GetPathAttribute(toPath);

            var path = new StringBuilder(260); // MAX_PATH
            if (PathRelativePathTo(
                    path,
                    fromPath,
                    fromAttr,
                    toPath,
                    toAttr) == 0)
                throw new ArgumentException("Paths must have a common prefix");
            return path.ToString();
        }

        private static int GetPathAttribute(string path)
        {
            var di = new DirectoryInfo(path);
            if (di.Exists)
                return FileAttributeDirectory;

            var fi = new FileInfo(path);
            if (fi.Exists)
                return FileAttributeNormal;

            throw new FileNotFoundException();
        }

        //https://stackoverflow.com/questions/275689/how-to-get-relative-path-from-absolute-path
        [DllImport("shlwapi.dll", SetLastError = true)]
        private static extern int PathRelativePathTo(StringBuilder pszPath,
            string pszFrom, int dwAttrFrom, string pszTo, int dwAttrTo);


        public static long CalculateFolderSize(string folder)
        {
            long folderSize = 0;

            if (!Directory.Exists(folder))
                return folderSize;

            foreach (var file in Directory.GetFiles(folder))
                try
                {
                    if (File.Exists(file))
                    {
                        var finfo = new FileInfo(file);
                        folderSize += finfo.Length;
                    }
                }
                catch (NotSupportedException e)
                {
                    // ReSharper disable once LocalizableElement
                    Console.WriteLine("Unable to calculate folder size: {0}", e.Message);
                }
                catch (UnauthorizedAccessException e)
                {
                    // ReSharper disable once LocalizableElement
                    Console.WriteLine("Unable to calculate folder size: {0}", e.Message);
                }

            folderSize += Directory.GetDirectories(folder).Sum(dir => CalculateFolderSize(dir));

            return folderSize;
        }
    }
}