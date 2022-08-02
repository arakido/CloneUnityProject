using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CloneProject {
    public static class CloneManager {
        public static Project GetCurrentProject() {
            string pathString = GetCurrentProjectPath();
            var path = Path.Combine(pathString, CloneConfig.ConfigPath);
            if (File.Exists(path)) {
                var json = File.ReadAllText(path);
                var project = JsonUtility.FromJson<Project>(json);
                CheckClones(project);
                return project;
            }
            return new Project(pathString);
        }
        
        public static void CheckClones(Project project) {
            if ( project.Clones is not { Length: > 0 } ) return;
            List<Project> temp = new List<Project>();
            foreach ( var clone in project.Clones ) {
                if ( Directory.Exists(clone.ProjectPath) ) {
                    temp.Add(clone);
                }
            }
            project.Clones = temp.ToArray();
            ReSaveConfig(project);
        }

        public static string GetCurrentProjectPath() {
            return Application.dataPath.Replace("/Assets", "");
        }

        public static Project AddExistingClone() {
            var path = EditorUtility.OpenFilePanel("Add Existing Clone", GetCurrentProjectPath(), "clone");
            if ( !File.Exists(path) ) return null;
            var json = File.ReadAllText(path);
            return JsonUtility.FromJson<Project>(json);
        }

        public static Project CreateCloneFromCurrent(Project sourceProject) {
            var parentProject = new DirectoryInfo(sourceProject.ProjectPath);
            var name = parentProject.Name + $"_Clone{sourceProject.ClonedCount() + 1}";
            var clonePath = EditorUtility.SaveFilePanel("Create Clone", parentProject.Parent.FullName, name, "");
            if ( string.IsNullOrEmpty(clonePath) ) return null;
            
            var cloneProject = CreateCloneFromPath(sourceProject.ProjectPath, clonePath);
            sourceProject.AddCloneProject(cloneProject);
            ReSaveConfig(sourceProject);
            return cloneProject;
        }

        public static Project CreateCloneFromPath(string sourcePath, string clonePath) {
            Project cloneProject = new Project(sourcePath, clonePath);

            Directory.CreateDirectory(cloneProject.ProjectPath);

            DirectoryInfo directory = new DirectoryInfo(sourcePath);
            // foreach ( var info in directory.GetFiles() ) {
            //     var linkFile = Path.Combine(clonePath, info.Name);
            //     if ( CloneConfig.CopyPaths.Contains(info.Name) ) CopyDirectoryWithProgressBar(info.FullName, linkFile);
            //     else LinkFolders(info.FullName, linkFile);
            // }

            foreach ( var info in directory.GetDirectories() ) {
                var linkDir = Path.Combine(clonePath, info.Name);
                if ( CloneConfig.CopyPaths.Contains(info.Name) ) CopyDirectoryWithProgressBar(info.FullName, linkDir);
                else LinkFolders(info.FullName, linkDir);
            }

            ReSaveConfig(cloneProject);

            return cloneProject;
        }
        
        public static void CopyDirectoryWithProgressBar(string sourcePath, string destinationPath, string progressBarPrefix = "")
        {
            var source = new DirectoryInfo(sourcePath);
            var destination = new DirectoryInfo(destinationPath);

            long totalBytes = 0;
            long copiedBytes = 0;

            CopyDirectoryWithProgressBarRecursive(source, destination, ref totalBytes, ref copiedBytes, progressBarPrefix);
            EditorUtility.ClearProgressBar();
        }

        private static void CopyDirectoryWithProgressBarRecursive(DirectoryInfo source, DirectoryInfo destination,
                                                                  ref long totalBytes, ref long copiedBytes,
                                                                  string progressBarPrefix = "") {
            if ( source.FullName.ToLower() == destination.FullName.ToLower() ) {
                UnityEngine.Debug.LogError("Source Path And Destination Path Is Same!");
                return;
            }

            if ( totalBytes == 0 ) {
                totalBytes = GetDirectorySize(source, true, progressBarPrefix);
            }

            if ( !Directory.Exists(destination.FullName) ) {
                Directory.CreateDirectory(destination.FullName);
            }

            foreach ( FileInfo file in source.GetFiles() ) {
                try {
                    file.CopyTo(Path.Combine(destination.ToString(), file.Name), true);
                }
                catch ( IOException ) {
                }

                copiedBytes += file.Length;

                float progress = (float)copiedBytes / totalBytes;
                bool cancelCopy = EditorUtility.DisplayCancelableProgressBar($"{progressBarPrefix} Clone:{source.FullName} To {destination.FullName}...", $"({progress * 100f:F2}%) Copying {file.Name}", progress);
                if ( cancelCopy ) return;
            }

            foreach ( DirectoryInfo info in source.GetDirectories() ) {
                DirectoryInfo directory = destination.CreateSubdirectory(info.Name);
                CopyDirectoryWithProgressBarRecursive(info, directory, ref totalBytes, ref copiedBytes, progressBarPrefix);
            }
        }

        private static long GetDirectorySize(DirectoryInfo directory, bool includeNested = false, string progressBarPrefix = "") {
            EditorUtility.DisplayProgressBar(progressBarPrefix + "Calculating size of directories...", "Scanning: " + directory.FullName , 0f);

            long filesSize = directory.GetFiles().Sum(file => file.Length);

            if ( !includeNested ) return filesSize;
            
            long directoriesSize = 0;
            foreach ( DirectoryInfo info in directory.GetDirectories() ) {
                directoriesSize += GetDirectorySize(info, true, progressBarPrefix);
            }

            return filesSize + directoriesSize;
        }

        public static void LinkFolders(string sourcePath, string destinationPath) {
            if ( !Directory.Exists(sourcePath) || Directory.Exists(destinationPath) ) {
                UnityEngine.Debug.LogWarning("Check SourcePath Is No Exists, Or DestinationPath Is Exists");
                return;
            }
            switch ( Application.platform ) {
                case RuntimePlatform.WindowsEditor:
                    CreateLinkWin(sourcePath, destinationPath);
                    break;
                case RuntimePlatform.OSXEditor:
                    CreateLinkMac(sourcePath, destinationPath);
                    break;
                default:
                    UnityEngine.Debug.LogWarning("Not in a known editor. Application.platform: " + Application.platform);
                    break;
            }
        }
        
        private static void CreateLinkWin(string sourcePath, string destinationPath)
        {
            string cmd = $"/C mklink /J \"{destinationPath}\" \"{sourcePath}\"";
            StartHiddenConsoleProcess("cmd.exe", cmd);
        }
        
        private static void CreateLinkMac(string sourcePath, string destinationPath)
        {
            sourcePath = sourcePath.Replace(" ", "\\ ");
            destinationPath = destinationPath.Replace(" ", "\\ ");
            var command = $"ln -s {sourcePath} {destinationPath}";
            ExecuteBashCommand(command);
        }

        private static void ExecuteBashCommand(string command) {
            command = command.Replace("\"", "\"\"");

            var proc = new System.Diagnostics.Process {
                                 StartInfo = new System.Diagnostics.ProcessStartInfo {
                                                 FileName = "/bin/bash",
                                                 Arguments = "-c \"" + command + "\"",
                                                 UseShellExecute = false,
                                                 RedirectStandardOutput = true,
                                                 RedirectStandardError = true,
                                                 CreateNoWindow = true
                                             }
                             };

            using ( proc ) {
                proc.Start();
                proc.WaitForExit();

                if ( !proc.StandardError.EndOfStream ) {
                    UnityEngine.Debug.LogError(proc.StandardError.ReadToEnd());
                }
            }
        }

        /// <summary>
        /// Registers a clone by placing an identifying ".clone" file in its root directory.
        /// </summary>
        /// <param name="cloneProject"></param>
        public static void ReSaveConfig(Project cloneProject) {
            string configFile = Path.Combine(cloneProject.ProjectPath, CloneConfig.ConfigPath);
            File.WriteAllText(configFile, JsonUtility.ToJson(cloneProject));
        }
        
        public static bool CheckProjectIsOpen(string projectPath) {
            return File.Exists(Path.Combine(projectPath, CloneConfig.LockFilePath));
        }

        public static void OpenProjectInFileExplorer(string projectPath) {
            System.Diagnostics.Process.Start(projectPath);
        }

        public static void OpenProject(string projectPath) {
            if ( !Directory.Exists(projectPath) ) {
                UnityEngine.Debug.LogError("Path Is No Exists: "+ projectPath);
                return;
            }

            if ( CheckProjectIsOpen(projectPath) ) {
                UnityEngine.Debug.LogError("Project Is Opening: "+ projectPath);
                return;
            }

            string fileName = GetApplicationPath();
            string args = "-projectPath \"" + projectPath + "\"";
            StartHiddenConsoleProcess(fileName, args);
        }

        private static string GetApplicationPath() {
            switch ( Application.platform ) {
                case RuntimePlatform.WindowsEditor: return EditorApplication.applicationPath;
                case RuntimePlatform.OSXEditor: return EditorApplication.applicationPath + "/Contents/MacOS/Unity";
                default: throw new System.NotImplementedException("Platform has not supported yet ;(");
            }
        }

        private static void StartHiddenConsoleProcess(string fileName, string args) {
            Debug.Log("Process.Start: " + fileName + " -> " +args);
            System.Diagnostics.Process.Start(fileName, args);
        }
        
        public static void DeleteClone(string projectPath) {
            if ( string.IsNullOrEmpty(projectPath) ) return;

            File.Delete(Path.Combine(projectPath, CloneConfig.ConfigPath));

            switch ( Application.platform ) {
                case RuntimePlatform.WindowsEditor:
                    string args = $"/c rmdir /s/q \"{projectPath}\"";
                    StartHiddenConsoleProcess("cmd.exe", args);
                    UnityEngine.Debug.Log("Attempting to delete folder \"" + projectPath + "\"");
                    break;
                case RuntimePlatform.OSXEditor:
                    FileUtil.DeleteFileOrDirectory(projectPath);
                    UnityEngine.Debug.Log("Attempting to delete folder \"" + projectPath + "\"");
                    break;
                default:
                    UnityEngine.Debug.LogWarning("Not in a known editor. Where are you!?");
                    break;
            }
        }
    }
}