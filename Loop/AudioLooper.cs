using NAudio.Wave;

namespace Loop;

public abstract class AudioLooper
{
	public abstract void MakeLoopable(string inputMp3File, string outputMp3File, int fadeTime);
}

public abstract class AudioLooper<TWaveFormat> : AudioLooper
	where TWaveFormat : WaveFormat
{
	public override void MakeLoopable(string inputMp3File, string outputMp3File, int fadeTime)
	{
		var (sampleData, waveFormat) = Load(inputMp3File);
		float[] crossfadedData = Crossfade(sampleData, waveFormat.SampleRate, fadeTime);
		float[] trimmedData = Trim(crossfadedData, sampleData.Length / 2);
		Save(outputMp3File, trimmedData, waveFormat);
	}
	
	protected abstract  (float[], TWaveFormat)  Load(string inputPath );
	protected abstract  void Save(string outputPath, float[] trimmedData, TWaveFormat waveFormat);
	
	private static float[] Crossfade(float[] sampleData, int sampleRate, int fadeTime)
	{
		int fadeSampleCount = 2 * (fadeTime * sampleRate / 1000);
		float[] crossfadedData = new float[sampleData.Length * 2 - fadeSampleCount];
		Array.Copy(sampleData, crossfadedData, sampleData.Length - fadeSampleCount);
		Array.Copy(sampleData, fadeSampleCount, crossfadedData, sampleData.Length, sampleData.Length - fadeSampleCount);

		for (int i = 0; i < fadeSampleCount; i++)
		{
			float fadeInFactor = i / (float) fadeSampleCount;
			float fadeOutFactor = 1 - fadeInFactor;
			
			crossfadedData[sampleData.Length - fadeSampleCount + i] 
				= sampleData[i] * fadeInFactor 
				  + sampleData[sampleData.Length - fadeSampleCount + i] * fadeOutFactor;
		}

		return crossfadedData;
	}

	private static float[] Trim(float[] crossfadedData, int samplesToSkip)
	{
		float[] trimmedData = new float[crossfadedData.Length - 2 * samplesToSkip];
		Array.Copy(crossfadedData, samplesToSkip, trimmedData, 0, trimmedData.Length);
		return trimmedData;
	}
}
