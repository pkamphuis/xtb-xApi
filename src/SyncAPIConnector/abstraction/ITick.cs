﻿using System;

namespace Xtb.XApi;

public interface ITick
{
    double? Ask { get; }

    double? Bid { get; }

    double? High { get; }

    double? Low { get; }

    DateTimeOffset? Time { get; }
}