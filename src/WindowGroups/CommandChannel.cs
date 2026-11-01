using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using WindowGroups.Core.Configuration;
using WindowGroups.Core.Models;

namespace WindowGroups;

/// <summary>
/// Cross-process command channel. A second instance writes a JSON command to a
/// small file and signals the running instance via a named event; the running
/// instance waits on that event and forwards each command to a handler.
/// </summary>
internal sealed class CommandChannel : IDisposable
{
    private const string EventName = @"Local\WindowGroups.CommandEvent";
    private const string PayloadFileName = "pending-command.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _payloadPath;
    private readonly EventWaitHandle _event;
    private Thread? _listenerThread;
    private volatile bool _stopped;

    public CommandChannel()
    {
        _payloadPath = Path.Combine(ConfigurationService.GetDefaultConfigDirectory(), PayloadFileName);
        _event = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
    }

    /// <summary>Writes the command payload and signals the running instance.</summary>
    public void Signal(CommandRequest command)
    {
        try
        {
            var json = JsonSerializer.Serialize(command, JsonOptions);

            var directory = Path.GetDirectoryName(_payloadPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write to a temp file then move, so the reader never sees a partial payload.
            var tempPath = _payloadPath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _payloadPath, overwrite: true);
        }
        finally
        {
            _event.Set();
        }
    }

    /// <summary>Starts a background thread that invokes <paramref name="handler"/> for each command.</summary>
    public void StartListening(Action<CommandRequest> handler)
    {
        _listenerThread = new Thread(() => Listen(handler))
        {
            IsBackground = true,
            Name = "WindowGroups.CommandListener"
        };
        _listenerThread.Start();
    }

    private void Listen(Action<CommandRequest> handler)
    {
        while (!_stopped)
        {
            try
            {
                _event.WaitOne();
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            if (_stopped)
            {
                break;
            }

            var command = ReadPayload();
            if (command is not null)
            {
                try
                {
                    handler(command);
                }
                catch
                {
                    // Best effort; never take the listener down.
                }
            }
        }
    }

    private CommandRequest? ReadPayload()
    {
        try
        {
            if (!File.Exists(_payloadPath))
            {
                return null;
            }

            return JsonSerializer.Deserialize<CommandRequest>(File.ReadAllText(_payloadPath), JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        _stopped = true;
        try
        {
            _event.Set();
        }
        catch
        {
            // Handle may already be closed.
        }

        _event.Dispose();
    }
}