﻿using System;
using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;

using PhotoSauce.MagicScaler.Interpolators;

namespace PhotoSauce.MagicScaler
{
	internal class KernelMap<T> : IDisposable where T : unmanaged
	{
		private static readonly GaussianInterpolator blur_0_50 = new GaussianInterpolator(0.50);
		private static readonly GaussianInterpolator blur_0_60 = new GaussianInterpolator(0.60);
		private static readonly GaussianInterpolator blur_0_75 = new GaussianInterpolator(0.75);
		private static readonly GaussianInterpolator blur_1_00 = new GaussianInterpolator(1.00);
		private static readonly GaussianInterpolator blur_1_50 = new GaussianInterpolator(1.50);

		private readonly int mapLen;
		private byte[] map;

		public int Pixels { get; }
		public int Samples { get; }
		public int Channels { get; }
		public ReadOnlySpan<byte> Map => new ReadOnlySpan<byte>(map, 0, mapLen);

		private static Exception getTypeException() => new NotSupportedException(nameof(T) + " must be int or float");

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static T convertWeight(float f)
		{
			if (typeof(T) == typeof(int))
				return (T)(object)MathUtil.Fix15(f);
			if (typeof(T) == typeof(float))
				return (T)(object)f;

			throw getTypeException();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static T add(T a, T b)
		{
			if (typeof(T) == typeof(int))
				return (T)(object)((int)(object)a + (int)(object)b);
			if (typeof(T) == typeof(float))
				return (T)(object)((float)(object)a + (float)(object)b);

			throw getTypeException();
		}

		private static void fillWeights(Span<float> kernel, IInterpolator interpolator, double start, double center, double scale)
		{
			double sum = 0d;
			for (int i = 0; i < kernel.Length; i++)
			{
				double weight = interpolator.GetValue(Math.Abs((start - center + i) * scale));
				sum += weight;
				kernel[i] = (float)weight;
			}

			float kscale = (float)(1 / sum);
			for (int i = 0; i < kernel.Length; i++)
				kernel[i] *= kscale;
		}

		private static int getPadding(int isize, int ksize, int channels)
		{
			int kpad = 0, inc = HWIntrinsics.IsSupported || channels == 3 ? 4 : Vector<T>.Count;
			if ((HWIntrinsics.IsSupported && ksize > 1) || ksize * channels % (inc * channels) > 1)
				kpad = MathUtil.DivCeiling(ksize * channels, inc * channels) * inc - ksize;

			return ksize + kpad > isize ? 0 : kpad;
		}

		private KernelMap(int pixels, int samples, int channels)
		{
			Pixels = pixels;
			Samples = samples;
			Channels = channels;

			mapLen = Pixels * (Samples * Channels * Unsafe.SizeOf<T>() + sizeof(int));
			map = ArrayPool<byte>.Shared.Rent(mapLen);
			map.AsSpan(0, mapLen).Clear();
		}

		unsafe private KernelMap<T> clamp(int ipix)
		{
			fixed (byte* mstart = Map)
			{
				int samp = Samples, chan = Channels;

				int* mp = (int*)mstart;
				int* mpe = (int*)(mstart + mapLen);
				while (mp < mpe)
				{
					int start = *mp;
					if (start < 0)
					{
						int o = 0 - start;

						*mp = 0;
						int* mpw = mp + 1;
						for (int k = 0; k < chan; k++)
						{
							var a = default(T);
							for (int j = 0; j <= o; j++)
								a = add(a, Unsafe.Read<T>(mpw + j * chan + k));

							Unsafe.Write(mpw + k, a);

							for (int j = 1; j < samp; j++)
								Unsafe.Write(mpw + j * chan + k, j < samp - o ? Unsafe.Read<T>(mpw + j * chan + o * chan + k) : default);
						}
					}
					else if (start + samp > ipix)
					{
						int ns = ipix - samp, last = samp - 1, o = start - ns;

						*mp = ns;
						int* mpw = mp + 1;
						for (int k = 0; k < chan; k++)
						{
							var a = default(T);
							for (int j = 0; j <= o; j++)
								a = add(a, Unsafe.Read<T>(mpw + last * chan - j * chan + k));

							Unsafe.Write(mpw + last * chan + k, a);

							for (int j = last - 1; j >= 0; j--)
								Unsafe.Write(mpw + j * chan + k, j >= o ? Unsafe.Read<T>(mpw + j * chan - o * chan + k) : default);
						}
					}

					mp += (samp * chan + 1);
				}
			}

			return this;
		}

		unsafe public static KernelMap<T> CreateResample(int isize, int osize, InterpolationSettings interpolator, int ichannels, bool subsampleOffset, bool vectored)
		{
			double offs = interpolator.WeightingFunction.Support < 0.1 ? 0.5 : subsampleOffset ? 0.25 : 0.0;
			double ratio = Math.Min((double)osize / isize, 1d);
			double cscale = ratio / interpolator.Blur;
			double support = Math.Min(interpolator.WeightingFunction.Support / cscale, isize / 2d);

			int channels = vectored ? ichannels : 1;
			int ksize = (int)Math.Ceiling(support * 2d);
			int kpad = vectored ? getPadding(isize, ksize, channels) : 0;

			var map = new KernelMap<T>(osize, ksize + kpad, channels);
			fixed (byte* mstart = map.Map)
			{
				int* mp = (int*)mstart;
				double inc = (double)isize / osize;
				double spoint = ((double)isize - osize) / (osize * 2d) + offs;

				Span<float> kernel = stackalloc float[ksize];

				for (int i = 0; i < osize; i++)
				{
					int start = (int)(spoint + support) - ksize + 1;
					fillWeights(kernel, interpolator.WeightingFunction, start, spoint, cscale);

					spoint += inc;
					*mp++ = start;

					for (int j = 0; j < kernel.Length; j++)
					{
						var w = convertWeight(kernel[j]);
						for (int k = 0; k < channels; k++)
							Unsafe.Write(mp++, w);
					}

					mp += kpad * channels;
				}
			}

			return map.clamp(isize);
		}

		unsafe public static KernelMap<T> CreateBlur(int size, double radius, int ichannels, bool vectored)
		{
			var interpolator = radius switch {
				0.50 => blur_0_50,
				0.60 => blur_0_60,
				0.75 => blur_0_75,
				1.00 => blur_1_00,
				1.50 => blur_1_50,
				_    => new GaussianInterpolator(radius)
			};

			int channels = vectored ? ichannels : 1;
			int dist = (int)Math.Ceiling(interpolator.Support);
			int ksize = Math.Min(dist * 2 + 1, size);
			int kpad = vectored ? getPadding(size, ksize, channels) : 0;

			var map = new KernelMap<T>(size, ksize + kpad, channels);
			fixed (byte* mstart = map.Map)
			{
				int* mp = (int*)mstart;

				Span<float> kernel = stackalloc float[ksize];
				fillWeights(kernel, interpolator, 0d, dist, 1d);

				for (int i = 0; i < size; i++)
				{
					int start = i - ksize / 2;
					*mp++ = start;

					for (int j = 0; j < kernel.Length; j++)
					{
						var w = convertWeight(kernel[j]);
						for (int k = 0; k < channels; k++)
							Unsafe.Write(mp++, w);
					}

					mp += kpad * channels;
				}
			}

			return map.clamp(size);
		}

		public void Dispose()
		{
			if (map is null)
				return;

			ArrayPool<byte>.Shared.Return(map);
			map = null!;
		}
	}
}