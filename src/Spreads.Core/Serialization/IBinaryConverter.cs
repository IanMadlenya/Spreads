﻿using System;
using System.IO;
using Spreads.Buffers;

namespace Spreads.Serialization {


    public enum BinaryConverterErrorCode : int {
        NotEnoughCapacity = -1
    }

    /// <summary>
    /// Convert a generic object T to a pointer prefixed with version and length.
    /// 
    /// 0                   1                   2                   3
    /// 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |  Fmt Version  |     Flags   |C|           Reserved            |
    /// +---------------------------------------------------------------+
    /// |R|                Length (including header)                    |
    /// +---------------------------------------------------------------+
    /// |                     Serialized Payload                      ...
    /// </summary>
    public interface IBinaryConverter<T> {
        /// <summary>
        /// Equivalent to check Size > 0
        /// </summary>
        bool IsFixedSize { get; }

        /// <summary>
        /// Zero for variable-length types, positive value for fixed-size types.
        /// </summary>
        int Size { get; }

        /// <summary>
        /// Version of the converter.
        /// </summary>
        byte Version { get; }

        /// <summary>
        /// Returns the size of serialized bytes including the version+lenght header.
        /// For types with non-fixed size this method could serialize value into the payloadStream if it is not 
        /// possible to calculate serialized bytes length without actually performing serialization.
        /// </summary>
        int SizeOf(T value, out MemoryStream payloadStream);

        /// <summary>
        /// Write serialized value to the buffer at offset if there is enough capacity
        /// </summary>
        int Write(T value, ref DirectBuffer destination, uint offset = 0u, MemoryStream payloadStream = null);

        /// <summary>
        /// Reads new value or fill existing value with data from the pointer, 
        /// returns number of bytes read including any header.
        /// If not IsFixedSize, checks that version from the pointer equals the Version property.
        /// </summary>
        int Read(IntPtr ptr, ref T value);
    }
}