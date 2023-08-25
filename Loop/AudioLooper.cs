using NAudio.Wave;

namespace Loop;

/// <summary>
/// Provides a method <see cref="MakeLoopable"/>to make an audio file loopable.
/// </summary>
public abstract class AudioLooper
{
	/// <summary>
	/// A simply quick-and-dirty program to make audio files loopable:
	/// </summary>
	/// <remarks>
	/// The algorithm does the following:
	/// <list type="numbered">
	/// <item>Read the input file into a float array</item>
	/// <item>Crossfade the float array with itself</item>
	/// <item>Trim the float array so that the beginning and end match up.</item>
	/// <item>Write the float array to the output file</item>
	/// </list>
	/// </remarks>
	public void MakeLoopable(string inputMp3File, string outputMp3File, int fadeTime)
	{
		var (sampleData, waveFormat) = Load(inputMp3File);
		float[] crossfadedData = Crossfade(sampleData, waveFormat.SampleRate, waveFormat.Channels, fadeTime);
		float[] trimmedData = Trim(crossfadedData, sampleData.Length / 2);
		Save(outputMp3File, trimmedData, waveFormat);
	}
	
	/// <summary>
	/// Load the audio file into a float array and return it along with the <see cref="WaveFormat"/>.
	/// </summary>
	/// <param name="inputPath">The path to the input audio file.</param>
	protected abstract  (float[], WaveFormat)  Load(string inputPath );
	
	/// <summary>
	/// Save the float array representing an audio clip to the output file.
	/// </summary>
	/// <param name="outputPath"> The path to the output file. The file extension determines the format. </param>
	/// <param name="trimmedData"> The float array representing the audio clip. </param>
	/// <param name="waveFormat"> The <see cref="WaveFormat"/> of the audio clip. </param>
	protected abstract  void Save(string outputPath, float[] trimmedData, WaveFormat waveFormat);
	
	/**
		The result of this method is the clip crossfaded into itself. Provided the clip is long enough,
		the middle of the clip will be repeated twice, with a crossfade between the two. this section
		is then trimmed by the Trim method. 
		
		0000012356788888            //original
		           0000012356788888 //copy
		000001235678642012356788888 //crossfaded, result of this method
		56786420123                 //Loopable, result of trim method
	*/
	private static float[] Crossfade(float[] sampleData, int sampleRate, int waveFormatChannels, int fadeTime)
	{
		int fadeSampleCount = waveFormatChannels * (fadeTime * sampleRate / 1000);
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
