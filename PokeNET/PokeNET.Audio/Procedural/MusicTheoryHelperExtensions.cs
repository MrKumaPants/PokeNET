using Melanchall.DryWetMidi.MusicTheory;

namespace PokeNET.Audio.Procedural
{
    public static partial class MusicTheoryHelper
    {
        /// <summary>
        /// Transposes a note name by semitones
        /// </summary>
        private static NoteName GetTransposedNoteName(NoteName noteName, int semitones)
        {
            var noteValue = (int)noteName;
            var transposed = (noteValue + semitones) % 12;
            if (transposed < 0)
                transposed += 12;
            return (NoteName)transposed;
        }
    }
}
