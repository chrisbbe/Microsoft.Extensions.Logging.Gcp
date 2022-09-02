using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;

[GitHubActions(
  "continuous",
  GitHubActionsImage.UbuntuLatest,
  On = new[] {GitHubActionsTrigger.Push},
  InvokedTargets = new[] {nameof(Test)}
)]
class Build : NukeBuild
{
  /// Support plugins are available for:
  ///   - JetBrains ReSharper        https://nuke.build/resharper
  ///   - JetBrains Rider            https://nuke.build/rider
  ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
  ///   - Microsoft VSCode           https://nuke.build/vscode
  public static int Main() => Execute<Build>(x => x.Compile);

  [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
  readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

  [Solution] readonly Solution Solution;

  Target Clean => _ => _
    .Before(Restore)
    .Executes(() =>
    {
    });

  Target Restore => _ => _
    .Executes(() =>
    {
      DotNetTasks.DotNetRestore(_ => _
        .SetProjectFile(Solution));
    });

  Target Compile => _ => _
    .DependsOn(Restore)
    .Executes(() =>
    {
      DotNetTasks.DotNetBuild(_ => _
        .SetProjectFile(Solution));
    });

  Target Test => _ => _
    .Executes(() =>
    {
      DotNetTasks.DotNetTest(_ => _
        .SetProjectFile(Solution));
    });
}
