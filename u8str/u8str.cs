using System.Buffers;
using System.Collections;
using System.ComponentModel;
using System.IO.Hashing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
namespace U8Str;

public readonly unsafe struct u8str : IEqualityOperators<u8str, u8str, bool>,
    IAdditionOperators<u8str, u8str, u8str>, IEquatable<u8str>, IEnumerable<byte>, IUtf8SpanFormattable, ISpanFormattable
#if NET9_0_OR_GREATER

#endif
{
    internal ref struct Ref
    {
        public ref byte Value;
    }

    static readonly nuint offset;

    static u8str()
    {
        var array = new byte[1];
        fixed (byte* p = array)
        {
            var head = Unsafe.As<byte[], IntPtr>(ref array);
            offset = (nuint)(p - head);
        }
    }


    readonly byte[]? bytes;
    readonly nuint ptr;
    readonly int length;

    public int Length => length;


    internal u8str(byte[] bytes)
    {
        this.bytes = bytes;
        ptr = offset;
        length = bytes.Length;
    }

    internal u8str(byte[]? bytes, nuint ptr, int length)
    {
        this.bytes = bytes;
        this.ptr = ptr;
        this.length = length;
    }

    public u8str(ReadOnlySpan<byte> bytes)
    {
        this.bytes = null;
        fixed (byte* p = bytes)
        {
            ptr = (nuint)p;
        }

        length = bytes.Length;
    }

    public u8str(string str) => this = new(Encoding.UTF8.GetBytes(str));

    public static u8str FromArrayNoCopy(params byte[] bytes)
    {
        return new(bytes);
    }
    
    public static u8str FromPtrNoCopy(IntPtr ptr, int length)
    {
        return new(null, (nuint)ptr, length);
    }

    public ReadOnlySpan<byte> AsSpan()
    {
        if (bytes == null && ptr == 0)
        {
            return ReadOnlySpan<byte>.Empty;
        }

        var buffer = bytes;
        ref var head = ref ((Ref*)(&buffer))->Value;
        return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref head, ptr), length);
    }

    public u8str SliceBytes(int start, int length)
    {
        if (start < 0 || start > this.length)
            throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0 || start + length > this.length)
            throw new ArgumentOutOfRangeException(nameof(length));
        return new(bytes, ptr + unchecked((nuint)start), length);
    }
    
    public ref readonly byte this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref AsSpan()[index];
    }
    
    public u8str this[Range range]
    {
        get
        {
            var tuple = range.GetOffsetAndLength(Length);
            return SliceBytes(tuple.Offset, tuple.Length);
        }
    }

    internal ref byte UnsafeGetRef()
    {
        var buffer = bytes;
        ref var head = ref ((Ref*)(&buffer))->Value;
        return ref Unsafe.Add(ref head, ptr);
    }

    internal ref byte UnsafeGetRef(int index)
    {
        var buffer = bytes;
        ref var head = ref ((Ref*)(&buffer))->Value;
        return ref Unsafe.Add(ref head, ptr + unchecked((nuint)index));
    }

    public static implicit operator u8str(ReadOnlySpan<byte> bytes) => new u8str(bytes);

    public static implicit operator ReadOnlySpan<byte> (u8str str) => str.AsSpan();

    public static u8str operator +(u8str left, u8str right)
    {
        var leftSpan = left.AsSpan();
        var rightSpan = right.AsSpan();
        var bytes = new byte[leftSpan.Length + rightSpan.Length];
        leftSpan.CopyTo(bytes.AsSpan());
        rightSpan.CopyTo(bytes.AsSpan()[leftSpan.Length..]);
        return new(bytes);
    }

    public static bool operator ==(u8str left, u8str right)
    {
        if(left.Length != right.Length) return false;
        return left.AsSpan().SequenceEqual(right.AsSpan());
    }

    public static bool operator !=(u8str left, u8str right)
    {
        if(left.Length != right.Length) return true;
        return !left.AsSpan().SequenceEqual(right.AsSpan());
    }
    
    public static bool operator ==(u8str left, ReadOnlySpan<byte> right)
    {
        if(left.Length != right.Length) return false;
        return left.AsSpan().SequenceEqual(right);
    }

    public static bool operator !=(u8str left, ReadOnlySpan<byte> right)
    {
        if(left.Length != right.Length) return true;
        return !left.AsSpan().SequenceEqual(right);
    }
    
    public static bool operator ==(ReadOnlySpan<byte> left, u8str right)
    {
        if(left.Length != right.Length) return false;
        return left.SequenceEqual(right.AsSpan());
    }

    public static bool operator !=(ReadOnlySpan<byte> left, u8str right)
    {
        if(left.Length != right.Length) return true;
        return !left.SequenceEqual(right.AsSpan());
    }
    
    public bool IsValid => Utf8.IsValid(AsSpan());

    public override string ToString()
    {
        if(length == 0) return string.Empty;
        return Encoding.UTF8.GetString(AsSpan());
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly byte GetPinnableReference() => ref UnsafeGetRef();


    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return ToString();
    }

    public bool TryCopyTo(Span<char> destination, out int charsWritten)
    {
        var source = AsSpan();
        var success = true;
        if (!source.IsEmpty)
        {
            var result = Utf8.ToUtf16(source, destination, out _, out charsWritten, replaceInvalidSequences: false);

            if (result is OperationStatus.DestinationTooSmall)
                success = false;
            else if (result is not OperationStatus.Done)
            {
                throw new InvalidOperationException("Invalid UTF-8");
            }
               
        }
        else charsWritten = 0;

        return success;
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (TryCopyTo(destination, out charsWritten))
        {
            return true;
        }

        charsWritten = 0;
        return false;
    }

    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        var span = AsSpan();
        if (span.TryCopyTo(utf8Destination))
        {
            bytesWritten = span.Length;
            return true;
        }

        bytesWritten = 0;
        return false;
    }

    public bool Equals(u8str other)
    {
        return AsSpan().SequenceEqual(other.AsSpan());
    }

    public Enumerator GetEnumerator()
    {
        return new(this);
    }

    IEnumerator<byte> IEnumerable<byte>.GetEnumerator()
    {
        return new Enumerator(this);
    }


    public override bool Equals(object? obj)
    {
        return obj is u8str other && Equals(other);
    }

    public override int GetHashCode()
    {
        return unchecked((int)XxHash3.HashToUInt64(AsSpan()));
    }

    public struct Enumerator(u8str str) : IEnumerator<byte>
    {
        int index = -1;
        byte current;
        public byte Current => current;
        object IEnumerator.Current => current;

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (++index >= str.length) return false;
            current = str.UnsafeGetRef(index);
            return true;
        }

        public void Reset() => index = -1;
    }
}