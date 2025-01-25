namespace fredapi.SignalR;

public interface ISubscriberTracker
{
    Task AddSubscriberAsync(string connectionId);
    Task RemoveSubscriberAsync(string connectionId);
    Task<bool> HasSubscribersAsync();
    Task<int> GetSubscriberCountAsync();
}

public class SubscriberTracker : ISubscriberTracker
{
    private readonly HashSet<string> _activeSubscribers = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task AddSubscriberAsync(string connectionId)
    {
        await _semaphore.WaitAsync();
        try
        {
            _activeSubscribers.Add(connectionId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task RemoveSubscriberAsync(string connectionId)
    {
        await _semaphore.WaitAsync();
        try
        {
            _activeSubscribers.Remove(connectionId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> HasSubscribersAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return _activeSubscribers.Count > 0;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<int> GetSubscriberCountAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return _activeSubscribers.Count;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}