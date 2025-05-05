// Assets/Editor/ImageUtility.cs
using System.IO;
using UnityEngine;

namespace GSLab.Utility
{
    public static class ImageUtility
    {
        public static bool CheckImageSize(string imagePath, int width, int height)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath)) return false;

            byte[] bytes;
            try { bytes = File.ReadAllBytes(imagePath); }
            catch { return false; }

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            bool loaded = tex.LoadImage(bytes, true);
            bool result = loaded && tex.width == width && tex.height == height;
            Object.DestroyImmediate(tex);
            return result;
        }
    }
}