using NDiscoPlus.Shared.Analyzer.Analysis;
using SpotifyAPI.Web;
using System.Collections.Immutable;

namespace NDiscoPlus.Shared.Models;

internal class Context
{
    public Random Random { get; }
    public NDPColorPalette Palette { get; }

    public AudioAnalysis Analysis { get; }

    public Context(Random random, NDPColorPalette palette, AudioAnalysis analysis)
    {
        Random = random;
        Palette = palette;
        Analysis = analysis;
    }
}

internal sealed class EffectContext : Context
{
    private EffectContext(Random random, NDPColorPalette palette, AudioAnalysis analysis, AudioAnalysisSection section) : base(random, palette, analysis)
    {
        Section = section;
    }

    public static EffectContext Extend(Context context, AudioAnalysisSection section)
    {
        return new EffectContext(
            random: context.Random,
            palette: context.Palette,
            analysis: context.Analysis,
            section: section
        );
    }

    public AudioAnalysisSection Section { get; }
}