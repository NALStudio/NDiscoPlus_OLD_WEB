﻿using NDiscoPlus.Shared.Analyzer.Analysis;
using NDiscoPlus.Shared.Models;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Analyzer;
internal static class AudioAnalyzer
{
    public static AudioAnalysis Analyze(TrackAudioFeatures features, TrackAudioAnalysis analysis)
    {
        static ImmutableArray<NDPInterval> CastSpotifyIntervals(List<TimeInterval> spotifyIntervals)
            => spotifyIntervals.Select(static interval => (NDPInterval)interval).ToImmutableArray();

        AudioAnalysisTimings timings = new(
            bars: CastSpotifyIntervals(analysis.Bars),
            beats: CastSpotifyIntervals(analysis.Beats),
            tatums: CastSpotifyIntervals(analysis.Tatums)
        );

        ImmutableArray<AudioAnalysisSection> sections = AnalyzeSections(analysis)
                                                            .Select(sec => AudioAnalysisSection.ConstructFromSpotify(timings, sec.OriginalSection, sec.AdjustedInterval))
                                                            .ToImmutableArray();

        return new AudioAnalysis(
            track: AudioAnalysisTrack.FromSpotify(analysis.Track),
            features: AudioAnalysisFeatures.FromSpotify(features),
            timings: timings,
            sections: sections,
            segments: new AudioAnalysisSegments()
        );
    }

    private static IEnumerable<(Section OriginalSection, NDPInterval? AdjustedInterval)> AnalyzeSections(TrackAudioAnalysis analysis)
    {
        foreach (Section section in analysis.Sections)
            yield return (section, null);
    }
}
