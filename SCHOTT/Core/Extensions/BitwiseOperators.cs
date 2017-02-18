namespace SCHOTT.Core.Extensions
{
	
	/// <summary>
	/// Binary Operators for uint types
	/// </summary>
	public static class uintExtensions
	{
		/// <summary>
		/// Checks the state of a particular bit in a unsigned integer.
		/// </summary>
		/// <param name="number">The number to run the bitwise operation on.</param>
		/// <param name="position">The position in the number to run the bitwise operation on.</param>
		/// <returns>True if bit at position == 1</returns>
		public static bool CheckBit(this uint number, int position)
		{
			return (number & ((uint)1 << position)) != 0;
		}

		/// <summary>
		/// Sets the state of a particular bit in a unsigned integer.
		/// </summary>
		/// <param name="number">The number to run the bitwise operation on.</param>
		/// <param name="position">The position in the number to run the bitwise operation on.</param>
		/// <returns></returns>
		public static uint SetBit(this uint number, int position)
		{
			return (uint)(number | (uint)1 << position);
		}

		/// <summary>
		/// Clears the state of a particular bit in a unsigned integer.
		/// </summary>
		/// <param name="number">The number to run the bitwise operation on.</param>
		/// <param name="position">The position in the number to run the bitwise operation on.</param>
		/// <returns></returns>
		public static uint ClearBit(this uint number, int position)
		{
			return (uint)(number & ~((uint)1 << position));
		}

		/// <summary>
		/// Toggles the state of a particular bit in a unsigned integer.
		/// </summary>
		/// <param name="number">The number to run the bitwise operation on.</param>
		/// <param name="position">The position in the number to run the bitwise operation on.</param>
		/// <returns></returns>
		public static uint ToggleBit(this uint number, int position)
		{
			return (uint)(number ^ (uint)1 << position);
		}
		
		/// <summary>
		/// Rotating Bit Shift Left
		/// </summary>
		/// <param name="value">Value to bitshift</param>
		/// <param name="count">How many bits to shift</param>
		/// <returns></returns>
		public static uint RotateLeft(this uint value, int count = 1)
		{
			const int size = sizeof(uint) << 3;
			count &= size - 1;
			return (uint)((value << count) | (value >> ((sizeof(uint) << 3) - count)));
		}

		/// <summary>
		/// Rotating Bit Shift Right
		/// </summary>
		/// <param name="value">Value to bitshift</param>
		/// <param name="count">How many bits to shift</param>
		/// <returns></returns>
		public static uint RotateRight(this uint value, int count = 1)
		{
			const int size = sizeof(uint) << 3;
			count &= size - 1;
			return (uint)((value >> count) | (value << ((sizeof(uint) << 3) - count)));
		}
	}
	
	/// <summary>
	/// Binary Operators for ushort types
	/// </summary>
	public static class ushortExtensions
	{
		/// <summary>
		/// Checks the state of a particular bit in a unsigned integer.
		/// </summary>
		/// <param name="number">The number to run the bitwise operation on.</param>
		/// <param name="position">The position in the number to run the bitwise operation on.</param>
		/// <returns>True if bit at position == 1</returns>
		public static bool CheckBit(this ushort number, int position)
		{
			return (number & ((ushort)1 << position)) != 0;
		}

		/// <summary>
		/// Sets the state of a particular bit in a unsigned integer.
		/// </summary>
		/// <param name="number">The number to run the bitwise operation on.</param>
		/// <param name="position">The position in the number to run the bitwise operation on.</param>
		/// <returns></returns>
		public static ushort SetBit(this ushort number, int position)
		{
			return (ushort)(number | (ushort)1 << position);
		}

		/// <summary>
		/// Clears the state of a particular bit in a unsigned integer.
		/// </summary>
		/// <param name="number">The number to run the bitwise operation on.</param>
		/// <param name="position">The position in the number to run the bitwise operation on.</param>
		/// <returns></returns>
		public static ushort ClearBit(this ushort number, int position)
		{
			return (ushort)(number & ~((ushort)1 << position));
		}

		/// <summary>
		/// Toggles the state of a particular bit in a unsigned integer.
		/// </summary>
		/// <param name="number">The number to run the bitwise operation on.</param>
		/// <param name="position">The position in the number to run the bitwise operation on.</param>
		/// <returns></returns>
		public static ushort ToggleBit(this ushort number, int position)
		{
			return (ushort)(number ^ (ushort)1 << position);
		}
		
		/// <summary>
		/// Rotating Bit Shift Left
		/// </summary>
		/// <param name="value">Value to bitshift</param>
		/// <param name="count">How many bits to shift</param>
		/// <returns></returns>
		public static ushort RotateLeft(this ushort value, int count = 1)
		{
			const int size = sizeof(ushort) << 3;
			count &= size - 1;
			return (ushort)((value << count) | (value >> ((sizeof(ushort) << 3) - count)));
		}

		/// <summary>
		/// Rotating Bit Shift Right
		/// </summary>
		/// <param name="value">Value to bitshift</param>
		/// <param name="count">How many bits to shift</param>
		/// <returns></returns>
		public static ushort RotateRight(this ushort value, int count = 1)
		{
			const int size = sizeof(ushort) << 3;
			count &= size - 1;
			return (ushort)((value >> count) | (value << ((sizeof(ushort) << 3) - count)));
		}
	}
	
	/// <summary>
	/// Binary Operators for byte types
	/// </summary>
	public static class byteExtensions
	{
		/// <summary>
		/// Checks the state of a particular bit in a unsigned integer.
		/// </summary>
		/// <param name="number">The number to run the bitwise operation on.</param>
		/// <param name="position">The position in the number to run the bitwise operation on.</param>
		/// <returns>True if bit at position == 1</returns>
		public static bool CheckBit(this byte number, int position)
		{
			return (number & ((byte)1 << position)) != 0;
		}

		/// <summary>
		/// Sets the state of a particular bit in a unsigned integer.
		/// </summary>
		/// <param name="number">The number to run the bitwise operation on.</param>
		/// <param name="position">The position in the number to run the bitwise operation on.</param>
		/// <returns></returns>
		public static byte SetBit(this byte number, int position)
		{
			return (byte)(number | (byte)1 << position);
		}

		/// <summary>
		/// Clears the state of a particular bit in a unsigned integer.
		/// </summary>
		/// <param name="number">The number to run the bitwise operation on.</param>
		/// <param name="position">The position in the number to run the bitwise operation on.</param>
		/// <returns></returns>
		public static byte ClearBit(this byte number, int position)
		{
			return (byte)(number & ~((byte)1 << position));
		}

		/// <summary>
		/// Toggles the state of a particular bit in a unsigned integer.
		/// </summary>
		/// <param name="number">The number to run the bitwise operation on.</param>
		/// <param name="position">The position in the number to run the bitwise operation on.</param>
		/// <returns></returns>
		public static byte ToggleBit(this byte number, int position)
		{
			return (byte)(number ^ (byte)1 << position);
		}
		
		/// <summary>
		/// Rotating Bit Shift Left
		/// </summary>
		/// <param name="value">Value to bitshift</param>
		/// <param name="count">How many bits to shift</param>
		/// <returns></returns>
		public static byte RotateLeft(this byte value, int count = 1)
		{
			const int size = sizeof(byte) << 3;
			count &= size - 1;
			return (byte)((value << count) | (value >> ((sizeof(byte) << 3) - count)));
		}

		/// <summary>
		/// Rotating Bit Shift Right
		/// </summary>
		/// <param name="value">Value to bitshift</param>
		/// <param name="count">How many bits to shift</param>
		/// <returns></returns>
		public static byte RotateRight(this byte value, int count = 1)
		{
			const int size = sizeof(byte) << 3;
			count &= size - 1;
			return (byte)((value >> count) | (value << ((sizeof(byte) << 3) - count)));
		}
	}
}