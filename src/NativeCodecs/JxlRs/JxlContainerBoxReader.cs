// Copyright ? Clinton Ingram and Contributors
// SPDX-License-Identifier: MIT

using System;
using System.Buffers.Binary;
using System.IO;

namespace PhotoSauce.NativeCodecs.JxlRs;

internal readonly struct JxlExifLocation(long offset, int length)
{
	public long Offset { get; } = offset;
	public int Length { get; } = length;
	public bool IsEmpty => Length == 0;
}

internal static class JxlContainerBoxReader
{
	private const uint BoxTypeJxl = 0x4a584c20;
	private const uint BoxTypeExif = 0x45786966;
	private const uint ContainerSignature = 0x0d0a870a;
	private const int BoxHeaderLength = sizeof(uint) * 2;
	private const int ExtendedBoxHeaderLength = BoxHeaderLength + sizeof(ulong);

	public static bool TryFindExif(Stream stream, long start, out JxlExifLocation location)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));
		if (!stream.CanSeek)
			throw new ArgumentException("The JPEG XL stream must be seekable.", nameof(stream));

		location = default;
		long originalPosition = stream.Position;
		try
		{
			long end = stream.Length;
			if (start < 0 || start > end || end - start < 12)
				return false;

			Span<byte> header = stackalloc byte[ExtendedBoxHeaderLength];
			stream.Position = start;
			if (!tryReadExactly(stream, header[..12]) ||
				BinaryPrimitives.ReadUInt32BigEndian(header) != 12 ||
				BinaryPrimitives.ReadUInt32BigEndian(header[4..]) != BoxTypeJxl ||
				BinaryPrimitives.ReadUInt32BigEndian(header[8..]) != ContainerSignature)
			{
				return false;
			}

			long position = start + 12;
			while (position <= end && end - position >= BoxHeaderLength)
			{
				stream.Position = position;
				if (!tryReadExactly(stream, header[..BoxHeaderLength]))
					return false;

				uint shortLength = BinaryPrimitives.ReadUInt32BigEndian(header);
				uint boxType = BinaryPrimitives.ReadUInt32BigEndian(header[4..]);
				int headerLength = BoxHeaderLength;
				ulong boxLength;
				if (shortLength == 1)
				{
					if (end - position < ExtendedBoxHeaderLength ||
						!tryReadExactly(stream, header[BoxHeaderLength..ExtendedBoxHeaderLength]))
					{
						return false;
					}

					headerLength = ExtendedBoxHeaderLength;
					boxLength = BinaryPrimitives.ReadUInt64BigEndian(header[BoxHeaderLength..]);
				}
				else if (shortLength == 0)
				{
					boxLength = (ulong)(end - position);
				}
				else
				{
					boxLength = shortLength;
				}

				ulong remaining = (ulong)(end - position);
				if (boxLength < (uint)headerLength || boxLength > remaining)
					return false;

				long payloadOffset = position + headerLength;
				ulong payloadLength = boxLength - (uint)headerLength;
				if (boxType == BoxTypeExif)
				{
					if (payloadLength < sizeof(uint))
						return false;

					stream.Position = payloadOffset;
					if (!tryReadExactly(stream, header[..sizeof(uint)]))
						return false;

					ulong tiffOffset = BinaryPrimitives.ReadUInt32BigEndian(header);
					ulong exifOffset = sizeof(uint) + tiffOffset;
					if (exifOffset >= payloadLength)
						return false;

					ulong exifLength = payloadLength - exifOffset;
					if (exifLength > int.MaxValue)
						return false;

					location = new JxlExifLocation(
						checked(payloadOffset + (long)exifOffset),
						(int)exifLength
					);
					return true;
				}

				if (shortLength == 0)
					break;

				position = checked(position + (long)boxLength);
			}

			return false;
		}
		finally
		{
			stream.Position = originalPosition;
		}
	}

	public static void CopyExif(Stream stream, in JxlExifLocation location, Span<byte> destination)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));
		if (destination.Length < location.Length)
			throw new ArgumentException("The destination is too small.", nameof(destination));
		if (location.Offset < 0 || location.IsEmpty)
			throw new ArgumentException("The EXIF location is invalid.", nameof(location));

		destination = destination[..location.Length];
		long originalPosition = stream.Position;
		try
		{
			if (stream is MemoryStream memory && memory.TryGetBuffer(out var segment))
			{
				long end = checked(location.Offset + location.Length);
				if (end > memory.Length)
					throw new EndOfStreamException("The JPEG XL EXIF box is truncated.");

				int offset = checked(segment.Offset + (int)location.Offset);
				new ReadOnlySpan<byte>(segment.Array, offset, location.Length).CopyTo(destination);
				return;
			}

			stream.Position = location.Offset;
			if (!tryReadExactly(stream, destination))
				throw new EndOfStreamException("The JPEG XL EXIF box is truncated.");
		}
		finally
		{
			stream.Position = originalPosition;
		}
	}

	private static bool tryReadExactly(Stream stream, Span<byte> destination)
	{
#if NETFRAMEWORK
		for (int i = 0; i < destination.Length; i++)
		{
			int value = stream.ReadByte();
			if (value < 0)
				return false;

			destination[i] = (byte)value;
		}
#else
		while (!destination.IsEmpty)
		{
			int read = stream.Read(destination);
			if (read == 0)
				return false;

			destination = destination[read..];
		}
#endif
		return true;
	}
}
