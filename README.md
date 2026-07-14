# NetworkTest

Windows WPF application for continuous ICMP Ping or TCP port-connectivity testing.

## Run in VS Code

Open this folder in VS Code, install Microsoft's **C# Dev Kit**, select **NetworkTest 실행** under Run and Debug, then press `F5`. Stop a running session with `Shift+F5` before starting it again.

## Features

- ICMP Ping when the port is blank; TCP connection timing when a port is supplied.
- Start/stop collection, Grid results, total/success/failure horizontal bars, and a responsive response-time graph.
- New sessions clear prior on-screen data.
- One CSV log per start/stop session, including host, port or ICMP mode, interval, start/end time, and results.

## GitHub release

This folder is not currently a Git repository. Create an empty GitHub repository, then run `git init`, `git add .`, `git commit -m "Initial release"`, add the GitHub remote, and push the `main` branch. The included `.gitignore` excludes build output, logs, Visual Studio data, and local user settings while retaining shared VS Code launch files.

For a Windows release, publish the app, create and push a version tag such as `v0.1.0`, then use the GitHub repository's **Releases** page to create a release from that tag and upload the ZIP package.

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish\win-x64
Compress-Archive -Path publish\win-x64\* -DestinationPath NetworkTest-v0.1.0-win-x64.zip
git tag v0.1.0
git push origin main --tags
```

GitHub releases are based on Git tags and can include release notes and downloadable binaries.
