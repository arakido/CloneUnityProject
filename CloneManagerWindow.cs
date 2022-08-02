using System.IO;
using UnityEditor;
using UnityEngine;

namespace CloneProject {
    public class CloneManagerWindow : EditorWindow {
        private Vector2 scrollPos = Vector2.zero;
        
        private Project currentProject;
        private Project CurrentProject => currentProject ?? ( currentProject = CloneManager.GetCurrentProject() );
        
        [MenuItem("Tools/CloneManager", priority = 0)]
        private static void InitWindow() {
            CloneManagerWindow window = GetWindow<CloneManagerWindow>("CloneManager");
            window.minSize = new Vector2(400, 200);
            window.Show();
        }

        private void OnGUI() {
            if ( CurrentProject.IsClone ) {
                DrawIsDuplicateUI();
            }
            else {
                DrawCloneManagerUI();
            }
        }

        private void DrawIsDuplicateUI() {
            if ( string.IsNullOrEmpty(CurrentProject.SourcePath) ) {
                EditorGUILayout.HelpBox("Don't Find Source Path，Please Go to Source Project", MessageType.Warning);
            }
            else {
                EditorGUILayout.HelpBox($"This Project Is Copy Of The ‘{Path.GetFileName(CurrentProject.SourcePath)}’", MessageType.Info);
            }

            DrawArguments();
        }

        private void DrawArguments() {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Arguments:", GUILayout.Width(70));

            var newArgument = EditorGUILayout.TextArea(CurrentProject.Arguments, GUILayout.MaxWidth(300));
            if ( newArgument != CurrentProject.Arguments ) {
                CurrentProject.Arguments = newArgument;
                CloneManager.ReSaveConfig(CurrentProject);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawCloneManagerUI() {
            GUILayout.BeginVertical("HelpBox");

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            var cloneProjects = CurrentProject.Clones;
            var len = cloneProjects?.Length;
            for ( int i = 0; i < len; i++ ) {
                GUILayout.BeginVertical("GroupBox");
                var project = cloneProjects[i];

                bool isOpen = CloneManager.CheckProjectIsOpen(project.ProjectPath);

                if ( isOpen ) EditorGUILayout.LabelField(project.GetName() + " (Opening)", EditorStyles.boldLabel);
                else EditorGUILayout.LabelField(project.GetName());


                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Path:", GUILayout.Width(40));
                EditorGUILayout.LabelField(project.ProjectPath);
                if ( GUILayout.Button("Open In Explorer", GUILayout.Width(120)) ) {
                    CloneManager.OpenProjectInFileExplorer(project.ProjectPath);
                }

                GUILayout.EndHorizontal();

                DrawArguments();

                GUILayout.BeginHorizontal();
                
                EditorGUI.BeginDisabledGroup(isOpen);

                if ( GUILayout.Button("Open In Unity") ) {
                    CloneManager.OpenProject(project.ProjectPath);
                }
                
                if ( GUILayout.Button("Delete") ) {
                    bool delete = EditorUtility.DisplayDialog("！", "Do you want to delete this project？？？", "Delete", "Cancel");
                    if ( delete ) {
                        CloneManager.DeleteClone(project.ProjectPath);
                        CurrentProject.RemoveCloneProject(project);
                    }
                }

                GUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();
                GUILayout.EndVertical();

            }

            GUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            if ( GUILayout.Button("Add Existing Clone") ) {
                var project = CloneManager.AddExistingClone();
                if ( project != null ) {
                    CurrentProject.AddCloneProject(project);
                    CloneManager.ReSaveConfig(CurrentProject);
                }
            }
            if ( GUILayout.Button("Create New Clone") ) {
                CloneManager.CreateCloneFromCurrent(CurrentProject);
            }
            GUILayout.EndHorizontal();
        }
    }
}