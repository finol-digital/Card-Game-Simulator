using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace UnityEditor.LinuxStandalone
{
    // ReSharper disable once UnusedType.Global
    public class MySysroot : Sysroot
    {
        public override string Name => "MySysroot";
        public override string HostPlatform => "linux";
        public override string HostArch => "x86_64";
        public override string TargetPlatform => "linux";
        public override string TargetArch => "x86_64";

        public override bool Initialize()
        {
            return true;
        }

        public override IEnumerable<string> GetIl2CppArguments()
        {
            yield return "--sysroot-path=/";
            yield return
                "--tool-chain-path=/opt/unity/Editor/Data/PlaybackEngines/LinuxStandaloneSupport/BuildTools/Emscripten_FastComp_Linux/clang";
        }
    }
}
