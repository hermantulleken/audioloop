using NAudio.Lame;
using NAudio.Wave;

namespace Loop;

public class Mp3Looper : AudioLooper
{
	private const int Max16BitInt = 32768;
	private const float Max16BitIntAsFloat = Max16BitInt;
	
	protected override void Save(string outputPath, float[] trimmedData, WaveFormat waveFormat)
	{
		var outputData = FloatToByte(trimmedData);

		var outputStream = new MemoryStream();
		using (var writer = new LameMP3FileWriter(outputStream, waveFormat, LAMEPreset.STANDARD))
		{
			writer.Write(outputData.ToArray(), 0, outputData.Count);
		}

		File.WriteAllBytes(outputPath, outputStream.ToArray());
	}

	protected override (float[], WaveFormat) Load(string inputMp3File)
	{
		using var reader = new Mp3FileReader(inputMp3File);
		var byteList = new List<byte>();
		int bytesRead;
		byte[] buffer = new byte[reader.Mp3WaveFormat.AverageBytesPerSecond * 4];

		while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
		{
			byteList.AddRange(buffer.Take(bytesRead));
		}

		float[] sampleData = ByteToFloat(byteList.ToArray());
		return (sampleData, reader.Mp3WaveFormat);
	}

	private static List<byte> FloatToByte(float[] trimmedData) 
		=> trimmedData.SelectMany(sample => BitConverter.GetBytes((short)(sample * Max16BitInt))).ToList();

	private static float[] ByteToFloat(byte[] bytes) =>
		Enumerable.Range(0, bytes.Length / 2)
			.Select(i => BitConverter.ToInt16(bytes, i * 2) / Max16BitIntAsFloat)
			.ToArray();
}
