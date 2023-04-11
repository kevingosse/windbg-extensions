using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution(GenerateProjects = true)]
    readonly Solution Solution;

    [Parameter("Project to publish")]
    readonly Project Project;

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            DotNetClean(_ => _.SetProject(Solution));
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(_ => _.SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(_ => _.SetProjectFile(Solution.History));
            DotNetBuild(_ => _.SetProjectFile(Solution.AiAssistant));
        });

    Target Install => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            if (Project == null)
            {
                PublishProject(Solution.AiAssistant);
                PublishProject(Solution.History);
            }
            else
            {
                PublishProject(Project);
            }
        });

    private void PublishProject(Project project)
    {
        DotNetPublish(_ => _.SetProject(project)
            .SetOutput(ExpandVariables(@"%LOCALAPPDATA%\DBG\UIExtensions")));
    }
}
