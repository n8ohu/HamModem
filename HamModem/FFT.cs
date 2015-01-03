using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HamModem
{
    public class FFT
    {
        // A class to impmplemnt a Fast Fourier Transform Algorithm 
        #region "Notes and documentation"
        //********************************************************************
        // Execution time for a 2048 point FFT on a 1700 MHz P4 was about 5 ms)
        // Some optimization could be made if only real inputs are insured.
        //   Rick Muething KN6KB, Mar 31, 2004
        //********************************************************************
        //--------------------------------------------------------------------
        // VB FFT Release 2-B
        // by Murphy McCauley (MurphyMc@Concentric.NET)
        // 10/01/99
        //--------------------------------------------------------------------
        // About:
        // This code is very, very heavily based on Don Cross's fourier.pas
        // Turbo Pascal Unit for calculating the Fast Fourier Transform.
        // I've not implemented all of his functions, though I may well do
        // so in the future.
        // For more info, you can contact me by email, check my website at:
        // http://www.fullspectrum.com/deeth/
        // or check Don Cross's FFT web page at:
        // http://www.intersrv.com/~dcross/fft.html
        // You also may be intrested in the FFT.DLL that I put together based
        // on Don Cross's FFT C code.  It's callable with Visual Basic and
        // includes VB declares.  You can get it from either website.
        //--------------------------------------------------------------------
        // History of Release 2-B:
        // Fixed a couple of errors that resulted from me mucking about with
        //   variable names after implementation and not re-checking.  BAD ME.
        //  --------
        // History of Release 2:
        // Added FrequencyOfIndex() which is Don Cross's Index_to_frequency().
        // FourierTransform() can now do inverse transforms.
        // Added CalcFrequency() which can do a transform for a single
        //   frequency.
        //--------------------------------------------------------------------
        // Usage:
        // The useful functions are:
        // FourierTransform() performs a Fast Fourier Transform on an pair of
        //  Double arrays -- one real, one imaginary.  Don't want/need
        //  imaginary numbers?  Just use an array of 0s.  This function can
        //  also do inverse FFTs.
        // FrequencyOfIndex() can tell you what actual frequency a given index
        //  corresponds to.
        // CalcFrequency() transforms a single frequency.
        //--------------------------------------------------------------------
        // Notes:
        // All arrays must be 0 based (i.e. Dim TheArray(0 To 1023) or
        //  Dim TheArray(1023)).
        // The number of samples must be a power of two (i.e. 2^x).
        // FrequencyOfIndex() and CalcFrequency() haven't been tested much.
        // Use this ENTIRELY AT YOUR OWN RISK.
        //--------------------------------------------------------------------
        #endregion


        #region "Private Subs and Functions "
        private byte NumberOfBitsNeeded(Int32 PowerOfTwo)
        {
            byte functionReturnValue = 0;
            byte I = 0;
            for (I = 0; I <= 16; I++)
            {
                if ((PowerOfTwo & (Math.Pow(2, I))) != 0)
                {
                    functionReturnValue = I;
                    return functionReturnValue;
                }
            }
            return 0;
            return functionReturnValue;
        }


        private bool IsPowerOfTwo(Int32 X)
        {
            bool functionReturnValue = false;
            functionReturnValue = false;
            if ((X < 2)) { functionReturnValue = false; return functionReturnValue; }
            if ((X & (X - 1)) == false)
                functionReturnValue = true;
            return functionReturnValue;
        }


        private Int32 ReverseBits(Int32 Index, byte NumBits)
        {
            byte I = 0;
            Int32 Rev = default(Int32);

            for (I = 0; I <= NumBits - 1; I++)
            {
                Rev = (Rev * 2) | (Index & 1);
                Index = Index / 2;
            }

            return Rev;
        }

        // Subroutine for debugging saving intermediate values to file for later analysis by DSP Scope
        private void SaveIQToFile(double[] aryI, double[] aryQ, string strFilename)
        {
            // used for debug to allow reading with DSP Scope
            if (IO.File.Exists(strFilename))
                IO.File.Delete(strFilename);
            StreamWriter sw = new StreamWriter(strFilename, true);
            for (int i = 0; i <= aryI.Length - 1; i++)
            {
                string strData = Strings.Format(aryI(i), "00000000.000000") + " " + Strings.Format(aryQ(i), "00000000.000000");
                sw.WriteLine(strData);
            }
            sw.Flush();
            sw.Close();
        }
        //SaveIQToFile
        #endregion

        #region "Friend Subs and Functions"

        internal void FourierTransform(long NumSamples, double[] RealIn, double[] ImageIn, ref double[] RealOut, ref double[] ImagOut, bool InverseTransform = false)
        {
            double AngleNumerator = 0;
            byte NumBits = 0;
            Int32 I = default(Int32);
            Int32 j = default(Int32);
            Int32 K = default(Int32);
            Int32 n = default(Int32);
            Int32 BlockSize = default(Int32);
            Int32 BlockEnd = default(Int32);
            double DeltaAngle = 0;
            double DeltaAr = 0;
            double Alpha = 0;
            double Beta = 0;
            double TR = 0;
            double TI = 0;
            double AR = 0;
            double AI = 0;

            if (InverseTransform)
            {
                AngleNumerator = -2.0 * PI;
            }
            else
            {
                AngleNumerator = 2.0 * PI;
            }

            if ((IsPowerOfTwo(NumSamples) == false) | (NumSamples < 2))
            {
                Logs.Exception("[FFT.FourierTransform] NumSamples is " + Convert.ToString(NumSamples) + ", which is not a positive integer power of two.");
                return;
            }

            NumBits = NumberOfBitsNeeded(NumSamples);
            for (I = 0; I <= (NumSamples - 1); I++)
            {
                j = ReverseBits(I, NumBits);
                RealOut(j) = RealIn(I);
                ImagOut(j) = ImageIn(I);
            }

            BlockEnd = 1;
            BlockSize = 2;

            while (BlockSize <= NumSamples)
            {
                DeltaAngle = AngleNumerator / BlockSize;
                Alpha = Sin(0.5 * DeltaAngle);
                Alpha = 2.0 * Alpha * Alpha;
                Beta = Sin(DeltaAngle);

                I = 0;
                while (I < NumSamples)
                {
                    AR = 1.0;
                    AI = 0.0;

                    j = I;
                    for (n = 0; n <= BlockEnd - 1; n++)
                    {
                        K = j + BlockEnd;
                        TR = AR * RealOut(K) - AI * ImagOut(K);
                        TI = AI * RealOut(K) + AR * ImagOut(K);
                        RealOut(K) = RealOut(j) - TR;
                        ImagOut(K) = ImagOut(j) - TI;
                        RealOut(j) = RealOut(j) + TR;
                        ImagOut(j) = ImagOut(j) + TI;
                        DeltaAr = Alpha * AR + Beta * AI;
                        AI = AI - (Alpha * AI - Beta * AR);
                        AR = AR - DeltaAr;
                        j = j + 1;
                    }

                    I = I + BlockSize;
                }

                BlockEnd = BlockSize;
                BlockSize = BlockSize * 2;
            }

            if (InverseTransform)
            {
                //Normalize the resulting time samples...
                for (I = 0; I <= NumSamples - 1; I++)
                {
                    RealOut(I) = RealOut(I) / NumSamples;
                    ImagOut(I) = ImagOut(I) / NumSamples;
                }
            }
        }


        internal double FrequencyOfIndex(Int32 NumberOfSamples, Int32 Index)
        {
            double functionReturnValue = 0;
            //Based on IndexToFrequency().  This name makes more sense to me.

            if (Index >= NumberOfSamples)
            {
                functionReturnValue = 0.0;
                return functionReturnValue;
            }
            else if (Index <= NumberOfSamples / 2)
            {
                functionReturnValue = Convert.ToDouble(Index) / Convert.ToDouble(NumberOfSamples);
                return functionReturnValue;
            }
            else
            {
                functionReturnValue = -Convert.ToDouble(NumberOfSamples - Index) / Convert.ToDouble(NumberOfSamples);
                return functionReturnValue;
            }
            return functionReturnValue;
        }



        internal void CalcFrequency(Int32 NumberOfSamples, Int32 FrequencyIndex, double[] RealIn, double[] ImagIn, double RealOut, double ImagOut)
        {
            Int32 K = default(Int32);
            double Cos1 = 0;
            double Cos2 = 0;
            double Cos3 = 0;
            double Theta = 0;
            double Beta = 0;
            double Sin1 = 0;
            double Sin2 = 0;
            double Sin3 = 0;

            Theta = 2 * PI * FrequencyIndex / Convert.ToDouble(NumberOfSamples);
            Sin1 = Sin(-2 * Theta);
            Sin2 = Sin(-Theta);
            Cos1 = Cos(-2 * Theta);
            Cos2 = Cos(-Theta);
            Beta = 2 * Cos2;

            for (K = 0; K <= NumberOfSamples - 2; K++)
            {
                //Update trig values
                Sin3 = Beta * Sin2 - Sin1;
                Sin1 = Sin2;
                Sin2 = Sin3;

                Cos3 = Beta * Cos2 - Cos1;
                Cos1 = Cos2;
                Cos2 = Cos3;

                RealOut = RealOut + RealIn(K) * Cos3 - ImagIn(K) * Sin3;
                ImagOut = ImagOut + ImagIn(K) * Cos3 + RealIn(K) * Sin3;
            }
        }
        #endregion

        #region "Public Subs and Functions"


        // returns a magnitude FFT from a byte array of real Sound Card/wave file 16 bit data 

        // aryData is buffered SC data 2 bytes per sample (circular buffer)  
        // intPtr is the pointer to the first data sample to use in the FFT
        // int FFTsize is the FFT points (must be power of 2) 
        // intDecimation is the decimation index (power of 2) value of 1 causes no decimation
        int static_MakeFFT_intWaveCnt;
        public double[] MakeFFT(byte[] aryData, int intPtr, int intFFTSize, int intDecimation = 1, bool blnWin = false)
        {
            double[] ReT = new double[(intFFTSize / intDecimation)];
            double[] ImT = new double[(intFFTSize / intDecimation)];
            double[] ReF = new double[(intFFTSize / intDecimation)];
            double[] ImF = new double[(intFFTSize / intDecimation)];
            double[] dblFFT = new double[(intFFTSize / intDecimation) / 2];
            // array for FFT output
            int intAryIndex = 0;
            int i = 0;
            if (blnWin)
            {
                double dblAngleInc = 2 * PI / (ReT.Length - 1);
                double dblAngle = 0;
                for (i = 0; i <= ReT.Length - 1; i++)
                {
                    // Read the captured data, window with Hanning window and convert to Real part of T array (double)
                    intAryIndex = (intPtr + (2 * i)) % aryData.Length;
                    ReT(i) = (0.5 - 0.5 * Math.Cos(dblAngle)) * System.BitConverter.ToInt16(aryData, intAryIndex);
                    dblAngle += dblAngleInc;
                }
            }
            else
            {
                for (i = 0; i <= ReT.Length - 1; i++)
                {
                    // Read the captured data without windowing and convert to Real part of T array (double)
                    intAryIndex = (intPtr + (2 * i)) % aryData.Length;
                    System.BitConverter.ToInt16(aryData, intAryIndex);
                }
            }
            // Do the FFT creating Real and Imaginary freq
            FourierTransform(ReT.Length, ReT, ImT, ReF, ImF);
            static_MakeFFT_intWaveCnt += 1;
            // compute the magnitude output array
            for (i = 0; i <= dblFFT.Length - 1; i++)
            {
                dblFFT(i) = Sqrt(Math.Pow(ReF(i), 2) + Math.Pow(ImF(i), 2));
                //dblFFT(i) = Abs(ReF(i))
            }
            return dblFFT;
        }

        // returns the interpolated Frequency in bins and the magnitude (by ref) at the bin  
        public double FindPeakAndMag(byte[] aryData, int intPtr, int intFFTSize, int StartBin, int StopBin, ref double Mag, ref double SN)
        {
            double functionReturnValue = 0;

            // aryData is buffered SC data 2 bytes per sample (circular buffer)  
            // intPtr is the pointer to the first data sample to use in the FFT
            // int FFTsize is the FFT points (must be power of 2) 
            // intDecimation is the decimation index (power of 2) value of 1 causes no decimation
            // Returns interpolate bin of peak energy between StartBin and StopBin
            // Also sets by Ref Magnitude at the peak and S/S+N at the peak
            double[] ReT = new double[intFFTSize];
            double[] ImT = new double[intFFTSize];
            double[] ReF = new double[intFFTSize];
            double[] ImF = new double[intFFTSize];
            int intAryIndex = 0;
            int i = 0;
            // Read the captured data without windowing and convert to Real part of T array (double)
            for (i = 0; i <= intFFTSize - 1; i++)
            {
                intAryIndex = (intPtr + (2 * i)) % aryData.Length;
                ReT(i) = System.BitConverter.ToInt16(aryData, intAryIndex);
            }
            // Do the FFT creating Real and Imaginary freq
            FourierTransform(intFFTSize, ReT, ImT, ReF, ImF);
            // Search for the peak...should only have to search the ReF since mag of ImF is same as ReF
            double dblPeak = 0;
            int intPeakIndex = 0;
            double dblEgr = 0;
            double dblSum = 0;
            for (int j = StartBin; j <= StopBin; j++)
            {
                dblEgr = Math.Pow(ReF(j), 2) + Math.Pow(ImF(j), 2);
                dblSum += Sqrt(dblEgr);
                if (dblEgr > dblPeak)
                {
                    dblPeak = dblEgr;
                    intPeakIndex = j;
                }
            }
            dblPeak = Sqrt(dblPeak);
            // take the square root of the peak^2 energy
            // do the interpolation based on formula found in Richard Lyons 
            // Understanding Digital Signal Processing, 2nd Ed p(525) (should be accurate to better than .1 bin) 
            // possible to do the interpolation
            if (intPeakIndex > 1 & intPeakIndex < ReF.Length - 2)
            {
                double Xk_1R = ReF(intPeakIndex - 1);
                // real component of one bin less than the peak
                double Xk_1I = ImF(intPeakIndex - 1);
                // imaginary component of one bin less than the peak
                double Xk1R = ReF(intPeakIndex + 1);
                // real component of one bin more than the peak
                double Xk1I = ImF(intPeakIndex + 1);
                // imaginary component of one bin more than the peak
                double DeltaNumR = Xk1R - Xk_1R;
                double DeltaNumI = Xk1I - Xk_1I;
                double DeltaDenomR = 2 * ReF(intPeakIndex) - Xk_1R - Xk1R;
                double DeltaDenomI = 2 * ImF(intPeakIndex) - Xk_1I - Xk1I;
                double DeltaMag = Sqrt(Math.Pow(DeltaNumR, 2) + Math.Pow(DeltaNumI, 2)) / Sqrt(Math.Pow(DeltaDenomR, 2) + Math.Pow(DeltaDenomI, 2));
                double DeltaAng = Atan2(DeltaNumI, DeltaNumR) - Atan2(DeltaDenomI, DeltaDenomR);
                double DeltaR = DeltaMag * Cos(DeltaAng);
                // the real part should be in the range -.5 to .5
                // alternate (old) method
                double dblOldInterp = 0;
                double dblOldSum = dblPeak;
                dblOldInterp = intPeakIndex * dblPeak;
                dblOldInterp += (intPeakIndex - 1) * Sqrt(Math.Pow(ReF(intPeakIndex - 1), 2) + Math.Pow(ImF(intPeakIndex - 1), 2));
                dblOldSum += Sqrt(Math.Pow(ReF(intPeakIndex - 1), 2) + Math.Pow(ImF(intPeakIndex - 1), 2));
                dblOldInterp += (intPeakIndex + 1) * Sqrt(Math.Pow(ReF(intPeakIndex + 1), 2) + Math.Pow(ImF(intPeakIndex + 1), 2));
                dblOldSum += Sqrt(Math.Pow(ReF(intPeakIndex + 1), 2) + Math.Pow(ImF(intPeakIndex + 1), 2));
                dblOldInterp = dblOldInterp / dblOldSum;
                functionReturnValue = dblOldInterp;
                Mag = dblOldSum;
            }
            else
            {
                functionReturnValue = intPeakIndex;
                // no interpolation
                Mag = dblPeak;
            }
            SN = Mag / dblSum;
            return functionReturnValue;
            // and the S/N (by Ref) 
        }

        // returns the high resolution interpolated Frequency in bins and the magnitude (by ref) at the bin  
        public double HighResPeakAndMag(byte[] aryData, int intPtr, int intFFTSize, int StartBin, int StopBin, ref double Mag, ref double SN)
        {
            double functionReturnValue = 0;
            // similar to FindPeakAndMag but always uses 1024 point FFT with 0 padding and windowing for improved resolution
            // aryData is buffered SC data 2 bytes per sample (circular buffer)  
            // intPtr is the pointer to the first data sample to use in the FFT
            // int FFTsize is the FFT points (must be power of 2) 
            // Start and Stop bins are relative to 256 point FFT with bin resolution of 43.066 Hz
            // Returns interpolate bin of peak energy between StartBin and StopBin relative to 256 point FFT (43.066 Hz/bin) 
            // Also sets by Ref Magnitude at the peak and S/S+N at the peak
            double[] ReT = new double[1024];
            double[] ImT = new double[1024];
            double[] ReF = new double[1024];
            double[] ImF = new double[1024];
            int intAryIndex = 0;
            int intOff = 0;
            switch (intFFTSize)
            {
                case 1024:
                    intOff = 0;
                    break;
                case 512:
                    intOff = 256;
                    break;
                case 256:
                    intOff = 384;
                    break;
            }

            double dblAngleInc = 2 * PI / (intFFTSize - 1);
            double dblAngle = 0;
            for (int i = 0; i <= intFFTSize - 1; i++)
            {
                // Read the captured data, window with Hanning window and convert to Real part of T array (double)
                intAryIndex = (intPtr + (2 * i)) % aryData.Length;
                ReT(i + intOff) = (0.5 - 0.5 * Math.Cos(dblAngle)) * System.BitConverter.ToInt16(aryData, intAryIndex);
                dblAngle += dblAngleInc;
            }

            // Do the 1024 point FFT creating Real and Imaginary freq
            FourierTransform(1024, ReT, ImT, ReF, ImF);
            // Search for the peak
            double dblPeak = 0;
            int intPeakIndex = 0;
            double dblEgr = 0;
            double dblSum = 0;
            // assume start and stop are relative to 256 points transform
            for (int j = 4 * StartBin; j <= 4 * StopBin; j++)
            {
                dblEgr = Math.Pow(ReF(j), 2) + Math.Pow(ImF(j), 2);
                dblSum += Sqrt(dblEgr);
                if (dblEgr > dblPeak)
                {
                    dblPeak = dblEgr;
                    intPeakIndex = j;
                }
            }
            dblPeak = Sqrt(dblPeak);
            // take the square root of the peak^2 energy
            // do the interpolation based on formula found in Richard Lyons 
            // Understanding Digital Signal Processing, 2nd Ed p(525) (should be accurate to better than .1 bin) 
            // basic interpolation based on adjacent bin energy
            double dblInterp = 0;
            double dblAdjSum = dblPeak;
            dblInterp = intPeakIndex * dblPeak;
            dblInterp += (intPeakIndex - 1) * Sqrt(Math.Pow(ReF(intPeakIndex - 1), 2) + Math.Pow(ImF(intPeakIndex - 1), 2));
            dblAdjSum += Sqrt(Math.Pow(ReF(intPeakIndex - 1), 2) + Math.Pow(ImF(intPeakIndex - 1), 2));
            dblInterp += (intPeakIndex + 1) * Sqrt(Math.Pow(ReF(intPeakIndex + 1), 2) + Math.Pow(ImF(intPeakIndex + 1), 2));
            dblAdjSum += Sqrt(Math.Pow(ReF(intPeakIndex + 1), 2) + Math.Pow(ImF(intPeakIndex + 1), 2));
            dblInterp = dblInterp / dblAdjSum;
            functionReturnValue = 0.25 * dblInterp;
            // scale interpolated index back to releative to 256 point transform
            Mag = dblAdjSum;
            SN = Mag / dblSum;
            return functionReturnValue;
            // and the S/N (by Ref) 
        }
        // HighResPeakAndMag

        #endregion

    }

}
