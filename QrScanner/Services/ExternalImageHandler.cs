using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QrScanner.Services;

/// <summary>
/// Facilitates receiving and queuing images sent from external apps (via Share sheet, Intent, Open-With, Drag & Drop, CLI)
/// so that ViewModels can process them asynchronously even if they arrive before the UI has finished initializing.
/// </summary>
public static class ExternalImageHandler
{
    private static readonly Queue<byte[]> PendingImages = new();
    private static readonly object SyncLock = new();
    private static Func<byte[], Task>? _receiver;
    private static int _activeProcessingCount;

    public static bool IsIngesting
    {
        get
        {
            lock (SyncLock)
            {
                return PendingImages.Count > 0 || _activeProcessingCount > 0;
            }
        }
    }

    public static void HandleImage(byte[] imageBytes)
    {
        if (imageBytes is null || imageBytes.Length == 0)
            return;

        Func<byte[], Task>? receiver;
        lock (SyncLock)
        {
            receiver = _receiver;
            if (receiver is null)
            {
                PendingImages.Enqueue(imageBytes);
                return;
            }
            _activeProcessingCount++;
        }

        _ = RunReceiverAsync(receiver, imageBytes);
    }

    public static void RegisterReceiver(Func<byte[], Task> receiver)
    {
        List<byte[]> queued = [];
        lock (SyncLock)
        {
            _receiver = receiver;
            while (PendingImages.TryDequeue(out var bytes))
            {
                queued.Add(bytes);
                _activeProcessingCount++;
            }
        }

        foreach (var bytes in queued)
        {
            _ = RunReceiverAsync(receiver, bytes);
        }
    }

    private static async Task RunReceiverAsync(Func<byte[], Task> receiver, byte[] bytes)
    {
        try
        {
            await receiver(bytes);
        }
        finally
        {
            lock (SyncLock)
            {
                _activeProcessingCount = Math.Max(0, _activeProcessingCount - 1);
            }
        }
    }

    public static void UnregisterReceiver(Func<byte[], Task>? receiver = null)
    {
        lock (SyncLock)
        {
            if (receiver is null || _receiver == receiver)
            {
                _receiver = null;
            }
        }
    }
}
