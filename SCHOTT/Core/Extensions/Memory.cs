using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace SCHOTT.Core.Extensions
{
    /// <summary>
    /// A class to provide extensions for memory format.
    /// </summary>
    public static class Memory
    {
        /// <summary>
        /// Use reflection to copy one object to another.
        /// </summary>
        /// <typeparam name="TD">Destination object type.</typeparam>
        /// <typeparam name="TS">Source object type.</typeparam>
        /// <param name="destinationObject">Destination object.</param>
        /// <param name="sourceObject">Source object.</param>
        public static void CopyFrom<TD, TS>(this TD destinationObject, TS sourceObject)
        {
            var sourceProps = typeof(TS).GetProperties().Where(x => x.CanRead).ToList();
            var destProps = typeof(TD).GetProperties().Where(x => x.CanWrite).ToList();
            var destPropNames = destProps.Select(x => x.Name);

            foreach (var sourceProp in sourceProps.Where(x => destPropNames.Contains(x.Name)))
            {
                var p = destProps.First(x => x.Name == sourceProp.Name);
                p.SetValue(destinationObject, sourceProp.GetValue(sourceObject, null), null);
            }
        }

        /// <summary>
        /// A deep clone method. Object must be serializable.
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="source">Object to clone</param>
        /// <returns>Cloned object</returns>
        public static T Clone<T>(T source)
        {
            if (!typeof(T).IsSerializable)
            {
                throw new Exception("Object type must be serializable to perform Clone.");
            }

            // Don't serialize a null object, simply return the default for that object
            if (ReferenceEquals(source, null))
            {
                return default(T);
            }

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                formatter.Serialize(stream, source);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }

        /// <summary>
        /// Copies contents of one stream to another
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        public static void CopyTo(this Stream input, Stream output)
        {
            var buffer = new byte[16 * 1024]; // Fairly arbitrary size
            int bytesRead;

            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
        }

        /// <summary>
        /// Convert a memory stream to a byte array.
        /// </summary>
        /// <param name="stream">The stream to convert.</param>
        /// <returns>A byte array to return.</returns>
        public static byte[] ToByteArray(this Stream stream)
        {
            stream.Position = 0;
            var buffer = new byte[stream.Length];
            for (var totalBytesCopied = 0; totalBytesCopied < stream.Length;)
                totalBytesCopied += stream.Read(buffer, totalBytesCopied, Convert.ToInt32(stream.Length) - totalBytesCopied);
            return buffer;
        }

        /// <summary>
        /// Clone one list into a new list.
        /// </summary>
        /// <typeparam name="TU">The type of list to clone.</typeparam>
        /// <param name="listToClone">The list to clone.</param>
        /// <returns>The cloned list.</returns>
        public static List<TU> CloneList<TU>(this List<TU> listToClone)
        {
            var list = new List<TU>();
            list.AddRange(listToClone);
            return list;
        }

        /// <summary>
        /// Find duplicate objects in a given list.
        /// </summary>
        /// <typeparam name="T">The input list type.</typeparam>
        /// <typeparam name="TU">The output list type.</typeparam>
        /// <param name="list">The input list.</param>
        /// <param name="keySelector">The lambda expression to find duplicates with.</param>
        /// <returns>The list of duplicate items.</returns>
        public static List<TU> FindDuplicates<T, TU>(this List<T> list, Func<T, TU> keySelector)
        {
            return list.GroupBy(keySelector)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key).ToList();
        }

    }

}
