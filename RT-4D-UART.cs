/* Copyright 2025 Dual Tachyon
 * https://github.com/DualTachyon
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 *     Unless required by applicable law or agreed to in writing, software
 *     distributed under the License is distributed on an "AS IS" BASIS,
 *     WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *     See the License for the specific language governing permissions and
 *     limitations under the License.
 */

using System;
using System.IO.Ports;

public class RT4D_UART {
	private SerialPort Port;

	private void Checksum(byte[] Command)
	{
		byte Sum = 0x48;
		int i;

		for (i = 0; i < Command.Length - 1; i++) {
			Sum += Command[i];
		}
		Command[i] = Sum;
	}

	private bool Verify(byte[] Command)
	{
		byte Sum = 0x48;
		int i;

		for (i = 0; i < Command.Length - 1; i++) {
			Sum += Command[i];
		}
		if (Command[i] == Sum) {
			return true;
		}

		return false;
	}

	public bool Command_EraseFlash()
	{
		byte[] Command = new byte[5];

		Command[0] = 0x39;
		Command[3] = 0x55;
		Checksum(Command);
		Port.Write(Command, 0, Command.Length);

		for (int i = 0; i < 400; i++) {
			try {
				return (Port.ReadByte() == 0x06) ? true : false;
			} catch {
			}
		}

		Console.WriteLine("Timed out erasing flash!");

		return false;
	}

	public bool Command_WriteFlash(UInt32 Offset, byte[] Data)
	{
		byte[] Command = new byte[1024 + 4];

		Command[0] = 0x57;
		Command[1] = (byte)((Offset >> 8) & 0xFF);
		Command[2] = (byte)((Offset >> 0) & 0xFF);
		int ChunkLength = (Data.Length - (int)Offset);
		if (ChunkLength > 1024) {
			ChunkLength = 1024;
		}
		Array.Copy(Data, Offset, Command, 3, ChunkLength);
		Checksum(Command);
		Port.Write(Command, 0, Command.Length);

		for (int i = 0; i < 100; i++) {
			try {
				return (Port.ReadByte() == 0x06) ? true : false;
			} catch {
			}
		}

		Console.WriteLine("Timed out writing to flash!");

		return false;
	}

	public void Open(string ComPort, int BaudRate = 115200)
	{
		Port = new SerialPort(ComPort);
		Port.BaudRate = BaudRate;
		Port.Parity = Parity.None;
		Port.StopBits = StopBits.One;
		Port.DataBits = 8;
		Port.ReadTimeout = 10;
		Port.WriteTimeout = 10;
		Port.Open();
	}

	public int Read()
	{
		try {
			return Port.ReadByte();
		} catch {
			return -1;
		}
	}

	public int Write(byte Data)
	{
		try {
			byte[] Buf = new [] { Data };
			Port.Write(Buf, 0, Buf.Length);
			return 0;
		} catch {
			return -1;
		}
	}

	public void AddDelegate(SerialDataReceivedEventHandler DataReceived)
	{
		Port.DataReceived += DataReceived;
	}

	public void Close()
	{
		Port.Close();
	}
}
