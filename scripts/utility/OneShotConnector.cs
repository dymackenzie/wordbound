using Godot;
using System;

/// <summary>
/// Small helper node that connects to a signal once, invokes a provided callback,
/// then disconnects and frees itself. Use OneShotConnector.ConnectOnce(...) to
/// attach without polluting caller code with try/catch connect/disconnect.
/// </summary>
public partial class OneShotConnector : Node
{
    private Node _source;
    private string _signalName;
    private Node _callbackTarget;
    private string _callbackMethod;

    /// <summary>
    /// Connects `callback` to `source`'s `signalName` exactly once. The connector
    /// node will be added as a child of the source and automatically freed after
    /// the first signal emission.
    /// </summary>
    public static void ConnectOnce(Node source, string signalName, Node callbackTarget, string callbackMethod)
    {
        if (source == null || string.IsNullOrEmpty(signalName) || callbackTarget == null || string.IsNullOrEmpty(callbackMethod))
            return;

        var conn = new OneShotConnector();
        conn._source = source;
        conn._signalName = signalName;
        conn._callbackTarget = callbackTarget;
        conn._callbackMethod = callbackMethod;

        try { source.AddChild(conn); } catch { }
        try { source.Connect(signalName, new Callable(conn, nameof(OnSignal))); } catch { }
    }

    /// <summary>
    /// Internal signal handler invoked by Godot when the connected signal fires.
    /// We forward the arguments to the user callback, then disconnect and free.
    /// </summary>
    public void OnSignal(params object[] args)
    {
        try
        {
            // convert to Godot array for Callable.Call
            // forward the arguments to the provided target.method
            try
            {
                if (_callbackTarget != null && !string.IsNullOrEmpty(_callbackMethod))
                {
                    try
                    {
                        // try direct reflection invoke for C# methods (avoids Variant conversion issues)
                        var mi = _callbackTarget.GetType().GetMethod(_callbackMethod, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        if (mi != null)
                        {
                            mi.Invoke(_callbackTarget, args);
                        }
                        else
                        {
                            // fallback to Godot's Call without args (safe fallback)
                            _callbackTarget.Call(_callbackMethod);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
        catch { }

        try { _source.Disconnect(_signalName, new Callable(this, nameof(OnSignal))); } catch { }
        QueueFree();
    }
}
