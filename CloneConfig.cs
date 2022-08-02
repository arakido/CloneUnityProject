using System.IO;
using System.Linq;

namespace CloneProject {
    public class CloneConfig {
        public const string ConfigPath = ".clone";
        public const string DefaultArgument = "client";
        public static string LockFilePath = Path.Combine("Temp", "UnityLockfile");
        
        /// <summary>
        /// Is Clone Path, Other Path Be Link
        /// </summary>
        public static readonly string[] CopyPaths = { "Library", "Logs", "obj", "Temp", "UserSettings" };
    }
    
    [System.Serializable]
    public class Project {
        public bool IsClone;
        public string ProjectPath;
        public string SourcePath;

        public string Arguments = CloneConfig.DefaultArgument;
        public Project[] Clones = System.Array.Empty<Project>();

        private string name;

        public Project(string path) {
            ProjectPath = path;
        }

        public Project(string sourcePath, string path) {
            IsClone = true;
            SourcePath = sourcePath;
            ProjectPath = path;
        }

        public string GetName() {
            if ( string.IsNullOrEmpty(name) ) {
                int index = ProjectPath.LastIndexOf('/');
                name = ProjectPath.Substring(index + 1);
            }
            return name;
        }

        public void AddCloneProject(Project project) {
            if ( Clones == null ) Clones = new[] { project };
            else Clones = Clones.Append(project).ToArray();
        }

        public void RemoveCloneProject(Project project) {
            if ( Clones == null ) return;
            Clones = Clones.Where(p => p != project).ToArray();
        }

        public string GetOriginalProjectPath() {
            return SourcePath;
        }

        public int ClonedCount() {
            if ( Clones == null ) return 0;
            return Clones.Length;
        }
    }
}