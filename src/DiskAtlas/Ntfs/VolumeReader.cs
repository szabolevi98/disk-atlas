using DiskAtlas.Native;
using Microsoft.Win32.SafeHandles;

namespace DiskAtlas.Ntfs;

/// <summary>
/// Reads arbitrary byte ranges from a raw volume. Volume handles only accept sector
/// aligned requests, so every read is widened to sector boundaries and then trimmed.
/// </summary>
internal sealed class VolumeReader : IDisposable
{
    private readonly SafeFileHandle _handle;
    private int _bytesPerSector = 512;

    public VolumeReader(string driveLetter)
    {
        _handle = NativeMethods.OpenVolume(driveLetter);
    }

    /// <summary>
    /// Set once the boot sector has been parsed, so later reads align to the real
    /// geometry instead of the 512 byte assumption used to bootstrap.
    /// </summary>
    public int BytesPerSector
    {
        get => _bytesPerSector;
        set => _bytesPerSector = value > 0 ? value : 512;
    }

    public void ReadExact(long offset, Span<byte> destination)
    {
        if (destination.IsEmpty)
        {
            return;
        }

        long alignedOffset = offset / _bytesPerSector * _bytesPerSector;
        int padding = (int)(offset - alignedOffset);
        int alignedLength = RoundUpToSector(padding + destination.Length);

        byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(alignedLength);
        try
        {
            Span<byte> window = buffer.AsSpan(0, alignedLength);
            int read = 0;

            while (read < alignedLength)
            {
                int chunk = RandomAccess.Read(_handle, window[read..], alignedOffset + read);
                if (chunk == 0)
                {
                    throw new EndOfStreamException(
                        $"The volume ended while reading {destination.Length} bytes at offset {offset}.");
                }

                read += chunk;
            }

            window.Slice(padding, destination.Length).CopyTo(destination);
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private int RoundUpToSector(int length)
    {
        int remainder = length % _bytesPerSector;
        return remainder == 0 ? length : length + (_bytesPerSector - remainder);
    }

    public void Dispose() => _handle.Dispose();
}
