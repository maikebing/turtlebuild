using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QQn.TurtleUtils.IO
{
	class QQnBinaryReader : BinaryReader
	{
		public QQnBinaryReader(Stream input)
			: base(input, Encoding.UTF8)
		{
		}

		public int ReadSmartInt()
		{
			return base.Read7BitEncodedInt();
		}
		
		public byte[] ReadByteArray()
		{
			int length = ReadSmartInt();
			return ReadBytes(length);
		}


	}
}
