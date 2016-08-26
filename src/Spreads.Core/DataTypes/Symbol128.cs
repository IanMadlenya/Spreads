﻿/*
    Copyright(c) 2014-2016 Victor Baybekov.

    This file is a part of Spreads library.

    Spreads library is free software; you can redistribute it and/or modify it under
    the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 3 of the License, or
    (at your option) any later version.

    Spreads library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.If not, see<http://www.gnu.org/licenses/>.
*/

using System;
using System.Diagnostics;
using System.Text;
using Spreads.Buffers;
using Spreads.Serialization;

namespace Spreads.DataTypes {

    [DebuggerDisplay("{AsString}")]
    public unsafe struct Symbol128 : IEquatable<Symbol128>
    {
        private const int Size = 128;
        private fixed byte Bytes[Size];

        public Symbol128(string symbol) {
            var byteCount = Encoding.UTF8.GetByteCount(symbol);
            if (byteCount > Size) {
                throw new ArgumentOutOfRangeException(nameof(symbol), "Symbol length is too large");
            }
            fixed (char* charPtr = symbol)
            fixed (byte* ptr = Bytes)
            {
                Encoding.UTF8.GetBytes(charPtr, symbol.Length, (byte*)ptr, Size);
            }
        }

        public string AsString => ToString();

        public bool Equals(Symbol128 other) {
            fixed (byte* thisPtr = Bytes)
            {
                for (int i = 0; i < Size; i++) {
                    if (*(byte*)(thisPtr + i) != *(byte*)(other.Bytes + i)) return false;
                }
            }
            return true;
        }

        public override string ToString() {
            var buffer = RecyclableMemoryManager.ThreadStaticBuffer;
            var len = 0;
            fixed (byte* thisPtr = Bytes)
            {
                for (int i = 0; i < Size; i++) {
                    var b = *(byte*)(thisPtr + i);
                    if (b == 0) {
                        break;
                    }
                    buffer[i] = b;
                    len = i + 1;
                }
            }

            return Encoding.UTF8.GetString(buffer, 0, len);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Symbol128 && Equals((Symbol128)obj);
        }

        public override int GetHashCode() {
            fixed (byte* ptr = Bytes)
            {
                unchecked {
                    const int p = 16777619;
                    int hash = (int)2166136261;

                    for (int i = 0; i < Size; i++) {
                        var b = *(ptr + i);
                        if (b == 0) break;
                        hash = (hash ^ b) * p;
                    }

                    hash += hash << 13;
                    hash ^= hash >> 7;
                    hash += hash << 3;
                    hash ^= hash >> 17;
                    hash += hash << 5;
                    return hash;
                }
            }

        }

        public static bool operator ==(Symbol128 x, Symbol128 y) {
            return x.Equals(y);
        }
        public static bool operator !=(Symbol128 x, Symbol128 y) {
            return !x.Equals(y);
        }
    }
}
