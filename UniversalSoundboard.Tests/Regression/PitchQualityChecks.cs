using AudioEffectComponent;

internal static class PitchQualityChecks
{
    public static void Run(Action<bool, string> check)
    {
        foreach (int rate in new[] { 44100, 48000 })
        foreach (float pitch in new[] { 0.5f, 1 / 1.75f, 1 / 1.5f, 0.8f, 1f, 1 / 0.75f, 2f, 4f })
        {
            var samples = Tone(rate, 440);
            var shifter = new PitchShifter();
            Process(shifter, samples, pitch, rate, 441);
            var steady = samples.AsSpan(rate / 2);
            double energy = 0;
            int crossings = 0;
            bool bounded = true;
            for (int i = 1; i < steady.Length; i++)
            {
                bounded &= float.IsFinite(steady[i]) && Math.Abs(steady[i]) < 1;
                energy += steady[i] * steady[i];
                if (steady[i - 1] <= 0 && steady[i] > 0) crossings++;
            }
            double frequency = crossings * (double)rate / steady.Length;
            check(bounded, "finite, bounded pitch output");
            if (pitch == 1)
                check(Math.Abs(Math.Sqrt(energy / steady.Length) - 0.2 / Math.Sqrt(2)) < 0.003,
                    $"unity pitch preserves signal level: RMS={Math.Sqrt(energy / steady.Length)}");
            check(Math.Abs(frequency - 440 * pitch) < 5, $"pitch accuracy {rate}/{pitch}: {frequency}");
            // This phase vocoder attenuates some tones at extreme upward shifts;
            // guard against silence without claiming constant loudness.
            check(Math.Sqrt(energy / steady.Length) > 0.001, $"non-silent steady output {rate}/{pitch}: RMS={Math.Sqrt(energy / steady.Length)}");
        }

        var source = Tone(48000, 440);
        var reference = (float[])source.Clone();
        Process(new PitchShifter(), reference, 0.8f, 48000, 441);
        var chunked = (float[])source.Clone();
        Process(new PitchShifter(), chunked, 0.8f, 48000, 127);
        check(reference.SequenceEqual(chunked), "output independent of audio block size");

        var a = new PitchShifter();
        var b = new PitchShifter();
        var interleaved = (float[])source.Clone();
        var other = Tone(48000, 970);
        for (int offset = 0; offset < source.Length; offset += 441)
        {
            int count = Math.Min(441, source.Length - offset);
            a.PitchShift(0.8f, count, 48000, interleaved.AsSpan(offset, count));
            b.PitchShift(2f, count, 48000, other.AsSpan(offset, count));
        }
        check(reference.SequenceEqual(interleaved), "simultaneous sounds/channels cannot contaminate each other");
        a.Reset();
        var reset = (float[])source.Clone();
        Process(a, reset, 0.8f, 48000, 441);
        check(reference.SequenceEqual(reset), "reset removes queued audio and phase history");

        var silent = new float[48000];
        Process(new PitchShifter(), silent, 2f, 48000, 441);
        check(silent.All(x => x == 0), "silent channel stays silent");

        var changing = Tone(48000, 440);
        var live = new PitchShifter();
        float[] pitches = { 0.5f, 0.8f, 1.3333334f, 2f, 4f };
        for (int offset = 0; offset < changing.Length; offset += 441)
        {
            int count = Math.Min(441, changing.Length - offset);
            live.PitchShift(pitches[(offset / 441) % pitches.Length], count, 48000,
                changing.AsSpan(offset, count));
        }
        check(changing.All(x => float.IsFinite(x) && Math.Abs(x) < 1),
            "rapid parameter changes produce finite, bounded output");

        // Warm the code before measuring managed allocations in the audio path.
        live.PitchShift(0.8f, 441, 48000, changing.AsSpan(0, 441));
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 20; i++)
            live.PitchShift(0.8f, 441, 48000, changing.AsSpan(0, 441));
        check(GC.GetAllocatedBytesForCurrentThread() == allocatedBefore,
            "DSP processing allocates no managed memory per block");
    }

    private static float[] Tone(int rate, double frequency) => Enumerable.Range(0, rate)
        .Select(i => (float)(0.2 * Math.Sin(2 * Math.PI * frequency * i / rate))).ToArray();

    private static void Process(PitchShifter shifter, float[] samples, float pitch, int rate, int block)
    {
        for (int offset = 0; offset < samples.Length; offset += block)
        {
            int count = Math.Min(block, samples.Length - offset);
            shifter.PitchShift(pitch, count, rate, samples.AsSpan(offset, count));
        }
    }
}
