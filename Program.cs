using System;
using System.IO.Ports;
using System.Diagnostics;

namespace RT_4D_Flasher_CLI {
	internal class Program {
		static void Usage()
		{
			string[] Parts = Environment.CommandLine.Split(new char[] { System.IO.Path.DirectorySeparatorChar });
			string Exe = Parts[Parts.Length - 1];
			Exe = Exe.Trim();
			Console.WriteLine("Usage:");
			Console.WriteLine("\t" + Exe + " -l                        List available COM ports");
			Console.WriteLine("\t" + Exe + " -p COMx -f firmware.bin   Flash a file");
		}

		static void Main(string[] args)
		{
			Console.WriteLine("RT-4D-Flash-CLI (c) Copyright 2025 Dual Tachyon\n");
			switch (args.Length) {
			case 1:
				if (args[0] != "-l") {
					Usage();
					break;
				}
				var Ports = SerialPort.GetPortNames();
				Console.Write("Ports available:");
				foreach (var Port in Ports) {
					Console.Write(" " + Port);
				}
				Console.WriteLine();
				break;

			case 4:
				if (args[0] != "-p") {
					Usage();
					break;
				}
				if (args[2] != "-f") {
					Usage();
					break;
				}

				// RT4D firmware must be padded to 251,904 bytes for proper reboot
				const int FW_4D_FLASH_SIZE = 251904;
				byte[] Firmware = new byte[FW_4D_FLASH_SIZE];
				try {
					byte[] FileData = System.IO.File.ReadAllBytes(args[3]);
					if (FileData.Length > FW_4D_FLASH_SIZE) {
						Console.WriteLine("Firmware file is too large! Maximum size is " + FW_4D_FLASH_SIZE + " bytes.");
						break;
					}
					Array.Copy(FileData, Firmware, FileData.Length);
					Console.WriteLine("Loaded firmware: " + FileData.Length + " bytes (padded to " + FW_4D_FLASH_SIZE + " bytes)");
				} catch {
					Console.WriteLine("Failed to read file!");
					break;
				}

				RT4D_UART RT = new RT4D_UART();
				Console.WriteLine("Opening COM port...");
				try {
					var Now = Stopwatch.StartNew();
					RT.Open(args[1]);
					Now.Stop();
					if (Now.ElapsedMilliseconds >= 1000) {
						Console.WriteLine("COM port seems stuck! Please reinsert the USB cable...");
						break;
					}
				} catch {
					Console.WriteLine("Failed to open COM port!");
					break;
				}

				bool Skip = false;
				bool Quit = false;

				Console.WriteLine("Probing RT-4D...");
				while (!Quit) {
					int Data = RT.Read();
					switch (Data) {
					case -1:
						RT.Write(0xFF);
						break;

					case 0xFF:
						Quit = true;
						break;

					default:
						if (!Skip) {
							Skip = true;
							Console.WriteLine("Unexpected serial data received!");
						}
						break;
					}
				}

				try {
					Console.WriteLine("Erasing flash...");
					if (!RT.Command_EraseFlash()) {
						Console.WriteLine("Failed to erase flash!");
						break;
					}
				} catch (Exception Ex) {
					Console.WriteLine("\rUnexpected failure erasing flash! Error: ", Ex.Message);
					break;
				}

				try {
					UInt32 i;

					for (i = 0; i < FW_4D_FLASH_SIZE; i += 1024) {
						Console.Write("\rFlashing at 0x" + i.ToString("X4"));
						Console.Out.Flush();
						if (!RT.Command_WriteFlash(i, Firmware)) {
							Console.WriteLine("\rFailed to flash at 0x" + i.ToString("X4") + "!");
							Console.Out.Flush();
							break;
						}
					}
					if (i == FW_4D_FLASH_SIZE) {
						Console.WriteLine("\rFlashing complete! ");
					}
				} catch (Exception Ex) {
					Console.WriteLine("\rUnexpected failure writing to flash! Error: ", Ex.Message);
				}
				Console.WriteLine();
				RT.Close();
				break;

			default:
				Usage();
				break;
			}
		}
	}
}
