using System;
using System.IO;
using CSUtilities.Extensions;
using CSUtilities.IO;
using CSUtilities.Text;
using Xunit;

namespace CSUtilities.Tests.IO;

public class StreamExactReadTests
{
	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(5)]
	public void ReadsAllBytesWhenSourceReturnsPartialReads(int maximumRead)
	{
		byte[] data = { 10, 20, 30, 40, 50 };
		using var stream = new PartialReadStream(data, maximumRead);
		byte[] buffer = { 99, 99, 99, 99, 99, 99, 99 };

		StreamExtensions.ReadExactly(stream, buffer, 1, data.Length);

		Assert.Equal(new byte[] { 99, 10, 20, 30, 40, 50, 99 }, buffer);
		Assert.Equal(data.Length, stream.Position);
	}

	[Fact]
	public void ThrowsAtEndInsteadOfReturningAnIncompleteBuffer()
	{
		using var stream = new PartialReadStream(new byte[] { 10, 20, 30 }, 2);
		byte[] buffer = { 99, 99, 99, 99, 99 };

		Assert.Throws<EndOfStreamException>(() => StreamExtensions.ReadExactly(stream, buffer, 0, buffer.Length));

		Assert.Equal(new byte[] { 10, 20, 30, 99, 99 }, buffer);
		Assert.Equal(3, stream.Position);
	}

	[Fact]
	public void EmptyReadDoesNotAccessTheSource()
	{
		using var stream = new ThrowingReadStream();
		StreamExtensions.ReadExactly(stream, Array.Empty<byte>(), 0, 0);
	}

	[Fact]
	public void PropagatesTheOriginalReadFailure()
	{
		using var stream = new ThrowingReadStream();

		var exception = Assert.Throws<IOException>(() => StreamExtensions.ReadExactly(stream, new byte[1], 0, 1));

		Assert.Same(stream.Failure, exception);
	}

	[Theory]
	[InlineData(-1, 1)]
	[InlineData(0, -1)]
	[InlineData(1, 1)]
	public void InvalidBufferRangesDoNotReadTheSource(int offset, int count)
	{
		using var stream = new ThrowingReadStream();

		Assert.ThrowsAny<ArgumentException>(() => StreamExtensions.ReadExactly(stream, new byte[1], offset, count));
	}

	[Fact]
	public void CopyConstructorPreservesAllBytesAndRestoresSourcePosition()
	{
		byte[] data = { 10, 20, 30, 40, 50 };
		using var source = new PartialReadStream(data, 2);
		source.Position = 3;

		using var copy = new StreamIO(source, createCopy: true);

		Assert.Equal(3, source.Position);
		Assert.Equal(0, copy.Position);
		Assert.Equal(data, copy.ReadBytes(data.Length));
	}

	[Fact]
	public void CopyConstructorRejectsAStreamThatEndsBeforeItsReportedLength()
	{
		using var source = new PartialReadStream(new byte[] { 10, 20, 30 }, 2, 5);

		Assert.Throws<EndOfStreamException>(() => new StreamIO(source, createCopy: true));
	}

	[Fact]
	public void ExistingReadBytesStillRejectsAShortSingleRead()
	{
		using var source = new PartialReadStream(new byte[] { 10, 20, 30 }, 1);
		using var reader = new StreamIO(source);

		Assert.Throws<EndOfStreamException>(() => reader.ReadBytes(2));
	}

	[Fact]
	public void ExplicitLegacyUtf7MappingPreservesItsEncoding()
	{
		var encoding = TextEncoding.GetListedEncoding(CodePage.Utf7);

		Assert.Equal(65000, encoding.CodePage);
		Assert.Equal("A£B", encoding.GetString(new byte[] { 65, 43, 65, 75, 77, 45, 66 }));
	}

	private sealed class PartialReadStream : MemoryStream
	{
		private readonly int _maximumRead;
		private readonly long? _reportedLength;

		public PartialReadStream(byte[] data, int maximumRead, long? reportedLength = null) : base(data)
		{
			_maximumRead = maximumRead;
			_reportedLength = reportedLength;
		}

		public override long Length => _reportedLength ?? base.Length;

		public override int Read(byte[] buffer, int offset, int count)
		{
			return base.Read(buffer, offset, Math.Min(count, _maximumRead));
		}
	}

	private sealed class ThrowingReadStream : MemoryStream
	{
		public IOException Failure { get; } = new IOException("Simulated stream failure.");

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw Failure;
		}
	}
}
