using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using QQn.TurtleUtils.Cryptography;

namespace QQn.TurtleUtils.IO
{
	/// <summary>
	/// Stream proxy which creates a hashed and optionally signed stream within a parent stream
	/// </summary>
	/// <remarks>The public key of a parent <see cref="AssuredStream"/> is used for signing the substream. 
	/// All between streams must implement <see cref="IServiceProvider"/> to allow finding a parent <see cref="AssuredStream"/></remarks>
	public class AssuredSubStream : ProxyStream
	{
		readonly StrongNameKey _snk;
		readonly HashType _hashType;
		long _headerPosition;
		long _hashPosition;
		byte[] _streamHash;
		byte[] _hashSignature;
		long _hashLength;
		bool _updating;
		

		/// <summary>
		/// Initializes a new instance of the <see cref="AssuredSubStream"/> class.
		/// </summary>
		/// <param name="parentStream">The parent stream.</param>
		/// <param name="verificationMode">The verification mode.</param>
		public AssuredSubStream(Stream parentStream, VerificationMode verificationMode)
			: base(parentStream, true)
		{
			AssuredStream signedParent = GetService<AssuredStream>();

			if (signedParent != null)
			{
				_snk = signedParent.AssemblyStrongNameKey;
				_hashType = signedParent.HashType;
			}
			else
				_hashType = HashType.SHA1;
			
			_headerPosition = parentStream.Position;

			_streamHash = new byte[QQnCryptoHelpers.GetHashBits(_hashType) / 8];
			_hashSignature = new byte[(_snk != null) ? _snk.SignatureLength : 0];

			if (parentStream.CanWrite && parentStream.CanSeek && parentStream.Position == parentStream.Length)
			{
				_updating = true;
				WriteHeader();
			}
			else
			{
				QQnBinaryReader br = new QQnBinaryReader(BaseStream);
				_streamHash = br.ReadBytes(_streamHash.Length);
				_hashSignature = br.ReadBytes(_hashSignature.Length);				
				_hashLength = br.ReadInt64();
				_hashPosition = BaseStream.Position;

				if(verificationMode != VerificationMode.None)
				{
					if(_snk != null && !_snk.VerifyHash(_streamHash, _hashSignature))
						throw new CryptographicException("Stream hash verification failed");
				}

				if (verificationMode == VerificationMode.Full)
				{
					if (!VerifyHash())
						throw new CryptographicException("Invalid hash value");
				}
			}
		}

		/// <summary>
		/// Verifies the hash.
		/// </summary>
		/// <returns></returns>
		public bool VerifyHash()
		{
			byte[] streamHash = CalculateHash(false);

			if (streamHash.Length == _streamHash.Length && _hashLength == Length)
			{
				for (int i = 0; i < streamHash.Length; i++)
				{
					if (streamHash[i] != _streamHash[i])
						return false;
				}
				return true;
			}
			return false;			
		}

		void WriteHeader()
		{
			if(BaseStream.Position != _headerPosition)
				BaseStream.Position = _headerPosition;

			QQnBinaryWriter bw = new QQnBinaryWriter(BaseStream);
			bw.Write(_streamHash);
			bw.Write(_hashSignature);			
			bw.Write(_hashLength);
			_hashPosition = BaseStream.Position;
		}

		/// <summary>
		/// Translates a position in the parent stream to one in the substream
		/// </summary>
		/// <param name="parentPosition">The parent position.</param>
		/// <returns></returns>
		protected sealed override long PositionToSubStream(long parentPosition)
		{
			return parentPosition - _hashPosition;
		}

		/// <summary>
		/// Translates a position in the substream to one in the parent stream
		/// </summary>
		/// <param name="subStreamPosition">The sub stream position.</param>
		/// <returns></returns>
		protected sealed override long PositionToParent(long subStreamPosition)
		{
			return subStreamPosition + _hashPosition;
		}

		/// <summary>
		/// Gets a string representation of the stream-hash.
		/// </summary>
		/// <value>The hash string.</value>
		/// <remarks>When creating the hash is not valid until after closing the stream</remarks>
		public string HashString
		{
			get { return QQnCryptoHelpers.HashString(_streamHash); }
		}

		/// <summary>
		/// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
		/// <param name="count">The number of bytes to be written to the current stream.</param>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		/// <exception cref="T:System.NotSupportedException">The stream does not support writing. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		/// <exception cref="T:System.ArgumentNullException">buffer is null. </exception>
		/// <exception cref="T:System.ArgumentException">The sum of offset and count is greater than the buffer length. </exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">offset or count is negative. </exception>
		public override void Write(byte[] buffer, int offset, int count)
		{			
			base.Write(buffer, offset, count);
			_updating = true;
		}

		/// <summary>
		/// Sets the length of the current stream
		/// </summary>
		/// <param name="value">The desired length of the current stream in bytes.</param>
		/// <exception cref="T:System.NotSupportedException">The stream does not support both writing and seeking, such as if the stream is constructed from a pipe or console output. </exception>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		public override void SetLength(long value)
		{
			base.SetLength(value);
			_updating = true;
		}

		bool _closed;
		/// <summary>
		/// Closes the stream
		/// </summary>
		public override void Close()
		{
			if (_closed)
				return;

			_closed = true;

			if (_updating)
				UpdateHash();

			base.Close();
		}

		/// <summary>
		/// Updates the hash.
		/// </summary>
		protected virtual void UpdateHash()
		{
			byte[] fileHash = CalculateHash(true);
			_streamHash = fileHash;

			if (_snk != null)
			{
				if (_snk.PublicOnly)
					throw new InvalidOperationException();

				byte[] signature = _snk.SignHash(fileHash);

				if (signature.Length != _hashSignature.Length)
					throw new InvalidOperationException();
				else if (fileHash.Length != _streamHash.Length)
					throw new InvalidOperationException();
				
				_hashSignature = signature;
			}			

			WriteHeader();
		}

		byte[] CalculateHash(bool create)
		{
			if (create)
				_hashLength = Length;

			long oldPos = Position;
			Position = 0;
			using (HashAlgorithm hasher = QQnCryptoHelpers.CreateHashAlgorithm(_hashType))
			{
				byte[] buffer;
				using (MemoryStream ms = new MemoryStream(16)) // Use memorystream and writer to resolve endian-issues
				using(QQnBinaryWriter bw = new QQnBinaryWriter(ms))
				{					
					bw.Write(_hashLength);
					buffer = ms.ToArray();
				}

				hasher.TransformBlock(buffer, 0, buffer.Length, null, 0);
				
				buffer = new byte[8192];

				int nRead;

				while (0 != (nRead = Read(buffer, 0, buffer.Length)))
				{
					hasher.TransformBlock(buffer, 0, nRead, null, 0);
				}

				hasher.TransformFinalBlock(buffer, 0, 0);

				if (CanSeek)
					Position = oldPos;

				return hasher.Hash;
			}
		}

		#region Sealed overrides. They are called indirectly from the constructor; see fxcop report if unsealed
		/// <summary>
		/// Gets the length in bytes of the stream.
		/// </summary>
		/// <value></value>
		/// <returns>A long value representing the length of the stream in bytes.</returns>
		/// <exception cref="T:System.NotSupportedException">A class derived from Stream does not support seeking. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		public sealed override long Length
		{
			get { return base.Length; }
		}

		/// <summary>
		/// Gets or sets the position within the current stream.
		/// </summary>
		/// <value></value>
		/// <returns>The current position within the stream.</returns>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		/// <exception cref="T:System.NotSupportedException">The stream does not support seeking. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		public sealed override long Position
		{
			get
			{
				return base.Position;
			}
			set
			{
				base.Position = value;
			}
		}

		/// <summary>
		/// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
		/// </summary>
		/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
		/// <param name="count">The maximum number of bytes to be read from the current stream.</param>
		/// <returns>
		/// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">The sum of offset and count is larger than the buffer length. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		/// <exception cref="T:System.NotSupportedException">The stream does not support reading. </exception>
		/// <exception cref="T:System.ArgumentNullException">buffer is null. </exception>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">offset or count is negative. </exception>
		public sealed override int Read(byte[] buffer, int offset, int count)
		{
			return base.Read(buffer, offset, count);
		}

		/// <summary>
		/// Gets a value indicating whether the current stream supports seeking.
		/// </summary>
		/// <value></value>
		/// <returns>true if the stream supports seeking; otherwise, false.</returns>
		public sealed override bool CanSeek
		{
			get { return base.CanSeek; }
		}
		#endregion
	}
}
