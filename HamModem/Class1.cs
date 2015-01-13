using System;
using System.Text;




namespace HamModem
{
   

    
    public class MainClass
    {
        public enum TestState
        {
            // used for initial receive testing...later put in correct protocol states
            SearchingForLeader,
            AcquireSymbolSync,
            AcquireFrameSync,
            AcquireFrameType,
            DecodeFrameType,
            AcquireFrame,
            DecodeFrame
        }

        public static TestState State;

        // Function to convert string Text (ASCII) to byte array
        public static byte[] GetBytes(string strText)
        {
            // Converts a text string to a byte array...

            byte[] bytBuffer = new byte[strText.Length];
            for (int intIndex = 0; intIndex <= bytBuffer.Length - 1; intIndex++)
            {
                bytBuffer[intIndex] = Convert.ToByte(Strings.Asc(strText.Substring(intIndex, 1)));
            }
            return bytBuffer;
        }
        //GetBytes


    }
}
