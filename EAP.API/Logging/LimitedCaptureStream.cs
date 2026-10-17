using System.Buffers;

namespace EAP.Api.Logging;

internal sealed class LimitedCaptureStream : Stream
{
    private readonly Stream _destination;
    private readonly int _maxBytes;
    private readonly Func<bool> _shouldCapture;
    private byte[]? _captureBuffer;
    private int _capturedLength;

    public LimitedCaptureStream(
        Stream destination,
        int maxBytes,
        Func<bool> shouldCapture)
    {
        _destination = destination;
        _maxBytes = maxBytes;
        _shouldCapture = shouldCapture;
    }

    public bool LimitExceeded { get; private set; }

    public byte[] GetCapturedBytes()
    {
        if (_captureBuffer is null || _capturedLength == 0)
        {
            return Array.Empty<byte>();
        }

        return _captureBuffer[.._capturedLength];
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Capture(buffer.AsSpan(offset, count));
        _destination.Write(buffer, offset, count);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        Capture(buffer.AsSpan(offset, count));
        await _destination.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        Capture(buffer.Span);
        await _destination.WriteAsync(buffer, cancellationToken);
    }

    public override void Flush() => _destination.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) =>
        _destination.FlushAsync(cancellationToken);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_captureBuffer is not null)
            {
                ArrayPool<byte>.Shared.Return(_captureBuffer, clearArray: true);
                _captureBuffer = null;
            }
        }

        base.Dispose(disposing);
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => _destination.CanWrite;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    private void Capture(ReadOnlySpan<byte> buffer)
    {
        if (!_shouldCapture())
        {
            return;
        }

        _captureBuffer ??= ArrayPool<byte>.Shared.Rent(_maxBytes);
        var remaining = _maxBytes - _capturedLength;
        if (remaining > 0)
        {
            var bytesToCopy = Math.Min(remaining, buffer.Length);
            buffer[..bytesToCopy].CopyTo(_captureBuffer.AsSpan(_capturedLength));
            _capturedLength += bytesToCopy;
        }

        if (buffer.Length > remaining)
        {
            LimitExceeded = true;
        }
    }
}
