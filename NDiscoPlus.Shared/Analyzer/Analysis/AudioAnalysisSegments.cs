using NDiscoPlus.Shared.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Analyzer.Analysis;

internal class AudioAnalysisSegments
{
    public ImmutableArray<NDPInterval> Segments { get; }

    public ImmutableArray<ImmutableArray<NDPInterval>> Bursts { get; }
}
