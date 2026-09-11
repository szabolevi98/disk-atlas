namespace DiskAtlas.Ntfs;

/// <summary>
/// One contiguous extent of a non resident attribute: a starting cluster on the volume
/// and how many clusters follow it.
/// </summary>
internal readonly record struct DataRun(long StartLcn, long ClusterCount)
{
    /// <summary>
    /// Decodes an NTFS run list. Every run begins with a header byte whose low nibble is
    /// the byte count of the length field and whose high nibble is the byte count of the
    /// offset field. Offsets are signed and relative to the previous run, which is what
    /// keeps fragmented files compact on disk. A zero header byte ends the list.
    /// </summary>
    public static List<DataRun> Decode(ReadOnlySpan<byte> runList)
    {
        List<DataRun> runs = [];
        long currentLcn = 0;
        int position = 0;

        while (position < runList.Length)
        {
            byte header = runList[position++];
            if (header == 0)
            {
                break;
            }

            int lengthBytes = header & 0x0F;
            int offsetBytes = (header >> 4) & 0x0F;

            if (lengthBytes == 0 || position + lengthBytes + offsetBytes > runList.Length)
            {
                throw new InvalidDataException("Malformed data run header.");
            }

            long clusterCount = ReadUnsigned(runList.Slice(position, lengthBytes));
            position += lengthBytes;

            if (offsetBytes == 0)
            {
                // A run without an offset is a sparse hole: it occupies no clusters on disk.
                position += offsetBytes;
                continue;
            }

            currentLcn += ReadSigned(runList.Slice(position, offsetBytes));
            position += offsetBytes;

            if (clusterCount > 0)
            {
                runs.Add(new DataRun(currentLcn, clusterCount));
            }
        }

        return runs;
    }

    private static long ReadUnsigned(ReadOnlySpan<byte> bytes)
    {
        long value = 0;
        for (int i = bytes.Length - 1; i >= 0; i--)
        {
            value = (value << 8) | bytes[i];
        }

        return value;
    }

    private static long ReadSigned(ReadOnlySpan<byte> bytes)
    {
        long value = (sbyte)bytes[^1];
        for (int i = bytes.Length - 2; i >= 0; i--)
        {
            value = (value << 8) | bytes[i];
        }

        return value;
    }
}
