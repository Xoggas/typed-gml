namespace TypedGML.CLI;

internal sealed class WatchRunner(string tgmlRoot, BuildRunner buildRunner)
{
    private readonly Lock _lock = new();
    private readonly SemaphoreSlim _buildGate = new(1, 1);
    private CancellationTokenSource? _pendingChange;

    public async Task<int> RunAsync()
    {
        using var watcher = NewWatcher();
        var stopSignal = new TaskCompletionSource<int>();
        ConsoleCancelEventHandler handler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            stopSignal.TrySetResult(0);
        };

        Console.CancelKeyPress += handler;
        try
        {
            watcher.EnableRaisingEvents = true;
            return await stopSignal.Task;
        }
        finally
        {
            Console.CancelKeyPress -= handler;
            CancelPendingChange();
        }
    }

    private FileSystemWatcher NewWatcher()
    {
        var watcher = new FileSystemWatcher(tgmlRoot, "*.tgml")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.DirectoryName
        };

        watcher.Changed += (_, eventArgs) => QueueRebuild(eventArgs.FullPath);
        watcher.Created += (_, eventArgs) => QueueRebuild(eventArgs.FullPath);
        watcher.Deleted += (_, eventArgs) => QueueRebuild(eventArgs.FullPath);
        watcher.Renamed += (_, eventArgs) => QueueRebuild(eventArgs.FullPath);
        return watcher;
    }

    private void QueueRebuild(string path)
    {
        if (!path.EndsWith(".tgml", StringComparison.OrdinalIgnoreCase))
            return;

        CancellationToken token;
        lock (_lock)
        {
            _pendingChange?.Cancel();
            _pendingChange?.Dispose();
            _pendingChange = new CancellationTokenSource();
            token = _pendingChange.Token;
        }

        _ = RunDebouncedAsync(token);
    }

    private async Task RunDebouncedAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(250, token);
            await _buildGate.WaitAsync(token);
            try
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Change detected, rebuilding...");
                buildRunner.Run();
            }
            finally
            {
                _buildGate.Release();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void CancelPendingChange()
    {
        lock (_lock)
        {
            _pendingChange?.Cancel();
            _pendingChange?.Dispose();
            _pendingChange = null;
        }
    }
}
