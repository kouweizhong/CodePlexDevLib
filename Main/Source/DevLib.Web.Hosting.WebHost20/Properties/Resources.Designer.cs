﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DevLib.Web.Hosting.WebHost20.Properties {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("DevLib.Web.Hosting.WebHost20.Properties.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to &lt;?xml version=&quot;1.0&quot; encoding=&quot;UTF-8&quot;?&gt;
        ///&lt;configuration&gt;
        ///  &lt;system.applicationHost&gt;
        ///    &lt;applicationPools&gt;
        ///      &lt;add name=&quot;$[AppPool]&quot; managedRuntimeVersion=&quot;v2.0&quot; managedPipelineMode=&apos;Integrated&apos; /&gt;
        ///    &lt;/applicationPools&gt;
        ///    &lt;sites&gt;
        ///      &lt;site name=&quot;$[SiteName]&quot; id=&quot;$[SiteId]&quot;&gt;
        ///        &lt;application path=&quot;/&quot; applicationPool=&quot;$[AppPool]&quot;&gt;
        ///          &lt;virtualDirectory path=&quot;$[VirtualPath]&quot; physicalPath=&quot;$[PhysicalPath]&quot; /&gt;
        ///        &lt;/application&gt;
        ///        &lt;bindings&gt;
        ///          &lt;binding protocol=&quot;htt [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string AppHostTemplate {
            get {
                return ResourceManager.GetString("AppHostTemplate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;?xml version=&quot;1.0&quot; encoding=&quot;UTF-8&quot;?&gt;
        ///&lt;configuration&gt;
        ///  &lt;configSections&gt;
        ///    &lt;sectionGroup name=&quot;system.applicationHost&quot;&gt;
        ///      &lt;section name=&quot;applicationPools&quot; allowDefinition=&quot;AppHostOnly&quot; overrideModeDefault=&quot;Deny&quot; /&gt;
        ///      &lt;section name=&quot;configHistory&quot; allowDefinition=&quot;AppHostOnly&quot; overrideModeDefault=&quot;Deny&quot; /&gt;
        ///      &lt;section name=&quot;customMetadata&quot; allowDefinition=&quot;AppHostOnly&quot; overrideModeDefault=&quot;Deny&quot; /&gt;
        ///      &lt;section name=&quot;listenerAdapters&quot; allowDefinition=&quot;AppHostOnly&quot; overrideModeDefault=&quot; [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string ApplicationHost {
            get {
                return ResourceManager.GetString("ApplicationHost", resourceCulture);
            }
        }
    }
}
