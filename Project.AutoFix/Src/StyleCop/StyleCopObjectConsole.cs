//-----------------------------------------------------------------------
// <copyright file="StyleCopObjectConsole.cs">
//   MS-PL
// </copyright>
// <license>
//   This source code is subject to terms and conditions of the Microsoft 
//   Public License. A copy of the license can be found in the License.html 
//   file at the root of this distribution. 
//   By using this source code in any fashion, you are agreeing to be bound 
//   by the terms of the Microsoft Public License. You must not remove this 
//   notice, or any other, from this software.
// </license>
//-----------------------------------------------------------------------
namespace StyleCop
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Security;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// A lightweight StyleCop host which does not depend on the file system for loading source and settings files. Source files and settings
    /// files can be loaded from any arbitrary source (in memory, database, etc.).
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "StyleCop", Justification = "This is the correct casing.")]
    public class StyleCopObjectConsole : StyleCopRunner
    {
        #region Private Fields

        /// <summary>
        /// The default settings document.
        /// </summary>
        private Settings defaultSettings;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the StyleCopObjectConsole class.
        /// </summary>
        /// <param name="environment">The environment.</param>
        /// <param name="defaultSettings">The default settings to use, or null to allow each project to specify its own settings.</param>
        /// <param name="addInPaths">The list of paths to search under for parser and analyzer addins.
        /// Can be null if no addin paths are provided.</param>
        /// <param name="loadFromDefaultPath">Indicates whether to load addins
        /// from the default application path.</param>
        public StyleCopObjectConsole(
            ObjectBasedEnvironment environment,
            Settings defaultSettings,
            ICollection<string> addInPaths,
            bool loadFromDefaultPath)
            : this(environment, defaultSettings, addInPaths, loadFromDefaultPath, null)
        {
            Param.Ignore(environment);
            Param.Ignore(defaultSettings);
            Param.Ignore(addInPaths);
            Param.Ignore(loadFromDefaultPath);
        }

        /// <summary>
        /// Initializes a new instance of the StyleCopObjectConsole class.
        /// </summary>
        /// <param name="environment">The environment.</param>
        /// <param name="defaultSettings">The default settings to use, or null to allow each project to specify its own settings.</param>
        /// <param name="addInPaths">The list of paths to search under for parser and analyzer addins.
        /// Can be null if no addin paths are provided.</param>
        /// <param name="loadFromDefaultPath">Indicates whether to load addins
        /// from the default application path.</param>
        /// <param name="hostTag">An optional tag which can be set by the host.</param>
        public StyleCopObjectConsole(
            ObjectBasedEnvironment environment,
            Settings defaultSettings,
            ICollection<string> addInPaths,
            bool loadFromDefaultPath,
            object hostTag)
        {
            Param.RequireNotNull(environment, "environment");
            Param.Ignore(defaultSettings);
            Param.Ignore(addInPaths);
            Param.Ignore(loadFromDefaultPath);
            Param.Ignore(hostTag);

            this.Core = new StyleCopCore(environment, hostTag);
            this.Core.Initialize(addInPaths, loadFromDefaultPath);
            this.Core.WriteResultsCache = false;
            this.InitCore();

            this.defaultSettings = defaultSettings;
        }

        #endregion Public Constructors

        #region Public Methods

        /// <summary>
        /// Starts analyzing the source code documents contained within the given project.
        /// </summary>
        /// <param name="project">The project to analyze.</param>
        /// <returns>Returns false if an error occurs during analysis.</returns>
        public bool Start(CodeProject project)
        {
            Param.RequireNotNull(project, "project");
            return this.Start(new CodeProject[] { project });
        }

        /// <summary>
        /// Starts analyzing the source code documents contained within the given projects.
        /// </summary>
        /// <param name="projects">The projects to analyze.</param>
        /// <returns>Returns false if an error occurs during analysis.</returns>
        public bool Start(IList<CodeProject> projects)
        {
            Param.RequireNotNull(projects, "projects");

            bool error = false;

            try
            {
                // Load the settings for each project.
                this.LoadSettings(projects);

                // Reset the violation count.
                this.ViolationCount = 0;

                // Analyze the files.
                this.Core.FullAnalyze(projects);

                if (this.ViolationCount > 0)
                {
                    this.OnOutputGenerated(new OutputEventArgs(string.Format(CultureInfo.CurrentCulture, Strings.ViolationsEncountered, this.ViolationCount)));
                }
                else
                {
                    this.OnOutputGenerated(new OutputEventArgs(Strings.NoViolationsEncountered));
                }

                Exception exception;
                this.Core.Environment.SaveAnalysisResults(null, this.Violations, out exception);
                {
                    error = true;
                }
            }
            catch (IOException ioex)
            {
                this.OnOutputGenerated(new OutputEventArgs(string.Format(CultureInfo.CurrentCulture, Strings.AnalysisErrorOccurred, ioex.Message)));
                error = true;
            }
            catch (XmlException xmlex)
            {
                this.OnOutputGenerated(new OutputEventArgs(string.Format(CultureInfo.CurrentCulture, Strings.AnalysisErrorOccurred, xmlex.Message)));
                error = true;
            }
            catch (SecurityException secex)
            {
                this.OnOutputGenerated(new OutputEventArgs(string.Format(CultureInfo.CurrentCulture, Strings.AnalysisErrorOccurred, secex.Message)));
                error = true;
            }
            catch (UnauthorizedAccessException unauthex)
            {
                this.OnOutputGenerated(new OutputEventArgs(string.Format(CultureInfo.CurrentCulture, Strings.AnalysisErrorOccurred, unauthex.Message)));
                error = true;
            }

            return !error;
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Gets the settings for the given project.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns>Returns the settings, or null if there </returns>
        protected virtual Settings GetSettingsForProject(CodeProject project)
        {
            Param.Ignore(project);
            return null;
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Loads the settings files to use for the analysis.
        /// </summary>
        /// <param name="projects">The list of projects to use.</param>
        private void LoadSettings(IEnumerable<CodeProject> projects)
        {
            Param.AssertNotNull(projects, "projects");

            foreach (CodeProject project in projects)
            {
                Settings settingsToUse = this.defaultSettings;
                if (settingsToUse == null)
                {
                    settingsToUse = this.Core.Environment.GetProjectSettings(project, true);
                }

                if (settingsToUse != null)
                {
                    project.Settings = settingsToUse;
                    project.SettingsLoaded = true;
                }
            }
        }

        #endregion Private Methods
    }
}
