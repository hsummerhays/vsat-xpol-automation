// <copyright file="SignalSettings.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Monitoring.VsatXpol
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Used to pass signal settings for wide search, narrow search, etc. to SpecAnalyzer class.
    /// </summary>
    public class SignalSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SignalSettings"/> class.
        /// </summary>
        public SignalSettings() :
            this(0, 0, 0, 0, 0)
        {
            // Nothing to do here.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalSettings"/> class.
        /// </summary>
        /// <param name="span">The span of the analyzer in Hz.</param>
        public SignalSettings(int span) :
            this(span, 0, 0, 0, 0)
        {
            // Nothing to do here.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalSettings"/> class.
        /// </summary>
        /// <param name="span">The span of the analyzer in Hz.</param>
        /// <param name="rbw">The resolution bandwidth of the anaylzer in Hz. Zero is auto.</param>
        /// <param name="vbw">The video bandwidth of the anaylzer in Hz. Zero is auto.</param>
        /// <param name="refLevel">The reference level of the anaylzer in dBm. Zero is auto.</param>
        /// <param name="sweepTime">The sweep time in ms. Zero is auto.</param>
        public SignalSettings(int span, short rbw, short vbw, float refLevel, short sweepTime)
        {
            this.Span = span;
            this.Rbw = rbw;
            this.Vbw = vbw;
            this.RefLevel = refLevel;
            this.SweepTime = sweepTime;
        }

        /// <summary>
        /// Gets or sets the span of the analyzer in Hz.
        /// </summary>
        public int Span { get; set; }

        /// <summary>
        /// Gets or sets the resolution bandwidth of the anaylzer in Hz. Zero is auto.
        /// </summary>
        public short Rbw { get; set; }

        /// <summary>
        /// Gets or sets the video bandwidth of the anaylzer in Hz. Zero is auto.
        /// </summary>
        public short Vbw { get; set; }

        /// <summary>
        /// Gets or sets the reference level of the anaylzer in dBm.
        /// </summary>
        public float RefLevel { get; set; }

        /// <summary>
        /// Gets or sets the sweep time in ms. Zero is auto.
        /// </summary>
        public short SweepTime { get; set; }

        /// <summary>
        /// Determines whether the specified <see cref="SignalSettings"/> are equal to the current <see cref="SignalSettings"/>.
        /// </summary>
        /// <param name="settings">The <see cref="SignalSettings"/> to compare with the current <see cref="SignalSettings"/>.</param>
        /// <returns>True if they have the same values.</returns>
        public bool Equals(SignalSettings settings)
        {
            bool isEqual = this.Span == settings.Span;
            isEqual &= this.Rbw == settings.Rbw;
            isEqual &= this.Vbw == settings.Vbw;
            isEqual &= this.RefLevel == settings.RefLevel;
            isEqual &= this.SweepTime == settings.SweepTime;
            return isEqual;
        }
    }
}