﻿using System.Collections.Generic;
using AForge;
using CCGCurator.Data;
using CCGCurator.Data.Model;

namespace CCGCurator
{
    internal class IdentifiedCard
    {
        public IdentifiedCard(DetectedCard detectedCard, Card bestMatch)
        {
            Card = bestMatch;
            Corners = detectedCard.Corners;
        }

        public List<IntPoint> Corners { get; }
        public Card Card { get; }
    }
}