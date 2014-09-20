using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace System.Collections.Generic
{
    public static partial class IEnumerableMethodExtensions
    {
        public static string EllipsisString(this string rawString, int maxLength = 30, char delimiter = '\\')
        {
            maxLength -= 3; //account for delimiter spacing

            if (rawString.Length <= maxLength)
            {
                return rawString;
            }

            string final = rawString;
            List<string> parts;

            int loops = 0;
            while (loops++ < 100)
            {
                parts = rawString.Split(delimiter).ToList();
                parts.RemoveRange(parts.Count - 1 - loops, loops);
                if (parts.Count == 1)
                {
                    return parts.Last();
                }

                parts.Insert(parts.Count - 1, "...");
                final = string.Join(delimiter.ToString(), parts);
                if (final.Length < maxLength)
                {
                    return final;
                }
            }

            return rawString.Split(delimiter).ToList().Last();
        }

        /// <summary>
        /// Used by IsImageFile to determine if a file is a graphic type we care about. (Not an Extension Method, just a helper method)
        /// </summary>
        public static string[] GraphicFileExtensions = new string[] { ".png", ".bmp", ".gif", ".jpg", ".jpeg" };

        public static string GetGraphicFilesFilter()
        {
            string returnString = "";
            foreach (string s in GraphicFileExtensions)
            {
                returnString = returnString + "*" + s + ";";
            }

            return returnString;
        }

        //public static IEnumerable<FileInfo> IsImageFile(this IEnumerable<FileInfo> files,
        //                    Predicate<FileInfo> isMatch)
        //{
        //    foreach (FileInfo file in files)
        //    {
        //        if (isMatch(file))
        //            yield return file;
        //    }
        //}

        ///// <summary>
        ///// Method Extension - specifies that FileInfo IEnumerable should only return files whose extension matches one in GraphicFileExtensions[]. 
        ///// </summary>
        ///// <param name="files"></param>
        ///// <returns></returns>
        //public static IEnumerable<FileInfo> IsImageFile(this IEnumerable<FileInfo> files)
        //{
        //    foreach (FileInfo file in files)
        //    {
        //        string ext = file.Extension.ToLower();
        //        if (GraphicFileExtensions.Contains(ext))
        //            yield return file;
        //    }
        //}


        /// <summary>
        /// Method Extension - specifies that FileInfo IEnumerable should only return files whose extension matches one in extensions[]. 
        /// </summary>
        /// <param name="files"></param>
        /// <param name="extensions"></param>
        /// <returns></returns>
        public static IEnumerable<FileInfo> FilterByFileExtension(this IEnumerable<FileInfo> files, string[] extensions)
        {
            foreach (FileInfo file in files)
            {
                string ext = file.Extension.ToLower();
                if (extensions.Contains(ext))
                    yield return file;
            }
        }

        ///// <summary>
        ///// Method Extension - used by "shuffle" methods when randomizing a list.
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="list"></param>
        ///// <param name="i"></param>
        ///// <param name="j"></param>
        //public static void Swap<T>(this IList<T> list, int i, int j)
        //{
        //    var temp = list[i];
        //    list[i] = list[j];
        //    list[j] = temp;
        //}

        ///// <summary>
        ///// Method Extension - Recursively builds and traverses a tree stucture.  Sort of.
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="source"></param>
        ///// <param name="childrenSelector"></param>
        ///// <returns></returns>
        ///// <remarks>I'm not going to pretend I understand this completely. I do know how to call it, though.</remarks>
        //public static IEnumerable<T> Traverse<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> childrenSelector)
        //{

        //    var stack = new Stack<T>(source);
        //    while (stack.Any())
        //    {
        //        var next = stack.Pop();
        //        yield return next;
        //        foreach (var child in childrenSelector(next))
        //            stack.Push(child);
        //    }
        //}


    }
}
