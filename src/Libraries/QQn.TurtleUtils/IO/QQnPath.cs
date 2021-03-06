using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Reflection;

namespace QQn.TurtleUtils.IO
{
	static class NativeMethods
	{
		[DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool PathRelativePathTo(StringBuilder pszPath, string pszFrom, [MarshalAs(UnmanagedType.U4)] int dwAttrFrom, string pszTo, [MarshalAs(UnmanagedType.U4)] int dwAttrTo);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		internal static extern IntPtr FindFirstFileW([In]StringBuilder lpFileName, out WIN32_FIND_DATA lpFindFileData);

		[DllImportAttribute("kernel32.dll", CharSet = CharSet.Unicode)]
		internal static extern bool FindClose(IntPtr handle);
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	struct WIN32_FIND_DATA
	{
		uint dwFileAttributes;
		long ftCreationTime;
		long ftLastAccessTime;
		long ftLastWriteTime;
		uint sizeLow;
		uint sizeHigh;
		uint reserved0;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=260)]
		public string filename;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=14)]
		string alternateFilename;
	}

	/// <summary>
	/// Static wrapper for several path utilities
	/// </summary>
	public static class QQnPath
	{
		/// <summary>
		/// Tries to make a relative path
		/// </summary>
		/// <param name="path"></param>
		/// <param name="relativeFrom"></param>
		/// <returns>The relative path; an absolute path is returned if a relative is not available</returns>
		public static string GetRelativePath(string path, string relativeFrom)
		{
			if (path == null || path.Length == 0)
				throw new ArgumentNullException("path");
			if (relativeFrom == null || path.Length == 0)
				throw new ArgumentNullException("relativeFrom");

			path = Path.GetFullPath(path);
			relativeFrom = Path.GetFullPath(relativeFrom);

			string pathRoot = Path.GetPathRoot(path);
			string relRoot = Path.GetPathRoot(relativeFrom);

			if (!string.Equals(pathRoot, relRoot, StringComparison.OrdinalIgnoreCase))
				return path;

			const int FILE_ATTRIBUTE_FILE = 0x00000000;
			const int FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
			StringBuilder result = new StringBuilder(260); // 260 = MAX_PATH
			if (NativeMethods.PathRelativePathTo(result, relativeFrom, FILE_ATTRIBUTE_DIRECTORY, path, FILE_ATTRIBUTE_FILE))
			{
				string p = result.ToString();
				if (p.Length > 2 && p[0] == '.' && IsDirectorySeparator(p[1]))
					p = p.Substring(2);

				return p;
			}

			return path;
		}


		/// <summary>
		/// Makes the itemPath relative to the origin directory
		/// </summary>
		/// <param name="originDirectory">The origin directory.</param>
		/// <param name="itemPath">The item path.</param>
		/// <returns></returns>
		public static string MakeRelativePath(string originDirectory, string itemPath)
		{
			if (string.IsNullOrEmpty(originDirectory))
				throw new ArgumentNullException("originDirectory");
			else if (string.IsNullOrEmpty(itemPath))
				throw new ArgumentNullException("itemPath");

			itemPath = Path.GetFullPath(itemPath);
			originDirectory = NormalizePath(Path.GetFullPath(originDirectory), false);

			const int FILE_ATTRIBUTE_FILE = 0x00000000;
			const int FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
			StringBuilder result = new StringBuilder(260);
			if (NativeMethods.PathRelativePathTo(result, originDirectory, FILE_ATTRIBUTE_DIRECTORY, itemPath, FILE_ATTRIBUTE_FILE))
			{
				string p = result.ToString();
				if (p.Length > 2 && p[0] == '.' && IsDirectorySeparator(p[1]))
					p = p.Substring(2);

				return p;
			}

			return Path.GetFullPath(itemPath);
		}		

		/// <summary>
		/// Gets the parent directory of a directory of file
		/// </summary>
		/// <param name="path">Name of the directory.</param>
		/// <returns></returns>
		public static string GetParentDirectory(string path)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException("path");

			if(path.Length > 0)
			{
				if(IsDirectorySeparator(path[path.Length-1]))
				{
					string root = Path.GetPathRoot(path);

					if(root.Length < path.Length)
						path = path.Substring(0, path.Length-1);
				}
			}

			return Path.GetFullPath(Path.GetDirectoryName(path));
		}

		/// <summary>
		/// Combines the specified path parts to a complete directory
		/// </summary>
		/// <param name="path">The path.</param>
		/// <param name="items">The items.</param>
		/// <returns></returns>
		public static string Combine(string path, params string[] items)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException("path");
			else if (items == null)
				throw new ArgumentNullException("items");

			foreach (string part in items)
			{
				if (string.IsNullOrEmpty(part))
					continue;

				path = Combine(path, part);
			}

			return path;
		}

		/// <summary>
		/// Determines whether the specified character is either <see cref="Path.DirectorySeparatorChar"/> or
		/// <see cref="Path.AltDirectorySeparatorChar"/>
		/// </summary>
		/// <param name="character">The character to test.</param>
		/// <returns>
		/// 	<c>true</c> if the specified character is a directory separator; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsDirectorySeparator(char character)
		{
			return (character == Path.DirectorySeparatorChar) || (character == Path.AltDirectorySeparatorChar);
		}

		/// <summary>
		/// Determines whether the specified path is rooted and absolute
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns>
		/// 	<c>true</c> if [is absolute path] [the specified path]; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsAbsolutePath(string path)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException("path");

			if (!Path.IsPathRooted(path))
				return false;

			if (IsRelativeViaRootPath(path))
				return false;

			return true;
		}

		/// <summary>
		/// Determines whether tje path is relative via the root path
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns>
		/// 	<c>true</c> if [is relative via root path] [the specified path]; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsRelativeViaRootPath(string path)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException("path");

			if (IsDirectorySeparator(path[0]) && (path.Length == 1 || !IsDirectorySeparator(path[1])))
				return true;

			return false;
		}

		/// <summary>
		/// Combines the specified path parts to a complete directory
		/// </summary>
		/// <param name="path">The path.</param>
		/// <param name="item">The item.</param>
		/// <returns></returns>
		public static string Combine(string path, string item)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException("path");
			else if (string.IsNullOrEmpty(item))
				throw new ArgumentNullException("item");

			string result = Path.Combine(path, item);

			if (!string.IsNullOrEmpty(result) && IsRelativeViaRootPath(result) && IsAbsolutePath(path))
			{
				// .Net combines @"f:\tools" and @"\test.txt" to @"\test.txt", removing the relative path origin of path
				if (result.Length == 1 || !IsDirectorySeparator(result[1])) // No UNC path
				{
					return Path.GetPathRoot(path) + TrimStartSlashes(result);
				}
			}

			return result;
		}

		/// <summary>
		/// Combines the specified path parts to a full path
		/// </summary>
		/// <param name="path">The path.</param>
		/// <param name="items">The items.</param>
		/// <returns></returns>
		public static string CombineFullPath(string path, params string[] items)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException("path");
			else if(items == null)
				throw new ArgumentNullException("items");

			return Path.GetFullPath(Combine(path, items));
		}

		/// <summary>
		/// Combines the specified path parts to a full path
		/// </summary>
		/// <param name="path">The path.</param>
		/// <param name="path2">The path2.</param>
		/// <returns></returns>
		public static string CombineFullPath(string path, string path2)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException("path");

			return Path.GetFullPath(Combine(path, path2));
		}

		/// <summary>
		/// Normalizes the path.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <param name="addEndSlash">if set to <c>true</c> adds a path separator at the end.</param>
		/// <returns></returns>
		public static string NormalizePath(string path, bool addEndSlash)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException("path");

			path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

			if (Path.IsPathRooted(path) || path.Contains(_dotSlash))
			{
				path = Path.GetFullPath(path);
				string root = Path.GetPathRoot(path);

				path = root + RemoveDoubleSlash(path.Substring(root.Length), addEndSlash);
			}
			else
				path = RemoveDoubleSlash(path, addEndSlash);

			return path;
		}

		/// <summary>
		/// Normalizes the path.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns></returns>
		public static string NormalizePath(string path)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException("path");

			return NormalizePath(path, false);
		}

		static string TrimStartSlashes(string path)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException("path");

			int i = 0;
			while (i < path.Length && IsDirectorySeparator(path[i]))
				i++;

			if (i > 0)
				path = path.Substring(i);

			return path;
		}

		/// <summary>
		/// Gets the <see cref="StringComparer"/> to use for comparing paths
		/// </summary>
		/// <value>The path comparer.</value>
		/// <remarks>Returns StringComparer.OrdinalIgnoreCase on Windows</remarks>
		public static StringComparer PathStringComparer
		{
			get { return StringComparer.OrdinalIgnoreCase; }
		}

		/// <summary>
		/// Removes all instances of the double slash sequence
		/// </summary>
		/// <param name="path">The path (with only primary directory separators)</param>
		/// <param name="addEndSlash">if set to <c>true</c> [add end slash].</param>
		/// <returns></returns>
		static string RemoveDoubleSlash(string path, bool addEndSlash)
		{
			if (path == null)
				throw new ArgumentNullException("path");

			int n;
			while (0 <= (n = path.IndexOf(_doubleSlash)))
				path = path.Remove(n, 1); // Remove the first slash

			char lastChar = (path.Length > 0) ? path[path.Length - 1] : '\0';

			if (addEndSlash != (lastChar == Path.DirectorySeparatorChar))
			{
				if (addEndSlash)
					path += Path.DirectorySeparatorChar;
				else if(path.Length > 0)
					path = path.Substring(0, path.Length - 1);
			}

			return path;
		}

		/// <summary>
		/// Normalizes the unix path.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <param name="addEndSlash">if set to <c>true</c> adds a path separator at the end.</param>
		/// <returns></returns>
		public static string NormalizeUnixPath(string path, bool addEndSlash)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException("path");

			return NormalizePath(path, addEndSlash).Replace(Path.DirectorySeparatorChar, '/');
		}

		/// <summary>
		/// Normalizes the unix path.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns></returns>
		public static string NormalizeUnixPath(string path)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException("path");

			return NormalizeUnixPath(path, false);
		}

		/// <summary>
		/// Copies the stream.
		/// </summary>
		/// <param name="from">From.</param>
		/// <param name="to">To.</param>
		public static void CopyStream(Stream from, Stream to)
		{
			CopyStream(from, to, 32768);
		}

		/// <summary>
		/// Copies the stream.
		/// </summary>
		/// <param name="from">From.</param>
		/// <param name="to">To.</param>
		/// <param name="bufferSize">Size of the buffer.</param>
		public static void CopyStream(Stream from, Stream to, int bufferSize)
		{
			if (from == null)
				throw new ArgumentNullException("from");
			else if (to == null)
				throw new ArgumentNullException("to");
			else if (bufferSize <= 0)
				throw new ArgumentOutOfRangeException("bufferSize", bufferSize, "Buffersize must be greater than 0");
			byte[] buffer = new byte[Math.Max(4096, bufferSize)];
			int nRead;

			while (0 < (nRead = from.Read(buffer, 0, buffer.Length)))
				to.Write(buffer, 0, nRead);
		}


		static readonly string _dotSlash = "." + Path.DirectorySeparatorChar;
		static readonly string _doubleSlash = String.Concat(Path.DirectorySeparatorChar, Path.DirectorySeparatorChar);

		/// <summary>
		/// Determines whether the specified path is a subpath.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns>
		/// 	<c>true</c> if [is safe sub path] [the specified path]; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsRelativeSubPath(string path)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException("path");

			if (Path.IsPathRooted(path)) // Absolute or relative via root directory
				return false;
			else if (path.IndexOf(Path.AltDirectorySeparatorChar) >= 0 || path.Contains(_dotSlash) || path.Contains(_doubleSlash))
				return false;

			return true;
		}

		/// <summary>
		/// Ensures the path is a relative path from the specified origin
		/// </summary>
		/// <param name="origin">The origin.</param>
		/// <param name="path">The path.</param>
		/// <returns>The unmodified path, or the path as relative from the specified origin</returns>
		public static string EnsureRelativePath(string origin, string path)
		{
			if (string.IsNullOrEmpty(origin))
				throw new ArgumentNullException("origin");
			else if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException("path");

			if (IsRelativeSubPath(path))
				return path;
			else
				return MakeRelativePath(origin, Combine(origin, path));
		}

		/// <summary>
		/// Finds the specified file in the provided path.
		/// </summary>
		/// <param name="file">The file.</param>
		/// <param name="pathList">The path list, separated by <see cref="Path.PathSeparator"/> characters</param>
		/// <returns></returns>
		public static string FindFileInPath(string file, string pathList)
		{
			if (string.IsNullOrEmpty(file))
				throw new ArgumentNullException("file");
			else if (string.IsNullOrEmpty(pathList))
				throw new ArgumentNullException("pathList");

			string[] paths = pathList.Split(Path.PathSeparator);

			foreach (string i in paths)
			{
				if (string.IsNullOrEmpty(i))
					continue;

				string fullPath = CombineFullPath(i, file);

				if (File.Exists(fullPath))
					return fullPath;
			}

			return null;
		}

		/// <summary>
		/// Finds the file in the system environment variable path, the current directory, or the directory containing the current application.
		/// </summary>
		/// <param name="file">the filename of the file to search</param>
		/// <returns>The full path of the file or <c>null</c> if the file is not found</returns>
		public static string FindFileInPath(string file)
		{
			if (string.IsNullOrEmpty(file))
				throw new ArgumentNullException("file");

			string path = Environment.GetEnvironmentVariable("PATH");

			string result;
			if (!string.IsNullOrEmpty(path))
			{
				result = FindFileInPath(file, path);

				if (!string.IsNullOrEmpty(result))
					return result;
			}
				
			result = FindFileInPath(file, ".");

			if (!string.IsNullOrEmpty(result))
				return result;

			Assembly asm = Assembly.GetEntryAssembly();

			if (asm == null)
				asm = Assembly.GetCallingAssembly();

			if(asm != null)
				result = FindFileNextToAssembly(file, asm);

			return result;
		}

		private static string FindFileNextToAssembly(string file, Assembly assembly)
		{
			if (string.IsNullOrEmpty(file))
				throw new ArgumentNullException("file");
			else if (assembly == null)
				throw new ArgumentNullException("assembly");

			if (assembly.CodeBase == null)
				return null;

			Uri uri = new Uri(assembly.CodeBase);

			if (uri.IsFile || uri.IsUnc)
				return FindFileInPath(file, Path.GetDirectoryName(uri.LocalPath));

			return null;
		}

		/// <summary>
		/// Gets a boolean indicating whether a filename might be of an assembly
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <returns>true for .exe and .dll files</returns>
		public static bool IsAssemblyFile(string filename)
		{
			if (string.IsNullOrEmpty(filename))
				throw new ArgumentNullException("filename");

			string extension = Path.GetExtension(filename);

			if (string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Makes sure the file has the specified extension, appending it if needed
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="extension">The extension.</param>
		/// <returns></returns>
		public static string EnsureExtension(string fileName, string extension)
		{
			if (string.IsNullOrEmpty(fileName))
				throw new ArgumentNullException("fileName");
			else if (string.IsNullOrEmpty(extension))
				throw new ArgumentNullException("extension");

			if (extension[0] != '.')
				extension = '.' + extension;

			if (!ExtensionEquals(fileName, extension))
				fileName = fileName + extension;

			return fileName;
		}

		/// <summary>
		/// Determines whether the specified path has the specified extension.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <param name="extension">The extension.</param>
		/// <returns>
		/// 	<c>true</c> if the specified path has the extension; otherwise, <c>false</c>.
		/// </returns>
		public static bool ExtensionEquals(string path, string extension)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException("path");
			else if (string.IsNullOrEmpty(extension))
				throw new ArgumentNullException("extension");

			if (extension[0] != '.')
				extension = '.' + extension;

			return string.Equals(Path.GetExtension(path), extension, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Compares the specified paths
		/// </summary>
		/// <param name="path1">The path1.</param>
		/// <param name="path2">The path2.</param>
		/// <returns><c>true</c> if the paths specify the same file, otherwise <c>false</c></returns>
		public static bool Equals(string path1, string path2)
		{
			if (string.IsNullOrEmpty(path1))
				throw new ArgumentNullException("path1");
			else if(string.IsNullOrEmpty(path2))
				throw new ArgumentNullException("path2");

			if (StringComparer.OrdinalIgnoreCase.Equals(path1, path2))
				return true;
			
			path1 = NormalizePath(path1);
			path2 = NormalizePath(path2);

			if (StringComparer.OrdinalIgnoreCase.Equals(path1, path2))
				return true;

			if (Path.IsPathRooted(path1) != Path.IsPathRooted(path2))
			{
				path1 = Path.GetFullPath(path1);
				path2 = Path.GetFullPath(path2);

				if (StringComparer.OrdinalIgnoreCase.Equals(NormalizePath(path1), NormalizePath(path2)))
					return true;
			}

			return false;
		}

		static int NextSlash(StringBuilder sb, int startAt)
		{
			if (sb == null)
				throw new ArgumentNullException("sb");

			for(int i = startAt; i < sb.Length; i++)
			{
				if(sb[i] == '\\')
					return i;
			}
			return -1;
		}

		// TODO: Implement something like SharpSvn's GetTruePath()
		/// <summary>
		/// Gets the true path.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns></returns>
		public static string GetTruePath(string path)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException("path");

			int n = path.IndexOf('/');
			if (n >= 0)
				path = path.Replace('/', '\\');

			String root = null;
			char c = path[0];
	
			if (c == '\\' && path.StartsWith("\\\\?\\", StringComparison.Ordinal))
				path = path.Substring(4); // We use this trick ourselves

			if (path.Length > 2 && (path[1] == ':') && ((c >= 'a') && (c <= 'z')) || ((c >= 'A') && (c <= 'Z')))
			{
				if ((path[2] == '\\') && !path.Contains("\\."))
					root = Path.GetPathRoot(path).ToUpperInvariant(); // 'a:\' -> 'A:\'
			}
			else if (path.StartsWith("\\\\", StringComparison.Ordinal))
			{		
				int next = path.IndexOf('\\', 2);

				if (next > 0)
					next = path.IndexOf('\\', next+1);

				if ((next > 0) && (0 > path.IndexOf("\\.", next+1)))
					root = Path.GetPathRoot(path);
			}

			if (root == null)
			{
				if(path[0] == '\\' || path.Contains("\\."))
				{
					path = Path.GetFullPath(path);
					root = Path.GetPathRoot(path); // UNC paths are not case sensitive, but we keep the casing
				}
				else
					root = "";
			}

			// Okay, now we have a normalized path and it's root in normal form. Now we need to find the exact casing of the next parts
			StringBuilder result = new StringBuilder(root, path.Length + 16);
			StringBuilder searcher = new StringBuilder("\\\\?\\" + path);

			int nStart = 4 + root.Length;
			bool isFirst = true;

			while(nStart < path.Length)
			{
				WIN32_FIND_DATA filedata;

				int nNext = NextSlash(searcher, nStart);

				if(nNext > 0)
					searcher[nNext] = '\0';

				IntPtr hSearch = NativeMethods.FindFirstFileW(searcher, out filedata);

				if(hSearch == (IntPtr)(-1))
					return null;

				if(!isFirst)
					result.Append('\\');

				result.Append(filedata.filename);

				NativeMethods.FindClose(hSearch); // Close search request

				if(nNext < 0)
					break;
				else
					searcher[nNext] = '\\'; // Revert 0 to '\'

				nStart = nNext+1;
				isFirst= false;
			}

			return result.ToString();
		}
	}
}
