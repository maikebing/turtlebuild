using System;
using System.Collections.Generic;
using System.Text;
using QQn.TurtleBuildUtils.Files.TBLog;
using System.IO;
using QQn.TurtleUtils.IO;
using System.Diagnostics;

namespace QQn.TurtlePackager.Origins
{
	[DebuggerDisplay("Type=MSBuild Project={ProjectName}")]
	class BuildOrigin : Origin
	{
		readonly TBLogFile _log;
		readonly string _projectFile;

		public BuildOrigin(TBLogFile logFile)
		{
			if (logFile == null)
				throw new ArgumentNullException("logFile");

			_log = logFile;
			_projectFile = Path.Combine(logFile.ProjectPath, logFile.Project.File);
		}

		public TBLogFile LogFile
		{
			get { return _log; }
		}

		public string ProjectName
		{
			get { return _log.Project.Name; }
		}

		public override void PublishOriginalFiles(PackageState state)
		{
			foreach (TBLogConfiguration config in LogFile.Configurations)
			{
				foreach (TBLogItem item in config.ProjectOutput.Items)
				{
					if (item.IsShared)
						continue;

					FileData fd = new FileData(item.FullSrc, state.Files);

					if(!string.IsNullOrEmpty(item.FromSrc))
						fd.CopiedFrom = item.FullFromSrc;

					fd.Origin = this;

					state.Files.Add(fd);
				}

				foreach (TBLogItem item in config.Content.Items)
				{
					if (item.IsShared)
						continue;

					FileData fd = new FileData(item.FullSrc, state.Files);

					if (!string.IsNullOrEmpty(item.FromSrc))
						fd.CopiedFrom = item.FullFromSrc;

					fd.Origin = this;

					state.Files.AddUnique(fd);
				}
			}

			foreach (TBLogItem item in LogFile.Scripts.Items)
			{
				if (item.IsShared)
					continue;

				FileData fd = new FileData(item.FullSrc, state.Files);

				if (!string.IsNullOrEmpty(item.FromSrc))
					fd.CopiedFrom = item.FullFromSrc;

				fd.Origin = this;

				state.Files.Add(fd);
			}
		}

		public override void PublishRequiredFiles(PackageState state)
		{
			foreach (TBLogConfiguration config in LogFile.Configurations)
			{
				foreach (TBLogItem item in config.ProjectOutput.Items)
				{
					if (!item.IsShared)
						continue;

					FileData fd = new FileData(item.FullSrc, state.Files);

					if (!string.IsNullOrEmpty(item.FromSrc))
						fd.CopiedFrom = item.FullFromSrc;

					fd.FindOrigin = true;

					state.Files.Add(fd);
				}

				foreach (TBLogItem item in config.Content.Items)
				{
					if (item.IsShared)
						continue;

					FileData fd = new FileData(item.FullSrc, state.Files);

					if (!string.IsNullOrEmpty(item.FromSrc))
						fd.CopiedFrom = item.FullFromSrc;

					fd.FindOrigin = true;

					state.Files.AddUnique(fd);
				}
			}
		}

		public override void ApplyProjectDependencies(PackageState state)
		{
			foreach (TBLogConfiguration config in LogFile.Configurations)
			{
				foreach(TBLogProjectReference project in config.References.Projects)
				{
					string src = QQnPath.NormalizePath(project.FullSrc);

					foreach (Origin o in state.Origins)
					{
						BuildOrigin bo = o as BuildOrigin;
						if (bo == null)
							continue;

						if (QQnPath.Equals(bo.ProjectFile, src) && !Dependencies.Contains(o))
							Dependencies.Add(o);
					}
				}
			}
		}

		public string ProjectFile
		{
			get { return _projectFile; }
		}


	}
}