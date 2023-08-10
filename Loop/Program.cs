using NAudio.Lame;
using NAudio.Wave;

namespace Loop;

public static class Program
{
	private const int DefaultCrossfadeDurationInMilliseconds = 1000; // This is F
	private const string DefaultInputFile = "input.mp3";
	public static void Main(string[] args)
	{
		if (args.Contains("-h"))
		{
			PrintUsage();
			return;
		}
		
		string inputFilePath;
		int fadeTime = ParseFadeTimeFromArgs(args);

		// Let's assume the file is the first argument if it doesn't start with a hyphen
		// Check if a file argument was provided
		inputFilePath = args.Length > 0
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
				Console.WriteLine("Unsupported file format.");
				break;
		}
	}

	private static void PrintUsage()
	{
		Console.WriteLine("Usage:");
		Console.WriteLine("  program.exe [inputfile] [-f fadeTime] [-h]");
		Console.WriteLine();
		Console.WriteLine("Arguments:");
		Console.WriteLine($"  inputfile        The path to the input .mp3 or .wav file. Defaults to {DefaultInputFile}.");
		Console.WriteLine($"  -f fadeTime      The duration of the crossfade in milliseconds. Defaults to {DefaultCrossfadeDurationInMilliseconds}.");
		Console.WriteLine($"  -h               Displays this help information.");
	}
	
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
		Console.WriteLine($"Original MP3 Duration: {reader.TotalTime.TotalSeconds} seconds");

		var byteList = new List<byte>();
		int bytesRead;
		byte[] buffer = new byte[reader.Mp3WaveFormat.AverageBytesPerSecond * 4];
		
		while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
		{
			byteList.AddRange(buffer.Take(bytesRead));
		}

		// Convert the bytes to float samples for crossfading
		// This step assumes that the MP3 is stereo and 16-bit
		float[] sampleData = new float[byteList.Count / 2];
		for (int i = 0, j = 0; i < byteList.Count; i += 2, j++)
		{
			sampleData[j] = BitConverter.ToInt16(byteList.ToArray(), i) / 32768f;
		}

		Crossfade(sampleData, out float[] crossfadedData, reader.Mp3WaveFormat.SampleRate, fadeTime);

		float[] trimmedData = Trim(crossfadedData);

		// Convert samples back to bytes for MP3 output
		var outputData = new List<byte>();
		foreach (float sample in trimmedData)
		{
			outputData.AddRange(BitConverter.GetBytes((short)(sample * 32768)));
		}

		var outputStream = new MemoryStream();
		using (var writer = new LameMP3FileWriter(outputStream, reader.Mp3WaveFormat, LAMEPreset.STANDARD))
		{
			writer.Write(outputData.ToArray(), 0, outputData.Count);
		}

		File.WriteAllBytes(outputMp3File, outputStream.ToArray());
	}

	private static void MakeWavsLoopable(string inputWavFile, string outputWavFile, int fadeTime)
	{
		using var reader = new AudioFileReader(inputWavFile);
		Console.WriteLine($"Original WAV Duration: {reader.TotalTime.TotalSeconds} seconds");

		// Convert the reader to a SampleProvider for easier manipulation
		var samples = reader.ToSampleProvider();

		// Load all samples into a buffer
		float[] sampleData = new float[(int)(reader.Length / sizeof(float))];
		samples.Read(sampleData, 0, sampleData.Length);

		Crossfade(sampleData, out float[] crossfadedData, reader.WaveFormat.SampleRate, fadeTime);

		float[] trimmedData = Trim(crossfadedData);

		// Save to output file
		using var writer = new WaveFileWriter(outputWavFile, reader.WaveFormat);
		writer.WriteSamples(trimmedData, 0, trimmedData.Length);
	}
	
	private static void Crossfade(float[] sampleData, out float[] crossfadedData, int sampleRate, int fadeTime)
	{
		int fadeSampleCount = fadeTime * sampleRate / 1000;

		crossfadedData = new float[sampleData.Length * 2 - fadeSampleCount];
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
	}

	private static float[] Trim(float[] crossfadedData)
	{
		int samplesToSkip = crossfadedData.Length / 4; // Original length was halved, so we divide by 4 to get C/2
		float[] trimmedData = new float[crossfadedData.Length - 2 * samplesToSkip];
		Array.Copy(crossfadedData, samplesToSkip, trimmedData, 0, trimmedData.Length);
		return trimmedData;
	}
}
