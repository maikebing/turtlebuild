using System;
using System.Collections.Generic;
using System.Text;
using QQn.TurtleBuildUtils;
using QQn.TurtleBuildUtils.Files.TBLog;

namespace QQn.TurtlePackager
{
	public sealed class PackageArgs
	{
		SortedFileList _projectsToPackage;
		TBLogCollection _logCollection;
		string _buildRoot;
		bool _dontUseProjectDependencies;

		/// <summary>
		/// Gets a list of project logfiles to package.
		/// </summary>
		/// <value>The projects to package.</value>
		public SortedFileList ProjectsToPackage
		{
			get { return _projectsToPackage ?? (_projectsToPackage = new SortedFileList()); }
		}

		/// <summary>
		/// Gets or sets the log cache to use
		/// </summary>
		/// <value>The log cache.</value>
		public TBLogCollection LogCache
		{
			get 
			{ 
				return _logCollection ?? (_logCollection = new TBLogCollection()); 
			}
			set { _logCollection = value; }
		}

		/// <summary>
		/// Gets or sets the build root.
		/// </summary>
		/// <value>The build root.</value>
		public string BuildRoot
		{
			get { return _buildRoot; }
			set { _buildRoot = value; ProjectsToPackage.BaseDirectory = value;  }
		}

		/// <summary>
		/// Gets or sets a value indicating whether to use project dependencies (Recommended)
		/// </summary>
		/// <value>
		/// 	<c>true</c> if [use project dependencies]; otherwise, <c>false</c>.
		/// </value>
		public bool UseProjectDependencies
		{
			get { return !_dontUseProjectDependencies; }
			set { _dontUseProjectDependencies = !value; }
		}

	}
}
