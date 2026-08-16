// Copyright © Clinton Ingram and Contributors
// SPDX-License-Identifier: MIT

using System;

using PhotoSauce.MagicScaler;

namespace PhotoSauce.NativeCodecs.JxlRs;

/// <summary>JPEG XL decoder options for the jxl-rs codec.</summary>
/// <param name="FrameRange"><inheritdoc cref="IMultiFrameDecoderOptions.FrameRange" path="/summary/node()" /></param>
public readonly record struct JxlRsDecoderOptions(Range FrameRange) : IMultiFrameDecoderOptions
{
	/// <summary>Default jxl-rs decoder options.</summary>
	public static JxlRsDecoderOptions Default => new(..);
}
