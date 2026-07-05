using COM;
using System.Runtime.InteropServices;
using WidgetsForUniGetUI;

try
{
    Logger.Log("Registering Widget Provider");
    Logger.Log($"Args: {string.Join(' ', args)}");
    Logger.Log($"Process path: {Environment.ProcessPath}");
    Logger.Log($"Current directory: {Environment.CurrentDirectory}");

    Guid clsidFactory = Guid.Parse("34D3940F-84D6-47C5-B446-32D6865D8852");
    int hresult = CoRegisterClassObject(clsidFactory, new WidgetProviderFactory<WidgetProvider>(), 0x4, 0x1, out uint cookie);
    Logger.Log($"CoRegisterClassObject returned 0x{hresult:X8}, cookie={cookie}");

    if (hresult != 0)
    {
        Logger.Log("COM registration failed; exiting.");
        return;
    }

    Logger.Log("Registered successfully.");

    if (GetConsoleWindow() != IntPtr.Zero && System.Diagnostics.Debugger.IsAttached)
    {
        Logger.Log("Debug console detected. Press ENTER to exit.");
        Console.ReadLine();
    }
    else
    {
        using ManualResetEvent emptyWidgetListEvent = WidgetProvider.GetEmptyWidgetListEvent();
        emptyWidgetListEvent.WaitOne();
    }

    CoRevokeClassObject(cookie);
    Logger.Log("Widget Provider stopped.");
}
catch (Exception ex)
{
    Logger.Log("Fatal error in widget provider startup:");
    Logger.Log(ex);
}

[DllImport("kernel32.dll")]
static extern IntPtr GetConsoleWindow();

[DllImport("ole32.dll")]
static extern int CoRegisterClassObject(
    [MarshalAs(UnmanagedType.LPStruct)] Guid rclsid,
    [MarshalAs(UnmanagedType.IUnknown)] object pUnk,
    uint dwClsContext,
    uint flags,
    out uint lpdwRegister);

[DllImport("ole32.dll")]
static extern int CoRevokeClassObject(uint dwRegister);
