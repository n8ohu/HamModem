using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
//using System.Math
using System.Windows.Forms;

namespace HamModem
{
	public class Filters
	{
		// These are the half coefficients of a 35 tap FIR Lowpass filter 12000 sample rate
		// Passband upper freq = 3000, stopband Lower freq = 4000, Ripple .5 dB Attn 80 dB (Simple Parks McClean) 
		private double[] dblLPF_3000 = {
			0.00255464340979176,
			0.00790802558679919,
			0.00583287306644319,
			-0.00595839005819471,
			-0.00528802646906538,
			0.0105916771445436,
			0.00426728119763051,
			-0.017882797330782,
			0.000255932825403501,
			0.0273858094431131,
			-0.0115390032436714,
			-0.0379266079611693,
			0.0349606360050518,
			0.047680692955391,
			-0.0867995186560231,
			-0.0546149071064857,
			0.311488849051135,
			0.557133922553607

		};
		// Half Coefficients for a 31 tap FIR Lowpass filter 12000 sample rate
		// Passband upper freq = 2600, stop band lower freq = 3500, Ripple .8 dB, Attn 67 dB (Simple Parks McClean)
		private double[] dblLPF_2600 = {
			0.00358198809542461,
			0.00826735031230895,
			0.00206959130812085,
			-0.0154878629733615,
			-0.0183936935261694,
			0.00567216895930917,
			0.0174711264318973,
			-0.0135807867554761,
			-0.0344528554202631,
			0.011635725134813,
			0.0530933703352793,
			-0.0147872308381155,
			-0.100687978527049,
			0.0143005876418001,
			0.31583669625202,
			0.484543000496754

		};

		// Half Coefficients for a 31 tap FIR Lowpass filter 12000 sample rate
		// Passband First corner 2800,  Attn 70 dB (Windowed Sinc, Dolph-Chebyshev, Gamma = 60)
		private double[] dblLPF_2700 = {
			0.0,
			0.000785949613313789,
			0.000323996494169497,
			-0.0026402689234065,
			-0.00187342010839623,
			0.006275179951502,
			0.00643109717913837,
			-0.0118960403815,
			-0.0170045950032045,
			0.0189438994663949,
			0.0391365675615197,
			-0.0260362083231724,
			-0.0893388784478099,
			0.0313494308000679,
			0.312269256291464,
			0.466548067659838

		};
		private double dblSQRT2 = Math.Sqrt(2);
		private double dblLog2E = 1.44269504088896;
		private double dblLn2 = Math.Log(2);

		private double dblErf2 = Erf(2);

		// Use this to filer white noise to get to "pink" in 0 - 3000 Hz bandwidth
		// Assumes sample rate is 12000 and dblCoeff are the half coef mirrored about the last coeff
		double[] static_LPF3000_12000SR_dblRegister;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_LPF3000_12000SR_intPtr_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// holds the history
		int static_LPF3000_12000SR_intPtr;
		public void LPF3000_12000SR(ref double[] dblSamples, ref double[] dblFiltered, bool blnInit = true)
		{
			lock (static_LPF3000_12000SR_intPtr_Init) {
				try {
					if (InitStaticVariableHelper(static_LPF3000_12000SR_intPtr_Init)) {
						static_LPF3000_12000SR_intPtr = 0;
					}
				} finally {
					static_LPF3000_12000SR_intPtr_Init.State = 1;
				}
			}
			double dblSum = 0;
			int intRegPtr = 0;
			if (blnInit) {
				static_LPF3000_12000SR_dblRegister = new double[dblLPF_3000.Length * 2 - 1];
				static_LPF3000_12000SR_intPtr = 0;
			}
			dblFiltered = new double[dblSamples.Length];
			for (int i = 0; i <= dblSamples.Length - 1; i++) {
				dblSum = 0;
				static_LPF3000_12000SR_dblRegister[static_LPF3000_12000SR_intPtr] = dblSamples[i];
				for (int j = 0; j <= static_LPF3000_12000SR_dblRegister.Length - 1; j++) {
					intRegPtr = static_LPF3000_12000SR_intPtr - j;
					if (intRegPtr < 0)
						intRegPtr += static_LPF3000_12000SR_dblRegister.Length;
					// circular pointer
					if (j < dblLPF_3000.Length) {
						dblSum += dblLPF_3000[j] * static_LPF3000_12000SR_dblRegister[intRegPtr];
					} else {
						dblSum += dblLPF_3000[(2 * dblLPF_3000.Length - 1) - j] * static_LPF3000_12000SR_dblRegister[intRegPtr];
					}
				}
				dblFiltered[i] = dblSum;
				static_LPF3000_12000SR_intPtr += 1;
				if (static_LPF3000_12000SR_intPtr >= static_LPF3000_12000SR_dblRegister.Length)
					static_LPF3000_12000SR_intPtr = 0;
				//circular buffer
			}

		}

		// Use this to filter the USB image from the mixer with NCO ~ 3000 Hz (2600 Hz bandwidth
		// Assumes sample rate is 12000 and dblCoeff are the half coef mirrored about the last coeff
		double[] static_LPF2600_12000SR_dblRegister;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_LPF2600_12000SR_intPtr_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// holds the history
		int static_LPF2600_12000SR_intPtr;
		public Int32[] LPF2600_12000SR(ref Int32[] intSamples, string strFilename, bool blnInit = true)
		{
			lock (static_LPF2600_12000SR_intPtr_Init) {
				try {
					if (InitStaticVariableHelper(static_LPF2600_12000SR_intPtr_Init)) {
						static_LPF2600_12000SR_intPtr = 0;
					}
				} finally {
					static_LPF2600_12000SR_intPtr_Init.State = 1;
				}
			}
			double dblSum = 0;
			int intRegPtr = 0;
			if (blnInit) {
				static_LPF2600_12000SR_dblRegister = new double[dblLPF_2700.Length * 2 - 1];
				static_LPF2600_12000SR_intPtr = 0;
			}
			double[] dblFiltered = new double[intSamples.Length];
			Int32[] intFiltered = new Int32[intSamples.Length];
			for (int i = 0; i <= intSamples.Length - 1; i++) {
				dblSum = 0;
				static_LPF2600_12000SR_dblRegister[static_LPF2600_12000SR_intPtr] = intSamples[i];
				for (int j = 0; j <= static_LPF2600_12000SR_dblRegister.Length - 1; j++) {
					intRegPtr = static_LPF2600_12000SR_intPtr - j;
					if (intRegPtr < 0)
						intRegPtr += static_LPF2600_12000SR_dblRegister.Length;
					// circular pointer
					if (j < dblLPF_2700.Length) {
						dblSum += dblLPF_2700[j] * static_LPF2600_12000SR_dblRegister[intRegPtr];
					} else {
						dblSum += dblLPF_2700[(2 * dblLPF_2700.Length - 1) - j] * static_LPF2600_12000SR_dblRegister[intRegPtr];
					}
				}
				dblFiltered[i] = dblSum;
				intFiltered[i] = Convert.ToInt32(dblFiltered[i]);
				static_LPF2600_12000SR_intPtr += 1;
				if (static_LPF2600_12000SR_intPtr >= static_LPF2600_12000SR_dblRegister.Length)
					static_LPF2600_12000SR_intPtr = 0;
				//circular buffer
			}

			// *********************************
			WaveTools objWT = new WaveTools();
			if (System.IO.Directory.Exists(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav") == false) {
				System.IO.Directory.CreateDirectory(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav");
			}
			objWT.WriteFloatingRIFF(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav\\" + strFilename, 12000, 16, ref dblFiltered);
			// End of debug code
			//************************************
			return intFiltered;
		}
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter1000_1500Hz_dblR_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();

		// assumes sample rate of 12000
		// implements 8 200 Hz wide sections centered on 1500 Hz  (~1000 Hz wide @ - 30dB centered on 1500 Hz)
		// FSF (Frequency Selective Filter) variables

		//  Filtered samples

		double static_FSRcvFilter1000_1500Hz_dblR;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter1000_1500Hz_intN_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// insures stability (must be < 1.0) (Value .9995 7/8/2013 gives good results)
		int static_FSRcvFilter1000_1500Hz_intN;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter1000_1500Hz_dblRn_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// Length of filter 12000/200
		double static_FSRcvFilter1000_1500Hz_dblRn;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter1000_1500Hz_dblR2_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		double static_FSRcvFilter1000_1500Hz_dblR2;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter1000_1500Hz_dblCoef_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		double[] static_FSRcvFilter1000_1500Hz_dblCoef;
		double static_FSRcvFilter1000_1500Hz_dblZin;
		double static_FSRcvFilter1000_1500Hz_dblZin_1;
		double static_FSRcvFilter1000_1500Hz_dblZin_2;
		//the coefficients
		double static_FSRcvFilter1000_1500Hz_dblZComb;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter1000_1500Hz_dblZout_0_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// Used in the comb generator
		// The resonators 
		double[] static_FSRcvFilter1000_1500Hz_dblZout_0;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter1000_1500Hz_dblZout_1_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// resonator outputs
		double[] static_FSRcvFilter1000_1500Hz_dblZout_1;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter1000_1500Hz_dblZout_2_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// resonator outputs delayed one sample
		double[] static_FSRcvFilter1000_1500Hz_dblZout_2;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter1000_1500Hz_intFilLen_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// resonator outputs delayed two samples
		int static_FSRcvFilter1000_1500Hz_intFilLen;
		public Int32[] FSRcvFilter1000_1500Hz(ref Int32[] intNewSamples, string strFilename)
		{
			Int32[] intFilteredSamples = new Int32[intNewSamples.Length];
			lock (static_FSRcvFilter1000_1500Hz_dblR_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter1000_1500Hz_dblR_Init)) {
						static_FSRcvFilter1000_1500Hz_dblR = 0.9995;
					}
				} finally {
					static_FSRcvFilter1000_1500Hz_dblR_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter1000_1500Hz_intN_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter1000_1500Hz_intN_Init)) {
						static_FSRcvFilter1000_1500Hz_intN = 60;
					}
				} finally {
					static_FSRcvFilter1000_1500Hz_intN_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter1000_1500Hz_dblRn_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter1000_1500Hz_dblRn_Init)) {
						static_FSRcvFilter1000_1500Hz_dblRn = Math.Pow(static_FSRcvFilter1000_1500Hz_dblR, static_FSRcvFilter1000_1500Hz_intN);
					}
				} finally {
					static_FSRcvFilter1000_1500Hz_dblRn_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter1000_1500Hz_dblR2_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter1000_1500Hz_dblR2_Init)) {
						static_FSRcvFilter1000_1500Hz_dblR2 = Math.Pow(static_FSRcvFilter1000_1500Hz_dblR, 2);
					}
				} finally {
					static_FSRcvFilter1000_1500Hz_dblR2_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter1000_1500Hz_dblCoef_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter1000_1500Hz_dblCoef_Init)) {
						static_FSRcvFilter1000_1500Hz_dblCoef = new double[12];
					}
				} finally {
					static_FSRcvFilter1000_1500Hz_dblCoef_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter1000_1500Hz_dblZout_0_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter1000_1500Hz_dblZout_0_Init)) {
						static_FSRcvFilter1000_1500Hz_dblZout_0 = new double[12];
					}
				} finally {
					static_FSRcvFilter1000_1500Hz_dblZout_0_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter1000_1500Hz_dblZout_1_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter1000_1500Hz_dblZout_1_Init)) {
						static_FSRcvFilter1000_1500Hz_dblZout_1 = new double[12];
					}
				} finally {
					static_FSRcvFilter1000_1500Hz_dblZout_1_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter1000_1500Hz_dblZout_2_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter1000_1500Hz_dblZout_2_Init)) {
						static_FSRcvFilter1000_1500Hz_dblZout_2 = new double[12];
					}
				} finally {
					static_FSRcvFilter1000_1500Hz_dblZout_2_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter1000_1500Hz_intFilLen_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter1000_1500Hz_intFilLen_Init)) {
						static_FSRcvFilter1000_1500Hz_intFilLen = static_FSRcvFilter1000_1500Hz_intN / 2;
					}
				} finally {
					static_FSRcvFilter1000_1500Hz_intFilLen_Init.State = 1;
				}
			}

			// Initialize the coefficients
			if (static_FSRcvFilter1000_1500Hz_dblCoef[11] == 0) {
				for (int i = 4; i <= 11; i++) {
					static_FSRcvFilter1000_1500Hz_dblCoef[i] = 2 * static_FSRcvFilter1000_1500Hz_dblR * Math.Cos(2 * Math.PI * i / static_FSRcvFilter1000_1500Hz_intN);
					// For Frequency = bin i
				}
			}
			try {
				for (int i = 0; i <= intNewSamples.Length - 1; i++) {
					if (i < static_FSRcvFilter1000_1500Hz_intN) {
						static_FSRcvFilter1000_1500Hz_dblZin = intNewSamples[i];
					} else {
						static_FSRcvFilter1000_1500Hz_dblZin = intNewSamples[i] - static_FSRcvFilter1000_1500Hz_dblRn * intNewSamples[i - static_FSRcvFilter1000_1500Hz_intN];
					}
					// Compute the Comb
					static_FSRcvFilter1000_1500Hz_dblZComb = static_FSRcvFilter1000_1500Hz_dblZin - static_FSRcvFilter1000_1500Hz_dblZin_2 * static_FSRcvFilter1000_1500Hz_dblR2;
					static_FSRcvFilter1000_1500Hz_dblZin_2 = static_FSRcvFilter1000_1500Hz_dblZin_1;
					static_FSRcvFilter1000_1500Hz_dblZin_1 = static_FSRcvFilter1000_1500Hz_dblZin;

					// DateTime.Now the resonators

					// calculate output for 4 resonators 
					for (int j = 4; j <= 11; j++) {
						static_FSRcvFilter1000_1500Hz_dblZout_0[j] = static_FSRcvFilter1000_1500Hz_dblZComb + static_FSRcvFilter1000_1500Hz_dblCoef[j] * static_FSRcvFilter1000_1500Hz_dblZout_1[j] - static_FSRcvFilter1000_1500Hz_dblR2 * static_FSRcvFilter1000_1500Hz_dblZout_2[j];
						static_FSRcvFilter1000_1500Hz_dblZout_2[j] = static_FSRcvFilter1000_1500Hz_dblZout_1[j];
						static_FSRcvFilter1000_1500Hz_dblZout_1[j] = static_FSRcvFilter1000_1500Hz_dblZout_0[j];
						// scale each by transition coeff and + (Even) or - (Odd) 
						// Resonators 6 and 9 scaled by .15 to get best shape and side lobe supression while keeping BW at 500 Hz
						// practical range of scaling .05 to 2.5
						// Scaling also accomodates for the filter "gain" of approx 60. 
						if (j == 4) {
							intFilteredSamples[i] += 0.15 * static_FSRcvFilter1000_1500Hz_dblZout_0[j];
						} else if (j == 11) {
							intFilteredSamples[i] -= 0.15 * static_FSRcvFilter1000_1500Hz_dblZout_0[j];
						} else if (j % 2 == 0) {
							intFilteredSamples[i] += static_FSRcvFilter1000_1500Hz_dblZout_0[j];
						} else {
							intFilteredSamples[i] -= static_FSRcvFilter1000_1500Hz_dblZout_0[j];
						}
					}
					intFilteredSamples[i] = intFilteredSamples[i] * 0.016666666666;
					// rescales for gain of filter
				}

				// *********************************
				// Debug code to look at filter output
				double[] dblInFiltered = new double[intFilteredSamples.Length];
				for (int k = 0; k <= dblInFiltered.Length - 1; k++) {
					dblInFiltered[k] = intFilteredSamples[k];
				}
				WaveTools objWT = new WaveTools();
				if (System.IO.Directory.Exists(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav") == false) {
					System.IO.Directory.CreateDirectory(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav");
				}
				objWT.WriteFloatingRIFF(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav\\" + strFilename, 12000, 16, ref dblInFiltered);
				// End of debug code
				//************************************

			} catch (Exception ex) {
				Debug.WriteLine("[Filters.FSRcvFilter1000_1500Hz] Exception: " + ex.ToString());
				return null;
			}
			return intFilteredSamples;
		}
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter500_1500Hz_dblR_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();


		// assumes sample rate of 12000
		// implements 4 200 Hz wide sections centered on 1500 Hz  (~500 Hz wide @ - 30dB centered on 1500 Hz)
		// FSF (Frequency Selective Filter) variables

		//  Filtered samples

		double static_FSRcvFilter500_1500Hz_dblR;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter500_1500Hz_intN_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// insures stability (must be < 1.0) (Value .9995 7/8/2013 gives good results)
		int static_FSRcvFilter500_1500Hz_intN;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter500_1500Hz_dblRn_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// Length of filter 12000/200
		double static_FSRcvFilter500_1500Hz_dblRn;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter500_1500Hz_dblR2_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		double static_FSRcvFilter500_1500Hz_dblR2;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter500_1500Hz_dblCoef_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		double[] static_FSRcvFilter500_1500Hz_dblCoef;
		double static_FSRcvFilter500_1500Hz_dblZin;
		double static_FSRcvFilter500_1500Hz_dblZin_1;
		double static_FSRcvFilter500_1500Hz_dblZin_2;
		//the coefficients
		double static_FSRcvFilter500_1500Hz_dblZComb;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter500_1500Hz_dblZout_0_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// Used in the comb generator
		// The resonators 
		double[] static_FSRcvFilter500_1500Hz_dblZout_0;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter500_1500Hz_dblZout_1_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// resonator outputs
		double[] static_FSRcvFilter500_1500Hz_dblZout_1;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter500_1500Hz_dblZout_2_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// resonator outputs delayed one sample
		double[] static_FSRcvFilter500_1500Hz_dblZout_2;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter500_1500Hz_intFilLen_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// resonator outputs delayed two samples
		int static_FSRcvFilter500_1500Hz_intFilLen;
		public Int32[] FSRcvFilter500_1500Hz(ref Int32[] intNewSamples, string strFilename)
		{
			Int32[] intFilteredSamples = new Int32[intNewSamples.Length];
			double[] dblUnfilteredSamples = new double[intNewSamples.Length];
			lock (static_FSRcvFilter500_1500Hz_dblR_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter500_1500Hz_dblR_Init)) {
						static_FSRcvFilter500_1500Hz_dblR = 0.9995;
					}
				} finally {
					static_FSRcvFilter500_1500Hz_dblR_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter500_1500Hz_intN_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter500_1500Hz_intN_Init)) {
						static_FSRcvFilter500_1500Hz_intN = 60;
					}
				} finally {
					static_FSRcvFilter500_1500Hz_intN_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter500_1500Hz_dblRn_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter500_1500Hz_dblRn_Init)) {
						static_FSRcvFilter500_1500Hz_dblRn = Math.Pow(static_FSRcvFilter500_1500Hz_dblR, static_FSRcvFilter500_1500Hz_intN);
					}
				} finally {
					static_FSRcvFilter500_1500Hz_dblRn_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter500_1500Hz_dblR2_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter500_1500Hz_dblR2_Init)) {
						static_FSRcvFilter500_1500Hz_dblR2 = Math.Pow(static_FSRcvFilter500_1500Hz_dblR, 2);
					}
				} finally {
					static_FSRcvFilter500_1500Hz_dblR2_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter500_1500Hz_dblCoef_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter500_1500Hz_dblCoef_Init)) {
						static_FSRcvFilter500_1500Hz_dblCoef = new double[10];
					}
				} finally {
					static_FSRcvFilter500_1500Hz_dblCoef_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter500_1500Hz_dblZout_0_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter500_1500Hz_dblZout_0_Init)) {
						static_FSRcvFilter500_1500Hz_dblZout_0 = new double[10];
					}
				} finally {
					static_FSRcvFilter500_1500Hz_dblZout_0_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter500_1500Hz_dblZout_1_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter500_1500Hz_dblZout_1_Init)) {
						static_FSRcvFilter500_1500Hz_dblZout_1 = new double[10];
					}
				} finally {
					static_FSRcvFilter500_1500Hz_dblZout_1_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter500_1500Hz_dblZout_2_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter500_1500Hz_dblZout_2_Init)) {
						static_FSRcvFilter500_1500Hz_dblZout_2 = new double[10];
					}
				} finally {
					static_FSRcvFilter500_1500Hz_dblZout_2_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter500_1500Hz_intFilLen_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter500_1500Hz_intFilLen_Init)) {
						static_FSRcvFilter500_1500Hz_intFilLen = static_FSRcvFilter500_1500Hz_intN / 2;
					}
				} finally {
					static_FSRcvFilter500_1500Hz_intFilLen_Init.State = 1;
				}
			}

			// Initialize the coefficients
			if (static_FSRcvFilter500_1500Hz_dblCoef[7] == 0) {
				for (int i = 6; i <= 9; i++) {
					static_FSRcvFilter500_1500Hz_dblCoef[i] = 2 * static_FSRcvFilter500_1500Hz_dblR * Math.Cos(2 * Math.PI * i / static_FSRcvFilter500_1500Hz_intN);
					// For Frequency = bin i
				}
			}
			try {
				for (int i = 0; i <= intNewSamples.Length - 1; i++) {
					dblUnfilteredSamples[i] = intNewSamples[i];
					if (i < static_FSRcvFilter500_1500Hz_intN) {
						static_FSRcvFilter500_1500Hz_dblZin = intNewSamples[i];
					} else {
						static_FSRcvFilter500_1500Hz_dblZin = intNewSamples[i] - static_FSRcvFilter500_1500Hz_dblRn * intNewSamples[i - static_FSRcvFilter500_1500Hz_intN];
					}
					// Compute the Comb
					static_FSRcvFilter500_1500Hz_dblZComb = static_FSRcvFilter500_1500Hz_dblZin - static_FSRcvFilter500_1500Hz_dblZin_2 * static_FSRcvFilter500_1500Hz_dblR2;
					static_FSRcvFilter500_1500Hz_dblZin_2 = static_FSRcvFilter500_1500Hz_dblZin_1;
					static_FSRcvFilter500_1500Hz_dblZin_1 = static_FSRcvFilter500_1500Hz_dblZin;

					// DateTime.Now the resonators

					// calculate output for 4 resonators 
					for (int j = 6; j <= 9; j++) {
						static_FSRcvFilter500_1500Hz_dblZout_0[j] = static_FSRcvFilter500_1500Hz_dblZComb + static_FSRcvFilter500_1500Hz_dblCoef[j] * static_FSRcvFilter500_1500Hz_dblZout_1[j] - static_FSRcvFilter500_1500Hz_dblR2 * static_FSRcvFilter500_1500Hz_dblZout_2[j];
						static_FSRcvFilter500_1500Hz_dblZout_2[j] = static_FSRcvFilter500_1500Hz_dblZout_1[j];
						static_FSRcvFilter500_1500Hz_dblZout_1[j] = static_FSRcvFilter500_1500Hz_dblZout_0[j];
						// scale each by transition coeff and + (Even) or - (Odd) 
						// Resonators 6 and 9 scaled by .15 to get best shape and side lobe supression while keeping BW at 500 Hz
						// practical range of scaling .05 to 2.5
						// Scaling also accomodates for the filter "gain" of approx 60. 
						if (j == 6) {
							intFilteredSamples[i] += 0.15 * static_FSRcvFilter500_1500Hz_dblZout_0[j];
						} else if (j == 7) {
							intFilteredSamples[i] -= static_FSRcvFilter500_1500Hz_dblZout_0[j];
						} else if (j == 8) {
							intFilteredSamples[i] += static_FSRcvFilter500_1500Hz_dblZout_0[j];
						} else {
							intFilteredSamples[i] -= 0.15 * static_FSRcvFilter500_1500Hz_dblZout_0[j];
						}
					}
					intFilteredSamples[i] = intFilteredSamples[i] * 0.016666666666;
					// rescales for gain of filter
				}

				// *********************************
				// Debug code to look at filter output
				double[] dblInFiltered = new double[intFilteredSamples.Length];
				for (int k = 0; k <= dblInFiltered.Length - 1; k++) {
					dblInFiltered[k] = intFilteredSamples[k];
				}
				WaveTools objWT = new WaveTools();
				if (System.IO.Directory.Exists(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav") == false) {
					System.IO.Directory.CreateDirectory(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav");
				}
				objWT.WriteFloatingRIFF(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav\\Unfiltered" + strFilename, 12000, 16, ref dblUnfilteredSamples);
				objWT.WriteFloatingRIFF(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav\\Filtered" + strFilename, 12000, 16, ref dblInFiltered);
				// End of debug code
				//************************************

			} catch (Exception ex) {
				Debug.WriteLine("[Filters.FSRcvFilter500_1500Hz] Exception: " + ex.ToString());
				return null;
			}
			return intFilteredSamples;
		}
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter2000_1500Hz_dblR_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();



		// Used for FSK modulation XMIT filter  
		// assumes sample rate of 12000
		// implements 19 100 Hz wide sections centered on 1500 Hz  (~2000 Hz wide @ - 30dB centered on 1500 Hz)
		// FSF (Frequency Selective Filter) variables



		double static_FSXmtFilter2000_1500Hz_dblR;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter2000_1500Hz_intN_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// insures stability (must be < 1.0) (Value .9995 7/8/2013 gives good results)
		int static_FSXmtFilter2000_1500Hz_intN;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter2000_1500Hz_dblRn_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// Length of filter 12000/100
		double static_FSXmtFilter2000_1500Hz_dblRn;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter2000_1500Hz_dblR2_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		double static_FSXmtFilter2000_1500Hz_dblR2;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter2000_1500Hz_dblCoef_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		double[] static_FSXmtFilter2000_1500Hz_dblCoef;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter2000_1500Hz_intFilLen_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		//the coefficients
		// Used in the comb generator
		// The resonators 
		// resonator outputs
		// resonator outputs delayed one sample
		// resonator outputs delayed two samples
		int static_FSXmtFilter2000_1500Hz_intFilLen;
		public Int32[] FSXmtFilter2000_1500Hz(ref Int32[] intNewSamples, string strFilename)
		{
			lock (static_FSXmtFilter2000_1500Hz_dblR_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter2000_1500Hz_dblR_Init)) {
						static_FSXmtFilter2000_1500Hz_dblR = 0.9995;
					}
				} finally {
					static_FSXmtFilter2000_1500Hz_dblR_Init.State = 1;
				}
			}
			lock (static_FSXmtFilter2000_1500Hz_intN_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter2000_1500Hz_intN_Init)) {
						static_FSXmtFilter2000_1500Hz_intN = 120;
					}
				} finally {
					static_FSXmtFilter2000_1500Hz_intN_Init.State = 1;
				}
			}
			lock (static_FSXmtFilter2000_1500Hz_dblRn_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter2000_1500Hz_dblRn_Init)) {
						static_FSXmtFilter2000_1500Hz_dblRn = Math.Pow(static_FSXmtFilter2000_1500Hz_dblR, static_FSXmtFilter2000_1500Hz_intN);
					}
				} finally {
					static_FSXmtFilter2000_1500Hz_dblRn_Init.State = 1;
				}
			}
			lock (static_FSXmtFilter2000_1500Hz_dblR2_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter2000_1500Hz_dblR2_Init)) {
						static_FSXmtFilter2000_1500Hz_dblR2 = Math.Pow(static_FSXmtFilter2000_1500Hz_dblR, 2);
					}
				} finally {
					static_FSXmtFilter2000_1500Hz_dblR2_Init.State = 1;
				}
			}
			lock (static_FSXmtFilter2000_1500Hz_dblCoef_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter2000_1500Hz_dblCoef_Init)) {
						static_FSXmtFilter2000_1500Hz_dblCoef = new double[25];
					}
				} finally {
					static_FSXmtFilter2000_1500Hz_dblCoef_Init.State = 1;
				}
			}
			double dblZin = 0;
			double dblZin_1 = 0;
			double dblZin_2 = 0;
			double dblZComb = 0;
			double[] dblZout_0 = new double[25];
			double[] dblZout_1 = new double[25];
			double[] dblZout_2 = new double[25];
			lock (static_FSXmtFilter2000_1500Hz_intFilLen_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter2000_1500Hz_intFilLen_Init)) {
						static_FSXmtFilter2000_1500Hz_intFilLen = static_FSXmtFilter2000_1500Hz_intN / 2;
					}
				} finally {
					static_FSXmtFilter2000_1500Hz_intFilLen_Init.State = 1;
				}
			}
			Int32[] intFilteredSamples = new Int32[intNewSamples.Length];
			//  Filtered samples
			double[] dblUnfilteredSamples = new double[intNewSamples.Length];
			//for debug wave plotting
			double[] dblFilteredSamples = new double[intNewSamples.Length];
			// for debug wave plotting


			Int32 intPeakSample = 0;
			// Initialize the coefficients
			if (static_FSXmtFilter2000_1500Hz_dblCoef[24] == 0) {
				for (int i = 6; i <= 24; i++) {
					static_FSXmtFilter2000_1500Hz_dblCoef[i] = 2 * static_FSXmtFilter2000_1500Hz_dblR * Math.Cos(2 * Math.PI * i / static_FSXmtFilter2000_1500Hz_intN);
					// For Frequency = bin i
				}
			}
			try {

				for (int i = 0; i <= intNewSamples.Length + static_FSXmtFilter2000_1500Hz_intFilLen - 1; i++) {
					if (i < static_FSXmtFilter2000_1500Hz_intN) {
						dblUnfilteredSamples[i] = intNewSamples[i];
						// debug code for waveform plotting.
						dblZin = intNewSamples[i];
					} else if (i < intNewSamples.Length) {
						dblUnfilteredSamples[i] = intNewSamples[i];
						// debug code for waveform plotting.
						dblZin = intNewSamples[i] - static_FSXmtFilter2000_1500Hz_dblRn * intNewSamples[i - static_FSXmtFilter2000_1500Hz_intN];
					} else {
						dblZin = -static_FSXmtFilter2000_1500Hz_dblRn * intNewSamples[i - static_FSXmtFilter2000_1500Hz_intN];
					}
					// Compute the Comb
					dblZComb = dblZin - dblZin_2 * static_FSXmtFilter2000_1500Hz_dblR2;
					dblZin_2 = dblZin_1;
					dblZin_1 = dblZin;

					// DateTime.Now the resonators

					// calculate output for 11 resonators 
					for (int j = 6; j <= 24; j++) {
						dblZout_0[j] = dblZComb + static_FSXmtFilter2000_1500Hz_dblCoef[j] * dblZout_1[j] - static_FSXmtFilter2000_1500Hz_dblR2 * dblZout_2[j];
						dblZout_2[j] = dblZout_1[j];
						dblZout_1[j] = dblZout_0[j];
						// scale each by transition coeff and + (Even) or - (Odd) 
						// Resonators 6 and 9 scaled by .15 to get best shape and side lobe supression to - 45 dB while keeping BW at 500 Hz @ -26 dB
						// practical range of scaling .05 to .25
						// Scaling also accomodates for the filter "gain" of approx 60. 

						if (i >= static_FSXmtFilter2000_1500Hz_intFilLen) {
							if (j == 6 | j == 24) {
								intFilteredSamples[i - static_FSXmtFilter2000_1500Hz_intFilLen] += 0.389 * dblZout_0[j];
							} else if (j % 2 == 0) {
								intFilteredSamples[i - static_FSXmtFilter2000_1500Hz_intFilLen] += dblZout_0[j];
							} else {
								intFilteredSamples[i - static_FSXmtFilter2000_1500Hz_intFilLen] -= dblZout_0[j];
							}
						}

					}
					if (i >= static_FSXmtFilter2000_1500Hz_intFilLen) {
						intFilteredSamples[i - static_FSXmtFilter2000_1500Hz_intFilLen] = intFilteredSamples[i - static_FSXmtFilter2000_1500Hz_intFilLen] * 0.00833333333;
						//  rescales for gain of filter
						// Hard clip above 32700
						if (intFilteredSamples[i - static_FSXmtFilter2000_1500Hz_intFilLen] > 32700) {
							intFilteredSamples[i - static_FSXmtFilter2000_1500Hz_intFilLen] = 32700;
						} else if (intFilteredSamples[i - static_FSXmtFilter2000_1500Hz_intFilLen] < -32700) {
							intFilteredSamples[i - static_FSXmtFilter2000_1500Hz_intFilLen] = -32700;
						}
						dblFilteredSamples[i - static_FSXmtFilter2000_1500Hz_intFilLen] = intFilteredSamples[i - static_FSXmtFilter2000_1500Hz_intFilLen];
						// debug code
						if (Math.Abs(intFilteredSamples[i - static_FSXmtFilter2000_1500Hz_intFilLen]) > Math.Abs(intPeakSample))
							intPeakSample = intFilteredSamples[i - static_FSXmtFilter2000_1500Hz_intFilLen];
					}
				}

				// *********************************
				//' Debug code to look at filter output
				WaveTools objWT = new WaveTools();
				if (System.IO.Directory.Exists(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav") == false) {
					System.IO.Directory.CreateDirectory(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav");
				}
				objWT.WriteFloatingRIFF(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav\\UnFiltered" + strFilename, 12000, 16, ref dblUnfilteredSamples);
				objWT.WriteFloatingRIFF(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav\\Filtered" + strFilename, 12000, 16, ref dblFilteredSamples);
				//' End of debug code
				//************************************

			} catch (Exception ex) {
				Debug.WriteLine("[Filters.FSRcvFilterFSK2000_1500Hz] Exception: " + ex.ToString());
				return null;
			}
			return intFilteredSamples;
		}
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter1000_1500Hz_dblR_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();



		// Used for FSK modulation XMIT filter  
		// assumes sample rate of 12000
		// implements 11 100 Hz wide sections centered on 1500 Hz  (~1000 Hz wide @ - 30dB centered on 1500 Hz)
		// FSF (Frequency Selective Filter) variables



		double static_FSXmtFilter1000_1500Hz_dblR;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter1000_1500Hz_intN_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// insures stability (must be < 1.0) (Value .9995 7/8/2013 gives good results)
		int static_FSXmtFilter1000_1500Hz_intN;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter1000_1500Hz_dblRn_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// Length of filter 12000/100
		double static_FSXmtFilter1000_1500Hz_dblRn;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter1000_1500Hz_dblR2_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		double static_FSXmtFilter1000_1500Hz_dblR2;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter1000_1500Hz_dblCoef_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		double[] static_FSXmtFilter1000_1500Hz_dblCoef;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter1000_1500Hz_intFilLen_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		//the coefficients
		// Used in the comb generator
		// The resonators 
		// resonator outputs
		// resonator outputs delayed one sample
		// resonator outputs delayed two samples
		int static_FSXmtFilter1000_1500Hz_intFilLen;
		public Int32[] FSXmtFilter1000_1500Hz(ref Int32[] intNewSamples, string strFilename)
		{
			lock (static_FSXmtFilter1000_1500Hz_dblR_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter1000_1500Hz_dblR_Init)) {
						static_FSXmtFilter1000_1500Hz_dblR = 0.9995;
					}
				} finally {
					static_FSXmtFilter1000_1500Hz_dblR_Init.State = 1;
				}
			}
			lock (static_FSXmtFilter1000_1500Hz_intN_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter1000_1500Hz_intN_Init)) {
						static_FSXmtFilter1000_1500Hz_intN = 120;
					}
				} finally {
					static_FSXmtFilter1000_1500Hz_intN_Init.State = 1;
				}
			}
			lock (static_FSXmtFilter1000_1500Hz_dblRn_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter1000_1500Hz_dblRn_Init)) {
						static_FSXmtFilter1000_1500Hz_dblRn = Math.Pow(static_FSXmtFilter1000_1500Hz_dblR, static_FSXmtFilter1000_1500Hz_intN);
					}
				} finally {
					static_FSXmtFilter1000_1500Hz_dblRn_Init.State = 1;
				}
			}
			lock (static_FSXmtFilter1000_1500Hz_dblR2_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter1000_1500Hz_dblR2_Init)) {
						static_FSXmtFilter1000_1500Hz_dblR2 = Math.Pow(static_FSXmtFilter1000_1500Hz_dblR, 2);
					}
				} finally {
					static_FSXmtFilter1000_1500Hz_dblR2_Init.State = 1;
				}
			}
			lock (static_FSXmtFilter1000_1500Hz_dblCoef_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter1000_1500Hz_dblCoef_Init)) {
						static_FSXmtFilter1000_1500Hz_dblCoef = new double[21];
					}
				} finally {
					static_FSXmtFilter1000_1500Hz_dblCoef_Init.State = 1;
				}
			}
			double dblZin = 0;
			double dblZin_1 = 0;
			double dblZin_2 = 0;
			double dblZComb = 0;
			double[] dblZout_0 = new double[21];
			double[] dblZout_1 = new double[21];
			double[] dblZout_2 = new double[21];
			lock (static_FSXmtFilter1000_1500Hz_intFilLen_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter1000_1500Hz_intFilLen_Init)) {
						static_FSXmtFilter1000_1500Hz_intFilLen = static_FSXmtFilter1000_1500Hz_intN / 2;
					}
				} finally {
					static_FSXmtFilter1000_1500Hz_intFilLen_Init.State = 1;
				}
			}
			Int32[] intFilteredSamples = new Int32[intNewSamples.Length];
			//  Filtered samples
			double[] dblUnfilteredSamples = new double[intNewSamples.Length];
			//for debug wave plotting
			double[] dblFilteredSamples = new double[intNewSamples.Length];
			// for debug wave plotting


			Int32 intPeakSample = 0;
			// Initialize the coefficients
			if (static_FSXmtFilter1000_1500Hz_dblCoef[20] == 0) {
				for (int i = 10; i <= 20; i++) {
					static_FSXmtFilter1000_1500Hz_dblCoef[i] = 2 * static_FSXmtFilter1000_1500Hz_dblR * Math.Cos(2 * Math.PI * i / static_FSXmtFilter1000_1500Hz_intN);
					// For Frequency = bin i
				}
			}
			try {

				for (int i = 0; i <= intNewSamples.Length + static_FSXmtFilter1000_1500Hz_intFilLen - 1; i++) {
					if (i < static_FSXmtFilter1000_1500Hz_intN) {
						dblUnfilteredSamples[i] = intNewSamples[i];
						// debug code for waveform plotting.
						dblZin = intNewSamples[i];
					} else if (i < intNewSamples.Length) {
						dblUnfilteredSamples[i] = intNewSamples[i];
						// debug code for waveform plotting.
						dblZin = intNewSamples[i] - static_FSXmtFilter1000_1500Hz_dblRn * intNewSamples[i - static_FSXmtFilter1000_1500Hz_intN];
					} else {
						dblZin = -static_FSXmtFilter1000_1500Hz_dblRn * intNewSamples[i - static_FSXmtFilter1000_1500Hz_intN];
					}
					// Compute the Comb
					dblZComb = dblZin - dblZin_2 * static_FSXmtFilter1000_1500Hz_dblR2;
					dblZin_2 = dblZin_1;
					dblZin_1 = dblZin;

					// DateTime.Now the resonators

					// calculate output for 11 resonators 
					for (int j = 10; j <= 20; j++) {
						dblZout_0[j] = dblZComb + static_FSXmtFilter1000_1500Hz_dblCoef[j] * dblZout_1[j] - static_FSXmtFilter1000_1500Hz_dblR2 * dblZout_2[j];
						dblZout_2[j] = dblZout_1[j];
						dblZout_1[j] = dblZout_0[j];
						// scale each by transition coeff and + (Even) or - (Odd) 
						// Resonators 6 and 9 scaled by .15 to get best shape and side lobe supression to - 45 dB while keeping BW at 500 Hz @ -26 dB
						// practical range of scaling .05 to .25
						// Scaling also accomodates for the filter "gain" of approx 60. 

						if (i >= static_FSXmtFilter1000_1500Hz_intFilLen) {
							if (j == 10 | j == 20) {
								intFilteredSamples[i - static_FSXmtFilter1000_1500Hz_intFilLen] += 0.389 * dblZout_0[j];
							} else if (j % 2 == 0) {
								intFilteredSamples[i - static_FSXmtFilter1000_1500Hz_intFilLen] += dblZout_0[j];
							} else {
								intFilteredSamples[i - static_FSXmtFilter1000_1500Hz_intFilLen] -= dblZout_0[j];
							}
						}

					}

					if (i >= static_FSXmtFilter1000_1500Hz_intFilLen) {
						intFilteredSamples[i - static_FSXmtFilter1000_1500Hz_intFilLen] = intFilteredSamples[i - static_FSXmtFilter1000_1500Hz_intFilLen] * 0.00833333333;
						//  rescales for gain of filter
						// Hard clip above 32700
						if (intFilteredSamples[i - static_FSXmtFilter1000_1500Hz_intFilLen] > 32700) {
							intFilteredSamples[i - static_FSXmtFilter1000_1500Hz_intFilLen] = 32700;
						} else if (intFilteredSamples[i - static_FSXmtFilter1000_1500Hz_intFilLen] < -32700) {
							intFilteredSamples[i - static_FSXmtFilter1000_1500Hz_intFilLen] = -32700;
						}
						dblFilteredSamples[i - static_FSXmtFilter1000_1500Hz_intFilLen] = intFilteredSamples[i - static_FSXmtFilter1000_1500Hz_intFilLen];
						// debug code
						if (Math.Abs(intFilteredSamples[i - static_FSXmtFilter1000_1500Hz_intFilLen]) > Math.Abs(intPeakSample))
							intPeakSample = intFilteredSamples[i - static_FSXmtFilter1000_1500Hz_intFilLen];
					}
				}

				// *********************************
				// Debug code to look at filter output
				WaveTools objWT = new WaveTools();
				if (System.IO.Directory.Exists(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav") == false) {
					System.IO.Directory.CreateDirectory(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav");
				}
				objWT.WriteFloatingRIFF(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav\\UnFiltered" + strFilename, 12000, 16, ref dblUnfilteredSamples);
				objWT.WriteFloatingRIFF(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav\\Filtered" + strFilename, 12000, 16, ref dblFilteredSamples);
				// End of debug code
				//************************************

			} catch (Exception ex) {
				Debug.WriteLine("[Filters.FSRcvFilterFSK500_1500Hz] Exception: " + ex.ToString());
				return null;
			}
			return intFilteredSamples;
		}
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter500_1500Hz_dblR_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();



		// Used for FSK modulation XMIT filter  
		// assumes sample rate of 12000
		// implements 7 100 Hz wide sections centered on 1500 Hz  (~500 Hz wide @ - 30dB centered on 1500 Hz)
		// FSF (Frequency Selective Filter) variables



		double static_FSXmtFilter500_1500Hz_dblR;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter500_1500Hz_intN_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// insures stability (must be < 1.0) (Value .9995 7/8/2013 gives good results)
		int static_FSXmtFilter500_1500Hz_intN;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter500_1500Hz_dblRn_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// Length of filter 12000/100
		double static_FSXmtFilter500_1500Hz_dblRn;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter500_1500Hz_dblR2_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		double static_FSXmtFilter500_1500Hz_dblR2;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter500_1500Hz_dblCoef_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		double[] static_FSXmtFilter500_1500Hz_dblCoef;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter500_1500Hz_intFilLen_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		//the coefficients
		// Used in the comb generator
		// The resonators 
		// resonator outputs
		// resonator outputs delayed one sample
		// resonator outputs delayed two samples
		int static_FSXmtFilter500_1500Hz_intFilLen;
		public Int32[] FSXmtFilter500_1500Hz(ref Int32[] intNewSamples, string strFilename)
		{
			lock (static_FSXmtFilter500_1500Hz_dblR_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter500_1500Hz_dblR_Init)) {
						static_FSXmtFilter500_1500Hz_dblR = 0.9995;
					}
				} finally {
					static_FSXmtFilter500_1500Hz_dblR_Init.State = 1;
				}
			}
			lock (static_FSXmtFilter500_1500Hz_intN_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter500_1500Hz_intN_Init)) {
						static_FSXmtFilter500_1500Hz_intN = 120;
					}
				} finally {
					static_FSXmtFilter500_1500Hz_intN_Init.State = 1;
				}
			}
			lock (static_FSXmtFilter500_1500Hz_dblRn_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter500_1500Hz_dblRn_Init)) {
						static_FSXmtFilter500_1500Hz_dblRn = Math.Pow(static_FSXmtFilter500_1500Hz_dblR, static_FSXmtFilter500_1500Hz_intN);
					}
				} finally {
					static_FSXmtFilter500_1500Hz_dblRn_Init.State = 1;
				}
			}
			lock (static_FSXmtFilter500_1500Hz_dblR2_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter500_1500Hz_dblR2_Init)) {
						static_FSXmtFilter500_1500Hz_dblR2 = Math.Pow(static_FSXmtFilter500_1500Hz_dblR, 2);
					}
				} finally {
					static_FSXmtFilter500_1500Hz_dblR2_Init.State = 1;
				}
			}
			lock (static_FSXmtFilter500_1500Hz_dblCoef_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter500_1500Hz_dblCoef_Init)) {
						static_FSXmtFilter500_1500Hz_dblCoef = new double[19];
					}
				} finally {
					static_FSXmtFilter500_1500Hz_dblCoef_Init.State = 1;
				}
			}
			double dblZin = 0;
			double dblZin_1 = 0;
			double dblZin_2 = 0;
			double dblZComb = 0;
			double[] dblZout_0 = new double[19];
			double[] dblZout_1 = new double[19];
			double[] dblZout_2 = new double[19];
			lock (static_FSXmtFilter500_1500Hz_intFilLen_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter500_1500Hz_intFilLen_Init)) {
						static_FSXmtFilter500_1500Hz_intFilLen = static_FSXmtFilter500_1500Hz_intN / 2;
					}
				} finally {
					static_FSXmtFilter500_1500Hz_intFilLen_Init.State = 1;
				}
			}
			Int32[] intFilteredSamples = new Int32[intNewSamples.Length];
			//  Filtered samples
			double[] dblUnfilteredSamples = new double[intNewSamples.Length];
			//for debug wave plotting
			double[] dblFilteredSamples = new double[intNewSamples.Length];
			// for debug wave plotting


			Int32 intPeakSample = 0;
			// Initialize the coefficients
			if (static_FSXmtFilter500_1500Hz_dblCoef[18] == 0) {
				for (int i = 12; i <= 18; i++) {
					static_FSXmtFilter500_1500Hz_dblCoef[i] = 2 * static_FSXmtFilter500_1500Hz_dblR * Math.Cos(2 * Math.PI * i / static_FSXmtFilter500_1500Hz_intN);
					// For Frequency = bin i
				}
			}
			try {

				for (int i = 0; i <= intNewSamples.Length + static_FSXmtFilter500_1500Hz_intFilLen - 1; i++) {
					if (i < static_FSXmtFilter500_1500Hz_intN) {
						dblUnfilteredSamples[i] = intNewSamples[i];
						// debug code for waveform plotting.
						dblZin = intNewSamples[i];
					} else if (i < intNewSamples.Length) {
						dblUnfilteredSamples[i] = intNewSamples[i];
						// debug code for waveform plotting.
						dblZin = intNewSamples[i] - static_FSXmtFilter500_1500Hz_dblRn * intNewSamples[i - static_FSXmtFilter500_1500Hz_intN];
					} else {
						dblZin = -static_FSXmtFilter500_1500Hz_dblRn * intNewSamples[i - static_FSXmtFilter500_1500Hz_intN];
					}
					// Compute the Comb
					dblZComb = dblZin - dblZin_2 * static_FSXmtFilter500_1500Hz_dblR2;
					dblZin_2 = dblZin_1;
					dblZin_1 = dblZin;

					// DateTime.Now the resonators

					// calculate output for 7 resonators 
					for (int j = 12; j <= 18; j++) {
						dblZout_0[j] = dblZComb + static_FSXmtFilter500_1500Hz_dblCoef[j] * dblZout_1[j] - static_FSXmtFilter500_1500Hz_dblR2 * dblZout_2[j];
						dblZout_2[j] = dblZout_1[j];
						dblZout_1[j] = dblZout_0[j];
						// scale each by transition coeff and + (Even) or - (Odd) 
						// Resonators 6 and 9 scaled by .15 to get best shape and side lobe supression to - 45 dB while keeping BW at 500 Hz @ -26 dB
						// practical range of scaling .05 to .25
						// Scaling also accomodates for the filter "gain" of approx 60. 

						if (i >= static_FSXmtFilter500_1500Hz_intFilLen) {
							if (j == 12 | j == 18) {
								intFilteredSamples[i - static_FSXmtFilter500_1500Hz_intFilLen] += 0.10601 * dblZout_0[j];
							} else if (j == 13 | j == 17) {
								intFilteredSamples[i - static_FSXmtFilter500_1500Hz_intFilLen] -= 0.59383 * dblZout_0[j];
							} else if (j % 2 == 0) {
								intFilteredSamples[i - static_FSXmtFilter500_1500Hz_intFilLen] += dblZout_0[j];
							} else {
								intFilteredSamples[i - static_FSXmtFilter500_1500Hz_intFilLen] -= dblZout_0[j];
							}
						}

					}

					if (i >= static_FSXmtFilter500_1500Hz_intFilLen) {
						intFilteredSamples[i - static_FSXmtFilter500_1500Hz_intFilLen] = intFilteredSamples[i - static_FSXmtFilter500_1500Hz_intFilLen] * 0.00833333333;
						//  rescales for gain of filter
						// Hard clip above 32700
						if (intFilteredSamples[i - static_FSXmtFilter500_1500Hz_intFilLen] > 32700) {
							intFilteredSamples[i - static_FSXmtFilter500_1500Hz_intFilLen] = 32700;
						} else if (intFilteredSamples[i - static_FSXmtFilter500_1500Hz_intFilLen] < -32700) {
							intFilteredSamples[i - static_FSXmtFilter500_1500Hz_intFilLen] = -32700;
						}
						dblFilteredSamples[i - static_FSXmtFilter500_1500Hz_intFilLen] = intFilteredSamples[i - static_FSXmtFilter500_1500Hz_intFilLen];
						// debug code
						if (Math.Abs(intFilteredSamples[i - static_FSXmtFilter500_1500Hz_intFilLen]) > Math.Abs(intPeakSample))
							intPeakSample = intFilteredSamples[i - static_FSXmtFilter500_1500Hz_intFilLen];
					}
				}

				// *********************************
				// Debug code to look at filter output
				WaveTools objWT = new WaveTools();
				if (System.IO.Directory.Exists(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav") == false) {
					System.IO.Directory.CreateDirectory(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav");
				}
				objWT.WriteFloatingRIFF(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav\\UnFiltered" + strFilename, 12000, 16, ref dblUnfilteredSamples);
				objWT.WriteFloatingRIFF(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav\\Filtered" + strFilename, 12000, 16, ref dblFilteredSamples);
				// End of debug code
				//************************************

			} catch (Exception ex) {
				Debug.WriteLine("[Filters.FSRcvFilterFSK500_1500Hz] Exception: " + ex.ToString());
				return null;
			}
			return intFilteredSamples;
		}
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter200_1500Hz_dblR_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();

		// Xmit filter 200 Hz for 1 Car PSK 4 modes

		// Used for PSK 200 Hz modulation XMIT filter  
		// assumes sample rate of 12000
		// implements 3 100 Hz wide sections centered on 1500 Hz  (~200 Hz wide @ - 30dB centered on 1500 Hz)
		// FSF (Frequency Selective Filter) variables

		double static_FSXmtFilter200_1500Hz_dblR;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter200_1500Hz_intN_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// insures stability (must be < 1.0) (Value .9995 7/8/2013 gives good results)
		int static_FSXmtFilter200_1500Hz_intN;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter200_1500Hz_dblRn_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// Length of filter 12000/100
		double static_FSXmtFilter200_1500Hz_dblRn;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter200_1500Hz_dblR2_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		double static_FSXmtFilter200_1500Hz_dblR2;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter200_1500Hz_dblCoef_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		double[] static_FSXmtFilter200_1500Hz_dblCoef;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSXmtFilter200_1500Hz_intFilLen_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		//the coefficients
		// Used in the comb generator
		// The resonators 
		// resonator outputs
		// resonator outputs delayed one sample
		// resonator outputs delayed two samples
		int static_FSXmtFilter200_1500Hz_intFilLen;
		public Int32[] FSXmtFilter200_1500Hz(ref Int32[] intNewSamples, string strFilename)
		{
			lock (static_FSXmtFilter200_1500Hz_dblR_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter200_1500Hz_dblR_Init)) {
						static_FSXmtFilter200_1500Hz_dblR = 0.9995;
					}
				} finally {
					static_FSXmtFilter200_1500Hz_dblR_Init.State = 1;
				}
			}
			lock (static_FSXmtFilter200_1500Hz_intN_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter200_1500Hz_intN_Init)) {
						static_FSXmtFilter200_1500Hz_intN = 120;
					}
				} finally {
					static_FSXmtFilter200_1500Hz_intN_Init.State = 1;
				}
			}
			lock (static_FSXmtFilter200_1500Hz_dblRn_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter200_1500Hz_dblRn_Init)) {
						static_FSXmtFilter200_1500Hz_dblRn = Math.Pow(static_FSXmtFilter200_1500Hz_dblR, static_FSXmtFilter200_1500Hz_intN);
					}
				} finally {
					static_FSXmtFilter200_1500Hz_dblRn_Init.State = 1;
				}
			}
			lock (static_FSXmtFilter200_1500Hz_dblR2_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter200_1500Hz_dblR2_Init)) {
						static_FSXmtFilter200_1500Hz_dblR2 = Math.Pow(static_FSXmtFilter200_1500Hz_dblR, 2);
					}
				} finally {
					static_FSXmtFilter200_1500Hz_dblR2_Init.State = 1;
				}
			}
			lock (static_FSXmtFilter200_1500Hz_dblCoef_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter200_1500Hz_dblCoef_Init)) {
						static_FSXmtFilter200_1500Hz_dblCoef = new double[19];
					}
				} finally {
					static_FSXmtFilter200_1500Hz_dblCoef_Init.State = 1;
				}
			}
			double dblZin = 0;
			double dblZin_1 = 0;
			double dblZin_2 = 0;
			double dblZComb = 0;
			double[] dblZout_0 = new double[19];
			double[] dblZout_1 = new double[19];
			double[] dblZout_2 = new double[19];
			lock (static_FSXmtFilter200_1500Hz_intFilLen_Init) {
				try {
					if (InitStaticVariableHelper(static_FSXmtFilter200_1500Hz_intFilLen_Init)) {
						static_FSXmtFilter200_1500Hz_intFilLen = static_FSXmtFilter200_1500Hz_intN / 2;
					}
				} finally {
					static_FSXmtFilter200_1500Hz_intFilLen_Init.State = 1;
				}
			}
			Int32[] intFilteredSamples = new Int32[intNewSamples.Length];
			//  Filtered samples
			double[] dblUnfilteredSamples = new double[intNewSamples.Length];
			//for debug wave plotting
			double[] dblFilteredSamples = new double[intNewSamples.Length];
			// for debug wave plotting
			// Initialize the coefficients
			if (static_FSXmtFilter200_1500Hz_dblCoef[15] == 0) {
				for (int i = 14; i <= 16; i++) {
					static_FSXmtFilter200_1500Hz_dblCoef[i] = 2 * static_FSXmtFilter200_1500Hz_dblR * Math.Cos(2 * Math.PI * i / static_FSXmtFilter200_1500Hz_intN);
					// For Frequency = bin i
				}
			}
			try {

				for (int i = 0; i <= intNewSamples.Length + static_FSXmtFilter200_1500Hz_intFilLen - 1; i++) {
					if (i < static_FSXmtFilter200_1500Hz_intN) {
						dblUnfilteredSamples[i] = intNewSamples[i];
						// debug code for waveform plotting.
						dblZin = intNewSamples[i];
					} else if (i < intNewSamples.Length) {
						dblUnfilteredSamples[i] = intNewSamples[i];
						// debug code for waveform plotting.
						dblZin = intNewSamples[i] - static_FSXmtFilter200_1500Hz_dblRn * intNewSamples[i - static_FSXmtFilter200_1500Hz_intN];
					} else {
						dblZin = -static_FSXmtFilter200_1500Hz_dblRn * intNewSamples[i - static_FSXmtFilter200_1500Hz_intN];
					}
					// Compute the Comb
					dblZComb = dblZin - dblZin_2 * static_FSXmtFilter200_1500Hz_dblR2;
					dblZin_2 = dblZin_1;
					dblZin_1 = dblZin;

					// DateTime.Now the resonators

					// calculate output for 3 resonators 
					for (int j = 14; j <= 16; j++) {
						dblZout_0[j] = dblZComb + static_FSXmtFilter200_1500Hz_dblCoef[j] * dblZout_1[j] - static_FSXmtFilter200_1500Hz_dblR2 * dblZout_2[j];
						dblZout_2[j] = dblZout_1[j];
						dblZout_1[j] = dblZout_0[j];
						// scale each by transition coeff and + (Even) or - (Odd) 
						// Resonators 6 and 9 scaled by .15 to get best shape and side lobe supression to - 45 dB while keeping BW at 500 Hz @ -26 dB
						// practical range of scaling .05 to .25
						// Scaling also accomodates for the filter "gain" of approx 60. 

						if (i >= static_FSXmtFilter200_1500Hz_intFilLen) {
							if (j == 14 | j == 16) {
								intFilteredSamples[i - static_FSXmtFilter200_1500Hz_intFilLen] += 0.389 * dblZout_0[j];
							} else {
								intFilteredSamples[i - static_FSXmtFilter200_1500Hz_intFilLen] -= dblZout_0[j];
							}
						}

					}
					if (i >= static_FSXmtFilter200_1500Hz_intFilLen) {
						intFilteredSamples[i - static_FSXmtFilter200_1500Hz_intFilLen] = intFilteredSamples[i - static_FSXmtFilter200_1500Hz_intFilLen] * 0.00833333333;
						//  rescales for gain of filter
						// Hard clip above 32700
						if (intFilteredSamples[i - static_FSXmtFilter200_1500Hz_intFilLen] > 32700) {
							intFilteredSamples[i - static_FSXmtFilter200_1500Hz_intFilLen] = 32700;
						} else if (intFilteredSamples[i - static_FSXmtFilter200_1500Hz_intFilLen] < -32700) {
							intFilteredSamples[i - static_FSXmtFilter200_1500Hz_intFilLen] = -32700;
						}
						dblFilteredSamples[i - static_FSXmtFilter200_1500Hz_intFilLen] = intFilteredSamples[i - static_FSXmtFilter200_1500Hz_intFilLen];
						// debug code
					}
				}

				// *********************************
				// Debug code to look at filter output
				//Dim objWT As New WaveTools
				//If IO.Directory.Exists(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\")) & "\Wav") = False Then
				//    IO.Directory.CreateDirectory(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\")) & "\Wav")
				//End If
				//objWT.WriteFloatingRIFF(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\")) & "\Wav\UnFiltered" & strFilename, 12000, 16, dblUnfilteredSamples)
				//objWT.WriteFloatingRIFF(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\")) & "\Wav\Filtered" & strFilename, 12000, 16, dblFilteredSamples)
				// End of debug code
				//************************************

			} catch (Exception ex) {
				Debug.WriteLine("[Filters.FSXmtFilterPSK200_1500Hz] Exception: " + ex.ToString());
				return null;
			}
			return intFilteredSamples;
		}
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter2000_1500Hz_dblR_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// assumes sample rate of 12000
		// implements  8 300 Hz wide sections centered on 1500 Hz  (~2000 Hz wide @ - 30dB centered on 1500 Hz)
		// FSF (Frequency Selective Filter) variables

		//  Filtered samples

		double static_FSRcvFilter2000_1500Hz_dblR;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter2000_1500Hz_intN_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// insures stability (must be < 1.0) (Value .9995 7/8/2013 gives good results)
		int static_FSRcvFilter2000_1500Hz_intN;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter2000_1500Hz_dblRn_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// Length of filter 12000/300
		double static_FSRcvFilter2000_1500Hz_dblRn;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter2000_1500Hz_dblR2_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		double static_FSRcvFilter2000_1500Hz_dblR2;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter2000_1500Hz_dblCoef_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		double[] static_FSRcvFilter2000_1500Hz_dblCoef;
		double static_FSRcvFilter2000_1500Hz_dblZin;
		double static_FSRcvFilter2000_1500Hz_dblZin_1;
		double static_FSRcvFilter2000_1500Hz_dblZin_2;
		//the coefficients
		double static_FSRcvFilter2000_1500Hz_dblZComb;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter2000_1500Hz_dblZout_0_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// Used in the comb generator
		// The resonators 
		double[] static_FSRcvFilter2000_1500Hz_dblZout_0;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter2000_1500Hz_dblZout_1_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// resonator outputs
		double[] static_FSRcvFilter2000_1500Hz_dblZout_1;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter2000_1500Hz_dblZout_2_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// resonator outputs delayed one sample
		double[] static_FSRcvFilter2000_1500Hz_dblZout_2;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter2000_1500Hz_intFilLen_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// resonator outputs delayed two samples
		int static_FSRcvFilter2000_1500Hz_intFilLen;
		public Int32[] FSRcvFilter2000_1500Hz(ref Int32[] intNewSamples, string strFilename)
		{
			Int32[] intFilteredSamples = new Int32[intNewSamples.Length];
			lock (static_FSRcvFilter2000_1500Hz_dblR_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter2000_1500Hz_dblR_Init)) {
						static_FSRcvFilter2000_1500Hz_dblR = 0.9995;
					}
				} finally {
					static_FSRcvFilter2000_1500Hz_dblR_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter2000_1500Hz_intN_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter2000_1500Hz_intN_Init)) {
						static_FSRcvFilter2000_1500Hz_intN = 40;
					}
				} finally {
					static_FSRcvFilter2000_1500Hz_intN_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter2000_1500Hz_dblRn_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter2000_1500Hz_dblRn_Init)) {
						static_FSRcvFilter2000_1500Hz_dblRn = Math.Pow(static_FSRcvFilter2000_1500Hz_dblR, static_FSRcvFilter2000_1500Hz_intN);
					}
				} finally {
					static_FSRcvFilter2000_1500Hz_dblRn_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter2000_1500Hz_dblR2_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter2000_1500Hz_dblR2_Init)) {
						static_FSRcvFilter2000_1500Hz_dblR2 = Math.Pow(static_FSRcvFilter2000_1500Hz_dblR, 2);
					}
				} finally {
					static_FSRcvFilter2000_1500Hz_dblR2_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter2000_1500Hz_dblCoef_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter2000_1500Hz_dblCoef_Init)) {
						static_FSRcvFilter2000_1500Hz_dblCoef = new double[9];
					}
				} finally {
					static_FSRcvFilter2000_1500Hz_dblCoef_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter2000_1500Hz_dblZout_0_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter2000_1500Hz_dblZout_0_Init)) {
						static_FSRcvFilter2000_1500Hz_dblZout_0 = new double[9];
					}
				} finally {
					static_FSRcvFilter2000_1500Hz_dblZout_0_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter2000_1500Hz_dblZout_1_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter2000_1500Hz_dblZout_1_Init)) {
						static_FSRcvFilter2000_1500Hz_dblZout_1 = new double[9];
					}
				} finally {
					static_FSRcvFilter2000_1500Hz_dblZout_1_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter2000_1500Hz_dblZout_2_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter2000_1500Hz_dblZout_2_Init)) {
						static_FSRcvFilter2000_1500Hz_dblZout_2 = new double[9];
					}
				} finally {
					static_FSRcvFilter2000_1500Hz_dblZout_2_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter2000_1500Hz_intFilLen_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter2000_1500Hz_intFilLen_Init)) {
						static_FSRcvFilter2000_1500Hz_intFilLen = static_FSRcvFilter2000_1500Hz_intN / 2;
					}
				} finally {
					static_FSRcvFilter2000_1500Hz_intFilLen_Init.State = 1;
				}
			}

			// Initialize the coefficients
			if (static_FSRcvFilter2000_1500Hz_dblCoef[7] == 0) {
				for (int i = 2; i <= 8; i++) {
					static_FSRcvFilter2000_1500Hz_dblCoef[i] = 2 * static_FSRcvFilter2000_1500Hz_dblR * Math.Cos(2 * Math.PI * i / static_FSRcvFilter2000_1500Hz_intN);
					// For Frequency = bin i
				}
			}
			try {
				for (int i = 0; i <= intNewSamples.Length - 1; i++) {
					if (i < static_FSRcvFilter2000_1500Hz_intN) {
						static_FSRcvFilter2000_1500Hz_dblZin = intNewSamples[i];
					} else {
						static_FSRcvFilter2000_1500Hz_dblZin = intNewSamples[i] - static_FSRcvFilter2000_1500Hz_dblRn * intNewSamples[i - static_FSRcvFilter2000_1500Hz_intN];
					}
					// Compute the Comb
					static_FSRcvFilter2000_1500Hz_dblZComb = static_FSRcvFilter2000_1500Hz_dblZin - static_FSRcvFilter2000_1500Hz_dblZin_2 * static_FSRcvFilter2000_1500Hz_dblR2;
					static_FSRcvFilter2000_1500Hz_dblZin_2 = static_FSRcvFilter2000_1500Hz_dblZin_1;
					static_FSRcvFilter2000_1500Hz_dblZin_1 = static_FSRcvFilter2000_1500Hz_dblZin;

					// DateTime.Now the resonators

					// calculate output for 8 resonators 
					for (int j = 2; j <= 8; j++) {
						static_FSRcvFilter2000_1500Hz_dblZout_0[j] = static_FSRcvFilter2000_1500Hz_dblZComb + static_FSRcvFilter2000_1500Hz_dblCoef[j] * static_FSRcvFilter2000_1500Hz_dblZout_1[j] - static_FSRcvFilter2000_1500Hz_dblR2 * static_FSRcvFilter2000_1500Hz_dblZout_2[j];
						static_FSRcvFilter2000_1500Hz_dblZout_2[j] = static_FSRcvFilter2000_1500Hz_dblZout_1[j];
						static_FSRcvFilter2000_1500Hz_dblZout_1[j] = static_FSRcvFilter2000_1500Hz_dblZout_0[j];
						// scale each by transition coeff and + (Even) or - (Odd) 
						// Resonators 2 and 8 scaled by .15 to get best shape and side lobe supression while keeping BW at 500 Hz
						// practical range of scaling .05 to 2.5
						// Scaling also accomodates for the filter "gain" of approx 40. 
						if (j == 2 | j == 8) {
							intFilteredSamples[i] += 0.389 * static_FSRcvFilter2000_1500Hz_dblZout_0[j];
						} else if (j % 2 == 0) {
							intFilteredSamples[i] += static_FSRcvFilter2000_1500Hz_dblZout_0[j];
						} else {
							intFilteredSamples[i] -= static_FSRcvFilter2000_1500Hz_dblZout_0(j);
						}
					}
					intFilteredSamples[i] = intFilteredSamples[i] * 0.025;
					// rescales for gain of filter
				}

				// *********************************
				// Debug code to look at filter output
				double[] dblInFiltered = new double[intFilteredSamples.Length];
				for (int k = 0; k <= dblInFiltered.Length - 1; k++) {
					dblInFiltered[k] = intFilteredSamples[k];
				}
				WaveTools objWT = new WaveTools();
				if (System.IO.Directory.Exists(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav") == false) {
					System.IO.Directory.CreateDirectory(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav");
				}
				objWT.WriteFloatingRIFF(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav\\" + strFilename, 12000, 16, ref dblInFiltered);
				// End of debug code
				//************************************

			} catch (Exception ex) {
				Debug.WriteLine("[Filters.FSRcvFilter2200_1500Hz] Exception: " + ex.ToString());
				return null;
			}
			return intFilteredSamples;
		}
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSMixFilter2500Hz_dblR_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();

		// assumes sample rate of 12000
		// implements  12 200 Hz wide sections   (~2400 Hz wide @ - 30dB centered on 1500 Hz)
		// FSF (Frequency Selective Filter) variables

		//  Filtered samples

		double static_FSMixFilter2500Hz_dblR;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSMixFilter2500Hz_intN_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// insures stability (must be < 1.0) (Value .9995 3/4/2014 gives good results)
		int static_FSMixFilter2500Hz_intN;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSMixFilter2500Hz_dblRn_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// Length of filter 12000/200
		double static_FSMixFilter2500Hz_dblRn;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSMixFilter2500Hz_dblR2_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		double static_FSMixFilter2500Hz_dblR2;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSMixFilter2500Hz_dblCoef_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		double[] static_FSMixFilter2500Hz_dblCoef;
		double static_FSMixFilter2500Hz_dblZin;
		double static_FSMixFilter2500Hz_dblZin_1;
		double static_FSMixFilter2500Hz_dblZin_2;
		//the coefficients
		double static_FSMixFilter2500Hz_dblZComb;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSMixFilter2500Hz_dblZout_0_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// Used in the comb generator
		// The resonators 
		double[] static_FSMixFilter2500Hz_dblZout_0;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSMixFilter2500Hz_dblZout_1_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// resonator outputs
		double[] static_FSMixFilter2500Hz_dblZout_1;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSMixFilter2500Hz_dblZout_2_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// resonator outputs delayed one sample
		double[] static_FSMixFilter2500Hz_dblZout_2;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSMixFilter2500Hz_intFilLen_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// resonator outputs delayed two samples
		int static_FSMixFilter2500Hz_intFilLen;
		public Int32[] FSMixFilter2500Hz(ref Int32[] intNewSamples, string strFilename)
		{
			Int32[] intFilteredSamples = new Int32[intNewSamples.Length];
			lock (static_FSMixFilter2500Hz_dblR_Init) {
				try {
					if (InitStaticVariableHelper(static_FSMixFilter2500Hz_dblR_Init)) {
						static_FSMixFilter2500Hz_dblR = 0.9995;
					}
				} finally {
					static_FSMixFilter2500Hz_dblR_Init.State = 1;
				}
			}
			lock (static_FSMixFilter2500Hz_intN_Init) {
				try {
					if (InitStaticVariableHelper(static_FSMixFilter2500Hz_intN_Init)) {
						static_FSMixFilter2500Hz_intN = 60;
					}
				} finally {
					static_FSMixFilter2500Hz_intN_Init.State = 1;
				}
			}
			lock (static_FSMixFilter2500Hz_dblRn_Init) {
				try {
					if (InitStaticVariableHelper(static_FSMixFilter2500Hz_dblRn_Init)) {
						static_FSMixFilter2500Hz_dblRn = Math.Pow(static_FSMixFilter2500Hz_dblR, static_FSMixFilter2500Hz_intN);
					}
				} finally {
					static_FSMixFilter2500Hz_dblRn_Init.State = 1;
				}
			}
			lock (static_FSMixFilter2500Hz_dblR2_Init) {
				try {
					if (InitStaticVariableHelper(static_FSMixFilter2500Hz_dblR2_Init)) {
						static_FSMixFilter2500Hz_dblR2 = Math.Pow(static_FSMixFilter2500Hz_dblR, 2);
					}
				} finally {
					static_FSMixFilter2500Hz_dblR2_Init.State = 1;
				}
			}
			lock (static_FSMixFilter2500Hz_dblCoef_Init) {
				try {
					if (InitStaticVariableHelper(static_FSMixFilter2500Hz_dblCoef_Init)) {
						static_FSMixFilter2500Hz_dblCoef = new double[14];
					}
				} finally {
					static_FSMixFilter2500Hz_dblCoef_Init.State = 1;
				}
			}
			lock (static_FSMixFilter2500Hz_dblZout_0_Init) {
				try {
					if (InitStaticVariableHelper(static_FSMixFilter2500Hz_dblZout_0_Init)) {
						static_FSMixFilter2500Hz_dblZout_0 = new double[14];
					}
				} finally {
					static_FSMixFilter2500Hz_dblZout_0_Init.State = 1;
				}
			}
			lock (static_FSMixFilter2500Hz_dblZout_1_Init) {
				try {
					if (InitStaticVariableHelper(static_FSMixFilter2500Hz_dblZout_1_Init)) {
						static_FSMixFilter2500Hz_dblZout_1 = new double[14];
					}
				} finally {
					static_FSMixFilter2500Hz_dblZout_1_Init.State = 1;
				}
			}
			lock (static_FSMixFilter2500Hz_dblZout_2_Init) {
				try {
					if (InitStaticVariableHelper(static_FSMixFilter2500Hz_dblZout_2_Init)) {
						static_FSMixFilter2500Hz_dblZout_2 = new double[14];
					}
				} finally {
					static_FSMixFilter2500Hz_dblZout_2_Init.State = 1;
				}
			}
			lock (static_FSMixFilter2500Hz_intFilLen_Init) {
				try {
					if (InitStaticVariableHelper(static_FSMixFilter2500Hz_intFilLen_Init)) {
						static_FSMixFilter2500Hz_intFilLen = static_FSMixFilter2500Hz_intN / 2;
					}
				} finally {
					static_FSMixFilter2500Hz_intFilLen_Init.State = 1;
				}
			}

			// Initialize the coefficients
			if (static_FSMixFilter2500Hz_dblCoef[13] == 0) {
				for (int i = 2; i <= 13; i++) {
					static_FSMixFilter2500Hz_dblCoef[i] = 2 * static_FSMixFilter2500Hz_dblR * Math.Cos(2 * Math.PI * i / static_FSMixFilter2500Hz_intN);
					// For Frequency = bin i
				}
			}
			try {
				for (int i = 0; i <= intNewSamples.Length - 1; i++) {
					if (i < static_FSMixFilter2500Hz_intN) {
						static_FSMixFilter2500Hz_dblZin = intNewSamples[i];
					} else {
						static_FSMixFilter2500Hz_dblZin = intNewSamples[i] - static_FSMixFilter2500Hz_dblRn * intNewSamples[i - static_FSMixFilter2500Hz_intN];
					}
					// Compute the Comb
					static_FSMixFilter2500Hz_dblZComb = static_FSMixFilter2500Hz_dblZin - static_FSMixFilter2500Hz_dblZin_2 * static_FSMixFilter2500Hz_dblR2;
					static_FSMixFilter2500Hz_dblZin_2 = static_FSMixFilter2500Hz_dblZin_1;
					static_FSMixFilter2500Hz_dblZin_1 = static_FSMixFilter2500Hz_dblZin;

					// DateTime.Now the resonators

					// calculate output for 13 resonators 
					for (int j = 2; j <= 13; j++) {
						static_FSMixFilter2500Hz_dblZout_0[j] = static_FSMixFilter2500Hz_dblZComb + static_FSMixFilter2500Hz_dblCoef[j] * static_FSMixFilter2500Hz_dblZout_1[j] - static_FSMixFilter2500Hz_dblR2 * static_FSMixFilter2500Hz_dblZout_2[j];
						static_FSMixFilter2500Hz_dblZout_2[j] = static_FSMixFilter2500Hz_dblZout_1[j];
						static_FSMixFilter2500Hz_dblZout_1[j] = static_FSMixFilter2500Hz_dblZout_0[j];
						// scale each by transition coeff and + (Even) or - (Odd) 
						// Resonators 2 and 13 scaled by .389 get best shape and side lobe supression 
						// Scaling also accomodates for the filter "gain" of approx 60. 
						if (j == 2) {
							intFilteredSamples[i] += 0.389 * static_FSMixFilter2500Hz_dblZout_0[j];
						} else if (j == 13) {
							intFilteredSamples[i] -= 0.389 * static_FSMixFilter2500Hz_dblZout_0[j];
						} else if (j % 2 == 0) {
							intFilteredSamples[i] += static_FSMixFilter2500Hz_dblZout_0[j];
						} else {
							intFilteredSamples[i] -= static_FSMixFilter2500Hz_dblZout_0[j];
						}
					}
					intFilteredSamples[i] = intFilteredSamples[i] * 0.01666666;
					// rescales for gain of filter
				}

				// *********************************
				// Debug code to look at filter output
				double[] dblInFiltered = new double[intFilteredSamples.Length];
				for (int k = 0; k <= dblInFiltered.Length - 1; k++) {
					dblInFiltered[k] = intFilteredSamples[k];
				}
				WaveTools objWT = new WaveTools();
				if (System.IO.Directory.Exists(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav") == false) {
					System.IO.Directory.CreateDirectory(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav");
				}
				objWT.WriteFloatingRIFF(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav\\" + strFilename, 12000, 16, ref dblInFiltered);
				// End of debug code
				//************************************

			} catch (Exception ex) {
				Debug.WriteLine("[Filters.FSRcvFilter2200_1500Hz] Exception: " + ex.ToString());
				return null;
			}
			return intFilteredSamples;
		}
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter1500BW_1500Hz_dblR_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();

		// assumes sample rate of 12000
		// implements  8 200 Hz wide sections centered on 1500 Hz  (~1500 Hz wide @ - 30dB centered on 1500 Hz)
		// FSF (Frequency Selective Filter) variables

		//  Filtered samples

		double static_FSRcvFilter1500BW_1500Hz_dblR;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter1500BW_1500Hz_intN_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// insures stability (must be < 1.0) (Value .9995 7/8/2013 gives good results)
		int static_FSRcvFilter1500BW_1500Hz_intN;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter1500BW_1500Hz_dblRn_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// Length of filter 12000/200
		double static_FSRcvFilter1500BW_1500Hz_dblRn;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter1500BW_1500Hz_dblR2_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		double static_FSRcvFilter1500BW_1500Hz_dblR2;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter1500BW_1500Hz_dblCoef_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		double[] static_FSRcvFilter1500BW_1500Hz_dblCoef;
		double static_FSRcvFilter1500BW_1500Hz_dblZin;
		double static_FSRcvFilter1500BW_1500Hz_dblZin_1;
		double static_FSRcvFilter1500BW_1500Hz_dblZin_2;
		//the coefficients
		double static_FSRcvFilter1500BW_1500Hz_dblZComb;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter1500BW_1500Hz_dblZout_0_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// Used in the comb generator
		// The resonators 
		double[] static_FSRcvFilter1500BW_1500Hz_dblZout_0;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter1500BW_1500Hz_dblZout_1_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// resonator outputs
		double[] static_FSRcvFilter1500BW_1500Hz_dblZout_1;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter1500BW_1500Hz_dblZout_2_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// resonator outputs delayed one sample
		double[] static_FSRcvFilter1500BW_1500Hz_dblZout_2;
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_FSRcvFilter1500BW_1500Hz_intFilLen_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();
		// resonator outputs delayed two samples
		int static_FSRcvFilter1500BW_1500Hz_intFilLen;
		public Int32[] FSRcvFilter1500BW_1500Hz(ref Int32[] intNewSamples, string strFilename)
		{
			Int32[] intFilteredSamples = new Int32[intNewSamples.Length];
			lock (static_FSRcvFilter1500BW_1500Hz_dblR_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter1500BW_1500Hz_dblR_Init)) {
						static_FSRcvFilter1500BW_1500Hz_dblR = 0.9995;
					}
				} finally {
					static_FSRcvFilter1500BW_1500Hz_dblR_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter1500BW_1500Hz_intN_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter1500BW_1500Hz_intN_Init)) {
						static_FSRcvFilter1500BW_1500Hz_intN = 60;
					}
				} finally {
					static_FSRcvFilter1500BW_1500Hz_intN_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter1500BW_1500Hz_dblRn_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter1500BW_1500Hz_dblRn_Init)) {
						static_FSRcvFilter1500BW_1500Hz_dblRn = Math.Pow(static_FSRcvFilter1500BW_1500Hz_dblR, static_FSRcvFilter1500BW_1500Hz_intN);
					}
				} finally {
					static_FSRcvFilter1500BW_1500Hz_dblRn_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter1500BW_1500Hz_dblR2_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter1500BW_1500Hz_dblR2_Init)) {
						static_FSRcvFilter1500BW_1500Hz_dblR2 = Math.Pow(static_FSRcvFilter1500BW_1500Hz_dblR, 2);
					}
				} finally {
					static_FSRcvFilter1500BW_1500Hz_dblR2_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter1500BW_1500Hz_dblCoef_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter1500BW_1500Hz_dblCoef_Init)) {
						static_FSRcvFilter1500BW_1500Hz_dblCoef = new double[12];
					}
				} finally {
					static_FSRcvFilter1500BW_1500Hz_dblCoef_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter1500BW_1500Hz_dblZout_0_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter1500BW_1500Hz_dblZout_0_Init)) {
						static_FSRcvFilter1500BW_1500Hz_dblZout_0 = new double[12];
					}
				} finally {
					static_FSRcvFilter1500BW_1500Hz_dblZout_0_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter1500BW_1500Hz_dblZout_1_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter1500BW_1500Hz_dblZout_1_Init)) {
						static_FSRcvFilter1500BW_1500Hz_dblZout_1 = new double[12];
					}
				} finally {
					static_FSRcvFilter1500BW_1500Hz_dblZout_1_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter1500BW_1500Hz_dblZout_2_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter1500BW_1500Hz_dblZout_2_Init)) {
						static_FSRcvFilter1500BW_1500Hz_dblZout_2 = new double[12];
					}
				} finally {
					static_FSRcvFilter1500BW_1500Hz_dblZout_2_Init.State = 1;
				}
			}
			lock (static_FSRcvFilter1500BW_1500Hz_intFilLen_Init) {
				try {
					if (InitStaticVariableHelper(static_FSRcvFilter1500BW_1500Hz_intFilLen_Init)) {
						static_FSRcvFilter1500BW_1500Hz_intFilLen = static_FSRcvFilter1500BW_1500Hz_intN / 2;
					}
				} finally {
					static_FSRcvFilter1500BW_1500Hz_intFilLen_Init.State = 1;
				}
			}

			// Initialize the coefficients
			if (static_FSRcvFilter1500BW_1500Hz_dblCoef[10 == 0]) {
				for (int i = 4; i <= 11; i++) {
					static_FSRcvFilter1500BW_1500Hz_dblCoef[i] = 2 * static_FSRcvFilter1500BW_1500Hz_dblR * Math.Cos(2 * Math.PI * i / static_FSRcvFilter1500BW_1500Hz_intN);
					// For Frequency = bin i
				}
			}
			try {
				for (int i = 0; i <= intNewSamples.Length - 1; i++) {
					if (i < static_FSRcvFilter1500BW_1500Hz_intN) {
						static_FSRcvFilter1500BW_1500Hz_dblZin = intNewSamples[i];
					} else {
						static_FSRcvFilter1500BW_1500Hz_dblZin = intNewSamples[i] - static_FSRcvFilter1500BW_1500Hz_dblRn * intNewSamples[i - static_FSRcvFilter1500BW_1500Hz_intN];
					}
					// Compute the Comb
					static_FSRcvFilter1500BW_1500Hz_dblZComb = static_FSRcvFilter1500BW_1500Hz_dblZin - static_FSRcvFilter1500BW_1500Hz_dblZin_2 * static_FSRcvFilter1500BW_1500Hz_dblR2;
					static_FSRcvFilter1500BW_1500Hz_dblZin_2 = static_FSRcvFilter1500BW_1500Hz_dblZin_1;
					static_FSRcvFilter1500BW_1500Hz_dblZin_1 = static_FSRcvFilter1500BW_1500Hz_dblZin;

					// DateTime.Now the resonators

					// calculate output for 6 resonators 
					for (int j = 4; j <= 11; j++) {
						static_FSRcvFilter1500BW_1500Hz_dblZout_0[j] = static_FSRcvFilter1500BW_1500Hz_dblZComb + static_FSRcvFilter1500BW_1500Hz_dblCoef[j] * static_FSRcvFilter1500BW_1500Hz_dblZout_1[j] - static_FSRcvFilter1500BW_1500Hz_dblR2 * static_FSRcvFilter1500BW_1500Hz_dblZout_2[j];
						static_FSRcvFilter1500BW_1500Hz_dblZout_2[j] = static_FSRcvFilter1500BW_1500Hz_dblZout_1[j];
						static_FSRcvFilter1500BW_1500Hz_dblZout_1[j] = static_FSRcvFilter1500BW_1500Hz_dblZout_0[j];
						// scale each by transition coeff and + (Even) or - (Odd) 
						// Resonators 2 and 8 scaled by .15 to get best shape and side lobe supression while keeping BW at 500 Hz
						// practical range of scaling .05 to 2.5
						// Scaling also accomodates for the filter "gain" of approx 40. 
						if (j == 4) {
							intFilteredSamples[i] += 0.389 * static_FSRcvFilter1500BW_1500Hz_dblZout_0[j];
						} else if (j == 11) {
							intFilteredSamples[i] -= 0.389 * static_FSRcvFilter1500BW_1500Hz_dblZout_0[j];
						} else if (j % 2 == 0) {
							intFilteredSamples[i] += static_FSRcvFilter1500BW_1500Hz_dblZout_0[j];
						} else {
							intFilteredSamples[i] -= static_FSRcvFilter1500BW_1500Hz_dblZout_0[j];
						}
					}
					intFilteredSamples[i] = intFilteredSamples[i] * 0.01666;
					// rescales for gain of filter
				}

				// *********************************
				// Debug code to look at filter output
				double[] dblInFiltered = new double[intFilteredSamples.Length];
				for (int k = 0; k <= dblInFiltered.Length - 1; k++) {
					dblInFiltered[k] = intFilteredSamples[k];
				}
				WaveTools objWT = new WaveTools();
				if (System.IO.Directory.Exists(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav") == false) {
					System.IO.Directory.CreateDirectory(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav");
				}
				objWT.WriteFloatingRIFF(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Wav\\" + strFilename, 12000, 16, ref dblInFiltered);
				// End of debug code
				//************************************

			} catch (Exception ex) {
				Debug.WriteLine("[Filters.FSRcvFilter150BW_1500Hz] Exception: " + ex.ToString());
				return null;
			}
			return intFilteredSamples;
		}

        static double Erf(double x)
        {
            // constants
            double a1 = 0.254829592;
            double a2 = -0.284496736;
            double a3 = 1.421413741;
            double a4 = -1.453152027;
            double a5 = 1.061405429;
            double p = 0.3275911;

            // Save the sign of x
            int sign = 1;
            if (x < 0)
                sign = -1;
            x = Math.Abs(x);

            // A&S formula 7.1.26
            double t = 1.0 / (1.0 + p * x);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

            return sign * y;
        }
        /*
		// Taylor series approximation for the Error function (VB doesn't have the native Erf Function)
		private double Erf(double dblZ)
		{
			double functionReturnValue = 0;
			// Status: Tested and appears to give close values 
			// may be able to reduce number of iterations at some degredation in accuracy
			// e.g. with 21 terms erf(2) = .997  with 10 terms erf(2) = .97
			functionReturnValue = dblZ;
			double dblZSq = dblZ * dblZ;
			double dblZPwr = dblZ;
			double dblNfact = 1;
			if (dblZ > 2) {
				return dblErf2 + ((dblZ - 2) / dblZ) * (1 - dblErf2);
				//an approximation for the tail where the series doesn't converger well
			} else if (dblZ < -2) {
				return -(dblErf2 + ((dblZ + 2) / dblZ) * (1 - dblErf2));
				//an approximation for the tail where the series doesn't converger well
			}
			// Use Taylor series for 2<= dblZ <= 2
			// 21 total terms
			for (int i = 1; i <= 20; i++) {
				dblNfact = dblNfact * i;
				dblZPwr = dblZPwr * dblZSq;
				if ((i % 2) == 0) {
					functionReturnValue += dblZPwr / (dblNfact * (2 * i + 1));
				} else {
					functionReturnValue -= dblZPwr / (dblNfact * (2 * i + 1));
				}
			}
			functionReturnValue = functionReturnValue * 2 / Math.Sqrt(Math.PI);
			return functionReturnValue;
		}
		// Erf */
         

		// Function to return the Log base 2 of a value (not native to VB)
		private double Log2(double dblX)
		{
			return Math.Log(dblX) / dblLn2;
		}
		// Log2

		// Function to compute the normal distribution
		public double Normal(double dblX)
		{
			return 0.5 + 0.5 * Erf(dblX / dblSQRT2);
		}
		static bool InitStaticVariableHelper(Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag flag)
		{
			if (flag.State == 0) {
				flag.State = 2;
				return true;
			} else if (flag.State == 2) {
				throw new Microsoft.VisualBasic.CompilerServices.IncompleteInitialization();
			} else {
				return false;
			}
		}
		// Normal

		public Filters ()
		{
		}
	}
}

