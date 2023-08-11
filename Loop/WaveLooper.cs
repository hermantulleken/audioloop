using NAudio.Wave;

namespace Loop;

public class WaveLooper : AudioLooper<WaveFormat>
{
	protected override (float[], WaveFormat) Load(string inputPath) 
	{
		using var reader = new AudioFileReader(inputPath);
		// Convert the reader to a SampleProvider for easier manipulation
		var samples = reader.ToSampleProvider();

		// Load all samples into a buffer
		var sampleData = new float[(int)(reader.Length / sizeof(float))];
		samples.Read(sampleData, 0, sampleData.Length);
		return (sampleData, reader.WaveFormat);
	}

	protected override void Save(string outputPath, float[] trimmedData, WaveFormat waveFormat)
	{		
		using var writer = new WaveFileWriter(outputPath, waveFormat);
		writer.WriteSamples(trimmedData, 0, trimmedData.Length);
	}
}