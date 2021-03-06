using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using QQn.TurtleUtils.Tokens;

[module: SuppressMessage("Microsoft.Design", "CA1023:IndexersShouldNotBeMultidimensional", Scope = "member", Target = "QQn.TurtleBuildUtils.Files.TBLog.TBLogConfigurationCollection.Item[System.String,System.String]")]

namespace QQn.TurtleBuildUtils.Files.TBLog
{
	/// <summary>
	/// 
	/// </summary>
	public class TBLogConfiguration : ITokenizerInitialize
	{
		TBLogReferences _references;
		TBLogTarget _target;
		TBLogProjectOutput _projectOutput;
		TBLogContent _content;
        TBLogAssembly _assembly;

		string _name;
		string _platform;
		string _outputPath;
		string _basePath;
		TBLogFile _logFile;

		/// <summary>
		/// Gets or sets the log file containing this configurations
		/// </summary>
		/// <value>The log file.</value>
		public TBLogFile LogFile
		{
			get { return _logFile; }
			internal set { _logFile = value; }
		}

        /// <summary>
        /// Gets or sets the assembly.
        /// </summary>
        /// <value>The assembly.</value>
        [TokenGroup("Assembly")]
        public TBLogAssembly Assembly
        {
            get { return _assembly ?? (_assembly = new TBLogAssembly()); }
            set { EnsureWritable(); _assembly = value; }
        }

		/// <summary>
		/// Gets or sets the target.
		/// </summary>
		/// <value>The target.</value>
		[TokenGroup("Target")]
		public TBLogTarget Target
		{
			get { return _target ?? (_target = new TBLogTarget()); }
			set { EnsureWritable(); _target = value; }
		}

		/// <summary>
		/// Gets or sets the project output.
		/// </summary>
		/// <value>The project output.</value>
		[TokenGroup("ProjectOutput")]
		public TBLogProjectOutput ProjectOutput
		{
			get { return _projectOutput ?? (_projectOutput = new TBLogProjectOutput()); }
			set { EnsureWritable(); _projectOutput = value; }
		}

		/// <summary>
		/// Gets or sets the content.
		/// </summary>
		/// <value>The content.</value>
		[TokenGroup("Content")]
		public TBLogContent Content
		{
			get { return _content ?? (_content = new TBLogContent()); }
			set { EnsureWritable(); _content = value; }
		}

		/// <summary>
		/// Gets or sets the references.
		/// </summary>
		/// <value>The references.</value>
		[TokenGroup("References")]
		public TBLogReferences References
		{
			get { return _references ?? (_references = new TBLogReferences()); }
			set { EnsureWritable(); _references = value; }
		}

		/// <summary>
		/// Gets or sets the configuration name.
		/// </summary>
		/// <value>The name.</value>
		[Token("name")]
		public string Name
		{
			get { return _name; }
			set { EnsureWritable(); _name = value; }
		}

		/// <summary>
		/// Gets or sets the platform.
		/// </summary>
		/// <value>The platform.</value>
		[Token("platform")]
		public string Platform
		{
			get { return _platform; }
			set { EnsureWritable(); _platform = value; }
		}

		/// <summary>
		/// Gets or sets the output path.
		/// </summary>
		/// <value>The output path.</value>
		[Token("outputPath")]
		public string OutputPath
		{
			get { return _outputPath; }
			set { EnsureWritable(); _outputPath = value; }
		}

		/// <summary>
		/// Gets or sets the output path.
		/// </summary>
		/// <value>The output path.</value>
		[Token("basePath")]
		public string BasePath
		{
			get { return _basePath; }
			set { EnsureWritable(); _basePath = value; }
		}

		bool _completed;
		void EnsureWritable()
		{
			if (_completed)
				throw new InvalidOperationException();
		}

		#region ITokenizerInitialize Members

		void ITokenizerInitialize.OnBeginInitialize(TokenizerEventArgs e)
		{
			//throw new Exception("The method or operation is not implemented.");
		}

		void ITokenizerInitialize.OnEndInitialize(TokenizerEventArgs e)
		{
			_completed = true;

			References.Configuration = this;
			ProjectOutput.Configuration = this;
			Content.Configuration = this;
			Target.Configuration = this;
		}

		#endregion
	}

	/// <summary>
	/// Keyed collection of <see cref="TBLogConfiguration"/> items
	/// </summary>
	public class TBLogConfigurationCollection : Collection<TBLogConfiguration>
	{
		internal TBLogFile LogFile
		{
			set
			{
				foreach (TBLogConfiguration c in this)
					c.LogFile = value;
			}
		}

		/// <summary>
		/// Determines whether the list contains the specified configuration
		/// </summary>
		/// <param name="configuration">The configuration.</param>
		/// <param name="platform">The platform.</param>
		/// <returns>
		/// 	<c>true</c> if the list contains the specified configuration; otherwise, <c>false</c>.
		/// </returns>
		public bool Contains(string configuration, string platform)
		{
			foreach (TBLogConfiguration config in this)
			{
				if (string.Equals(configuration, config.Name, StringComparison.OrdinalIgnoreCase) &&
					string.Equals(platform, config.Platform, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Determines whether the list contains the specified configuration
		/// </summary>
		/// <param name="configuration">The configuration.</param>
		/// <returns>
		/// 	<c>true</c> if the list contains the specified configuration; otherwise, <c>false</c>.
		/// </returns>
		public bool Contains(string configuration)
		{
			foreach (TBLogConfiguration config in this)
			{
				if (string.Equals(configuration, config.Name, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Gets the <see cref="QQn.TurtleBuildUtils.Files.TBLog.TBLogConfiguration"/> with the specified configuration and platform.
		/// </summary>
		/// <value></value>
		public TBLogConfiguration this[string configuration, string platform]
		{
			get
			{
				foreach (TBLogConfiguration config in this)
				{
					if (string.Equals(configuration, config.Name, StringComparison.OrdinalIgnoreCase) &&
						string.Equals(platform, config.Platform, StringComparison.OrdinalIgnoreCase))
					{
						return config;
					}
				}

				throw new ArgumentException("Configuration not found", "configuration");
			}
		}

		/// <summary>
		/// Gets the <see cref="QQn.TurtleBuildUtils.Files.TBLog.TBLogConfiguration"/> with the specified configuration.
		/// </summary>
		/// <value></value>
		public TBLogConfiguration this[string configuration]
		{
			get
			{
				foreach (TBLogConfiguration config in this)
				{
					if (string.Equals(configuration, config.Name, StringComparison.OrdinalIgnoreCase))
					{
						return config;
					}
				}

				throw new ArgumentException("Configuration not found", "configuration");
			}
		}
	}
}
