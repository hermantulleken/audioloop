using NAudio.Lame;
using NAudio.Wave;
using static System.Console;

namespace Loop;

/**
	A simply quick-and-dirty program to make MP3s and WAVs loopable, following the following rough process:
	
	1. Read the input file into a byte array
	2. Convert the byte array to a float array
	3. Crossfade the float array with itself
	4. Trim the float array so that the beginning and end match up. 
	5. Convert the float array back to a byte array
	6. Write the byte array to the output file
*/
public static class Program
{
	private static readonly string UsageMessage = @"
Usage:
  program.exe [inputfile] [-f fadeTime] [-h]

Arguments:
  inputfile        The path to the input .mp3 or .wav file. Defaults to " + DefaultInputFile + @". 
  -f fadeTime      The duration of the crossfade in milliseconds. Defaults to " + DefaultCrossfadeDurationInMilliseconds + @".
  -h               Displays this help information.";

	private const int DefaultCrossfadeDurationInMilliseconds = 1000;
	private const string DefaultInputFile = "input.mp3";
	private const int Max16BitInt = 32768;
	private const float Max16BitIntAsFloat = Max16BitInt;
	public static void Main(string[] args)
	{
		if (args.Contains("-h"))
		{
			PrintUsage();
			return;
		}

		int fadeTime = ParseFadeTimeFromArgs(args);

		// Let's assume the file is the first argument if it doesn't start with a hyphen
		// Check if a file argument was provided
		string inputFilePath = args.Length > 0
			? !args[0].StartsWith("-") ? args[0] : DefaultInputFile
			: DefaultInputFile;

		// Check the file extension and call the appropriate method
		string fileExtension = Path.GetExtension(inputFilePath).ToLower();
		string outputFilePath = Path.GetFileNameWithoutExtension(inputFilePath) + "-loop" + fileExtension;

		switch (fileExtension)
		{
			case ".mp3":
				MakeMp3sLoopable(inputFilePath, outputFilePath, fadeTime);
				break;

			case ".wav":
				MakeWavsLoopable(inputFilePath, outputFilePath, fadeTime);
				break;

			default:
				WriteLine("Unsupported file format.");
				break;
		}
	}

	private static void PrintUsage() => WriteLine(UsageMessage);

	private static int ParseFadeTimeFromArgs(IReadOnlyList<string> args)
	{
		for (int i = 0; i < args.Count; i++)
		{
			if (args[i] != "-f" || i + 1 >= args.Count) continue;
			
			if (int.TryParse(args[i + 1], out int fadeTime))
			{
				return fadeTime;
			}
		}

		return DefaultCrossfadeDurationInMilliseconds;
	}


	// Now, modify the MakeMp3sLoopable and MakeWavsLoopable methods to accept the input and output file paths as parameters:
	// ReSharper disable once InconsistentNaming
	private static void MakeMp3sLoopable(string inputMp3File, string outputMp3File, int fadeTime)
	{
		using var reader = new Mp3FileReader(inputMp3File);

		var byteList = new List<byte>();
		int bytesRead;
		byte[] buffer = new byte[reader.Mp3WaveFormat.AverageBytesPerSecond * 4];
		
		while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
		{
			byteList.AddRange(buffer.Take(bytesRead));
		}

		float[] sampleData = ByteToFloat(byteList);
		float[] crossfadedData = Crossfade(sampleData, reader.Mp3WaveFormat.SampleRate, fadeTime);
		float[] trimmedData = Trim(crossfadedData);
		var outputData = FloatToByte(trimmedData);
		
		var outputStream = new MemoryStream();
		using (var writer = new LameMP3FileWriter(outputStream, reader.Mp3WaveFormat, LAMEPreset.STANDARD))
		{
			writer.Write(outputData.ToArray(), 0, outputData.Count);
		}

		File.WriteAllBytes(outputMp3File, outputStream.ToArray());
	}

	private static List<byte> FloatToByte(float[] trimmedData)
	{
		var outputData = new List<byte>();
		foreach (float sample in trimmedData)
		{
			outputData.AddRange(BitConverter.GetBytes((short)(sample * Max16BitInt)));
		}

		return outputData;
	}

	private static float[] ByteToFloat(List<byte> byteList)
	{
		float[] sampleData = new float[byteList.Count / 2];
		
		for (int i = 0, j = 0; i < byteList.Count; i += 2, j++)
		{
			sampleData[j] = BitConverter.ToInt16(byteList.ToArray(), i) / Max16BitIntAsFloat;
		}

		return sampleData;
	}

	private static void MakeWavsLoopable(string inputWavFile, string outputWavFile, int fadeTime)
	{
		using var reader = new AudioFileReader(inputWavFile);
		// Convert the reader to a SampleProvider for easier manipulation
		var samples = reader.ToSampleProvider();

		// Load all samples into a buffer
		float[] sampleData = new float[(int)(reader.Length / sizeof(float))];
		samples.Read(sampleData, 0, sampleData.Length);
		float[] crossfadedData = Crossfade(sampleData, reader.WaveFormat.SampleRate, fadeTime);
		float[] trimmedData = Trim(crossfadedData);

		// Save to output file
		using var writer = new WaveFileWriter(outputWavFile, reader.WaveFormat);
		writer.WriteSamples(trimmedData, 0, trimmedData.Length);
	}
	
	private static float[] Crossfade(float[] sampleData, int sampleRate, int fadeTime)
	{
		int fadeSampleCount = fadeTime * sampleRate / 1000;
		float[] crossfadedData = new float[sampleData.Length * 2 - fadeSampleCount];
		Array.Copy(sampleData, crossfadedData, sampleData.Length);
		Array.Copy(sampleData, 0, crossfadedData, sampleData.Length, sampleData.Length - fadeSampleCount);

		for (int i = 0; i < fadeSampleCount; i++)
		{
			float fadeOutFactor = 1 - (i / (float) fadeSampleCount);
			float fadeInFactor = i / (float) fadeSampleCount;
			crossfadedData[sampleData.Length + i] 
				= sampleData[i] * fadeInFactor 
				  + crossfadedData[sampleData.Length - fadeSampleCount + i] * fadeOutFactor;
		}

		return crossfadedData;
	}

	private static float[] Trim(float[] crossfadedData)
	{
		int samplesToSkip = crossfadedData.Length / 4;
		float[] trimmedData = new float[crossfadedData.Length - 2 * samplesToSkip];
		Array.Copy(crossfadedData, samplesToSkip, trimmedData, 0, trimmedData.Length);
		return trimmedData;
	}
}
