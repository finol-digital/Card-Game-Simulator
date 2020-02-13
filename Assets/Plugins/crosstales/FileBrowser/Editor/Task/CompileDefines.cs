#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.FB.EditorTask
{
   /// <summary>Adds the given define symbols to PlayerSettings define symbols.</summary>
   [InitializeOnLoad]
   public class CompileDefines : Common.EditorTask.BaseCompileDefines
   {
      private static readonly string[] symbols = {"CT_FB", "CT_FB_PRO"};

      static CompileDefines()
      {
         addSymbolsToAllTargets(symbols);
      }
   }
}
#endif
// © 2017-2020 crosstales LLC (https://www.crosstales.com)