using static System.Console;

namespace Loop;

/**
	A simply quick-and-dirty program to make MP3s and WAVs loopable, following the following rough process:
	
	1. Read the input file into a float array
	2. Crossfade the float array with itself
	3. Trim the float array so that the beginning and end match up. 
	4. Write the float array to the output file
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

	public static void Main(string[] args)
	{
		if (args.Contains("-h"))
		{
			PrintUsage();
			return;
		}

		int fadeTime = ParseFadeTimeFromArgs(args);

		// Assume the file is the first argument if it doesn't start with a hyphen
		// Check if a file argument was provided
		string inputFilePath = args.Length > 0
			? !args[0].StartsWith("-") ? args[0] : DefaultInputFile
			: DefaultInputFile;

		// Check the file extension and call the appropriate method
		string fileExtension = Path.GetExtension(inputFilePath).ToLower();
		string outputFilePath = Path.GetFileNameWithoutExtension(inputFilePath) + "-loop" + fileExtension;

		AudioLooper looper = fileExtension switch
		{
			".mp3" => new Mp3Looper(),
			".wav" => new WaveLooper(),
			_ => throw new Exception($"Unsupported file extension: {fileExtension}")
		};
		
		looper.MakeLoopable(inputFilePath, outputFilePath, fadeTime);
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
}
