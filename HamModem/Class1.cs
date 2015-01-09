using System;


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

    }
}
