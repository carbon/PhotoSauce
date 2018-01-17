﻿//------------------------------------------------------------------------------
//	<auto-generated>
//		This code was generated from a template.
//		Manual changes to this file will be overwritten if the code is regenerated.
//	</auto-generated>
//------------------------------------------------------------------------------

using System;

using static PhotoSauce.MagicScaler.MathUtil;

namespace PhotoSauce.MagicScaler
{
	unsafe internal sealed class ConvolverBgraByte : IConvolver
	{
		private const int Channels = 4;

		void IConvolver.ConvolveSourceLine(byte* istart, byte* tstart, int cb, byte* mapxstart, int smapx, int smapy)
		{
			int* pmapx = (int*)mapxstart;
			int* tp = (int*)tstart;
			int* tpe = (int*)(tstart + cb);
			int tstride = smapy * Channels;

			while (tp < tpe)
			{
				int a0 = 0, a1 = 0, a2 = 0, aa = 0, aw = 0;

				int ix = *pmapx++;
				byte* ip = istart + ix * Channels + 4 * Channels;
				byte* ipe = ip + smapx * Channels - 3 * Channels;
				int* mp = pmapx + 4;
				pmapx += smapx;

				while (ip < ipe)
				{
					int alpha = ip[-13];
					int w = mp[-4];
					if (alpha == 0)
					{
						aw += w;
					}
					else
					{
						aa += alpha * w;
						a0 += ip[-16] * w;
						a1 += ip[-15] * w;
						a2 += ip[-14] * w;
					}

					alpha = ip[-9];
					w = mp[-3];
					if (alpha == 0)
					{
						aw += w;
					}
					else
					{
						aa += alpha * w;
						a0 += ip[-12] * w;
						a1 += ip[-11] * w;
						a2 += ip[-10] * w;
					}

					alpha = ip[-5];
					w = mp[-2];
					if (alpha == 0)
					{
						aw += w;
					}
					else
					{
						aa += alpha * w;
						a0 += ip[-8] * w;
						a1 += ip[-7] * w;
						a2 += ip[-6] * w;
					}

					alpha = ip[-1];
					w = mp[-1];
					if (alpha == 0)
					{
						aw += w;
					}
					else
					{
						aa += alpha * w;
						a0 += ip[-4] * w;
						a1 += ip[-3] * w;
						a2 += ip[-2] * w;
					}

					ip += 4 * Channels;
					mp += 4;
				}

				ip -= 3 * Channels;
				mp -= 3;
				while (ip < ipe)
				{
					int alpha = ip[-1];
					int w = mp[-1];
					if (alpha == 0)
					{
						aw += w;
					}
					else
					{
						aa += alpha * w;
						a0 += ip[-4] * w;
						a1 += ip[-3] * w;
						a2 += ip[-2] * w;
					}

					ip += Channels;
					mp++;
				}

				if (aw != 0)
				{
					int wf = aw == UQ15One ? UQ15One : ((UQ15One << 15) / (UQ15One - aw));
					a0 = UnFix15(a0) * wf;
					a1 = UnFix15(a1) * wf;
					a2 = UnFix15(a2) * wf;
				}

				tp[0] = UnFix15(a0);
				tp[1] = UnFix15(a1);
				tp[2] = UnFix15(a2);
				tp[3] = UnFix15(aa);
				tp += tstride;
			}
		}

		void IConvolver.WriteDestLine(byte* tstart, byte* ostart, int ox, int ow, byte* pmapy, int smapy)
		{
			byte* op = ostart;
			int xc = ox + ow, tstride = smapy * Channels;

			while (ox < xc)
			{
				int a0 = 0, a1 = 0, a2 = 0, aa = 0, aw = 0;

				int* tp = (int*)tstart + ox * tstride + 2 * Channels;
				int* tpe = tp + tstride - Channels;
				int* mp = (int*)pmapy + 2;

				while (tp < tpe)
				{
					int alpha = tp[-5];
					int w = mp[-2];
					if (alpha == 0)
					{
						aw += w;
					}
					else
					{
						aa += alpha * w;
						a0 += tp[-8] * w;
						a1 += tp[-7] * w;
						a2 += tp[-6] * w;
					}

					alpha = tp[-1];
					w = mp[-1];
					if (alpha == 0)
					{
						aw += w;
					}
					else
					{
						aa += alpha * w;
						a0 += tp[-4] * w;
						a1 += tp[-3] * w;
						a2 += tp[-2] * w;
					}

					tp += 2 * Channels;
					mp += 2;
				}

				tp -= Channels;
				mp--;
				while (tp < tpe)
				{
					int alpha = tp[-1];
					int w = mp[-1];
					if (alpha == 0)
					{
						aw += w;
					}
					else
					{
						aa += alpha * w;
						a0 += tp[-4] * w;
						a1 += tp[-3] * w;
						a2 += tp[-2] * w;
					}

					tp += Channels;
					mp++;
				}

				if (aa <= UQ15Round)
				{
					a0 = a1 = a2 = aa = 0;
				}
				else if (aw != 0)
				{
					int wf = aw == UQ15One ? UQ15One : ((UQ15One << 15) / (UQ15One - aw));
					a0 = UnFix15(a0) * wf;
					a1 = UnFix15(a1) * wf;
					a2 = UnFix15(a2) * wf;
				}

				op[0] = UnFix15ToByte(a0);
				op[1] = UnFix15ToByte(a1);
				op[2] = UnFix15ToByte(a2);
				op[3] = UnFix15ToByte(aa);
				op += Channels;
				ox++;
			}
		}

		void IConvolver.SharpenLine(byte* cstart, byte* ystart, byte* bstart, byte* ostart, int ox, int ow, int amt, int thresh, bool gamma)
		{
			int iamt = Fix15(amt * 0.01);
			int threshold = thresh;

			byte* ip = cstart, yp = ystart, bp = bstart, op = ostart;

			int xc = ox + ow;
			for (int x = ox; x < xc; x++, ip += Channels, op += Channels)
			{
				int dif = *yp++ - *bp++;

				byte c0 = ip[0], c1 = ip[1], c2 = ip[2], c3 = ip[3];
				if (threshold == 0 || Math.Abs(dif) > threshold)
				{
					dif = UnFix15(dif * iamt);
					op[0] = ClampToByte(c0 + dif);
					op[1] = ClampToByte(c1 + dif);
					op[2] = ClampToByte(c2 + dif);
					op[3] = c3;
				}
				else
				{
					op[0] = c0;
					op[1] = c1;
					op[2] = c2;
					op[3] = c3;
				}
			}
		}
	}

	unsafe internal sealed class ConvolverBgraUQ15 : IConvolver
	{
		private const int Channels = 4;

		void IConvolver.ConvolveSourceLine(byte* istart, byte* tstart, int cb, byte* mapxstart, int smapx, int smapy)
		{
			int* pmapx = (int*)mapxstart;
			int* tp = (int*)tstart;
			int* tpe = (int*)(tstart + cb);
			int tstride = smapy * Channels;

			while (tp < tpe)
			{
				int a0 = 0, a1 = 0, a2 = 0, aa = 0, aw = 0;

				int ix = *pmapx++;
				ushort* ip = (ushort*)istart + ix * Channels + 4 * Channels;
				ushort* ipe = ip + smapx * Channels - 3 * Channels;
				int* mp = pmapx + 4;
				pmapx += smapx;

				while (ip < ipe)
				{
					int alpha = ip[-13];
					int w = mp[-4];
					if (alpha == 0)
					{
						aw += w;
					}
					else
					{
						aa += alpha * w;
						a0 += ip[-16] * w;
						a1 += ip[-15] * w;
						a2 += ip[-14] * w;
					}

					alpha = ip[-9];
					w = mp[-3];
					if (alpha == 0)
					{
						aw += w;
					}
					else
					{
						aa += alpha * w;
						a0 += ip[-12] * w;
						a1 += ip[-11] * w;
						a2 += ip[-10] * w;
					}

					alpha = ip[-5];
					w = mp[-2];
					if (alpha == 0)
					{
						aw += w;
					}
					else
					{
						aa += alpha * w;
						a0 += ip[-8] * w;
						a1 += ip[-7] * w;
						a2 += ip[-6] * w;
					}

					alpha = ip[-1];
					w = mp[-1];
					if (alpha == 0)
					{
						aw += w;
					}
					else
					{
						aa += alpha * w;
						a0 += ip[-4] * w;
						a1 += ip[-3] * w;
						a2 += ip[-2] * w;
					}

					ip += 4 * Channels;
					mp += 4;
				}

				ip -= 3 * Channels;
				mp -= 3;
				while (ip < ipe)
				{
					int alpha = ip[-1];
					int w = mp[-1];
					if (alpha == 0)
					{
						aw += w;
					}
					else
					{
						aa += alpha * w;
						a0 += ip[-4] * w;
						a1 += ip[-3] * w;
						a2 += ip[-2] * w;
					}

					ip += Channels;
					mp++;
				}

				if (aw != 0)
				{
					int wf = aw == UQ15One ? UQ15One : ((UQ15One << 15) / (UQ15One - aw));
					a0 = UnFix15(a0) * wf;
					a1 = UnFix15(a1) * wf;
					a2 = UnFix15(a2) * wf;
				}

				tp[0] = UnFix15(a0);
				tp[1] = UnFix15(a1);
				tp[2] = UnFix15(a2);
				tp[3] = UnFix15(aa);
				tp += tstride;
			}
		}

		void IConvolver.WriteDestLine(byte* tstart, byte* ostart, int ox, int ow, byte* pmapy, int smapy)
		{
			ushort* op = (ushort*)ostart;
			int xc = ox + ow, tstride = smapy * Channels;

			while (ox < xc)
			{
				int a0 = 0, a1 = 0, a2 = 0, aa = 0, aw = 0;

				int* tp = (int*)tstart + ox * tstride + 2 * Channels;
				int* tpe = tp + tstride - Channels;
				int* mp = (int*)pmapy + 2;

				while (tp < tpe)
				{
					int alpha = tp[-5];
					int w = mp[-2];
					if (alpha == 0)
					{
						aw += w;
					}
					else
					{
						aa += alpha * w;
						a0 += tp[-8] * w;
						a1 += tp[-7] * w;
						a2 += tp[-6] * w;
					}

					alpha = tp[-1];
					w = mp[-1];
					if (alpha == 0)
					{
						aw += w;
					}
					else
					{
						aa += alpha * w;
						a0 += tp[-4] * w;
						a1 += tp[-3] * w;
						a2 += tp[-2] * w;
					}

					tp += 2 * Channels;
					mp += 2;
				}

				tp -= Channels;
				mp--;
				while (tp < tpe)
				{
					int alpha = tp[-1];
					int w = mp[-1];
					if (alpha == 0)
					{
						aw += w;
					}
					else
					{
						aa += alpha * w;
						a0 += tp[-4] * w;
						a1 += tp[-3] * w;
						a2 += tp[-2] * w;
					}

					tp += Channels;
					mp++;
				}

				if (aa <= UQ15Round)
				{
					a0 = a1 = a2 = aa = 0;
				}
				else if (aw != 0)
				{
					int wf = aw == UQ15One ? UQ15One : ((UQ15One << 15) / (UQ15One - aw));
					a0 = UnFix15(a0) * wf;
					a1 = UnFix15(a1) * wf;
					a2 = UnFix15(a2) * wf;
				}

				op[0] = UnFixToUQ15(a0);
				op[1] = UnFixToUQ15(a1);
				op[2] = UnFixToUQ15(a2);
				op[3] = UnFixToUQ15(aa);
				op += Channels;
				ox++;
			}
		}

		void IConvolver.SharpenLine(byte* cstart, byte* ystart, byte* bstart, byte* ostart, int ox, int ow, int amt, int thresh, bool gamma)
		{
			fixed (byte* gtstart = LookupTables.Gamma)
			fixed (ushort* igtstart = LookupTables.InverseGammaUQ15)
			{
				int iamt = Fix15(amt * 0.01);
				int threshold = thresh;

				byte* gt = gtstart;
				ushort* ip = (ushort*)cstart, yp = (ushort*)ystart, bp = (ushort*)bstart, op = (ushort*)ostart, igt = igtstart;

				int xc = ox + ow;
				for (int x = ox; x < xc; x++, ip += Channels, op += Channels)
				{
					int dif = *yp++ - *bp++;

					ushort c0 = ip[0], c1 = ip[1], c2 = ip[2], c3 = ip[3];
					if (threshold == 0 || Math.Abs(dif) > threshold)
					{
						dif = UnFix15(dif * iamt);
						op[0] = igt[ClampToByte(gt[c0] + dif)];
						op[1] = igt[ClampToByte(gt[c1] + dif)];
						op[2] = igt[ClampToByte(gt[c2] + dif)];
						op[3] = c3;
					}
					else
					{
						op[0] = c0;
						op[1] = c1;
						op[2] = c2;
						op[3] = c3;
					}
				}
			}
		}
	}

	unsafe internal sealed class Convolver4ChanByte : IConvolver
	{
		private const int Channels = 4;

		void IConvolver.ConvolveSourceLine(byte* istart, byte* tstart, int cb, byte* mapxstart, int smapx, int smapy)
		{
			int* pmapx = (int*)mapxstart;
			int* tp = (int*)tstart;
			int* tpe = (int*)(tstart + cb);
			int tstride = smapy * Channels;

			while (tp < tpe)
			{
				int a0 = 0, a1 = 0, a2 = 0, a3 = 0;

				int ix = *pmapx++;
				byte* ip = istart + ix * Channels + 4 * Channels;
				byte* ipe = ip + smapx * Channels - 3 * Channels;
				int* mp = pmapx + 4;
				pmapx += smapx;

				while (ip < ipe)
				{
					int w = mp[-4];
					a0 += ip[-16] * w;
					a1 += ip[-15] * w;
					a2 += ip[-14] * w;
					a3 += ip[-13] * w;

					w = mp[-3];
					a0 += ip[-12] * w;
					a1 += ip[-11] * w;
					a2 += ip[-10] * w;
					a3 += ip[-9] * w;

					w = mp[-2];
					a0 += ip[-8] * w;
					a1 += ip[-7] * w;
					a2 += ip[-6] * w;
					a3 += ip[-5] * w;

					w = mp[-1];
					a0 += ip[-4] * w;
					a1 += ip[-3] * w;
					a2 += ip[-2] * w;
					a3 += ip[-1] * w;

					ip += 4 * Channels;
					mp += 4;
				}

				ip -= 3 * Channels;
				mp -= 3;
				while (ip < ipe)
				{
					int w = mp[-1];
					a0 += ip[-4] * w;
					a1 += ip[-3] * w;
					a2 += ip[-2] * w;
					a3 += ip[-1] * w;

					ip += Channels;
					mp++;
				}

				tp[0] = UnFix15(a0);
				tp[1] = UnFix15(a1);
				tp[2] = UnFix15(a2);
				tp[3] = UnFix15(a3);
				tp += tstride;
			}
		}

		void IConvolver.WriteDestLine(byte* tstart, byte* ostart, int ox, int ow, byte* pmapy, int smapy)
		{
			byte* op = ostart;
			int xc = ox + ow, tstride = smapy * Channels;

			while (ox < xc)
			{
				int a0 = 0, a1 = 0, a2 = 0, a3 = 0;

				int* tp = (int*)tstart + ox * tstride + 2 * Channels;
				int* tpe = tp + tstride - Channels;
				int* mp = (int*)pmapy + 2;

				while (tp < tpe)
				{
					int w = mp[-2];
					a0 += tp[-8] * w;
					a1 += tp[-7] * w;
					a2 += tp[-6] * w;
					a3 += tp[-5] * w;

					w = mp[-1];
					a0 += tp[-4] * w;
					a1 += tp[-3] * w;
					a2 += tp[-2] * w;
					a3 += tp[-1] * w;

					tp += 2 * Channels;
					mp += 2;
				}

				tp -= Channels;
				mp--;
				while (tp < tpe)
				{
					int w = mp[-1];
					a0 += tp[-4] * w;
					a1 += tp[-3] * w;
					a2 += tp[-2] * w;
					a3 += tp[-1] * w;

					tp += Channels;
					mp++;
				}

				op[0] = UnFix15ToByte(a0);
				op[1] = UnFix15ToByte(a1);
				op[2] = UnFix15ToByte(a2);
				op[3] = UnFix15ToByte(a3);
				op += Channels;
				ox++;
			}
		}

		void IConvolver.SharpenLine(byte* cstart, byte* ystart, byte* bstart, byte* ostart, int ox, int ow, int amt, int thresh, bool gamma)
		{
			int iamt = Fix15(amt * 0.01);
			int threshold = thresh;

			byte* ip = cstart, yp = ystart, bp = bstart, op = ostart;

			int xc = ox + ow;
			for (int x = ox; x < xc; x++, ip += Channels, op += Channels)
			{
				int dif = *yp++ - *bp++;

				byte c0 = ip[0], c1 = ip[1], c2 = ip[2], c3 = ip[3];
				if (threshold == 0 || Math.Abs(dif) > threshold)
				{
					dif = UnFix15(dif * iamt);
					op[0] = ClampToByte(c0 + dif);
					op[1] = ClampToByte(c1 + dif);
					op[2] = ClampToByte(c2 + dif);
					op[3] = c3;
				}
				else
				{
					op[0] = c0;
					op[1] = c1;
					op[2] = c2;
					op[3] = c3;
				}
			}
		}
	}

	unsafe internal sealed class Convolver4ChanUQ15 : IConvolver
	{
		private const int Channels = 4;

		void IConvolver.ConvolveSourceLine(byte* istart, byte* tstart, int cb, byte* mapxstart, int smapx, int smapy)
		{
			int* pmapx = (int*)mapxstart;
			int* tp = (int*)tstart;
			int* tpe = (int*)(tstart + cb);
			int tstride = smapy * Channels;

			while (tp < tpe)
			{
				int a0 = 0, a1 = 0, a2 = 0, a3 = 0;

				int ix = *pmapx++;
				ushort* ip = (ushort*)istart + ix * Channels + 4 * Channels;
				ushort* ipe = ip + smapx * Channels - 3 * Channels;
				int* mp = pmapx + 4;
				pmapx += smapx;

				while (ip < ipe)
				{
					int w = mp[-4];
					a0 += ip[-16] * w;
					a1 += ip[-15] * w;
					a2 += ip[-14] * w;
					a3 += ip[-13] * w;

					w = mp[-3];
					a0 += ip[-12] * w;
					a1 += ip[-11] * w;
					a2 += ip[-10] * w;
					a3 += ip[-9] * w;

					w = mp[-2];
					a0 += ip[-8] * w;
					a1 += ip[-7] * w;
					a2 += ip[-6] * w;
					a3 += ip[-5] * w;

					w = mp[-1];
					a0 += ip[-4] * w;
					a1 += ip[-3] * w;
					a2 += ip[-2] * w;
					a3 += ip[-1] * w;

					ip += 4 * Channels;
					mp += 4;
				}

				ip -= 3 * Channels;
				mp -= 3;
				while (ip < ipe)
				{
					int w = mp[-1];
					a0 += ip[-4] * w;
					a1 += ip[-3] * w;
					a2 += ip[-2] * w;
					a3 += ip[-1] * w;

					ip += Channels;
					mp++;
				}

				tp[0] = UnFix15(a0);
				tp[1] = UnFix15(a1);
				tp[2] = UnFix15(a2);
				tp[3] = UnFix15(a3);
				tp += tstride;
			}
		}

		void IConvolver.WriteDestLine(byte* tstart, byte* ostart, int ox, int ow, byte* pmapy, int smapy)
		{
			ushort* op = (ushort*)ostart;
			int xc = ox + ow, tstride = smapy * Channels;

			while (ox < xc)
			{
				int a0 = 0, a1 = 0, a2 = 0, a3 = 0;

				int* tp = (int*)tstart + ox * tstride + 2 * Channels;
				int* tpe = tp + tstride - Channels;
				int* mp = (int*)pmapy + 2;

				while (tp < tpe)
				{
					int w = mp[-2];
					a0 += tp[-8] * w;
					a1 += tp[-7] * w;
					a2 += tp[-6] * w;
					a3 += tp[-5] * w;

					w = mp[-1];
					a0 += tp[-4] * w;
					a1 += tp[-3] * w;
					a2 += tp[-2] * w;
					a3 += tp[-1] * w;

					tp += 2 * Channels;
					mp += 2;
				}

				tp -= Channels;
				mp--;
				while (tp < tpe)
				{
					int w = mp[-1];
					a0 += tp[-4] * w;
					a1 += tp[-3] * w;
					a2 += tp[-2] * w;
					a3 += tp[-1] * w;

					tp += Channels;
					mp++;
				}

				op[0] = UnFixToUQ15(a0);
				op[1] = UnFixToUQ15(a1);
				op[2] = UnFixToUQ15(a2);
				op[3] = UnFixToUQ15(a3);
				op += Channels;
				ox++;
			}
		}

		void IConvolver.SharpenLine(byte* cstart, byte* ystart, byte* bstart, byte* ostart, int ox, int ow, int amt, int thresh, bool gamma)
		{
			fixed (byte* gtstart = LookupTables.Gamma)
			fixed (ushort* igtstart = LookupTables.InverseGammaUQ15)
			{
				int iamt = Fix15(amt * 0.01);
				int threshold = thresh;

				byte* gt = gtstart;
				ushort* ip = (ushort*)cstart, yp = (ushort*)ystart, bp = (ushort*)bstart, op = (ushort*)ostart, igt = igtstart;

				int xc = ox + ow;
				for (int x = ox; x < xc; x++, ip += Channels, op += Channels)
				{
					int dif = *yp++ - *bp++;

					ushort c0 = ip[0], c1 = ip[1], c2 = ip[2], c3 = ip[3];
					if (threshold == 0 || Math.Abs(dif) > threshold)
					{
						dif = UnFix15(dif * iamt);
						op[0] = igt[ClampToByte(gt[c0] + dif)];
						op[1] = igt[ClampToByte(gt[c1] + dif)];
						op[2] = igt[ClampToByte(gt[c2] + dif)];
						op[3] = c3;
					}
					else
					{
						op[0] = c0;
						op[1] = c1;
						op[2] = c2;
						op[3] = c3;
					}
				}
			}
		}
	}

	unsafe internal sealed class ConvolverBgrByte : IConvolver
	{
		private const int Channels = 3;

		void IConvolver.ConvolveSourceLine(byte* istart, byte* tstart, int cb, byte* mapxstart, int smapx, int smapy)
		{
			int* pmapx = (int*)mapxstart;
			int* tp = (int*)tstart;
			int* tpe = (int*)(tstart + cb);
			int tstride = smapy * Channels;

			while (tp < tpe)
			{
				int a0 = 0, a1 = 0, a2 = 0;

				int ix = *pmapx++;
				byte* ip = istart + ix * Channels + 5 * Channels;
				byte* ipe = ip + smapx * Channels - 4 * Channels;
				int* mp = pmapx + 5;
				pmapx += smapx;

				while (ip < ipe)
				{
					int w = mp[-5];
					a0 += ip[-15] * w;
					a1 += ip[-14] * w;
					a2 += ip[-13] * w;

					w = mp[-4];
					a0 += ip[-12] * w;
					a1 += ip[-11] * w;
					a2 += ip[-10] * w;

					w = mp[-3];
					a0 += ip[-9] * w;
					a1 += ip[-8] * w;
					a2 += ip[-7] * w;

					w = mp[-2];
					a0 += ip[-6] * w;
					a1 += ip[-5] * w;
					a2 += ip[-4] * w;

					w = mp[-1];
					a0 += ip[-3] * w;
					a1 += ip[-2] * w;
					a2 += ip[-1] * w;

					ip += 5 * Channels;
					mp += 5;
				}

				ip -= 4 * Channels;
				mp -= 4;
				while (ip < ipe)
				{
					int w = mp[-1];
					a0 += ip[-3] * w;
					a1 += ip[-2] * w;
					a2 += ip[-1] * w;

					ip += Channels;
					mp++;
				}

				tp[0] = UnFix15(a0);
				tp[1] = UnFix15(a1);
				tp[2] = UnFix15(a2);
				tp += tstride;
			}
		}

		void IConvolver.WriteDestLine(byte* tstart, byte* ostart, int ox, int ow, byte* pmapy, int smapy)
		{
			byte* op = ostart;
			int xc = ox + ow, tstride = smapy * Channels;

			while (ox < xc)
			{
				int a0 = 0, a1 = 0, a2 = 0;

				int* tp = (int*)tstart + ox * tstride + 2 * Channels;
				int* tpe = tp + tstride - Channels;
				int* mp = (int*)pmapy + 2;

				while (tp < tpe)
				{
					int w = mp[-2];
					a0 += tp[-6] * w;
					a1 += tp[-5] * w;
					a2 += tp[-4] * w;

					w = mp[-1];
					a0 += tp[-3] * w;
					a1 += tp[-2] * w;
					a2 += tp[-1] * w;

					tp += 2 * Channels;
					mp += 2;
				}

				tp -= Channels;
				mp--;
				while (tp < tpe)
				{
					int w = mp[-1];
					a0 += tp[-3] * w;
					a1 += tp[-2] * w;
					a2 += tp[-1] * w;

					tp += Channels;
					mp++;
				}

				op[0] = UnFix15ToByte(a0);
				op[1] = UnFix15ToByte(a1);
				op[2] = UnFix15ToByte(a2);
				op += Channels;
				ox++;
			}
		}

		void IConvolver.SharpenLine(byte* cstart, byte* ystart, byte* bstart, byte* ostart, int ox, int ow, int amt, int thresh, bool gamma)
		{
			int iamt = Fix15(amt * 0.01);
			int threshold = thresh;

			byte* ip = cstart, yp = ystart, bp = bstart, op = ostart;

			int xc = ox + ow;
			for (int x = ox; x < xc; x++, ip += Channels, op += Channels)
			{
				int dif = *yp++ - *bp++;

				byte c0 = ip[0], c1 = ip[1], c2 = ip[2];
				if (threshold == 0 || Math.Abs(dif) > threshold)
				{
					dif = UnFix15(dif * iamt);
					op[0] = ClampToByte(c0 + dif);
					op[1] = ClampToByte(c1 + dif);
					op[2] = ClampToByte(c2 + dif);
				}
				else
				{
					op[0] = c0;
					op[1] = c1;
					op[2] = c2;
				}
			}
		}
	}

	unsafe internal sealed class ConvolverBgrUQ15 : IConvolver
	{
		private const int Channels = 3;

		void IConvolver.ConvolveSourceLine(byte* istart, byte* tstart, int cb, byte* mapxstart, int smapx, int smapy)
		{
			int* pmapx = (int*)mapxstart;
			int* tp = (int*)tstart;
			int* tpe = (int*)(tstart + cb);
			int tstride = smapy * Channels;

			while (tp < tpe)
			{
				int a0 = 0, a1 = 0, a2 = 0;

				int ix = *pmapx++;
				ushort* ip = (ushort*)istart + ix * Channels + 5 * Channels;
				ushort* ipe = ip + smapx * Channels - 4 * Channels;
				int* mp = pmapx + 5;
				pmapx += smapx;

				while (ip < ipe)
				{
					int w = mp[-5];
					a0 += ip[-15] * w;
					a1 += ip[-14] * w;
					a2 += ip[-13] * w;

					w = mp[-4];
					a0 += ip[-12] * w;
					a1 += ip[-11] * w;
					a2 += ip[-10] * w;

					w = mp[-3];
					a0 += ip[-9] * w;
					a1 += ip[-8] * w;
					a2 += ip[-7] * w;

					w = mp[-2];
					a0 += ip[-6] * w;
					a1 += ip[-5] * w;
					a2 += ip[-4] * w;

					w = mp[-1];
					a0 += ip[-3] * w;
					a1 += ip[-2] * w;
					a2 += ip[-1] * w;

					ip += 5 * Channels;
					mp += 5;
				}

				ip -= 4 * Channels;
				mp -= 4;
				while (ip < ipe)
				{
					int w = mp[-1];
					a0 += ip[-3] * w;
					a1 += ip[-2] * w;
					a2 += ip[-1] * w;

					ip += Channels;
					mp++;
				}

				tp[0] = UnFix15(a0);
				tp[1] = UnFix15(a1);
				tp[2] = UnFix15(a2);
				tp += tstride;
			}
		}

		void IConvolver.WriteDestLine(byte* tstart, byte* ostart, int ox, int ow, byte* pmapy, int smapy)
		{
			ushort* op = (ushort*)ostart;
			int xc = ox + ow, tstride = smapy * Channels;

			while (ox < xc)
			{
				int a0 = 0, a1 = 0, a2 = 0;

				int* tp = (int*)tstart + ox * tstride + 2 * Channels;
				int* tpe = tp + tstride - Channels;
				int* mp = (int*)pmapy + 2;

				while (tp < tpe)
				{
					int w = mp[-2];
					a0 += tp[-6] * w;
					a1 += tp[-5] * w;
					a2 += tp[-4] * w;

					w = mp[-1];
					a0 += tp[-3] * w;
					a1 += tp[-2] * w;
					a2 += tp[-1] * w;

					tp += 2 * Channels;
					mp += 2;
				}

				tp -= Channels;
				mp--;
				while (tp < tpe)
				{
					int w = mp[-1];
					a0 += tp[-3] * w;
					a1 += tp[-2] * w;
					a2 += tp[-1] * w;

					tp += Channels;
					mp++;
				}

				op[0] = UnFixToUQ15(a0);
				op[1] = UnFixToUQ15(a1);
				op[2] = UnFixToUQ15(a2);
				op += Channels;
				ox++;
			}
		}

		void IConvolver.SharpenLine(byte* cstart, byte* ystart, byte* bstart, byte* ostart, int ox, int ow, int amt, int thresh, bool gamma)
		{
			fixed (byte* gtstart = LookupTables.Gamma)
			fixed (ushort* igtstart = LookupTables.InverseGammaUQ15)
			{
				int iamt = Fix15(amt * 0.01);
				int threshold = thresh;

				byte* gt = gtstart;
				ushort* ip = (ushort*)cstart, yp = (ushort*)ystart, bp = (ushort*)bstart, op = (ushort*)ostart, igt = igtstart;

				int xc = ox + ow;
				for (int x = ox; x < xc; x++, ip += Channels, op += Channels)
				{
					int dif = *yp++ - *bp++;

					ushort c0 = ip[0], c1 = ip[1], c2 = ip[2];
					if (threshold == 0 || Math.Abs(dif) > threshold)
					{
						dif = UnFix15(dif * iamt);
						op[0] = igt[ClampToByte(gt[c0] + dif)];
						op[1] = igt[ClampToByte(gt[c1] + dif)];
						op[2] = igt[ClampToByte(gt[c2] + dif)];
					}
					else
					{
						op[0] = c0;
						op[1] = c1;
						op[2] = c2;
					}
				}
			}
		}
	}

	unsafe internal sealed class Convolver2ChanByte : IConvolver
	{
		private const int Channels = 2;

		void IConvolver.ConvolveSourceLine(byte* istart, byte* tstart, int cb, byte* mapxstart, int smapx, int smapy)
		{
			int* pmapx = (int*)mapxstart;
			int* tp = (int*)tstart;
			int* tpe = (int*)(tstart + cb);
			int tstride = smapy * Channels;

			while (tp < tpe)
			{
				int a0 = 0, a1 = 0;

				int ix = *pmapx++;
				byte* ip = istart + ix * Channels + 8 * Channels;
				byte* ipe = ip + smapx * Channels - 7 * Channels;
				int* mp = pmapx + 8;
				pmapx += smapx;

				while (ip < ipe)
				{
					int w = mp[-8];
					a0 += ip[-16] * w;
					a1 += ip[-15] * w;

					w = mp[-7];
					a0 += ip[-14] * w;
					a1 += ip[-13] * w;

					w = mp[-6];
					a0 += ip[-12] * w;
					a1 += ip[-11] * w;

					w = mp[-5];
					a0 += ip[-10] * w;
					a1 += ip[-9] * w;

					w = mp[-4];
					a0 += ip[-8] * w;
					a1 += ip[-7] * w;

					w = mp[-3];
					a0 += ip[-6] * w;
					a1 += ip[-5] * w;

					w = mp[-2];
					a0 += ip[-4] * w;
					a1 += ip[-3] * w;

					w = mp[-1];
					a0 += ip[-2] * w;
					a1 += ip[-1] * w;

					ip += 8 * Channels;
					mp += 8;
				}

				ip -= 7 * Channels;
				mp -= 7;
				while (ip < ipe)
				{
					int w = mp[-1];
					a0 += ip[-2] * w;
					a1 += ip[-1] * w;

					ip += Channels;
					mp++;
				}

				tp[0] = UnFix15(a0);
				tp[1] = UnFix15(a1);
				tp += tstride;
			}
		}

		void IConvolver.WriteDestLine(byte* tstart, byte* ostart, int ox, int ow, byte* pmapy, int smapy)
		{
			byte* op = ostart;
			int xc = ox + ow, tstride = smapy * Channels;

			while (ox < xc)
			{
				int a0 = 0, a1 = 0;

				int* tp = (int*)tstart + ox * tstride + 4 * Channels;
				int* tpe = tp + tstride - 3 * Channels;
				int* mp = (int*)pmapy + 4;

				while (tp < tpe)
				{
					int w = mp[-4];
					a0 += tp[-8] * w;
					a1 += tp[-7] * w;

					w = mp[-3];
					a0 += tp[-6] * w;
					a1 += tp[-5] * w;

					w = mp[-2];
					a0 += tp[-4] * w;
					a1 += tp[-3] * w;

					w = mp[-1];
					a0 += tp[-2] * w;
					a1 += tp[-1] * w;

					tp += 4 * Channels;
					mp += 4;
				}

				tp -= 3 * Channels;
				mp -= 3;
				while (tp < tpe)
				{
					int w = mp[-1];
					a0 += tp[-2] * w;
					a1 += tp[-1] * w;

					tp += Channels;
					mp++;
				}

				op[0] = UnFix15ToByte(a0);
				op[1] = UnFix15ToByte(a1);
				op += Channels;
				ox++;
			}
		}

		void IConvolver.SharpenLine(byte* cstart, byte* ystart, byte* bstart, byte* ostart, int ox, int ow, int amt, int thresh, bool gamma) => throw new NotImplementedException();
	}

	unsafe internal sealed class Convolver1ChanByte : IConvolver
	{
		private const int Channels = 1;

		void IConvolver.ConvolveSourceLine(byte* istart, byte* tstart, int cb, byte* mapxstart, int smapx, int smapy)
		{
			int* pmapx = (int*)mapxstart;
			int* tp = (int*)tstart;
			int* tpe = (int*)(tstart + cb);
			int tstride = smapy * Channels;

			while (tp < tpe)
			{
				int a0 = 0;

				int ix = *pmapx++;
				byte* ip = istart + ix * Channels + 8 * Channels;
				byte* ipe = ip + smapx * Channels - 7 * Channels;
				int* mp = pmapx + 8;
				pmapx += smapx;

				while (ip < ipe)
				{
					a0 += ip[-8] * mp[-8];
					a0 += ip[-7] * mp[-7];
					a0 += ip[-6] * mp[-6];
					a0 += ip[-5] * mp[-5];
					a0 += ip[-4] * mp[-4];
					a0 += ip[-3] * mp[-3];
					a0 += ip[-2] * mp[-2];
					a0 += ip[-1] * mp[-1];
					ip += 8 * Channels;
					mp += 8;
				}

				ip -= 7 * Channels;
				mp -= 7;
				while (ip < ipe)
				{
					a0 += ip[-1] * mp[-1];

					ip += Channels;
					mp++;
				}

				tp[0] = UnFix15(a0);
				tp += tstride;
			}
		}

		void IConvolver.WriteDestLine(byte* tstart, byte* ostart, int ox, int ow, byte* pmapy, int smapy)
		{
			byte* op = ostart;
			int xc = ox + ow, tstride = smapy * Channels;

			while (ox < xc)
			{
				int a0 = 0;

				int* tp = (int*)tstart + ox * tstride + 4 * Channels;
				int* tpe = tp + tstride - 3 * Channels;
				int* mp = (int*)pmapy + 4;

				while (tp < tpe)
				{
					a0 += tp[-4] * mp[-4];
					a0 += tp[-3] * mp[-3];
					a0 += tp[-2] * mp[-2];
					a0 += tp[-1] * mp[-1];
					tp += 4 * Channels;
					mp += 4;
				}

				tp -= 3 * Channels;
				mp -= 3;
				while (tp < tpe)
				{
					a0 += tp[-1] * mp[-1];

					tp += Channels;
					mp++;
				}

				op[0] = UnFix15ToByte(a0);
				op += Channels;
				ox++;
			}
		}

		void IConvolver.SharpenLine(byte* cstart, byte* ystart, byte* bstart, byte* ostart, int ox, int ow, int amt, int thresh, bool gamma)
		{
			int iamt = Fix15(amt * 0.01);
			int threshold = thresh;

			byte* ip = cstart, yp = ystart, bp = bstart, op = ostart;

			int xc = ox + ow;
			for (int x = ox; x < xc; x++, ip += Channels, op += Channels)
			{
				int dif = *yp++ - *bp++;

				byte c0 = ip[0];
				if (threshold == 0 || Math.Abs(dif) > threshold)
				{
					dif = UnFix15(dif * iamt);
					op[0] = ClampToByte(c0 + dif);
				}
				else
				{
					op[0] = c0;
				}
			}
		}
	}

	unsafe internal sealed class Convolver1ChanUQ15 : IConvolver
	{
		private const int Channels = 1;

		void IConvolver.ConvolveSourceLine(byte* istart, byte* tstart, int cb, byte* mapxstart, int smapx, int smapy)
		{
			int* pmapx = (int*)mapxstart;
			int* tp = (int*)tstart;
			int* tpe = (int*)(tstart + cb);
			int tstride = smapy * Channels;

			while (tp < tpe)
			{
				int a0 = 0;

				int ix = *pmapx++;
				ushort* ip = (ushort*)istart + ix * Channels + 8 * Channels;
				ushort* ipe = ip + smapx * Channels - 7 * Channels;
				int* mp = pmapx + 8;
				pmapx += smapx;

				while (ip < ipe)
				{
					a0 += ip[-8] * mp[-8];
					a0 += ip[-7] * mp[-7];
					a0 += ip[-6] * mp[-6];
					a0 += ip[-5] * mp[-5];
					a0 += ip[-4] * mp[-4];
					a0 += ip[-3] * mp[-3];
					a0 += ip[-2] * mp[-2];
					a0 += ip[-1] * mp[-1];
					ip += 8 * Channels;
					mp += 8;
				}

				ip -= 7 * Channels;
				mp -= 7;
				while (ip < ipe)
				{
					a0 += ip[-1] * mp[-1];

					ip += Channels;
					mp++;
				}

				tp[0] = UnFix15(a0);
				tp += tstride;
			}
		}

		void IConvolver.WriteDestLine(byte* tstart, byte* ostart, int ox, int ow, byte* pmapy, int smapy)
		{
			ushort* op = (ushort*)ostart;
			int xc = ox + ow, tstride = smapy * Channels;

			while (ox < xc)
			{
				int a0 = 0;

				int* tp = (int*)tstart + ox * tstride + 4 * Channels;
				int* tpe = tp + tstride - 3 * Channels;
				int* mp = (int*)pmapy + 4;

				while (tp < tpe)
				{
					a0 += tp[-4] * mp[-4];
					a0 += tp[-3] * mp[-3];
					a0 += tp[-2] * mp[-2];
					a0 += tp[-1] * mp[-1];
					tp += 4 * Channels;
					mp += 4;
				}

				tp -= 3 * Channels;
				mp -= 3;
				while (tp < tpe)
				{
					a0 += tp[-1] * mp[-1];

					tp += Channels;
					mp++;
				}

				op[0] = UnFixToUQ15(a0);
				op += Channels;
				ox++;
			}
		}

		void IConvolver.SharpenLine(byte* cstart, byte* ystart, byte* bstart, byte* ostart, int ox, int ow, int amt, int thresh, bool gamma)
		{
			fixed (byte* gtstart = LookupTables.Gamma)
			fixed (ushort* igtstart = LookupTables.InverseGammaUQ15)
			{
				int iamt = Fix15(amt * 0.01);
				int threshold = thresh;

				byte* gt = gtstart;
				ushort* ip = (ushort*)cstart, yp = (ushort*)ystart, bp = (ushort*)bstart, op = (ushort*)ostart, igt = igtstart;

				int xc = ox + ow;
				for (int x = ox; x < xc; x++, ip += Channels, op += Channels)
				{
					int dif = *yp++ - *bp++;

					ushort c0 = ip[0];
					if (threshold == 0 || Math.Abs(dif) > threshold)
					{
						dif = UnFix15(dif * iamt);
						op[0] = igt[ClampToByte(gt[c0] + dif)];
					}
					else
					{
						op[0] = c0;
					}
				}
			}
		}
	}
}
