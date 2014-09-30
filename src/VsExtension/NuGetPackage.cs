#if VS11 || VS10
extern alias dialog10;
extern alias dialog11;
#endif

#if VS12
extern alias dialog12;
#endif

#if VS14
extern alias dialog14;
#endif

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.Options;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Resources;
using NuGet.VisualStudio11;

using NuGetConsole;
using NuGet.Client;
using NuGetConsole.Implementation;
using NuGet.Client.VisualStudio;
using NuGet.Client.VisualStudio.UI;

#if VS11 || VS10
using VS10ManagePackageDialog = dialog10::NuGet.Dialog.PackageManagerWindow;
using VS11ManagePackageDialog = dialog11::NuGet.Dialog.PackageManagerWindow;
#endif

#if VS12
using VS12ManagePackageDialog = dialog12::NuGet.Dialog.PackageManagerWindow;
#endif

#if VS14
using VS14ManagePackageDialog = dialog14::NuGet.Dialog.PackageManagerWindow;
#endif

namespace NuGet.Tools
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", NuGetPackage.ProductVersion, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(PowerConsoleToolWindow),
        Style = VsDockStyle.Tabbed,
        Window = "{34E76E81-EE4A-11D0-AE2E-00A0C90FFFC3}",      // this is the guid of the Output tool window, which is present in both VS and VWD
        Orientation = ToolWindowOrientation.Right)]
    [ProvideToolWindow(typeof(DebugConsoleToolWindow),
        Style = VsDockStyle.Tabbed,
        Window = "{34E76E81-EE4A-11D0-AE2E-00A0C90FFFC3}",      // this is the guid of the Output tool window, which is present in both VS and VWD
        Orientation = ToolWindowOrientation.Right)]
    [ProvideOptionPage(typeof(PackageSourceOptionsPage), "NuGet Package Manager", "Package Sources", 113, 114, true)]
    [ProvideOptionPage(typeof(GeneralOptionPage), "NuGet Package Manager", "General", 113, 115, true)]
    [ProvideSearchProvider(typeof(NuGetSearchProvider), "NuGet Search")]
    [ProvideBindingPath] // Definition dll needs to be on VS binding path
    [ProvideAutoLoad(GuidList.guidAutoLoadNuGetString)]
    [FontAndColorsRegistration(
        "Package Manager Console",
        NuGetConsole.Implementation.GuidList.GuidPackageManagerConsoleFontAndColorCategoryString,
        "{" + GuidList.guidNuGetPkgString + "}")]
    [ProvideEditorExtension(typeof(PackageManagerEditorFactory), ".PackageManagerDocumentWindow", 50,
              ProjectGuid = "{D1DCDB85-C5E8-11d2-BFCA-00C04F990235}",
              TemplateDir = "Templates",
              NameResourceID = 300,
              DefaultName = "PackageManagerDocumentWindow")]
    [Guid(GuidList.guidNuGetPkgString)]
    public sealed class NuGetPackage : Package, IVsPackageExtensionProvider
    {
        // This product version will be updated by the build script to match the daily build version.
        // It is displayed in the Help - About box of Visual Studio
        public const string ProductVersion = "2.8.0.0";
        private static readonly string[] _visualizerSupportedSKUs = new[] { "Premium", "Ultimate" };

        private uint _solutionNotBuildingAndNotDebuggingContextCookie;
        private DTE _dte;
        private DTEEvents _dteEvents;
        private IConsoleStatus _consoleStatus;
        private IVsMonitorSelection _vsMonitorSelection;
        private bool? _isVisualizerSupported;
        private IPackageRestoreManager _packageRestoreManager;
        private ISolutionManager _solutionManager;
        private IDeleteOnRestartManager _deleteOnRestart;
        private OleMenuCommand _managePackageDialogCommand;
        private OleMenuCommand _managePackageForSolutionDialogCommand;
        private OleMenuCommandService _mcs;
        private bool _powerConsoleCommandExecuting;
        private IMachineWideSettings _machineWideSettings;
        private IShimControllerProvider _shimControllerProvider;

        private Dictionary<Project, int> _projectToToolWindowId;

        public NuGetPackage()
        {
            ServiceLocator.InitializePackageServiceProvider(this);
            _projectToToolWindowId = new Dictionary<Project, int>();
        }

        private IVsMonitorSelection VsMonitorSelection
        {
            get
            {
                if (_vsMonitorSelection == null)
                {
                    // get the UI context cookie for the debugging mode
                    _vsMonitorSelection = (IVsMonitorSelection)GetService(typeof(IVsMonitorSelection));

                    // get the solution not building and not debugging cookie
                    Guid guid = VSConstants.UICONTEXT.SolutionExistsAndNotBuildingAndNotDebugging_guid;
                    _vsMonitorSelection.GetCmdUIContextCookie(ref guid, out _solutionNotBuildingAndNotDebuggingContextCookie);
                }
                return _vsMonitorSelection;
            }
        }

        private IConsoleStatus ConsoleStatus
        {
            get
            {
                if (_consoleStatus == null)
                {
                    _consoleStatus = ServiceLocator.GetInstance<IConsoleStatus>();
                    Debug.Assert(_consoleStatus != null);
                }

                return _consoleStatus;
            }
        }

        private IPackageRestoreManager PackageRestoreManager
        {
            get
            {
                if (_packageRestoreManager == null)
                {
                    _packageRestoreManager = ServiceLocator.GetInstance<IPackageRestoreManager>();
                    Debug.Assert(_packageRestoreManager != null);
                }
                return _packageRestoreManager;
            }
        }

        private IShimControllerProvider ShimControllerProvider
        {
            get
            {
                if (_shimControllerProvider == null)
                {
                    _shimControllerProvider = ServiceLocator.GetInstance<IShimControllerProvider>();
                    Debug.Assert(_shimControllerProvider != null);
                }

                return _shimControllerProvider;
            }
        }

        private IVsPackageSourceProvider VsPackageSourceProvider
        {
            get
            {
                var vsPackageSource = ServiceLocator.GetInstance<IVsPackageSourceProvider>();
                Debug.Assert(vsPackageSource != null);

                return vsPackageSource;
            }
        }

        private ISolutionManager SolutionManager
        {
            get
            {
                if (_solutionManager == null)
                {
                    _solutionManager = ServiceLocator.GetInstance<ISolutionManager>();
                    Debug.Assert(_solutionManager != null);
                }
                return _solutionManager;
            }
        }

        private IDeleteOnRestartManager DeleteOnRestart
        {
            get
            {
                if (_deleteOnRestart == null)
                {
                    _deleteOnRestart = ServiceLocator.GetInstance<IDeleteOnRestartManager>();
                    Debug.Assert(_deleteOnRestart != null);
                }

                return _deleteOnRestart;
            }
        }

        private IMachineWideSettings MachineWideSettings
        {
            get
            {
                if (_machineWideSettings == null)
                {
                    _machineWideSettings = ServiceLocator.GetInstance<IMachineWideSettings>();
                    Debug.Assert(_machineWideSettings != null);
                }

                return _machineWideSettings;
            }
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            VsNuGetDiagnostics.Initialize(
                ServiceLocator.GetInstance<IDebugConsoleController>());

            // Add our command handlers for menu (commands must exist in the .vsct file)
            AddMenuCommandHandlers();

            //Create Editor Factory. Note that the base Package class will call Dispose on it.
            base.RegisterEditorFactory(new PackageManagerEditorFactory(
                ServiceLocator.GetInstance<SourceRepositoryManager>(),
                ServiceLocator.GetInstance<IUserInterfaceService>()));

            // IMPORTANT: Do NOT do anything that can lead to a call to ServiceLocator.GetGlobalService(). 
            // Doing so is illegal and may cause VS to hang.

            _dte = (DTE)GetService(typeof(SDTE));
            Debug.Assert(_dte != null);

            _dteEvents = _dte.Events.DTEEvents;
            _dteEvents.OnBeginShutdown += OnBeginShutDown;

            // set default credential provider for the HttpClient
            var webProxy = (IVsWebProxy)GetService(typeof(SVsWebProxy));
            Debug.Assert(webProxy != null);

            var settings = Settings.LoadDefaultSettings(
                _solutionManager == null ? null : _solutionManager.SolutionFileSystem,
                configFileName: null,
                machineWideSettings: MachineWideSettings);
            var packageSourceProvider = new PackageSourceProvider(settings);
            HttpClient.DefaultCredentialProvider = new SettingsCredentialProvider(new VSRequestCredentialProvider(webProxy), packageSourceProvider);

            // Add the v3 shim client
            if (ShimControllerProvider != null)
            {
                ShimControllerProvider.Controller.Enable(VsPackageSourceProvider);
            }

            // when NuGet loads, if the current solution has package 
            // restore mode enabled, we make sure every thing is set up correctly.
            // For example, projects which were added outside of VS need to have
            // the <Import> element added.
            if (PackageRestoreManager.IsCurrentSolutionEnabledForRestore)
            {
                if (VsVersionHelper.IsVisualStudio2013)
                {
                    // Run on a background thread in VS2013 to avoid CPS hangs. The modal loading dialog will block 
                    // until this completes.
                    ThreadPool.QueueUserWorkItem(new WaitCallback((obj) =>
                    PackageRestoreManager.EnableCurrentSolutionForRestore(fromActivation: false)));
                }
                else
                {
                    PackageRestoreManager.EnableCurrentSolutionForRestore(fromActivation: false);
                }
            }

            // when NuGet loads, if the current solution has some package 
            // folders marked for deletion (because a previous uninstalltion didn't succeed),
            // delete them now.
            if (SolutionManager.IsSolutionOpen)
            {
                DeleteOnRestart.DeleteMarkedPackageDirectories();
            }
        }

        private void AddMenuCommandHandlers()
        {
            _mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != _mcs)
            {
                // menu command for opening Package Manager Console
                CommandID toolwndCommandID = new CommandID(GuidList.guidNuGetConsoleCmdSet, PkgCmdIDList.cmdidPowerConsole);
                OleMenuCommand powerConsoleExecuteCommand = new OleMenuCommand(ExecutePowerConsoleCommand, null, BeforeQueryStatusForPowerConsole, toolwndCommandID);
                // '$' - This indicates that the input line other than the argument forms a single argument string with no autocompletion
                //       Autocompletion for filename(s) is supported for option 'p' or 'd' which is not applicable for this command
                powerConsoleExecuteCommand.ParametersDescription = "$";
                _mcs.AddCommand(powerConsoleExecuteCommand);

                // menu command for opening NuGet Debug Console
                CommandID debugWndCommandID = new CommandID(GuidList.guidNuGetDebugConsoleCmdSet, PkgCmdIDList.cmdidDebugConsole);
                OleMenuCommand debugConsoleExecuteCommand = new OleMenuCommand(ShowDebugConsole, null, null, debugWndCommandID);
                // '$' - This indicates that the input line other than the argument forms a single argument string with no autocompletion
                //       Autocompletion for filename(s) is supported for option 'p' or 'd' which is not applicable for this command
                debugConsoleExecuteCommand.ParametersDescription = "$";
                _mcs.AddCommand(debugConsoleExecuteCommand);

                // menu command for opening Manage NuGet packages dialog
                CommandID managePackageDialogCommandID = new CommandID(GuidList.guidNuGetDialogCmdSet, PkgCmdIDList.cmdidAddPackageDialog);
                _managePackageDialogCommand = new OleMenuCommand(ShowManageLibraryPackageDialog, null, BeforeQueryStatusForAddPackageDialog, managePackageDialogCommandID);
                // '$' - This indicates that the input line other than the argument forms a single argument string with no autocompletion
                //       Autocompletion for filename(s) is supported for option 'p' or 'd' which is not applicable for this command
                _managePackageDialogCommand.ParametersDescription = "$";
                _mcs.AddCommand(_managePackageDialogCommand);

                // menu command for opening "Manage NuGet packages for solution" dialog
                CommandID managePackageForSolutionDialogCommandID = new CommandID(GuidList.guidNuGetDialogCmdSet, PkgCmdIDList.cmdidAddPackageDialogForSolution);
                _managePackageForSolutionDialogCommand = new OleMenuCommand(ShowManageLibraryPackageForSolutionDialog, null, BeforeQueryStatusForAddPackageForSolutionDialog, managePackageForSolutionDialogCommandID);
                // '$' - This indicates that the input line other than the argument forms a single argument string with no autocompletion
                //       Autocompletion for filename(s) is supported for option 'p' or 'd' which is not applicable for this command
                _managePackageForSolutionDialogCommand.ParametersDescription = "$";
                _mcs.AddCommand(_managePackageForSolutionDialogCommand);

                // menu command for opening Package Source settings options page
                CommandID settingsCommandID = new CommandID(GuidList.guidNuGetConsoleCmdSet, PkgCmdIDList.cmdidSourceSettings);
                OleMenuCommand settingsMenuCommand = new OleMenuCommand(ShowPackageSourcesOptionPage, settingsCommandID);
                _mcs.AddCommand(settingsMenuCommand);

                // menu command for opening General options page
                CommandID generalSettingsCommandID = new CommandID(GuidList.guidNuGetToolsGroupCmdSet, PkgCmdIDList.cmdIdGeneralSettings);
                OleMenuCommand generalSettingsCommand = new OleMenuCommand(ShowGeneralSettingsOptionPage, generalSettingsCommandID);
                _mcs.AddCommand(generalSettingsCommand);

                // menu command for Package Visualizer
                CommandID visualizerCommandID = new CommandID(GuidList.guidNuGetToolsGroupCmdSet, PkgCmdIDList.cmdIdVisualizer);
                OleMenuCommand visualizerCommand = new OleMenuCommand(ExecuteVisualizer, null, QueryStatusForVisualizer, visualizerCommandID);
                _mcs.AddCommand(visualizerCommand);
            }
        }

        private void ExecutePowerConsoleCommand(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.FindToolWindow(typeof(PowerConsoleToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());

            // Parse the arguments to determine the command and arguments to be passed to IHost
            // passed which is of type OleMenuCmdEventArgs
            string command = null;
            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;
            if (eventArgs != null && eventArgs.InValue != null)
            {
                command = eventArgs.InValue as string;
            }

            // If the command string is null or empty, simply launch the console and return
            if (!String.IsNullOrEmpty(command))
            {
                IPowerConsoleService powerConsoleService = (IPowerConsoleService)window;

                if (powerConsoleService.Execute(command, null))
                {
                    _powerConsoleCommandExecuting = true;
                    powerConsoleService.ExecuteEnd += PowerConsoleService_ExecuteEnd;
                }
            }
        }

        private void ShowDebugConsole(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.FindToolWindow(typeof(DebugConsoleToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        private void PowerConsoleService_ExecuteEnd(object sender, EventArgs e)
        {
            _powerConsoleCommandExecuting = false;
        }

        /// <summary>
        /// Executes the NuGet Visualizer.
        /// </summary>
        private void ExecuteVisualizer(object sender, EventArgs e)
        {
            var visualizer = new NuGet.Dialog.Visualizer(
                ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                ServiceLocator.GetInstance<ISolutionManager>());
            string outputFile = visualizer.CreateGraph();
            _dte.ItemOperations.OpenFile(outputFile);
        }

        IEnumerable<IVsWindowFrame> EnumDocumentWindows(IVsUIShell uiShell)
        {
            IEnumWindowFrames ppenum;
            int hr = uiShell.GetDocumentWindowEnum(out ppenum);
            if (ppenum == null)
            {
                yield break;
            }

            IVsWindowFrame[] windowFrames = new IVsWindowFrame[1];
            uint frameCount;
            while (ppenum.Next(1, windowFrames, out frameCount) == VSConstants.S_OK && 
                frameCount == 1)
            {
                yield return windowFrames[0];
            }
        }

        private IVsWindowFrame FindExistingWindowFrame(
            Project project)
        {
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            foreach (var windowFrame in EnumDocumentWindows(uiShell))
            {
                object property;
                int hr = windowFrame.GetProperty(
                    (int)__VSFPROPID.VSFPROPID_DocData,
                    out property);
                if (hr == VSConstants.S_OK && property is PackageManagerModel)
                {
                    // TODO: Find a cleaner way to do this.
                    var packageManagerDocData = (PackageManagerModel)property;
                    var target = packageManagerDocData.Target as VsProjectInstallationTarget;
                    if (target != null && target.Project == project)
                    {
                        return windowFrame;
                    }
                }
            }

            return null;
        }

        private void ShowDocWindow(Project project)
        {
            var windowFrame = FindExistingWindowFrame(project);
            if (windowFrame == null)
            {
                windowFrame = CreateNewWindowFrame(project);
            }

            if (windowFrame != null)
            {
                windowFrame.Show();
            }
        }

        private IVsWindowFrame CreateNewWindowFrame(Project project)
        {
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));

            var vsProject = project.ToVsHierarchy();
            uint windowFlags =
                (uint)_VSRDTFLAGS.RDT_DontAddToMRU |
                (uint)_VSRDTFLAGS.RDT_DontSaveAs;

            object firstChild;
            vsProject.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_FirstChild, out firstChild);

            var context = ServiceLocator.GetInstance<VsPackageManagerContext>();
            var myDoc = new PackageManagerModel(
                context.SourceManager, 
                context.CreateProjectInstallationTarget(project));
            var NewEditor = new PackageManagerWindowPane(myDoc, ServiceLocator.GetInstance<IUserInterfaceService>());
            var ppunkDocView = Marshal.GetIUnknownForObject(NewEditor);
            var ppunkDocData = Marshal.GetIUnknownForObject(myDoc);
            var guidEditorType = PackageManagerEditorFactory.EditorFactoryGuid;
            var guidCommandUI = Guid.Empty;
            var caption = "PackageManager";
            var documentName = String.Format("Package Manager: {0}", project.Name);
            IVsWindowFrame windowFrame;
            int hr = uiShell.CreateDocumentWindow(
                windowFlags,
                documentName,
                (IVsUIHierarchy)vsProject,
                (uint)VSConstants.VSITEMID.Root,
                ppunkDocView,
                ppunkDocData,
                ref guidEditorType,
                null,
                ref guidCommandUI,
                null,
                caption,
                string.Empty,
                null,
                out windowFrame);
            ErrorHandler.ThrowOnFailure(hr);
            return windowFrame;
        }

        private void ShowManageLibraryPackageDialog(object sender, EventArgs e)
        {
            Project project = VsMonitorSelection.GetActiveProject();
            if (project != null && !project.IsUnloaded() && project.IsSupported())
            {
                ShowDocWindow(project);
            }
            else
            {
                // show error message when no supported project is selected.
                string projectName = project != null ? project.Name : String.Empty;

                string errorMessage = String.IsNullOrEmpty(projectName)
                    ? Resources.NoProjectSelected
                    : String.Format(CultureInfo.CurrentCulture, VsResources.DTE_ProjectUnsupported, projectName);

                MessageHelper.ShowWarningMessage(errorMessage, Resources.ErrorDialogBoxTitle);
            }
        }

        private IVsWindowFrame FindExistingSolutionWindowFrame()
        {
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            foreach (var windowFrame in EnumDocumentWindows(uiShell))
            {
                object property;
                int hr = windowFrame.GetProperty(
                    (int)__VSFPROPID.VSFPROPID_DocData,
                    out property);
                if (hr == VSConstants.S_OK && property is PackageManagerModel)
                {
                    // TODO: Find a cleaner way to do this.
                    var packageManagerDocData = (PackageManagerModel)property;
                    var target = packageManagerDocData.Target as VsSolutionInstallationTarget;
                    if (target != null)
                    {
                        return windowFrame;
                    }
                }
            }

            return null;
        }

        private void ShowManageLibraryPackageForSolutionDialog(object sender, EventArgs e)
        {
            var windowFrame = FindExistingSolutionWindowFrame();
            if (windowFrame == null)
            {
                // Create the window frame
                //!!! Need to wait until solution is loaded
                IVsSolution solution = ServiceLocator.GetInstance<IVsSolution>();
                IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
                uint windowFlags =
                    (uint)_VSRDTFLAGS.RDT_DontAddToMRU |
                    (uint)_VSRDTFLAGS.RDT_DontSaveAs;

                _dte.Solution.GetName();
                var context = ServiceLocator.GetInstance<VsPackageManagerContext>();
                var myDoc = new PackageManagerModel(context.SourceManager, context.CreateSolutionInstallationTarget());
                var NewEditor = new PackageManagerWindowPane(myDoc, ServiceLocator.GetInstance<IUserInterfaceService>());
                var ppunkDocView = Marshal.GetIUnknownForObject(NewEditor);
                var ppunkDocData = Marshal.GetIUnknownForObject(myDoc);
                var guidEditorType = PackageManagerEditorFactory.EditorFactoryGuid;
                var guidCommandUI = Guid.Empty;
                var caption = "PackageManager";
                var documentName = String.Format("Package Manager: {0}", _dte.Solution.GetName());
                int hr = uiShell.CreateDocumentWindow(
                    windowFlags,
                    documentName,
                    (IVsUIHierarchy)solution,
                    (uint)VSConstants.VSITEMID.Root,
                    ppunkDocView,
                    ppunkDocData,
                    ref guidEditorType,
                    null,
                    ref guidCommandUI,
                    null,
                    caption,
                    string.Empty,
                    null,
                    out windowFrame);
                ErrorHandler.ThrowOnFailure(hr);
            }
            windowFrame.Show();

            /* +++
            string parameterString = null;
            OleMenuCmdEventArgs args = e as OleMenuCmdEventArgs;
            if (args != null)
            {
                parameterString = args.InValue as string;
            }
            ShowManageLibraryPackageDialog(null, parameterString); */
        }

        private static void ShowManageLibraryPackageDialog(Project project, string parameterString = null)
        {
            try
            {
                DialogWindow window;

#if VS10 || VS11
                if (VsVersionHelper.IsVisualStudio2010)
                {
                    window = GetVS10PackageManagerWindow(project, parameterString);
                }
                else 
                {
                    // VS 2012
                    window = GetVS11PackageManagerWindow(project, parameterString);
                }
#endif
                
#if VS12
				window = GetVS12PackageManagerWindow(project, parameterString);
#endif

#if VS14
				window = GetVS14PackageManagerWindow(project, parameterString);
#endif
                

                window.ShowModal();
            }
            catch (Exception exception)
            {
                MessageHelper.ShowErrorMessage(exception, Resources.ErrorDialogBoxTitle);
                ExceptionHelper.WriteToActivityLog(exception);
            }
        }

#if VS10 || VS11
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static DialogWindow GetVS10PackageManagerWindow(Project project, string parameterString)
        {
            return new VS10ManagePackageDialog(project, parameterString);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static DialogWindow GetVS11PackageManagerWindow(Project project, string parameterString)
        {
            return new VS11ManagePackageDialog(project, parameterString);
        }
#endif

#if VS12
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static DialogWindow GetVS12PackageManagerWindow(Project project, string parameterString)
        {
            return new VS12ManagePackageDialog(project, parameterString);
        }
#endif

#if VS14
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static DialogWindow GetVS14PackageManagerWindow(Project project, string parameterString)
        {
            return new VS14ManagePackageDialog(project, parameterString);
        }
#endif

        private void EnablePackagesRestore(object sender, EventArgs args)
        {
            if (VsVersionHelper.IsVisualStudio2013)
            {
                // This method is called by the UI thread when the user clicks the menu item. To avoid
                // hangs on CPS project systems this needs to be done on a background thread.
                ThreadPool.QueueUserWorkItem(new WaitCallback((obj) =>
                    _packageRestoreManager.EnableCurrentSolutionForRestore(fromActivation: true)));
            }
            else
            {
                _packageRestoreManager.EnableCurrentSolutionForRestore(fromActivation: true);
            }
        }

        private void QueryStatusEnablePackagesRestore(object sender, EventArgs args)
        {
            OleMenuCommand command = (OleMenuCommand)sender;
            command.Visible = SolutionManager.IsSolutionOpen && !PackageRestoreManager.IsCurrentSolutionEnabledForRestore;
            command.Enabled = !ConsoleStatus.IsBusy;
        }

        private void BeforeQueryStatusForPowerConsole(object sender, EventArgs args)
        {
            OleMenuCommand command = (OleMenuCommand)sender;
            command.Enabled = !ConsoleStatus.IsBusy && !_powerConsoleCommandExecuting;
        }

        private void BeforeQueryStatusForAddPackageDialog(object sender, EventArgs args)
        {
            OleMenuCommand command = (OleMenuCommand)sender;
            command.Visible = IsSolutionExistsAndNotDebuggingAndNotBuilding() && HasActiveLoadedSupportedProject;
            command.Enabled = !ConsoleStatus.IsBusy;
        }

        private void BeforeQueryStatusForAddPackageForSolutionDialog(object sender, EventArgs args)
        {
            OleMenuCommand command = (OleMenuCommand)sender;
            command.Visible = IsSolutionExistsAndNotDebuggingAndNotBuilding();
            // disable the dialog menu if the console is busy executing a command;
            command.Enabled = !ConsoleStatus.IsBusy;
        }

        private void QueryStatusForVisualizer(object sender, EventArgs args)
        {
            OleMenuCommand command = (OleMenuCommand)sender;
            command.Visible = SolutionManager.IsSolutionOpen && IsVisualizerSupported;
        }

        private bool IsSolutionExistsAndNotDebuggingAndNotBuilding()
        {
            int pfActive;
            int result = VsMonitorSelection.IsCmdUIContextActive(_solutionNotBuildingAndNotDebuggingContextCookie, out pfActive);
            return (result == VSConstants.S_OK && pfActive > 0);
        }

        private void ShowPackageSourcesOptionPage(object sender, EventArgs args)
        {
            ShowOptionPageSafe(typeof(PackageSourceOptionsPage));
        }

        private void ShowGeneralSettingsOptionPage(object sender, EventArgs args)
        {
            ShowOptionPageSafe(typeof(GeneralOptionPage));
        }

        private void ShowOptionPageSafe(Type optionPageType)
        {
            try
            {
                ShowOptionPage(optionPageType);
            }
            catch (Exception exception)
            {
                MessageHelper.ShowErrorMessage(exception, Resources.ErrorDialogBoxTitle);
                ExceptionHelper.WriteToActivityLog(exception);
            }
        }

        /// <summary>
        /// Gets whether the current IDE has an active, supported and non-unloaded project, which is a precondition for
        /// showing the Add Library Package Reference dialog
        /// </summary>
        private bool HasActiveLoadedSupportedProject
        {
            get
            {
                Project project = VsMonitorSelection.GetActiveProject();
                return project != null && !project.IsUnloaded() && project.IsSupported();
            }
        }

        private bool IsVisualizerSupported
        {
            get
            {
                if (_isVisualizerSupported == null)
                {
                    _isVisualizerSupported = _visualizerSupportedSKUs.Contains(_dte.Edition, StringComparer.OrdinalIgnoreCase);
                }
                return _isVisualizerSupported.Value;
            }
        }

        public dynamic CreateExtensionInstance(ref Guid extensionPoint, ref Guid instance)
        {
            if (instance == typeof(NuGetSearchProvider).GUID)
            {
                return new NuGetSearchProvider(_mcs, _managePackageDialogCommand, _managePackageForSolutionDialogCommand);
            }

            return null;
        }

        private void OnBeginShutDown()
        {
            _dteEvents.OnBeginShutdown -= OnBeginShutDown;
            _dteEvents = null;

            OptimizedZipPackage.PurgeCache();
        }
    }
}
