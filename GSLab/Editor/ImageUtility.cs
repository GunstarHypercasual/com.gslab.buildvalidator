// ImageUtility.cs
namespace GSLab.BuildValidator
{
    using UnityEngine;
    using System.IO;

    public static class ImageUtility
    {
        public static bool CheckImageSize(string path, int w, int h)
        {
            if (!File.Exists(path)) return false;
            var data = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2);
            tex.LoadImage(data);
            bool ok = tex.width == w && tex.height == h;
            Object.DestroyImmediate(tex);
            return ok;
        }
    }
}