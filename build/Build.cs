using System.Collections.Generic;
using System.IO;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.MSBuild;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Solution] readonly Solution Solution;

    string PluginName => Solution.Name;

    Target Compile => _ => _
        .Executes(() =>
        {
            var project = Solution.GetProject(PluginName);
            if (project == null)
                throw new FileNotFoundException("Not found!");

            var build = new List<string>();
            foreach (var (_, c) in project.Configurations)
            {
                var configuration = c.Split("|")[0];

                if (configuration == "Debug" || build.Contains(configuration))
                    continue;

                Logger.Normal($"Configuration: {configuration}");

                build.Add(configuration);

                MSBuild(_ => _
                    .SetProjectFile(project.Path)
                    .SetConfiguration(configuration)
                    .SetTargets("Restore"));
                MSBuild(_ => _
                    .SetProjectFile(project.Path)
                    .SetConfiguration(configuration)
                    .SetTargets("Rebuild"));
            }
        });
}