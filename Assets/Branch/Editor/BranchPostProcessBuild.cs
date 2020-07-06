#if UNITY_IOS

using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections;
using UnityEditor.iOS.Xcode;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

public class BranchPostProcessBuild {

	[PostProcessBuild(900)]
	public static void ChangeBranchBuiltProject(BuildTarget buildTarget, string pathToBuiltProject) {
		
		if ( buildTarget == BuildTarget.iOS ) {
			ChangeXcodePlist(pathToBuiltProject);
			ChangeXcodeProject(pathToBuiltProject);
            		ChangeEntitlements(pathToBuiltProject);
        	}
	}

	public static void ChangeXcodePlist(string pathToBuiltProject) {
		// Get plist
		string plistPath = pathToBuiltProject + "/Info.plist";
		PlistDocument plist = new PlistDocument();
		plist.ReadFromString(File.ReadAllText(plistPath));
		
		// Get root
		PlistElementDict rootDict = plist.root;
		PlistElementArray urlTypesArray = null;
		PlistElementDict  urlTypesItems = null;
		PlistElementArray urlSchemesArray = null;

		//----------------------------------------------------------------------------------
		// set branch uri
		if (!rootDict.values.ContainsKey("CFBundleURLTypes")) {
			urlTypesArray = rootDict.CreateArray("CFBundleURLTypes");
		}
		else {
			urlTypesArray = rootDict.values["CFBundleURLTypes"].AsArray();

			if (urlTypesArray == null) {
				urlTypesArray = rootDict.CreateArray("CFBundleURLTypes");
			}
		}

		if (urlTypesArray.values.Count == 0) {
			urlTypesItems = urlTypesArray.AddDict();
		}
		else {
			urlTypesItems = urlTypesArray.values[0].AsDict();

			if (urlTypesItems == null) {
				urlTypesItems = urlTypesArray.AddDict();
			}
		}

		if (!urlTypesItems.values.ContainsKey("CFBundleURLSchemes")) {
			urlSchemesArray = urlTypesItems.CreateArray("CFBundleURLSchemes");
		}
		else {
			urlSchemesArray = urlTypesItems.values["CFBundleURLSchemes"].AsArray();

			if (urlSchemesArray == null) {
				urlSchemesArray = urlTypesItems.CreateArray("CFBundleURLSchemes");
			}
		}

		// delete old URIs
		foreach(PlistElement elem in urlSchemesArray.values) {
			if (elem.AsString() != null && elem.AsString().Equals(BranchData.Instance.liveBranchUri)) {
				urlSchemesArray.values.Remove(elem);
				break;
			}
		}

		foreach(PlistElement elem in urlSchemesArray.values) {
			if (elem.AsString() != null && elem.AsString().Equals(BranchData.Instance.testBranchUri)) {
				urlSchemesArray.values.Remove(elem);
				break;
			}
		}

		// add new URI
		if (BranchData.Instance.testMode && !string.IsNullOrEmpty(BranchData.Instance.testBranchUri) ) {
			urlSchemesArray.AddString(BranchData.Instance.testBranchUri);
		}
		else if (!BranchData.Instance.testMode && !string.IsNullOrEmpty(BranchData.Instance.liveBranchUri)) {
			urlSchemesArray.AddString(BranchData.Instance.liveBranchUri);
		}

		// Write to file
		File.WriteAllText(plistPath, plist.WriteToString());
	}

    public static void ChangeEntitlements(string pathToBuiltProject)
    {
        //This is the default path to the default pbxproj file. Yours might be different
        string projectPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";
        //Default target name. Yours might be different
        string targetName = "Unity-iPhone";
        //Set the entitlements file name to what you want but make sure it has this extension
        string entitlementsFileName = "branch_domains.entitlements";

        var entitlements = new ProjectCapabilityManager(projectPath, entitlementsFileName, targetName);

        entitlements.AddAssociatedDomains(BuildEntitlements());
        //Apply
        entitlements.WriteToFile();
    }

    private static string[] BuildEntitlements()
    {
        var links = BranchData.Instance.liveAppLinks;
        if(BranchData.Instance.testMode)
            links = BranchData.Instance.testAppLinks;

        if (links == null)
            return null;

        string[] domains = new string[links.Length];
        for (int i = 0; i < links.Length; i++)
        {
            domains[i] = "applinks:" + links[i];
        }

        return domains;
       
    }

    public static void ChangeXcodeProject(string pathToBuiltProject) {
		// Get xcodeproj
		string pathToProject = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";
		string[] lines = File.ReadAllLines(pathToProject);

		// We'll open/replace project.pbxproj for writing and iterate over the old
		// file in memory, copying the original file and inserting every extra we need.
		// Create new file and open it for read and write, if the file exists overwrite it.
		FileStream fileProject = new FileStream(pathToProject, FileMode.Create);
		fileProject.Close();

		// Will be used for writing
		StreamWriter fCurrentXcodeProjFile = new StreamWriter(pathToProject) ;

		// Write all lines to new file and enable objective C exceptions
		foreach (string line in lines) {
			
			if (line.Contains("GCC_ENABLE_OBJC_EXCEPTIONS")) {
                fCurrentXcodeProjFile.Write("\t\t\t\tGCC_ENABLE_OBJC_EXCEPTIONS = YES;\n");
            }
            else if (line.Contains("GCC_ENABLE_CPP_EXCEPTIONS")) {
                fCurrentXcodeProjFile.Write("\t\t\t\tGCC_ENABLE_CPP_EXCEPTIONS = YES;\n");
            }
            else if (line.Contains("CLANG_ENABLE_MODULES")) {
				fCurrentXcodeProjFile.Write("\t\t\t\tCLANG_ENABLE_MODULES = YES;\n");
			}
			else {                          
				fCurrentXcodeProjFile.WriteLine(line);
			}
		}

        // Close file
        fCurrentXcodeProjFile.Close();

		// Add frameworks
		PBXProject proj = new PBXProject();
		proj.ReadFromString(File.ReadAllText(pathToProject));

		string target = "";
#if UNITY_2019_3_OR_NEWER
		target = proj.GetUnityFrameworkTargetGuid();
#else
		target = proj.TargetGuidByName("Unity-iPhone");
#endif


#if UNITY_2017_1_OR_NEWER

		if (!proj.ContainsFramework(target, "AdSupport.framework")) {
			proj.AddFrameworkToProject(target, "AdSupport.framework", false);
		}

		if (!proj.ContainsFramework(target, "CoreTelephony.framework")) {
			proj.AddFrameworkToProject(target, "CoreTelephony.framework", false);
		}

		if (!proj.ContainsFramework(target, "CoreSpotlight.framework")) {
			proj.AddFrameworkToProject(target, "CoreSpotlight.framework", false);
		}

		if (!proj.ContainsFramework(target, "Security.framework")) {
			proj.AddFrameworkToProject(target, "Security.framework", false);
		}

        if (!proj.ContainsFramework(target, "WebKit.framework")) {
            proj.AddFrameworkToProject(target, "WebKit.framework", false);
        }

#else

		if (!proj.HasFramework("AdSupport.framework")) {
			proj.AddFrameworkToProject(target, "AdSupport.framework", false);
		}

		if (!proj.HasFramework("CoreTelephony.framework")) {
			proj.AddFrameworkToProject(target, "CoreTelephony.framework", false);
		}

		if (!proj.HasFramework("CoreSpotlight.framework")) {
			proj.AddFrameworkToProject(target, "CoreSpotlight.framework", false);
		}

		if (!proj.HasFramework("Security.framework")) {
			proj.AddFrameworkToProject(target, "Security.framework", false);
		}

#endif

        File.WriteAllText(pathToProject, proj.WriteToString());
	}
}
#endif
