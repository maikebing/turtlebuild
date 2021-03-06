using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace QQn.TurtleUtils.IO
{
	/// <summary>
	/// Allows reading multiple substreams from one parent stream. Reads back the streams created with a <see cref="MultipleStreamWriter"/>
	/// </summary>
	public class MultipleStreamReader : IDisposable
	{
		readonly VerificationMode _verificationMode;
		readonly Stream _input;
		readonly QQnBinaryReader _reader;
		//readonly int _maxCount;
		readonly List<MultiStreamItemHeader> _items = new List<MultiStreamItemHeader>();
		MultipleSubStreamReader _openReader;
		int _next = 0;

		/// <summary>
		/// Initializes a new instance of the <see cref="MultipleStreamReader"/> class.
		/// </summary>
		/// <param name="input">The input.</param>
		/// <param name="args">The args.</param>
		public MultipleStreamReader(Stream input, MultipleStreamCreateArgs args)
		{
			if (input == null)
				throw new ArgumentNullException("input");
			else if (args == null)
				throw new ArgumentNullException("args");

			_input = input;
			_reader = new QQnBinaryReader(_input);
			_verificationMode = args.VerificationMode;

			/*_maxCount =*/ _reader.ReadInt32();
			int count = _reader.ReadInt32();

			long nextHeader = _reader.ReadInt64();

			for (int i = 0; i < count; i++)
			{
				_items.Add(new MultiStreamItemHeader(_reader));
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MultipleStreamReader"/> class.
		/// </summary>
		/// <param name="input">The input.</param>
		public MultipleStreamReader(Stream input)
			: this(input, new MultipleStreamCreateArgs())
		{
		}

		/// <summary>
		/// Gets the base stream.
		/// </summary>
		/// <value>The base stream.</value>
		protected Stream BaseStream
		{
			get { return _input; }
		}

		[SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
		void IDisposable.Dispose()
		{
			Dispose(true);
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected virtual void Dispose(bool disposing)
		{
			if(disposing)
				Close();
		}

		/// <summary>
		/// Closes this instance.
		/// </summary>
		public virtual void Close()
		{
			if (_openReader != null)
				_openReader.Close();

			_reader.Close();

			BaseStream.Close();			
		}

		/// <summary>
		/// Gets the next stream.
		/// </summary>
		/// <returns>The next substream</returns>
		/// <exception cref="InvalidOperationException">The previous stream is still open</exception>
		public Stream GetNextStream()
		{
			if (_openReader != null)
				throw new InvalidOperationException();
			else if (_next >= _items.Count)
				return null;

			MultiStreamItemHeader header = _items[_next++];
			MultipleSubStreamReader reader = new MultipleSubStreamReader(this, BaseStream, header);
			Stream s = reader;

			if(0 != (header.ItemType & MultiStreamItemHeader.AssuredFlag))
				s = new AssuredSubStream(s, _verificationMode);

			if (0 != (header.ItemType & MultiStreamItemHeader.ZippedFlag))
				s = new ZLibSubStream(s, System.IO.Compression.CompressionMode.Decompress);

			_openReader = reader;
			return s;
		}

		/// <summary>
		/// Gets the next stream of the specified type
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		public Stream GetNextStream(int type)
		{
			if (_openReader != null)
				throw new InvalidOperationException();

			int next = _next;
			while (next < _items.Count)
			{
				if ((_items[next].ItemType >> 4) == type)
				{
					_next = next;
					return GetNextStream();
				}
				next++;
			}

			return null;
		}

		/// <summary>
		/// Resets this instance.
		/// </summary>
		/// <returns></returns>
		public bool Reset()
		{
			if (_openReader != null)
				throw new InvalidOperationException();

			_next = 0;

			return true;
		}

		internal void CloseStream(MultipleSubStreamReader multiSubStreamReader)
		{
			if (_openReader != multiSubStreamReader)
				throw new InvalidOperationException();

			_openReader = null;
		}
	}
}
