namespace CSUtilities.Extensions;

/// <summary>
/// Stream utility extensions.
/// </summary>
#if PUBLIC
public
#else
internal
#endif
static class StreamExtensions
{
	/// <summary>
	/// Reads the requested number of bytes, including streams that return partial reads.
	/// </summary>
	/// <param name="stream">The source stream.</param>
	/// <param name="buffer">The destination buffer.</param>
	/// <param name="offset">The first destination index.</param>
	/// <param name="count">The number of bytes to read.</param>
	/// <exception cref="System.IO.EndOfStreamException">The stream ends before the requested bytes have been read.</exception>
	public static void ReadExactly(this System.IO.Stream stream, byte[] buffer, int offset, int count)
	{
		if (stream is null)
		{
			throw new System.ArgumentNullException(nameof(stream));
		}
		if (buffer is null)
		{
			throw new System.ArgumentNullException(nameof(buffer));
		}
		if (offset < 0)
		{
			throw new System.ArgumentOutOfRangeException(nameof(offset));
		}
		if (count < 0)
		{
			throw new System.ArgumentOutOfRangeException(nameof(count));
		}
		if (offset > buffer.Length - count)
		{
			throw new System.ArgumentException("The requested range exceeds the buffer length.");
		}

		while (count > 0)
		{
			int bytesRead = stream.Read(buffer, offset, count);
			if (bytesRead == 0)
			{
				throw new System.IO.EndOfStreamException();
			}
			offset += bytesRead;
			count -= bytesRead;
		}
	}

#if NETFRAMEWORK
	/// <summary>
	/// When overridden in a derived class, writes a sequence of bytes to the current
	/// stream and advances the current position within this stream by the number of
	/// bytes written.
	/// </summary>
	/// <param name="stream"></param>
	/// <param name="buffer">A region of memory. This method copies the contents of this region to the current stream. </param>
	public static void Write(this System.IO.Stream stream, byte[] buffer)
	{
		stream.Write(buffer, 0, buffer.Length);
	}
#endif
}
