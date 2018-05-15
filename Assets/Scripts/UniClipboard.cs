using UnityEngine;
#if UNITY_IOS
using System.Runtime.InteropServices;
#endif

public class UniClipboard
{
    static IBoard _board;

    static IBoard Board
    {
        get
        {
            if (_board == null)
            {
#if UNITY_ANDROID && !UNITY_STANDALONE
                _board = new AndroidBoard();
#elif UNITY_IOS && !UNITY_STANDALONE
                _board = new IOSBoard ();
#else
                _board = new EditorBoard();
#endif
            }
            return _board;
        }
    }

    public static void SetText(string str)
    {
        Board.SetText(str);
    }

    public static string GetText()
    {
        return Board.GetText();
    }
}

internal interface IBoard
{
    void SetText(string str);

    string GetText();
}

#if UNITY_ANDROID
internal class AndroidBoard : IBoard
{
    readonly AndroidJavaClass _cb = new AndroidJavaClass("jp.ne.donuts.uniclipboard.Clipboard");

    public void SetText(string str)
    {
        _cb.CallStatic("setText", str);
    }

    public string GetText()
    {
        return _cb.CallStatic<string>("getText");
    }
}
#endif

#if UNITY_IOS
internal class IOSBoard : IBoard
{
    [DllImport("__Internal")]
    static extern void SetText_ (string str);
    [DllImport("__Internal")]
    static extern string GetText_();

    public void SetText(string str)
    {
        if (Application.platform != RuntimePlatform.OSXEditor)
            SetText_(str);
    }

    public string GetText()
    {
        return GetText_();
    }
}
#endif

internal class EditorBoard : IBoard
{
    public void SetText(string str)
    {
        GUIUtility.systemCopyBuffer = str;
    }

    public string GetText()
    {
        return GUIUtility.systemCopyBuffer;
    }
}
