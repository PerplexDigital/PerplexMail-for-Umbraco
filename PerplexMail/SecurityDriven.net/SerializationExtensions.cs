using System;
using System.IO;

namespace PerplexMail.SecurityDrivenDotNet
{
    internal static class SerializationExtensions
	{
		public static string DeserializeToString(this byte[] bytes)
		{
			using (var m = new MemoryStream(bytes))
			using (var r = new SerializingBinaryReader(m))
				return r.ReadBinaryString();
		}

		public static byte[] SerializeToBytes(this string str)
		{
			using (var m = new MemoryStream())
			{
				using (var w = new SerializingBinaryWriter(m))
					w.WriteBinaryString(str);

				return m.ToArray();
			}
		}
	}//class SerializationExtensions

	class SerializingBinaryReader : BinaryReader
	{
		public SerializingBinaryReader(Stream input) : base(input) { }

		public string ReadBinaryString()
		{
			int charCount = base.Read7BitEncodedInt();
			byte[] bytes = base.ReadBytes(charCount * 2);

			char[] chars = new char[charCount];
			for (int i = 0; i < charCount; ++i)
			{
				chars[i] = (char)(bytes[i * 2] | (bytes[i * 2 + 1] << 8));
			}
			return new String(chars);
		}//ReadBinaryString()

		// should never call this method since it will produce wrong results
		public override string ReadString() { throw new NotImplementedException(); }
	}//class SerializingBinaryReader

	// This is a special BinaryWriter which serializes strings in a way that is 
	// entirely round-trippable. For example, the string "\ud800" is a valid .NET 
	// Framework string, but since U+D800 is an unpaired Unicode surrogate the
	// built-in Encoding types will not round-trip it. Strings are serialized as a 
	// 7-bit character count (not byte count!) followed by a UTF-16LE payload.
	class SerializingBinaryWriter : BinaryWriter
	{
		public SerializingBinaryWriter(Stream output) : base(output) { }

		public void WriteBinaryString(string str)
		{
			var length = str.Length;
			base.Write7BitEncodedInt(length);
			byte[] bytes = new byte[length * 2];
			char c;
			for (int i = 0; i < length; ++i)
			{
				c = str[i];
				bytes[i * 2] = (byte)c;
				bytes[i * 2 + 1] = (byte)(c >> 8);
			}
			base.Write(bytes);
		}//WriteBinaryString()

		// should never call this method since it will produce wrong results
		public override void Write(string value) { throw new NotImplementedException(); }
	}//class SerializingBinaryWriter
}//ns