#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.IO;

namespace NOVA.Scripts
{
    /// <summary>
    /// This class is used to install the pre-commit hook for the project.
    /// </summary>
    [InitializeOnLoad]
    public class InstallPreCommitHook : MonoBehaviour
    {
        /// <summary>
        /// This is used to check if the project is opened for the first time. 
        /// </summary>
        private const string ProjectOpened = "ProjectOpened";

        /// <summary>
        /// This is the path to the pre-commit hook in the .git/hooks directory.
        /// </summary>
        private const string PreCommitHookPath = ".git/hooks/pre-commit";

        /// <summary>
        /// This is the path to the pre-commit hook in the project.
        /// </summary>
        private const string ProjectPreCommitPath = "pre-commit";

        /// <summary>
        /// This is a constructor that installs the pre-commit hook for the project.
        /// </summary>
        static InstallPreCommitHook()
        {
            // Check if the project is opened for the first time and in editor mode
            if (!SessionState.GetBool(ProjectOpened, false) && EditorApplication.isPlayingOrWillChangePlaymode == false)
            {
                SessionState.SetBool(ProjectOpened, true);

                // Get the necessary paths
                var projectPath = Path.Combine(Application.dataPath, "..");
                var preCommitPath = Path.Combine(projectPath, PreCommitHookPath);
                var projectPreCommitPath = Path.Combine(projectPath, ProjectPreCommitPath);

                // Check if the pre-commit hook is already installed
                if (File.Exists(preCommitPath))
                {
                    Debug.Log("Pre-commit hook is already installed...proceed with your development <3");
                    return;
                }

                // Install the hook
                try
                {
                    // Copy the pre-commit hook to the .git/hooks directory
                    File.Copy(projectPreCommitPath, preCommitPath);
                    Debug.Log("Pre-commit hook installed successfully!");
                }
                catch (IOException e)
                {
                    Debug.LogError($"Failed to install pre-commit hook: {e.Message}");
                }
            }
            else
            {
                Debug.Log("Pre-commit hook is already installed...proceed with your development <3");
            }
        }
    }
}

#endif
