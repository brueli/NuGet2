﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18051
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace NuGet.VsEvents {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("NuGet.VsEvents.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error occurred while restoring NuGet packages: {0}..
        /// </summary>
        internal static string ErrorOccurredRestoringPackages {
            get {
                return ResourceManager.GetString("ErrorOccurredRestoringPackages", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to All packages are already installed and there is nothing to restore..
        /// </summary>
        internal static string NothingToRestore {
            get {
                return ResourceManager.GetString("NothingToRestore", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to One or more NuGet packages need to be restored but couldn&apos;t be because consent has not been granted. To give consent, open the Visual Studio Options dialog, click on the Package Manager node and check &apos;Allow NuGet to download missing packages during build.&apos; You can also give consent by setting the environment variable &apos;EnableNuGetPackageRestore&apos; to &apos;true&apos;. 
        ///
        ///Missing packages: {0}.
        /// </summary>
        internal static string PackageNotRestoredBecauseOfNoConsent {
            get {
                return ResourceManager.GetString("PackageNotRestoredBecauseOfNoConsent", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to NuGet package restore canceled..
        /// </summary>
        internal static string PackageRestoreCanceled {
            get {
                return ResourceManager.GetString("PackageRestoreCanceled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to NuGet Package {0} is restored..
        /// </summary>
        internal static string PackageRestored {
            get {
                return ResourceManager.GetString("PackageRestored", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to NuGet Package restore failed for project {0}: {1}..
        /// </summary>
        internal static string PackageRestoreFailedForProject {
            get {
                return ResourceManager.GetString("PackageRestoreFailedForProject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to NuGet Package restore failed for solution {0}: {1}..
        /// </summary>
        internal static string PackageRestoreFailedForSolution {
            get {
                return ResourceManager.GetString("PackageRestoreFailedForSolution", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to NuGet package restore finished..
        /// </summary>
        internal static string PackageRestoreFinished {
            get {
                return ResourceManager.GetString("PackageRestoreFinished", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to NuGet Package restored finished for project {0}..
        /// </summary>
        internal static string PackageRestoreFinishedForProject {
            get {
                return ResourceManager.GetString("PackageRestoreFinishedForProject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to NuGet Package restored finished for solution {0}..
        /// </summary>
        internal static string PackageRestoreFinishedForSolution {
            get {
                return ResourceManager.GetString("PackageRestoreFinishedForSolution", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to NuGet package restore failed..
        /// </summary>
        internal static string PackageRestoreFinishedWithError {
            get {
                return ResourceManager.GetString("PackageRestoreFinishedWithError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Restoring NuGet packages...
        ///To prevent NuGet from restoring packages during build, open the Visual Studio Options dialog, click on the Package Manager node and uncheck &apos;Allow NuGet to download missing packages during build.&apos;.
        /// </summary>
        internal static string PackageRestoreOptOutMessage {
            get {
                return ResourceManager.GetString("PackageRestoreOptOutMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to NuGet package restore started..
        /// </summary>
        internal static string PackageRestoreStarted {
            get {
                return ResourceManager.GetString("PackageRestoreStarted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Some NuGet packages were installed using a target framework different from the current target framework and may need to be reinstalled. For more information, visit http://docs.nuget.org/workflows/reinstalling-packages.  Packages affected: {0}.
        /// </summary>
        internal static string ProjectUpgradeAndRetargetErrorMessage {
            get {
                return ResourceManager.GetString("ProjectUpgradeAndRetargetErrorMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Restoring NuGet package {0}..
        /// </summary>
        internal static string RestoringPackage {
            get {
                return ResourceManager.GetString("RestoringPackage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Restoring NuGet packages....
        /// </summary>
        internal static string RestoringPackages {
            get {
                return ResourceManager.GetString("RestoringPackages", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Restoring NuGet packages for project {0}..
        /// </summary>
        internal static string RestoringPackagesForProject {
            get {
                return ResourceManager.GetString("RestoringPackagesForProject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Restoring NuGet packages for solution {0}..
        /// </summary>
        internal static string RestoringPackagesForSolution {
            get {
                return ResourceManager.GetString("RestoringPackagesForSolution", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Restoring NuGet packages listed in file {0}..
        /// </summary>
        internal static string RestoringPackagesListedInFile {
            get {
                return ResourceManager.GetString("RestoringPackagesListedInFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Skipping NuGet package {0} since it is already installed..
        /// </summary>
        internal static string SkippingInstalledPackage {
            get {
                return ResourceManager.GetString("SkippingInstalledPackage", resourceCulture);
            }
        }
    }
}
